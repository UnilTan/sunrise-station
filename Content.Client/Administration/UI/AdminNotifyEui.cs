using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI
{
    public sealed class AdminNotifyEui : BaseEui
    {
        private readonly AdminNotifyWindow _window;

        public AdminNotifyEui()
        {
            _window = new AdminNotifyWindow();
            _window.OnClose += () => SendMessage(new CloseEuiMessage());
            _window.NotifyButton.OnPressed += NotifyButtonOnPressed;
        }

        private void NotifyButtonOnPressed(BaseButton.ButtonEventArgs obj)
        {
            SendMessage(new AdminNotifyEuiMsg.DoNotify
            {
                Target = _window.Target.Text,
                Title = _window.NotifyTitle.Text,
                Message = Rope.Collapse(_window.Message.TextRope),
                CloseAfter = !_window.KeepWindowOpen.Pressed,
            });
        }

        public override void Opened()
        {
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }
    }
}