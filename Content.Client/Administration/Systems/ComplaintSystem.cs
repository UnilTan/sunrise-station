using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Administration.Systems
{
    [UsedImplicitly]
    public sealed class ComplaintSystem : SharedComplaintSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        public event EventHandler<ComplaintTextMessage>? OnComplaintTextMessageReceived;
        public event EventHandler<ComplaintCreatedResponse>? OnComplaintCreatedResponse;
        public event EventHandler<ComplaintHistoryResponse>? OnComplaintHistoryResponse;
        public event EventHandler<ComplaintListUpdated>? OnComplaintListUpdated;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<ComplaintTextMessage>(OnComplaintTextMessageReceived);
            SubscribeNetworkEvent<ComplaintCreatedResponse>(OnComplaintCreatedResponseReceived);
            SubscribeNetworkEvent<ComplaintHistoryResponse>(OnComplaintHistoryResponseReceived);
            SubscribeNetworkEvent<ComplaintListUpdated>(OnComplaintListUpdatedReceived);
        }

        protected override void OnComplaintTextMessage(ComplaintTextMessage message, EntitySessionEventArgs eventArgs)
        {
            OnComplaintTextMessageReceived?.Invoke(this, message);
        }

        private void OnComplaintCreatedResponseReceived(ComplaintCreatedResponse response, EntitySessionEventArgs eventArgs)
        {
            OnComplaintCreatedResponse?.Invoke(this, response);
        }

        private void OnComplaintHistoryResponseReceived(ComplaintHistoryResponse response, EntitySessionEventArgs eventArgs)
        {
            OnComplaintHistoryResponse?.Invoke(this, response);
        }

        private void OnComplaintListUpdatedReceived(ComplaintListUpdated update, EntitySessionEventArgs eventArgs)
        {
            OnComplaintListUpdated?.Invoke(this, update);
        }

        public void CreateComplaint(NetUserId againstUserId, string reason, string description)
        {
            RaiseNetworkEvent(new CreateComplaintRequest(againstUserId, reason, description));
        }

        public void RequestComplaintHistory(int? complaintId = null, NetUserId? aboutUserId = null)
        {
            RaiseNetworkEvent(new ComplaintRequestHistory(complaintId, aboutUserId));
        }

        public void UpdateComplaintStatus(int complaintId, ComplaintStatus status, string? note = null)
        {
            RaiseNetworkEvent(new ComplaintUpdateStatusRequest(complaintId, status, note));
        }

        public void SendComplaintMessage(int complaintId, string text, bool isAdminMessage = false)
        {
            RaiseNetworkEvent(new ComplaintTextMessage(complaintId, default, text, isAdminMessage: isAdminMessage));
        }
    }
}