using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using System.Linq;

namespace Content.Server.Administration.UI
{
    public sealed class AdminNotifyEui : BaseEui
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private QuickDialogSystem _quickDialogSystem = default!;

        public AdminNotifyEui()
        {
            IoCManager.InjectDependencies(this);
            _quickDialogSystem = _entitySystemManager.GetEntitySystem<QuickDialogSystem>();
        }

        public override void Opened()
        {
            StateDirty();
        }

        public override EuiStateBase GetNewState()
        {
            return new AdminNotifyEuiState();
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case AdminNotifyEuiMsg.DoNotify doNotify:
                    if (!_adminManager.HasAdminFlag(Player, AdminFlags.Moderator))
                    {
                        Close();
                        break;
                    }

                    SendNotification(doNotify.Target, doNotify.Title, doNotify.Message);

                    if (doNotify.CloseAfter)
                        Close();

                    break;
            }
        }

        private void SendNotification(string target, string title, string message)
        {
            if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                // Send to all players
                _quickDialogSystem.NotifyAllPlayers(title, message);
            }
            else
            {
                // Send to specific players
                var usernames = target.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(u => u.Trim())
                                      .ToArray();

                _quickDialogSystem.NotifyPlayers(title, message, usernames);
            }
        }
    }
}