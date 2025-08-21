using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems
{
    [UsedImplicitly]
    public sealed class ComplaintSystem : SharedComplaintSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly IPlayerLocator _playerLocator = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("COMPLAINTS");
            
            SubscribeNetworkEvent<CreateComplaintRequest>(OnCreateComplaintRequest);
            SubscribeNetworkEvent<ComplaintRequestHistory>(OnComplaintRequestHistory);
            SubscribeNetworkEvent<ComplaintUpdateStatusRequest>(OnComplaintUpdateStatusRequest);
        }

        private async void OnCreateComplaintRequest(CreateComplaintRequest request, EntitySessionEventArgs eventArgs)
        {
            var senderSession = eventArgs.SenderSession;
            
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.Description))
            {
                RaiseNetworkEvent(new ComplaintCreatedResponse(0, false, "Reason and description are required"), 
                    senderSession.Channel);
                return;
            }

            if (request.Reason.Length > 100 || request.Description.Length > 1000)
            {
                RaiseNetworkEvent(new ComplaintCreatedResponse(0, false, "Reason or description too long"), 
                    senderSession.Channel);
                return;
            }

            // Can't complain against yourself
            if (request.AgainstUserId == senderSession.UserId)
            {
                RaiseNetworkEvent(new ComplaintCreatedResponse(0, false, "Cannot create complaint against yourself"), 
                    senderSession.Channel);
                return;
            }

            // Check if target user exists
            var targetData = await _playerLocator.LookupIdAsync(request.AgainstUserId);
            if (targetData == null)
            {
                RaiseNetworkEvent(new ComplaintCreatedResponse(0, false, "Target player not found"), 
                    senderSession.Channel);
                return;
            }

            try
            {
                // Create complaint in database
                var complaintId = await _dbManager.CreateComplaintAsync(
                    senderSession.UserId,
                    request.AgainstUserId,
                    request.Reason,
                    request.Description);

                _sawmill.Info($"Complaint created: ID={complaintId}, By={senderSession.Name}, Against={targetData.Username}, Reason={request.Reason}");

                // Notify sender
                RaiseNetworkEvent(new ComplaintCreatedResponse(complaintId, true), senderSession.Channel);

                // Notify admins
                await NotifyAdminsOfNewComplaint(complaintId, senderSession.UserId, request.AgainstUserId, request.Reason);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to create complaint: {ex}");
                RaiseNetworkEvent(new ComplaintCreatedResponse(0, false, "Failed to create complaint"), 
                    senderSession.Channel);
            }
        }

        private async void OnComplaintRequestHistory(ComplaintRequestHistory request, EntitySessionEventArgs eventArgs)
        {
            var senderSession = eventArgs.SenderSession;
            var isAdmin = _adminManager.GetAdminData(senderSession)?.HasFlag(AdminFlags.Adminhelp) ?? false;

            try
            {
                List<ComplaintInfo> complaints;

                if (request.ComplaintId.HasValue)
                {
                    // Get specific complaint
                    var complaint = await _dbManager.GetComplaintAsync(request.ComplaintId.Value);
                    if (complaint == null || (!isAdmin && complaint.ComplainantUserId != senderSession.UserId))
                    {
                        complaints = new List<ComplaintInfo>();
                    }
                    else
                    {
                        complaints = new List<ComplaintInfo> { complaint };
                    }
                }
                else if (isAdmin && request.AboutUserId.HasValue)
                {
                    // Get all complaints about a specific user (admin only)
                    complaints = await _dbManager.GetComplaintsAboutUserAsync(request.AboutUserId.Value);
                }
                else if (isAdmin)
                {
                    // Get all complaints (admin only)
                    complaints = await _dbManager.GetAllComplaintsAsync();
                }
                else
                {
                    // Get user's own complaints
                    complaints = await _dbManager.GetComplaintsByUserAsync(senderSession.UserId);
                }

                RaiseNetworkEvent(new ComplaintHistoryResponse(complaints), senderSession.Channel);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to get complaint history: {ex}");
                RaiseNetworkEvent(new ComplaintHistoryResponse(new List<ComplaintInfo>()), senderSession.Channel);
            }
        }

        private async void OnComplaintUpdateStatusRequest(ComplaintUpdateStatusRequest request, EntitySessionEventArgs eventArgs)
        {
            var senderSession = eventArgs.SenderSession;
            var isAdmin = _adminManager.GetAdminData(senderSession)?.HasFlag(AdminFlags.Adminhelp) ?? false;

            if (!isAdmin)
            {
                _sawmill.Warning($"Non-admin {senderSession.Name} tried to update complaint status");
                return;
            }

            try
            {
                await _dbManager.UpdateComplaintStatusAsync(request.ComplaintId, request.Status, 
                    senderSession.UserId, request.Note);

                _sawmill.Info($"Complaint {request.ComplaintId} status updated to {request.Status} by {senderSession.Name}");

                // Notify all admins of status change
                await NotifyAdminsOfComplaintUpdate(request.ComplaintId, request.Status, senderSession.Name);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to update complaint status: {ex}");
            }
        }

        protected override void OnComplaintTextMessage(ComplaintTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnComplaintTextMessage(message, eventArgs);
            
            var senderSession = eventArgs.SenderSession;
            var isAdmin = _adminManager.GetAdminData(senderSession)?.HasFlag(AdminFlags.Adminhelp) ?? false;
            
            // Verify sender has access to this complaint
            Task.Run(async () => await HandleComplaintMessage(message, senderSession, isAdmin));
        }

        private async Task HandleComplaintMessage(ComplaintTextMessage message, ICommonSession senderSession, bool isAdmin)
        {
            try
            {
                // Get the complaint to verify access
                var complaint = await _dbManager.GetComplaintAsync(message.ComplaintId);
                if (complaint == null)
                {
                    _sawmill.Warning($"User {senderSession.Name} tried to send message to non-existent complaint {message.ComplaintId}");
                    return;
                }

                // Verify sender has access to this complaint
                var hasAccess = isAdmin || 
                               complaint.ComplainantUserId == senderSession.UserId ||
                               complaint.AgainstUserId == senderSession.UserId;
                
                if (!hasAccess)
                {
                    _sawmill.Warning($"User {senderSession.Name} tried to send message to complaint {message.ComplaintId} without access");
                    return;
                }

                // Save message to database
                await _dbManager.AddComplaintMessageAsync(message.ComplaintId, senderSession.UserId, message.Text, isAdmin);

                _sawmill.Info($"Complaint message: {senderSession.Name} -> Complaint #{message.ComplaintId}");

                // Create the message with proper timestamp for relay
                var relayMessage = new ComplaintTextMessage(
                    message.ComplaintId, 
                    senderSession.UserId, 
                    message.Text, 
                    DateTime.UtcNow, 
                    isAdmin);

                // Relay to relevant participants
                await RelayComplaintMessage(relayMessage, complaint);
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to handle complaint message: {ex}");
            }
        }

        private async Task RelayComplaintMessage(ComplaintTextMessage message, SharedComplaintSystem.ComplaintInfo complaint)
        {
            var recipients = new List<INetChannel>();

            // Add complainant if online
            var complainantSession = _playerManager.Sessions.FirstOrDefault(s => s.UserId == complaint.ComplainantUserId);
            if (complainantSession != null)
                recipients.Add(complainantSession.Channel);

            // Add against user if online  
            var againstSession = _playerManager.Sessions.FirstOrDefault(s => s.UserId == complaint.AgainstUserId);
            if (againstSession != null)
                recipients.Add(againstSession.Channel);

            // Add all admins with adminhelp permission
            var adminChannels = GetTargetAdmins();
            recipients.AddRange(adminChannels);

            // Remove duplicates
            recipients = recipients.Distinct().ToList();

            // Send to all recipients
            foreach (var channel in recipients)
            {
                RaiseNetworkEvent(message, channel);
            }
        }

        private async Task NotifyAdminsOfNewComplaint(int complaintId, NetUserId complainantId, NetUserId againstId, string reason)
        {
            var complainantData = await _playerLocator.LookupIdAsync(complainantId);
            var againstData = await _playerLocator.LookupIdAsync(againstId);

            if (complainantData == null || againstData == null)
                return;

            var message = $"New complaint #{complaintId}: {complainantData.Username} vs {againstData.Username} - {reason}";
            
            var admins = GetTargetAdmins();
            var systemMessage = new ComplaintTextMessage(complaintId, SystemUserId, message, isAdminMessage: true);

            foreach (var admin in admins)
            {
                RaiseNetworkEvent(systemMessage, admin);
            }

            RaiseNetworkEvent(new ComplaintListUpdated(), Filter.Broadcast());
        }

        private async Task NotifyAdminsOfComplaintUpdate(int complaintId, ComplaintStatus status, string adminName)
        {
            var message = $"Complaint #{complaintId} status changed to {status} by {adminName}";
            
            var admins = GetTargetAdmins();
            var systemMessage = new ComplaintTextMessage(complaintId, SystemUserId, message, isAdminMessage: true);

            foreach (var admin in admins)
            {
                RaiseNetworkEvent(systemMessage, admin);
            }

            RaiseNetworkEvent(new ComplaintListUpdated(), Filter.Broadcast());
        }

        private IList<INetChannel> GetTargetAdmins()
        {
            return _adminManager.ActiveAdmins
                .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false)
                .Select(p => p.Channel)
                .ToList();
        }
    }
}