using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.RateLimiting;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Systems
{
    /// <summary>
    /// Server-side mentor help system for managing tickets
    /// </summary>
    [UsedImplicitly]
    public sealed class MentorHelpSystem : SharedMentorHelpSystem
    {
        private const string RateLimitKey = "MentorHelp";

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly PlayerRateLimitManager _rateLimit = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("MHELP");

            _rateLimit.Register(
                RateLimitKey,
                new RateLimitRegistration(CCVars.AhelpRateLimitPeriod, // Reuse ahelp rate limit config
                    CCVars.AhelpRateLimitCount,
                    PlayerRateLimitedAction)
            );

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        private void PlayerRateLimitedAction(ICommonSession session)
        {
            _sawmill.Warning($"Player {session.Name} ({session.UserId}) was rate limited for mentor help");
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            // Could notify mentors about player connection status for active tickets
            // For now, keep it simple
        }

        protected override async void OnCreateTicketMessage(MentorHelpCreateTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            // Rate limiting
            if (_rateLimit.CountAction(session, RateLimitKey) != RateLimitStatus.Allowed)
                return;

            // Validate input
            if (string.IsNullOrWhiteSpace(message.Subject) || string.IsNullOrWhiteSpace(message.Message))
            {
                _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to create mentor help ticket with empty subject or message");
                return;
            }

            if (message.Subject.Length > 512 || message.Message.Length > 4096)
            {
                _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to create mentor help ticket with too long subject or message");
                return;
            }

            try
            {
                var now = DateTimeOffset.UtcNow;
                var ticket = new MentorHelpTicket
                {
                    PlayerId = session.UserId.UserId,
                    Subject = message.Subject.Trim(),
                    InitialMessage = message.Message.Trim(),
                    Status = MentorHelpTicketStatus.Open,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RoundId = _gameTicker.RoundId,
                    ServerId = await GetServerIdAsync()
                };

                await _dbManager.AddMentorHelpTicketAsync(ticket);

                _sawmill.Info($"Player {session.Name} ({session.UserId}) created mentor help ticket #{ticket.Id}: {ticket.Subject}");

                // Notify player
                var ticketData = await ConvertToTicketDataAsync(ticket);
                RaiseNetworkEvent(new MentorHelpTicketUpdateMessage(ticketData), session.Channel);

                // Notify mentors/admins
                await NotifyMentorsOfNewTicket(ticketData);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error creating mentor help ticket for {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnClaimTicketMessage(MentorHelpClaimTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            // Check permissions
            if (!HasMentorPermissions(session))
            {
                _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to claim mentor help ticket without permissions");
                return;
            }

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    _sawmill.Warning($"Mentor {session.Name} ({session.UserId}) tried to claim non-existent ticket #{message.TicketId}");
                    return;
                }

                if (ticket.Status == MentorHelpTicketStatus.Closed)
                {
                    _sawmill.Warning($"Mentor {session.Name} ({session.UserId}) tried to claim closed ticket #{message.TicketId}");
                    return;
                }

                // Claim the ticket
                ticket.AssignedToUserId = session.UserId.UserId;
                ticket.Status = MentorHelpTicketStatus.Assigned;
                ticket.UpdatedAt = DateTimeOffset.UtcNow;

                await _dbManager.UpdateMentorHelpTicketAsync(ticket);

                _sawmill.Info($"Mentor {session.Name} ({session.UserId}) claimed ticket #{ticket.Id}");

                // Notify all relevant parties
                var ticketData = await ConvertToTicketDataAsync(ticket);
                await NotifyTicketUpdate(ticketData);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error claiming mentor help ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnReplyMessage(MentorHelpReplyMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            // Rate limiting
            if (_rateLimit.CountAction(session, RateLimitKey) != RateLimitStatus.Allowed)
                return;

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to reply to non-existent ticket #{message.TicketId}");
                    return;
                }

                if (ticket.Status == MentorHelpTicketStatus.Closed)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to reply to closed ticket #{message.TicketId}");
                    return;
                }

                // Check permissions - player can reply to their own ticket, mentors/admins can reply to any
                var isTicketOwner = ticket.PlayerId == session.UserId.UserId;
                var hasMentorPerms = HasMentorPermissions(session);

                if (!isTicketOwner && !hasMentorPerms)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to reply to ticket #{message.TicketId} without permissions");
                    return;
                }

                // Staff-only messages can only be sent by mentors/admins
                if (message.IsStaffOnly && !hasMentorPerms)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to send staff-only message without permissions");
                    return;
                }

                // Validate message
                if (string.IsNullOrWhiteSpace(message.Message) || message.Message.Length > 4096)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to send invalid message to ticket #{message.TicketId}");
                    return;
                }

                // Create the message
                var ticketMessage = new MentorHelpMessage
                {
                    TicketId = message.TicketId,
                    SenderUserId = session.UserId.UserId,
                    Message = message.Message.Trim(),
                    SentAt = DateTimeOffset.UtcNow,
                    IsStaffOnly = message.IsStaffOnly
                };

                await _dbManager.AddMentorHelpMessageAsync(ticketMessage);

                // Update ticket status
                if (hasMentorPerms && ticket.Status == MentorHelpTicketStatus.Open)
                {
                    // Mentor replied to open ticket, mark as assigned
                    ticket.AssignedToUserId = session.UserId.UserId;
                    ticket.Status = MentorHelpTicketStatus.Assigned;
                }
                else if (hasMentorPerms)
                {
                    // Mentor replied, awaiting player response
                    ticket.Status = MentorHelpTicketStatus.AwaitingResponse;
                }
                else if (isTicketOwner && ticket.Status == MentorHelpTicketStatus.AwaitingResponse)
                {
                    // Player replied, mark as assigned again
                    ticket.Status = MentorHelpTicketStatus.Assigned;
                }

                ticket.UpdatedAt = DateTimeOffset.UtcNow;
                await _dbManager.UpdateMentorHelpTicketAsync(ticket);

                _sawmill.Info($"Player {session.Name} ({session.UserId}) replied to ticket #{message.TicketId}");

                // Notify relevant parties
                var ticketData = await ConvertToTicketDataAsync(ticket);
                var messageData = await ConvertToMessageDataAsync(ticketMessage);
                await NotifyTicketMessage(ticketData, messageData);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error adding reply to mentor help ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnCloseTicketMessage(MentorHelpCloseTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to close non-existent ticket #{message.TicketId}");
                    return;
                }

                if (ticket.Status == MentorHelpTicketStatus.Closed)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to close already closed ticket #{message.TicketId}");
                    return;
                }

                // Check permissions - player can close their own ticket, mentors/admins can close any
                var isTicketOwner = ticket.PlayerId == session.UserId.UserId;
                var hasMentorPerms = HasMentorPermissions(session);

                if (!isTicketOwner && !hasMentorPerms)
                {
                    _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to close ticket #{message.TicketId} without permissions");
                    return;
                }

                // Close the ticket
                ticket.Status = MentorHelpTicketStatus.Closed;
                ticket.ClosedAt = DateTimeOffset.UtcNow;
                ticket.ClosedByUserId = session.UserId.UserId;
                ticket.UpdatedAt = DateTimeOffset.UtcNow;

                await _dbManager.UpdateMentorHelpTicketAsync(ticket);

                _sawmill.Info($"Player {session.Name} ({session.UserId}) closed ticket #{ticket.Id}");

                // Notify relevant parties
                var ticketData = await ConvertToTicketDataAsync(ticket);
                await NotifyTicketUpdate(ticketData);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error closing mentor help ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnRequestTicketsMessage(MentorHelpRequestTicketsMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            try
            {
                List<MentorHelpTicket> tickets;

                if (message.OnlyMine)
                {
                    // Player requesting their own tickets
                    tickets = await _dbManager.GetMentorHelpTicketsByPlayerAsync(session.UserId.UserId);
                }
                else
                {
                    // Mentor/admin requesting all tickets
                    if (!HasMentorPermissions(session))
                    {
                        _sawmill.Warning($"Player {session.Name} ({session.UserId}) tried to request all mentor help tickets without permissions");
                        return;
                    }

                    tickets = await _dbManager.GetOpenMentorHelpTicketsAsync();
                }

                var ticketDataList = new List<MentorHelpTicketData>();
                foreach (var ticket in tickets)
                {
                    ticketDataList.Add(await ConvertToTicketDataAsync(ticket));
                }

                RaiseNetworkEvent(new MentorHelpTicketsListMessage(ticketDataList), session.Channel);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error requesting mentor help tickets for {session.Name} ({session.UserId}): {ex}");
            }
        }

        private bool HasMentorPermissions(ICommonSession session)
        {
            var adminData = _adminManager.GetAdminData(session);
            return adminData?.HasFlag(AdminFlags.Adminhelp) ?? false;
        }

        private async Task<int?> GetServerIdAsync()
        {
            // Implementation would depend on how server ID is tracked
            // For now, return null
            return null;
        }

        private async Task<MentorHelpTicketData> ConvertToTicketDataAsync(MentorHelpTicket ticket)
        {
            var playerName = await GetPlayerNameAsync(ticket.PlayerId);
            var assignedToName = ticket.AssignedToUserId.HasValue ? await GetPlayerNameAsync(ticket.AssignedToUserId.Value) : null;
            var closedByName = ticket.ClosedByUserId.HasValue ? await GetPlayerNameAsync(ticket.ClosedByUserId.Value) : null;

            return new MentorHelpTicketData
            {
                Id = ticket.Id,
                PlayerId = new NetUserId(ticket.PlayerId),
                PlayerName = playerName,
                AssignedToUserId = ticket.AssignedToUserId.HasValue ? new NetUserId(ticket.AssignedToUserId.Value) : null,
                AssignedToName = assignedToName,
                Subject = ticket.Subject,
                InitialMessage = ticket.InitialMessage,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt.DateTime,
                UpdatedAt = ticket.UpdatedAt.DateTime,
                ClosedAt = ticket.ClosedAt?.DateTime,
                ClosedByUserId = ticket.ClosedByUserId.HasValue ? new NetUserId(ticket.ClosedByUserId.Value) : null,
                ClosedByName = closedByName,
                RoundId = ticket.RoundId,
                HasUnreadMessages = false // Would need to implement read tracking
            };
        }

        private async Task<MentorHelpMessageData> ConvertToMessageDataAsync(MentorHelpMessage message)
        {
            var senderName = await GetPlayerNameAsync(message.SenderUserId);

            return new MentorHelpMessageData
            {
                Id = message.Id,
                TicketId = message.TicketId,
                SenderUserId = new NetUserId(message.SenderUserId),
                SenderName = senderName,
                Message = message.Message,
                SentAt = message.SentAt.DateTime,
                IsStaffOnly = message.IsStaffOnly
            };
        }

        private async Task<string> GetPlayerNameAsync(Guid userId)
        {
            var playerData = await _dbManager.GetPlayerRecordByUserId(new NetUserId(userId));
            return playerData?.LastSeenUserName ?? "Unknown";
        }

        private async Task NotifyMentorsOfNewTicket(MentorHelpTicketData ticketData)
        {
            var mentors = GetTargetMentors();
            foreach (var mentor in mentors)
            {
                RaiseNetworkEvent(new MentorHelpTicketUpdateMessage(ticketData), mentor);
            }
        }

        private async Task NotifyTicketUpdate(MentorHelpTicketData ticketData)
        {
            // Notify the player
            if (_playerManager.TryGetSessionById(ticketData.PlayerId, out var playerSession))
            {
                RaiseNetworkEvent(new MentorHelpTicketUpdateMessage(ticketData), playerSession.Channel);
            }

            // Notify mentors
            var mentors = GetTargetMentors();
            foreach (var mentor in mentors)
            {
                RaiseNetworkEvent(new MentorHelpTicketUpdateMessage(ticketData), mentor);
            }
        }

        private async Task NotifyTicketMessage(MentorHelpTicketData ticketData, MentorHelpMessageData messageData)
        {
            // Notify the player (if not staff-only)
            if (!messageData.IsStaffOnly && _playerManager.TryGetSessionById(ticketData.PlayerId, out var playerSession))
            {
                RaiseNetworkEvent(new MentorHelpTicketMessagesMessage(ticketData.Id, new List<MentorHelpMessageData> { messageData }), playerSession.Channel);
            }

            // Notify mentors
            var mentors = GetTargetMentors();
            foreach (var mentor in mentors)
            {
                RaiseNetworkEvent(new MentorHelpTicketMessagesMessage(ticketData.Id, new List<MentorHelpMessageData> { messageData }), mentor);
            }
        }

        private IList<INetChannel> GetTargetMentors()
        {
            return _adminManager.ActiveAdmins
                .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false)
                .Select(p => p.Channel)
                .ToList();
        }
    }
}