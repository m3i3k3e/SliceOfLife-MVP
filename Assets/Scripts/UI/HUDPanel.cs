using UnityEngine;

/// <summary>
/// Base class for modular HUD panels. Handles registration with the
/// <see cref="HUD"/> container and exposes references to core systems.
/// Derived panels subscribe to only the events they care about.
/// </summary>
public abstract class HUDPanel : MonoBehaviour
{
    // Cached reference to the parent HUD so we can register/unregister.
    private HUD _hud;

    /// <summary>GameManager dependency supplied by the HUD.</summary>
    protected IGameManager GM { get; private set; }

    /// <summary>Event bus dependency supplied by the HUD.</summary>
    protected IEventBus Events { get; private set; }

    protected virtual void Awake()
    {
        // Grab the parent HUD once. Avoids calling GetComponentInParent repeatedly.
        _hud = GetComponentInParent<HUD>();
    }

    protected virtual void OnEnable()
    {
        // Register with the HUD so it can provide dependencies.
        _hud?.RegisterPanel(this);
    }

    protected virtual void OnDisable()
    {
        // Unregister to drop event subscriptions when disabled.
        _hud?.UnregisterPanel(this);
    }

    /// <summary>
    /// Called by <see cref="HUD"/> when the GameManager and EventBus are ready.
    /// Panels should override <see cref="OnBind"/> to subscribe to events.
    /// </summary>
    public void Bind(IGameManager gm, IEventBus events)
    {
        GM = gm;
        Events = events;
        OnBind();
    }

    /// <summary>
    /// Called by <see cref="HUD"/> when dependencies are being torn down.
    /// Panels should override <see cref="OnUnbind"/> to unsubscribe.
    /// </summary>
    public void Unbind()
    {
        OnUnbind();
        GM = null;
        Events = null;
    }

    /// <summary>
    /// Override to hook up event handlers. Called after dependencies are set.
    /// </summary>
    protected virtual void OnBind() { }

    /// <summary>
    /// Override to remove event handlers. Called before dependencies are cleared.
    /// </summary>
    protected virtual void OnUnbind() { }
}

