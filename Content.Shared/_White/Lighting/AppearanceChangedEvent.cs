namespace Content.Shared._White.Lighting;

public sealed class AppearanceChangedEvent : EntityEventArgs
{
    public Enum State;

    public AppearanceChangedEvent(Enum key)
    {
        State = key;
    }
}

