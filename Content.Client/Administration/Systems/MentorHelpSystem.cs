using System;
using System.Collections.Generic;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Client.Administration.Systems
{
    /// <summary>
    /// Client-side mentor help system
    /// </summary>
    [UsedImplicitly]
    public sealed class MentorHelpSystem : SharedMentorHelpSystem
    {
        public event EventHandler<MentorHelpTicketUpdateMessage>? OnTicketUpdated;
        public event EventHandler<MentorHelpTicketsListMessage>? OnTicketsListReceived;
        public event EventHandler<MentorHelpTicketMessagesMessage>? OnTicketMessagesReceived;

        protected override void OnCreateTicketMessage(MentorHelpCreateTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnClaimTicketMessage(MentorHelpClaimTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnReplyMessage(MentorHelpReplyMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnCloseTicketMessage(MentorHelpCloseTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnRequestTicketsMessage(MentorHelpRequestTicketsMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<MentorHelpTicketUpdateMessage>(OnTicketUpdate);
            SubscribeNetworkEvent<MentorHelpTicketsListMessage>(OnTicketsList);
            SubscribeNetworkEvent<MentorHelpTicketMessagesMessage>(OnTicketMessages);
        }

        private void OnTicketUpdate(MentorHelpTicketUpdateMessage message, EntitySessionEventArgs eventArgs)
        {
            OnTicketUpdated?.Invoke(this, message);
        }

        private void OnTicketsList(MentorHelpTicketsListMessage message, EntitySessionEventArgs eventArgs)
        {
            OnTicketsListReceived?.Invoke(this, message);
        }

        private void OnTicketMessages(MentorHelpTicketMessagesMessage message, EntitySessionEventArgs eventArgs)
        {
            OnTicketMessagesReceived?.Invoke(this, message);
        }

        /// <summary>
        /// Create a new mentor help ticket
        /// </summary>
        public void CreateTicket(string subject, string message)
        {
            RaiseNetworkEvent(new MentorHelpCreateTicketMessage(subject, message));
        }

        /// <summary>
        /// Claim a mentor help ticket
        /// </summary>
        public void ClaimTicket(int ticketId)
        {
            RaiseNetworkEvent(new MentorHelpClaimTicketMessage(ticketId));
        }

        /// <summary>
        /// Reply to a mentor help ticket
        /// </summary>
        public void ReplyToTicket(int ticketId, string message, bool isStaffOnly = false)
        {
            RaiseNetworkEvent(new MentorHelpReplyMessage(ticketId, message, isStaffOnly));
        }

        /// <summary>
        /// Close a mentor help ticket
        /// </summary>
        public void CloseTicket(int ticketId)
        {
            RaiseNetworkEvent(new MentorHelpCloseTicketMessage(ticketId));
        }

        /// <summary>
        /// Request tickets (either all for mentors/admins, or only own for players)
        /// </summary>
        public void RequestTickets(bool onlyMine = false)
        {
            RaiseNetworkEvent(new MentorHelpRequestTicketsMessage(onlyMine));
        }

        /// <summary>
        /// Request messages for a specific ticket
        /// </summary>
        public void RequestTicketMessages(int ticketId)
        {
            // For now, we'll implement this as part of ticket selection
            // In a more complete implementation, you'd want a separate message type
            // For simplicity, we'll trigger this when a ticket is selected
        }
    }
}