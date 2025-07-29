namespace Content.Client._White.Genetics
{
    [UserImplicitly]
    public sealed class DNAScannerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DNAScannerWindow? _window;

        public DNAScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new DNAScannerWindow
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.OpenCentered();
        }

    }

}
