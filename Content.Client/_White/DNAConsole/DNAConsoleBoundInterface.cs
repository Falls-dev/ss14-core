using Content.Shared.DNAConsole;
using JetBrains.Annotations;

namespace Content.Client.DNAConsole.UI
{
    [UsedImplicitly]
    public sealed class DNAConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DNAConsoleWindow? _window;

        public DNAConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new DNAConsoleWindow
            {
                Title = "DNAConsole"
            };
            _window.OnClose += Close;
          //  _window.ModifierButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone));
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _window?.Populate((DNAConsoleBoundUserInterfaceState) state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_window != null)
            {
                _window.OnClose -= Close;
                //_window.DNAButton.OnPressed -= _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone));
            }
            _window?.Dispose();
        }
    }
}
