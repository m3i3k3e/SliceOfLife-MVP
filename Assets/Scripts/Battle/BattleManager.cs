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
public class BattleManager : MonoBehaviour
{
    // --- Events so UI can stay dumb and reactive ---
    public event Action<int, int, int> OnPlayerStatsChanged; // (hp, maxHp, armor)
    public event Action<int, int> OnEnemyStatsChanged;       // (hp, maxHp)
    public event Action<string> OnInfoChanged;               // "Your turn", "Enemy intends X", etc.
    public event Action<EnemyIntent> OnEnemyIntentChanged;   // next enemy action preview
    public event Action<bool, int> OnBattleEnded;            // (victory?, rewardEssence)

    // Bubble up events from subsystems so callers don't need direct refs
    public event Action<string> OnPlayerStatusChanged;
    public event Action<string> OnEnemyStatusChanged;
    public event Action<int, int> OnEnergyChanged; // (current, max)
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

    // --- Utility: if you have no playable cards, auto end the turn after a tiny beat ---
    private void MaybeAutoEndTurn()
    {
        if (!_playerTurn) return;

        bool anyPlayable = false;
        foreach (var c in _deck.Hand)
        {
            if (c && c.cost <= _energy.Current) { anyPlayable = true; break; }
        }

        if (!anyPlayable) StartCoroutine(AutoEndTurnAfterBeat());
    }

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

    private EnemyIntent _nextEnemyIntent;

    private bool _playerTurn; // simple flag for whose turn it is

    // Cheap link to our tiny enemy AI
    private EnemyAI _enemy;

    [Header("Dependencies")]
    [Tooltip("Reference to a GameManager implementing IGameManager.")]
    [SerializeField] private MonoBehaviour gameManagerSource;

    // Helper to cast the serialized MonoBehaviour to the interface.
    private IGameManager GM => gameManagerSource as IGameManager;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("BattleManager: Missing BattleConfigSO. Assign one in the Inspector.");
        }
        _enemy = GetComponent<EnemyAI>();
        if (_enemy == null) _enemy = gameObject.AddComponent<EnemyAI>(); // safe default

        // Instantiate reward service with injected GameManager dependency.
        _rewards = new BattleRewardService(GM);

        // Bubble subsystem events up to our public surface
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

    private void Start()
    {
        // Initialize runtime stats from config
        _playerHP = config.playerMaxHP;
        _playerArmor = 0;
        _playerMendUsesLeft = Mathf.Max(0, config.mendUses);

        _enemyHP = config.enemyMaxHP;

        // First enemy intent is rolled up-front so the player can see it
        _nextEnemyIntent = _enemy.DecideNextIntent(config);

        // Notify UI of initial values
        PushPlayerStats();
        PushEnemyStats();
        OnEnemyIntentChanged?.Invoke(_nextEnemyIntent);

        // Init subsystems and start the fight
        _energy.SetMax(Mathf.Max(config.maxEnergy, config.energyPerTurn));
        _status.PushLabels();
        _deck.BuildAndShuffle(startingDeck);   // <- NEW
        BeginPlayerTurn();

    }

    /// <summary>Execute a specific card by reference. Used by CardView to keep the
    /// battle brain decoupled from UI prefabs.</summary>
    public void PlayCard(CardSO card)
    {
        if (card == null || !_playerTurn) return;

        // Card must exist in hand (prevents clicking stale UI)
        if (!_deck.Hand.Contains(card)) return;

        // Spend energy first so UI disables unaffordable cards immediately
        if (!TrySpendEnergy(Mathf.Max(0, card.cost))) return;

        // Apply effect
        ApplyActionEffect(card.action);

        // Move the card to discard (all cards are one-use per turn in this MVP)
        _deck.Discard(card);

        // Win/flow
        PostPlayerCardResolved(); // will end turn if energy hit 0
    }


    /// <summary>Apply the effect of an action without spending energy or ending the turn.</summary>
    private void ApplyActionEffect(BattleAction action)
    {
        switch (action)
        {
            case BattleAction.Attack:
                _enemyHP -= _status.ModPlayerToEnemyDamage(Mathf.Max(0, config.attackDamage));
                PushEnemyStats();
                break;

            case BattleAction.Guard:
                _playerArmor += Mathf.Max(0, config.guardBlock);
                PushPlayerStats();
                break;

            case BattleAction.Mend:
                if (_playerMendUsesLeft <= 0) return;
                _playerHP = Mathf.Min(config.playerMaxHP, _playerHP + Mathf.Max(0, config.mendHeal));
                _playerMendUsesLeft--;
                PushPlayerStats();
                break;

            case BattleAction.ApplyWeak:
                _status.ApplyWeakToEnemy(2);   // stackable; tune later
                break;

            case BattleAction.ApplyVulnerable:
                _status.ApplyVulnerableToEnemy(2);
                break;
        }
    }
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

    private void EndPlayerTurn()
    {
        // Victory() is handled earlier; here we simply hand control to the enemy.
        _status.TickEndOfPlayerTurn();  // decrement player status durations
        _playerTurn = false;

        _deck.DiscardHand();          // dump any unplayed cards so the enemy can't see them

        StartCoroutine(EnemyTurn()); // async to allow small pauses/FX
    }

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
                // Enemy heals itself; magnitude stored in config
                _enemyHP = Mathf.Min(config.enemyMaxHP, _enemyHP + config.enemyLeechHeal);
                break;
        }

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
        _nextEnemyIntent = _enemy.DecideNextIntent(config);
        OnEnemyIntentChanged?.Invoke(_nextEnemyIntent);

        // 6) Tick statuses and return control to player
        _status.TickEndOfEnemyTurn();
        BeginPlayerTurn();
    }

    // --- Endings ---

    private async void Victory()
    {
        // Await the reward calculation so the save operation completes before returning to UI.
        int reward = await _rewards.GrantVictoryReward(config);

        OnInfoChanged?.Invoke($"Victory! +{reward} Essence");
        OnBattleEnded?.Invoke(true, reward);

        StartCoroutine(ReturnToStartAfterDelay());
    }

    private void Defeat()
    {
        OnInfoChanged?.Invoke("Defeat! Returning to tavern...");
        OnBattleEnded?.Invoke(false, 0);

        // NEW: apply the loss penalty (immediate essence loss + next-day click debuff)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyDungeonLossPenalty();
            // (Optional) If you didn’t mark attempt on enter for any reason:
            GameManager.Instance.MarkDungeonAttempted();
        }

        StartCoroutine(ReturnToStartAfterDelay());
    }

    private IEnumerator ReturnToStartAfterDelay()
    {
        yield return new WaitForSeconds(config.returnDelay);
        SceneManager.LoadScene("Start"); // make sure "Start" is in Build Settings
    }

    // --- Helpers to notify UI ---

    private void PushPlayerStats() => OnPlayerStatsChanged?.Invoke(_playerHP, config.playerMaxHP, _playerArmor);
    private void PushEnemyStats()  => OnEnemyStatsChanged?.Invoke(_enemyHP, config.enemyMaxHP);
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

