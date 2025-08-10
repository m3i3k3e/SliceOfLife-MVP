using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal, readable turn loop:
/// PlayerTurn -> EnemyTurn -> check win/lose -> back to PlayerTurn.
/// UI is driven via events; buttons call the public Player* methods.
/// </summary>
public class BattleManager : MonoBehaviour
{
    // --- Events so UI can stay dumb and reactive ---
    public event Action<int, int, int> OnPlayerStatsChanged; // (hp, maxHp, armor)
    public event Action<int, int> OnEnemyStatsChanged;       // (hp, maxHp)
    public event Action<string> OnInfoChanged;               // "Your turn", "Enemy intends X", etc.
    public event Action<EnemyIntent> OnEnemyIntentChanged;   // next enemy action preview
    public event Action<bool, int> OnBattleEnded;            // (victory?, rewardEssence)

    public event Action<string> OnPlayerStatusChanged; // e.g., "Weak (1), Vulnerable (2)"
    public event Action<string> OnEnemyStatusChanged;
    
    // Public so UI can end the turn early.
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
    var hand = _deck.Hand;
    for (int i = 0; i < hand.Count; i++)
    {
        var c = hand[i];
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


    // --- Energy ---
    public event Action<int, int> OnEnergyChanged; // (current, max)

    public void PlayCard(CardSO card)
    {
        if (card == null || !_playerTurn) return;

        // Card must exist in hand (prevents clicking stale UI)
        if (!_deck.Contains(card)) return;

        // Spend energy first so UI disables unaffordable cards immediately
        if (!_energy.TrySpend(Mathf.Max(0, card.cost))) return;

        // Apply effect
        ApplyActionEffect(card.action);

        // Move the card to discard (all cards are one-use per turn in this MVP)
        _deck.RemoveFromHand(card);

        // Win/flow
        PostPlayerCardResolved(); // will end turn if energy hit 0
    }


    /// <summary>Apply the effect of an action without spending energy or ending the turn.</summary>
    private void ApplyActionEffect(BattleAction action)
    {
        switch (action)
        {
            case BattleAction.Attack:
                _enemyHP -= _status.ModifyPlayerToEnemy(Mathf.Max(0, config.attackDamage));
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
                _status.ApplyToEnemy(StatusType.Weak, 2);
                break;

            case BattleAction.ApplyVulnerable:
                _status.ApplyToEnemy(StatusType.Vulnerable, 2);
                break;
        }
    }
    /// <summary>Try to spend energy; update UI if successful.</summary>
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

    /// <summary>
    /// Maps a high-level action to the existing specific methods.
    /// Keeps the manager decoupled from UI implementations.
    /// </summary>

    public void PlayAction(BattleAction action)
    {
        if (!_playerTurn) return;

        switch (action)
        {
            case BattleAction.Attack: PlayerAttack(); break;
            case BattleAction.Guard: PlayerGuard(); break;
            case BattleAction.Mend: PlayerMend(); break;
            default: return;
        }
    }

    [Header("Config (assign in Inspector)")]
    [SerializeField] private BattleConfigSO config;

    // --- Runtime state ---
    private int _playerHP;
    private int _playerArmor;
    private int _playerMendUsesLeft;

    private int _enemyHP;

    private EnemyIntent _nextEnemyIntent;

    private bool _playerTurn; // simple flag for whose turn it is

    // --- Cards/Deck ---
    [Tooltip("Defines the full deck for this battle (duplicates allowed). Shuffled at start.")]
    [SerializeField] private System.Collections.Generic.List<CardSO> startingDeck = new();

    // Subsystems extracted for clarity and future reuse.
    private IDeckManager _deck;
    private IEnergySystem _energy;
    private IStatusController _status;
    private BattleRewardService _rewardService;

    /// <summary>UI subscribes to rebuild the hand visuals whenever it changes.</summary>
    public event System.Action<System.Collections.Generic.IReadOnlyList<CardSO>> OnHandChanged;


    // Cheap link to our tiny enemy AI
    private EnemyAI _enemy;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("BattleManager: Missing BattleConfigSO. Assign one in the Inspector.");
        }
        _enemy = GetComponent<EnemyAI>();
        if (_enemy == null) _enemy = gameObject.AddComponent<EnemyAI>(); // safe default

        // Instantiate subsystems. They are lightweight classes, not MonoBehaviours.
        _deck = new DeckManager();
        _energy = new EnergySystem();
        _status = new StatusController();
        _rewardService = new BattleRewardService(GameManager.Instance != null ? GameManager.Instance.Upgrades : null);

        // Bridge internal events outward so UI stays decoupled.
        _deck.OnHandChanged += h => OnHandChanged?.Invoke(h);
        _energy.OnEnergyChanged += (c, m) => OnEnergyChanged?.Invoke(c, m);
        _status.OnPlayerStatusChanged += s => OnPlayerStatusChanged?.Invoke(s);
        _status.OnEnemyStatusChanged += s => OnEnemyStatusChanged?.Invoke(s);
    }

    /// <summary>Refill energy and hand control to the player.</summary>
    private void BeginPlayerTurn()
    {
        _playerTurn = true;
        _energy.Refill(Mathf.Max(0, config.energyPerTurn));
        _deck.Draw(Mathf.Max(0, config.handSize));
        // After drawing, re-emit energy so new cards grey out properly.
        OnEnergyChanged?.Invoke(_energy.Current, _energy.Max);
        MaybeAutoEndTurn();
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

        int maxEnergy = Mathf.Max(config.maxEnergy, config.energyPerTurn);
        _energy.Initialize(maxEnergy);
        _status.Initialize();
        _deck.Initialize(startingDeck);
        BeginPlayerTurn();

    }

    public void PlayerAttack()
    {
        if (!_playerTurn) return;
        if (!TrySpendEnergy(1)) return;        // <-- NEW (legacy buttons cost 1)
        ApplyActionEffect(BattleAction.Attack);
        PostPlayerCardResolved();               // <-- NEW
    }

    public void PlayerGuard()
    {
        if (!_playerTurn) return;
        if (!TrySpendEnergy(1)) return;        // <-- NEW
        ApplyActionEffect(BattleAction.Guard);
        PostPlayerCardResolved();               // <-- NEW
    }

    public void PlayerMend()
    {
        if (!_playerTurn) return;
        if (!TrySpendEnergy(1)) return;        // <-- NEW (legacy cost)
        ApplyActionEffect(BattleAction.Mend);
        PostPlayerCardResolved();               // <-- NEW
    }

    // --- Turn transitions ---

    private void EndPlayerTurn()
    {
        // If enemy already dead, Victory() handled elsewhere.
        TickEndOfPlayerTurn();  // from statuses step (safe even if 0)
        _playerTurn = false;

        _deck.DiscardHand();          // dump unplayed cards

        StartCoroutine(EnemyTurn());
    }

    private void TickEndOfPlayerTurn() => _status.TickEndOfPlayerTurn();

    private IEnumerator EnemyTurn()
    {
        // Small beat for readability; optional
        OnInfoChanged?.Invoke($"Enemy uses {_nextEnemyIntent.label}...");
        yield return new WaitForSeconds(0.25f);

        // Execute the currently previewed intent
        switch (_nextEnemyIntent.type)
        {
            case EnemyIntentType.LightAttack:
            case EnemyIntentType.HeavyAttack:
                int dmg = _status.ModifyEnemyToPlayer(_nextEnemyIntent.magnitude);
                // Armor reduces damage and is then consumed by the amount absorbed
                int absorbed = Mathf.Min(_playerArmor, dmg);
                int through = dmg - absorbed;
                _playerArmor -= absorbed;
                _playerHP -= through;
                break;

            case EnemyIntentType.LeechHeal:
                _enemyHP = Mathf.Min(config.enemyMaxHP, _enemyHP + config.enemyLeechHeal);
                break;
        }

        // Push updated stats after enemy action
        PushPlayerStats();
        PushEnemyStats();

        // Lose check
        if (_playerHP <= 0)
        {
            Defeat();
            yield break;
        }

        // Roll the next intent for preview
        _nextEnemyIntent = _enemy.DecideNextIntent(config);
        OnEnemyIntentChanged?.Invoke(_nextEnemyIntent);

        TickEndOfEnemyTurn();
        BeginPlayerTurn(); // refills energy + draws new hand
    }

    private void TickEndOfEnemyTurn() => _status.TickEndOfEnemyTurn();

    // --- Endings ---
    
    private void Victory()
    {
        int reward = _rewardService.CalculateReward(config.baseEssenceReward);

        OnInfoChanged?.Invoke($"Victory! +{reward} Essence");
        OnBattleEnded?.Invoke(true, reward);

        if (GameManager.Instance != null && GameManager.Instance.Essence != null)
        {
            GameManager.Instance.Essence.AddExternal(reward);
            SaveSystem.Save(GameManager.Instance); // optional: persist immediately
        }

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

public enum EnemyIntentType { LightAttack, HeavyAttack, LeechHeal }
