using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public sealed class AdminNotifyEuiState : EuiStateBase
    {
    }

    public static class AdminNotifyEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class DoNotify : EuiMessageBase
        {
            public bool CloseAfter;
            public string Target = default!;
            public string Title = default!;
            public string Message = default!;
        }
    }
}