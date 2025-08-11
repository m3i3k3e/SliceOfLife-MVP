/*
 * HUD.cs
 * Role: Root hub that hands core dependencies to HUD panels and coordinates their binding lifecycle.
 * Key dependencies: IGameManager for game state, IEventBus for global notifications, HUDPanel components.
 * Expansion: Add new HUD panels by deriving from HUDPanel and registering them in scenes.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight container that simply passes core dependencies to child
/// <see cref="HUDPanel"/> components. Panels handle their own event
/// subscriptions so the root stays agnostic of specific UI details.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Dependencies")]
    /// <summary>Reference to a GameManager instance implementing IGameManager.</summary>
    [Tooltip("Reference to a GameManager instance implementing IGameManager.")]
    [SerializeField] private MonoBehaviour gameManagerSource;

    /// <summary>Reference to an event bus implementing IEventBus.</summary>
    [Tooltip("Reference to an event bus implementing IEventBus.")]
    [SerializeField] private MonoBehaviour eventBusSource;

    // Cast serialized refs to interfaces so callers remain decoupled from
    // concrete MonoBehaviour types.
    private IGameManager GM => gameManagerSource as IGameManager;
    private IEventBus Events => eventBusSource as IEventBus;

    /// <summary>Keep track of registered panels so we can bind/unbind them as a group.</summary>
    private readonly List<HUDPanel> _panels = new();

    /// <summary>Tracks whether core dependencies are ready before binding panels.</summary>
    private bool _ready;

    /// <summary>Unity OnEnable: delay binding until dependencies are assigned.</summary>
    private void OnEnable()
    {
        // Wait for the GameManager to exist before initializing panels.
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // GameManager is injected via the inspector; defer until assigned.
        while (GM == null) yield return null;

        // At this point we can safely pass references to all registered panels.
        _ready = true;
        foreach (var panel in _panels)
            panel.Bind(GM, Events);
    }

    /// <summary>Unity OnDisable: unbind all panels to drop event subscriptions.</summary>
    private void OnDisable()
    {
        foreach (var panel in _panels)
            panel.Unbind();
        _ready = false;
    }

    /// <summary>
    /// Registers a panel with the HUD. Called automatically from
    /// <see cref="HUDPanel.OnEnable"/>.
    /// </summary>
    public void RegisterPanel(HUDPanel panel)
    {
        if (panel == null || _panels.Contains(panel)) return;

        _panels.Add(panel);

        // If dependencies are already ready, bind immediately so late-joining
        // panels (e.g., added via prefab) still receive events.
        if (_ready)
            panel.Bind(GM, Events);
    }

    /// <summary>
    /// Unregisters a panel. Called automatically when a panel disables.
    /// </summary>
    public void UnregisterPanel(HUDPanel panel)
    {
        if (panel == null) return;

        if (_panels.Remove(panel) && _ready)
            panel.Unbind();
    }
}

