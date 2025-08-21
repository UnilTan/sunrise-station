using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    public abstract class SharedComplaintSystem : EntitySystem
    {
        // System users
        public static NetUserId SystemUserId { get; } = new NetUserId(Guid.Empty);

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<ComplaintTextMessage>(OnComplaintTextMessage);
        }

        protected virtual void OnComplaintTextMessage(ComplaintTextMessage message, EntitySessionEventArgs eventArgs)
        {
            // Specific side code in target.
        }

        protected void LogComplaint(ComplaintTextMessage message)
        {
        }

        /// <summary>
        /// Request to create a new complaint
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class CreateComplaintRequest : EntityEventArgs
        {
            public NetUserId AgainstUserId { get; }
            public string Reason { get; }
            public string Description { get; }

            public CreateComplaintRequest(NetUserId againstUserId, string reason, string description)
            {
                AgainstUserId = againstUserId;
                Reason = reason;
                Description = description;
            }
        }

        /// <summary>
        /// Response when a complaint is created
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ComplaintCreatedResponse : EntityEventArgs
        {
            public int ComplaintId { get; }
            public bool Success { get; }
            public string? ErrorMessage { get; }

            public ComplaintCreatedResponse(int complaintId, bool success, string? errorMessage = null)
            {
                ComplaintId = complaintId;
                Success = success;
                ErrorMessage = errorMessage;
            }
        }

        /// <summary>
        /// Request to load complaint history
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ComplaintRequestHistory : EntityEventArgs
        {
            public int? ComplaintId { get; }
            public NetUserId? AboutUserId { get; }

            public ComplaintRequestHistory(int? complaintId = null, NetUserId? aboutUserId = null)
            {
                ComplaintId = complaintId;
                AboutUserId = aboutUserId;
            }
        }

        /// <summary>
        /// Response with complaint history
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ComplaintHistoryResponse : EntityEventArgs
        {
            public List<ComplaintInfo> Complaints { get; }

            public ComplaintHistoryResponse(List<ComplaintInfo> complaints)
            {
                Complaints = complaints;
            }
        }

        /// <summary>
        /// A text message within a complaint thread
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ComplaintTextMessage : EntityEventArgs
        {
            public DateTime SentAt { get; }
            public int ComplaintId { get; }
            public NetUserId SenderUserId { get; }
            public string Text { get; }
            public bool IsAdminMessage { get; }

            public ComplaintTextMessage(int complaintId, NetUserId senderUserId, string text, 
                DateTime? sentAt = default, bool isAdminMessage = false)
            {
                SentAt = sentAt ?? DateTime.Now;
                ComplaintId = complaintId;
                SenderUserId = senderUserId;
                Text = text;
                IsAdminMessage = isAdminMessage;
            }
        }

        /// <summary>
        /// Request to update complaint status
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ComplaintUpdateStatusRequest : EntityEventArgs
        {
            public int ComplaintId { get; }
            public ComplaintStatus Status { get; }
            public string? Note { get; }

            public ComplaintUpdateStatusRequest(int complaintId, ComplaintStatus status, string? note = null)
            {
                ComplaintId = complaintId;
                Status = status;
                Note = note;
            }
        }

        /// <summary>
        /// Information about a complaint
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ComplaintInfo
        {
            public int Id { get; set; }
            public NetUserId ComplainantUserId { get; set; }
            public NetUserId AgainstUserId { get; set; }
            public string ComplainantName { get; set; } = "";
            public string AgainstName { get; set; } = "";
            public string Reason { get; set; } = "";
            public string Description { get; set; } = "";
            public ComplaintStatus Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public NetUserId? AssignedAdminId { get; set; }
            public string? AssignedAdminName { get; set; }
            public List<ComplaintTextMessage> Messages { get; set; } = new();
        }

        /// <summary>
        /// Status of a complaint
        /// </summary>
        [Serializable, NetSerializable]
        public enum ComplaintStatus
        {
            Open,
            InProgress,
            Resolved,
            Rejected,
            Closed
        }

        /// <summary>
        /// Notification that complaint list has been updated
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ComplaintListUpdated : EntityEventArgs
        {
            public ComplaintListUpdated()
            {
            }
        }
    }
}