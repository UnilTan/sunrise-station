using Content.Shared.MusicConsole;

namespace Content.Client.MusicConsole.UI
{
    public sealed class MusicConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MusicConsoleMenu? _menu;

        public MusicConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();

            _menu = new MusicConsoleMenu();
            _menu.OnClose += Close;
            _menu.OpenCentered();

            _menu.OnPlayPressed += args =>
            {
                if (args)
                {
                    SendMessage(new MusicConsolePlayMessage());
                }
                else
                {
                    SendMessage(new MusicConsolePauseMessage());
                }
            };
            _menu.OnStopPressed += () => SendMessage(new MusicConsoleStopMessage());
            _menu.SetVolume += volume => SendMessage(new MusicConsoleSetVolumeMessage(volume));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not MusicConsoleInterfaceState musicState || _menu == null)
                return;

            _menu.SetVolumeSlider(musicState.MusicVolume * 100f);

            if (musicState.CurrentSongName != null)
            {
                _menu.SetSelectedSong(musicState.CurrentSongName, 0f);
            }
            else
            {
                _menu.SetSelectedSong(string.Empty, 0f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnClose -= Close;
            _menu.Dispose();
            _menu = null;
        }
    }
}
