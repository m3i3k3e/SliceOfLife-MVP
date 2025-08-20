/*
 * BattleManager.cs
 * Purpose: Orchestrates the Player vs. Enemy turn loop and coordinates subsystems.
 * Dependencies: DeckManager, EnergyPool, StatusController, IGameManager, IEventBus.
 * Expansion Hooks: Public events allow UI or other systems to react without tight coupling.
 *                 New card behaviors are introduced via <see cref="CardEffect"/> assets
 *                 and new enemy intents by extending EnemyIntentType and EnemyTurn's switch.
 * Rationale: Interfaces and event bus keep this class testable and decoupled; async reward grant.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal, readable turn loop:
/// PlayerTurn -> EnemyTurn -> check win/lose -> back to PlayerTurn.
/// Coordinates specialized subsystems (deck, energy, statuses, rewards).
/// UI is driven via events; <see cref="CardView"/> instances call <see cref="PlayCard"/>.
/// Legacy button actions have been removed in favor of the card system.
/// </summary>
/// <remarks>
/// References to <see cref="IGameManager"/> and <see cref="IEventBus"/> are injected to avoid
/// direct dependencies on scene singletons.
/// </remarks>
public class BattleManager : MonoBehaviour
{
    // --- Events so UI can stay dumb and reactive ---
    /// <summary>Player HP/MaxHP/Armor changed.</summary>
    public event Action<int, int, int> OnPlayerStatsChanged; // (hp, maxHp, armor)
    /// <summary>Enemy HP/MaxHP changed.</summary>
    public event Action<int, int> OnEnemyStatsChanged;       // (hp, maxHp)
    /// <summary>Informational string for the HUD.</summary>
    public event Action<string> OnInfoChanged;               // "Your turn", "Enemy intends X", etc.
    /// <summary>Telegraphed enemy intent for next turn.</summary>
    public event Action<EnemyIntent> OnEnemyIntentChanged;   // next enemy action preview
    /// <summary>Battle concluded. Bool indicates victory, int is essence reward.</summary>
    public event Action<bool, int> OnBattleEnded;            // (victory?, rewardEssence)

    // Bubble up events from subsystems so callers don't need direct refs
    /// <summary>Status label added/removed on the player.</summary>
    public event Action<string> OnPlayerStatusChanged;
    /// <summary>Status label added/removed on the enemy.</summary>
    public event Action<string> OnEnemyStatusChanged;
    /// <summary>Energy pool updated. (current, max)</summary>
    public event Action<int, int> OnEnergyChanged; // (current, max)
    /// <summary>Player hand changed. Provides current list of cards.</summary>
    public event Action<IReadOnlyList<CardSO>> OnHandChanged;

    /// <summary>
    /// UI hook to end the player's turn early. Part of the public surface so
    /// buttons remain ignorant of internal flow.
    /// </summary>
    public void EndTurn()
    {
        if (!_playerTurn) return;
        EndPlayerTurn();
    }

    /// <summary>
    /// Checks the player's hand and queues an automatic end-turn if nothing is affordable.
    /// </summary>
    private void MaybeAutoEndTurn()
    {
        if (!_playerTurn) return; // only care during the player's turn

        bool anyPlayable = false;
        foreach (var c in _deck.Hand)
        {
            if (c && c.cost <= _energy.Current) { anyPlayable = true; break; }
        }

        if (!anyPlayable) StartCoroutine(AutoEndTurnAfterBeat());
    }

    /// <summary>
    /// Small coroutine delay so players can visually confirm no actions are available
    /// before the turn auto-ends.
    /// </summary>
    private IEnumerator AutoEndTurnAfterBeat()
    {
        OnInfoChanged?.Invoke("No playable cards… ending turn.");
        yield return new WaitForSeconds(config.autoEndTurnDelay);
        if (_playerTurn) EndPlayerTurn();
    }

    [Header("Config (assign in Inspector)")]
    [SerializeField] private BattleConfigSO config;

    [Tooltip("Defines the full deck for this battle (duplicates allowed). Shuffled at start.")]
    [SerializeField] private List<CardSO> startingDeck = new();

    // --- Subsystems ---
    private readonly DeckManager _deck = new();
    private readonly EnergyPool _energy = new();
    private readonly StatusController _status = new();
    private BattleRewardService _rewards;

    // --- Runtime state ---
    private int _playerHP;
    private int _playerArmor;
    private int _playerMendUsesLeft;

    private int _enemyHP;
    private EnemySO _enemyData;           // data for the current enemy

    private EnemyIntent _nextEnemyIntent;

    private bool _playerTurn; // simple flag for whose turn it is

    // Reference to pluggable enemy AI strategy
    private IEnemyAI _enemyAI;

    [Header("Dependencies")]
    [Tooltip("Reference to a GameManager implementing IGameManager.")]
    [SerializeField] private MonoBehaviour gameManagerSource;

    [Tooltip("Reference to an event bus implementing IEventBus.")]
    [SerializeField] private MonoBehaviour eventBusSource;

    // Helper accessors cast the serialized MonoBehaviours to their interfaces.
    // This lets designers assign any implementation in the Inspector while keeping code
    // dependent only on abstractions.
    private IGameManager GM => gameManagerSource as IGameManager;
    private IEventBus Events => eventBusSource as IEventBus;

    /// <summary>
    /// Resolve references and wire up subsystem events.
    /// Using serialized <see cref="MonoBehaviour"/> fields lets us inject interface
    /// implementations while still using the Unity inspector.
    /// </summary>
    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("BattleManager: Missing BattleConfigSO. Assign one in the Inspector.");
        }
        // Instantiate reward service with injected GameManager dependency.
        _rewards = new BattleRewardService(GM);

        // Bubble subsystem events up to our public surface so callers only depend on BattleManager.
        _deck.OnHandChanged += h => OnHandChanged?.Invoke(h);
        _energy.OnEnergyChanged += (c, m) => OnEnergyChanged?.Invoke(c, m);
        _status.OnPlayerStatusChanged += s => OnPlayerStatusChanged?.Invoke(s);
        _status.OnEnemyStatusChanged += s => OnEnemyStatusChanged?.Invoke(s);
    }

    /// <summary>Refill energy and hand control to the player.</summary>
    private void BeginPlayerTurn()
    {
        _playerTurn = true;
        _energy.RefillForTurn(Mathf.Max(0, config.energyPerTurn));
        _deck.Draw(Mathf.Max(0, config.handSize));
        // After spawning a new hand, ping energy again so affordability greys out correctly
        OnEnergyChanged?.Invoke(_energy.Current, _energy.Max);
        MaybeAutoEndTurn();// No playable cards? - auto-ends turn
        OnInfoChanged?.Invoke("Your turn");
    }

    /// <summary>
    /// Initialize battle state from config and hand control to the player.
    /// </summary>
    private void Start()
    {
        // Initialize runtime stats from config
        _playerHP = config.playerMaxHP;
        _playerArmor = 0;
        _playerMendUsesLeft = 0;
        // Look for a healing card to determine how many mend uses are available.
        foreach (var c in startingDeck)
        {
            if (c && c.Effect != null && c.Effect.Heal > 0 && c.Effect.MaxUses > 0)
            {
                _playerMendUsesLeft = c.Effect.MaxUses;
                break; // assume a single Mend card for MVP
            }
        }

        // Cache enemy data and AI from config so encounters can swap enemies easily
        _enemyData = config.enemy;
        if (_enemyData == null)
        {
            Debug.LogError("BattleManager: No enemy assigned in BattleConfig.");
            return;
        }
        _enemyAI = _enemyData.AIStrategy;
        if (_enemyAI == null) _enemyAI = ScriptableObject.CreateInstance<EnemyAI>(); // safe default

        _enemyHP = _enemyData.MaxHP;

        // First enemy intent is rolled up-front so the player can see it
        _nextEnemyIntent = _enemyAI.DecideNextIntent(_enemyData);

        // Notify UI of initial values
        PushPlayerStats();
        PushEnemyStats();
        OnEnemyIntentChanged?.Invoke(_nextEnemyIntent);

        // Init subsystems and start the fight
        _energy.SetMax(Mathf.Max(config.maxEnergy, config.energyPerTurn));
        _status.PushLabels();
        _deck.BuildAndShuffle(startingDeck);   // build draw pile from starting deck
        BeginPlayerTurn();

    }

    /// <summary>Execute a specific card by reference. Used by CardView to keep the
    /// battle brain decoupled from UI prefabs.</summary>
    public void PlayCard(CardSO card)
    {
        if (card == null || !_playerTurn) return;

        // Card must exist in hand (prevents clicking stale UI)
        if (!System.Linq.Enumerable.Contains(_deck.Hand, card)) return;

        // Spend energy first so UI disables unaffordable cards immediately
        if (!TrySpendEnergy(Mathf.Max(0, card.cost))) return;

        // Apply effect using the card's polymorphic CardEffect asset
        if (card.Effect != null)
        {
            var ctx = new BattleContext(this);
            card.Effect.Execute(ctx);
        }

        // Move the card to discard (all cards are one-use per turn in this MVP)
        _deck.Discard(card);

        // Win/flow
        PostPlayerCardResolved(); // will end turn if energy hit 0
    }

    // --- Methods exposed to CardEffect via BattleContext ---
    /// <summary>Deal player-sourced damage to the enemy and update UI.</summary>
    internal void DamageEnemy(int amount)
    {
        _enemyHP -= _status.ModPlayerToEnemyDamage(Mathf.Max(0, amount));
        PushEnemyStats();
    }

    /// <summary>Grant temporary armor to the player.</summary>
    internal void AddPlayerArmor(int amount)
    {
        _playerArmor += Mathf.Max(0, amount);
        PushPlayerStats();
    }

    /// <summary>Heal the player if any Mend uses remain.</summary>
    internal bool MendPlayer(int amount)
    {
        if (_playerMendUsesLeft <= 0) return false;
        _playerHP = Mathf.Min(config.playerMaxHP, _playerHP + Mathf.Max(0, amount));
        _playerMendUsesLeft--;
        PushPlayerStats();
        return true;
    }

    /// <summary>Apply Weak to the enemy.</summary>
    internal void ApplyWeak(int stacks) => _status.ApplyWeakToEnemy(stacks);

    /// <summary>Apply Vulnerable to the enemy.</summary>
    internal void ApplyVulnerable(int stacks) => _status.ApplyVulnerableToEnemy(stacks);

    /// <summary>Wrapper over EnergyPool.TrySpend that also surfaces a UI message.</summary>
    private bool TrySpendEnergy(int amount)
    {
        if (!_energy.TrySpend(amount))
        {
            OnInfoChanged?.Invoke("Not enough energy");
            return false;
        }
        return true;
    }

    /// <summary>After a card resolves: win check, maybe end the turn on 0 energy.</summary>
    private void PostPlayerCardResolved()
    {
        // Win?
        if (_enemyHP <= 0)
        {
            Victory();
            return;
        }

        // Out of energy → pass to enemy
        if (_energy.Current <= 0)
        {
            EndPlayerTurn();
            return;
        }
        // Still the player's turn but might be stuck with expensive cards
        MaybeAutoEndTurn();
    }

    // --- Turn transitions ---

    /// <summary>
    /// Wrap up the player's turn and begin the enemy's response.
    /// </summary>
    private void EndPlayerTurn()
    {
        // Victory() is handled earlier; here we simply hand control to the enemy.
        _status.TickEndOfPlayerTurn();  // decrement player status durations
        _playerTurn = false;

        _deck.DiscardHand();          // dump any unplayed cards so the enemy can't see them

        StartCoroutine(EnemyTurn()); // async to allow small pauses/FX
    }

    /// <summary>
    /// Coroutine executing the enemy's intent before returning control to the player.
    /// </summary>
    private IEnumerator EnemyTurn()
    {
        // 1) Short pause so players can read the intent telegraph
        OnInfoChanged?.Invoke($"Enemy uses {_nextEnemyIntent.label}...");
        yield return new WaitForSeconds(0.25f);

        // 2) Execute the telegraphed intent
        switch (_nextEnemyIntent.type)
        {
            case EnemyIntentType.LightAttack:
            case EnemyIntentType.HeavyAttack:
                int dmg = _status.ModEnemyToPlayerDamage(_nextEnemyIntent.magnitude);
                // Armor absorbs damage first, then the remainder hits HP
                int absorbed = Mathf.Min(_playerArmor, dmg);
                int through = dmg - absorbed;
                _playerArmor -= absorbed;
                _playerHP -= through;
                break;

            case EnemyIntentType.LeechHeal:
                // Enemy heals itself using its configured amount
                _enemyHP = Mathf.Min(_enemyData.MaxHP, _enemyHP + _enemyData.LeechHeal);
                break;
        }
        // Add new EnemyIntentType cases above and update EnemyIntent enum accordingly.

        // 3) Update UI with new stats
        PushPlayerStats();
        PushEnemyStats();

        // 4) Check defeat before rolling next intent
        if (_playerHP <= 0)
        {
            Defeat();
            yield break;
        }

        // 5) Preview next enemy move so player can plan
        _nextEnemyIntent = _enemyAI.DecideNextIntent(_enemyData);
        OnEnemyIntentChanged?.Invoke(_nextEnemyIntent);

        // 6) Tick statuses and return control to player
        _status.TickEndOfEnemyTurn();
        BeginPlayerTurn();
    }

    // --- Endings ---

    /// <summary>
    /// Handle victory flow and award essence asynchronously.
    /// </summary>
    private void Victory()
    {
        // Calculate the reward synchronously.
        int reward = _rewards.GrantVictoryReward(config);
        _rewards.DropShards(); // shards drop win or lose

        OnInfoChanged?.Invoke($"Victory! +{reward} Essence");
        OnBattleEnded?.Invoke(true, reward);

        // Record that the player attempted the dungeon today so daily gating works.
        GameManager.Instance?.MarkDungeonAttempted();

        StartCoroutine(ReturnToBasementAfterDelay());
    }

    /// <summary>
    /// Handle defeat flow and apply penalties.
    /// </summary>
    private void Defeat()
    {
        OnInfoChanged?.Invoke("Defeat! Returning to tavern...");
        _rewards.DropShards();
        OnBattleEnded?.Invoke(false, 0);

        // Apply the loss penalty (immediate essence loss + next-day click debuff)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyDungeonLossPenalty();
            // (Optional) If you didn’t mark attempt on enter for any reason:
            GameManager.Instance.MarkDungeonAttempted();
        }

        StartCoroutine(ReturnToBasementAfterDelay());
    }

    /// <summary>
    /// Delay helper so victory/defeat messages linger before returning to the town scene.
    /// </summary>
    private IEnumerator ReturnToBasementAfterDelay()
    {
        yield return new WaitForSeconds(config.returnDelay);
        // After battle conclude, send the player back to the Basement (home base).
        SceneManager.LoadScene("Basement"); // ensure "Basement" is in Build Settings
    }

    // --- Helpers to notify UI ---

    /// <summary>Push current player stats to any UI listeners.</summary>
    private void PushPlayerStats() => OnPlayerStatsChanged?.Invoke(_playerHP, config.playerMaxHP, _playerArmor);

    /// <summary>Push current enemy stats to any UI listeners.</summary>
    private void PushEnemyStats()  => OnEnemyStatsChanged?.Invoke(_enemyHP, _enemyData.MaxHP);
}

/// <summary>Simple, serializable intent so UI can show the enemy's plan.</summary>
[Serializable]
public struct EnemyIntent
{
    public EnemyIntentType type;
    public int magnitude;     // damage amount for attacks; 0 for heal (we show label instead)
    public string label;      // human-readable text for UI
}

/// <summary>Types of enemy actions the turn loop understands.</summary>
public enum EnemyIntentType { LightAttack, HeavyAttack, LeechHeal }

