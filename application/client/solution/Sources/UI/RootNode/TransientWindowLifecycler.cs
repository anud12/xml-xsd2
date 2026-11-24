namespace NewGameProject.UI;

/// <summary>Ownership of one hover-derived window, independent of its control.</summary>
public sealed class TransientWindowLifecycler
{
    public enum Ownership { Transient, Pinned, Dismissed }
    public Ownership State { get; private set; } = Ownership.Transient;
    public bool SourceActive { get; private set; } = true;

    public void Pin()
    {
        if (State == Ownership.Transient) State = Ownership.Pinned;
    }

    public void Unpin()
    {
        if (State == Ownership.Pinned)
            State = SourceActive ? Ownership.Transient : Ownership.Dismissed;
    }

    public void RemoveDelta()
    {
        SourceActive = false;
        if (State == Ownership.Transient) State = Ownership.Dismissed;
    }

    public void SourceHoverEnter() { if (State != Ownership.Dismissed) SourceActive = true; }
    public void SourceHoverExit() { if (State != Ownership.Dismissed) SourceActive = false; }

    // A pinned re-add updates source activity but never replaces its instance.
    // A dismissed instance cannot be resurrected; a new add gets a new lifecycler.
    public void AddDelta()
    {
        if (State != Ownership.Dismissed) SourceActive = true;
    }

    // Pinned windows outlive their source, just as they outlive hover exit.
    public void SourceRemoved() => RemoveDelta();
}
