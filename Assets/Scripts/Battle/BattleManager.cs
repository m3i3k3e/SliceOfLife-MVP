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

    // --- Statuses (NEW) ---
    // “Turns” count down at the end of that unit's own turn.
    private int _playerWeakTurns, _playerVulnTurns;
    private int _enemyWeakTurns,  _enemyVulnTurns;
    public event Action<string> OnPlayerStatusChanged; // e.g., "Weak (1), Vulnerable (2)"
    public event Action<string> OnEnemyStatusChanged;
    private void PushStatusLabels()
    {
        string P()
        {
            System.Collections.Generic.List<string> parts = new();
            if (_playerWeakTurns > 0) parts.Add($"Weak ({_playerWeakTurns})");
            if (_playerVulnTurns > 0) parts.Add($"Vulnerable ({_playerVulnTurns})");
            return string.Join(", ", parts);
        }
        string E()
        {
            System.Collections.Generic.List<string> parts = new();
            if (_enemyWeakTurns > 0) parts.Add($"Weak ({_enemyWeakTurns})");
            if (_enemyVulnTurns > 0) parts.Add($"Vulnerable ({_enemyVulnTurns})");
            return string.Join(", ", parts);
        }

        OnPlayerStatusChanged?.Invoke(P());
        OnEnemyStatusChanged?.Invoke(E());
    }
    /// <summary>Outgoing damage from the player → reduced by player's Weak, increased by enemy's Vulnerable.</summary>
    private int ModPlayerToEnemyDamage(int baseDmg)
    {
        float d = baseDmg;
        if (_playerWeakTurns > 0)  d *= 0.75f; // -25%
        if (_enemyVulnTurns  > 0)  d *= 1.50f; // +50%
        return Mathf.Max(0, Mathf.RoundToInt(d));
    }

    /// <summary>Outgoing damage from the enemy → reduced by enemy's Weak, increased by player's Vulnerable.</summary>
    private int ModEnemyToPlayerDamage(int baseDmg)
    {
        float d = baseDmg;
        if (_enemyWeakTurns  > 0)  d *= 0.75f;
        if (_playerVulnTurns > 0)  d *= 1.50f;
        return Mathf.Max(0, Mathf.RoundToInt(d));
    }

    // --- Energy (NEW) ---
    public event Action<int, int> OnEnergyChanged; // (current, max)
    private int _currentEnergy;
    private int _maxEnergy;

    public void PlayCard(CardSO card)
    {
        if (card == null || !_playerTurn) return;

        // Spend card cost first so UI can disable newly unaffordable cards
        if (!TrySpendEnergy(Mathf.Max(0, card.cost))) return;

        // Apply the chosen action
        ApplyActionEffect(card.action);

        // Handle win / end-of-turn decisions
        PostPlayerCardResolved();
    }

    /// <summary>Apply the effect of an action without spending energy or ending the turn.</summary>
    private void ApplyActionEffect(BattleAction action)
    {
        switch (action)
        {
            case BattleAction.Attack:
                _enemyHP -= ModPlayerToEnemyDamage(Mathf.Max(0, config.attackDamage));
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
                _enemyWeakTurns += 2;   // stackable; tune later
                PushStatusLabels();
                break;

            case BattleAction.ApplyVulnerable:
                _enemyVulnTurns += 2;
                PushStatusLabels();
                break;
        }
    }
    /// <summary>Try to spend energy; update UI if successful.</summary>
    private bool TrySpendEnergy(int amount)
    {
        if (amount > _currentEnergy)
        {
            OnInfoChanged?.Invoke("Not enough energy");
            return false;
        }
        _currentEnergy -= amount;
        OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
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
        if (_currentEnergy <= 0)
        {
            _playerTurn = false;
            StartCoroutine(EnemyTurn());
        }
        else
        {
            // Nudge UI if you want; EnergyChanged already fired when we spent.
        }
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
    }

    /// <summary>Refill energy and hand control to the player.</summary>
    private void BeginPlayerTurn()
    {
        _playerTurn = true;
        _currentEnergy = Mathf.Min(config.energyPerTurn, _maxEnergy);
        OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
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

        // Init energy and start the turn
        _maxEnergy = Mathf.Max(config.maxEnergy, config.energyPerTurn);
        PushStatusLabels();
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
        // Win check after player's action
        if (_enemyHP <= 0)
        {
            Victory();
            return;
        }

        _playerTurn = false;
        TickEndOfPlayerTurn();
        StartCoroutine(EnemyTurn());
    }
private void TickEndOfPlayerTurn()
    {
        if (_playerWeakTurns  > 0) _playerWeakTurns--;
        if (_playerVulnTurns  > 0) _playerVulnTurns--;
        PushStatusLabels();
    }
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
                int dmg = ModEnemyToPlayerDamage(_nextEnemyIntent.magnitude);
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
        BeginPlayerTurn();
    }
private void TickEndOfEnemyTurn()
    {
        if (_enemyWeakTurns   > 0) _enemyWeakTurns--;
        if (_enemyVulnTurns   > 0) _enemyVulnTurns--;
        PushStatusLabels();
    }
    // --- Endings ---

    private void Victory()
    {
    // 1) Start from the base reward set in your BattleConfig
    int baseReward = Mathf.Max(0, config.baseEssenceReward);

    // 2) Read the current reward multiplier from the Upgrade system.
    //    If anything is missing (e.g., no GameManager or no Upgrades yet), fall back to 1.0x.
    float multiplier = 1f;
    var upgrades = GameManager.Instance != null ? GameManager.Instance.Upgrades : null;
    if (upgrades != null)
        multiplier = Mathf.Max(0f, upgrades.RewardMultiplier);

    // 3) Apply multiplier and round to an int for currency
    int reward = Mathf.RoundToInt(baseReward * multiplier);

    // 4) UI events (inform the player how much they got)
    OnInfoChanged?.Invoke($"Victory! +{reward} Essence");
    OnBattleEnded?.Invoke(true, reward);

    // 5) Grant the reward through the currency system (bypasses click cap)
    if (GameManager.Instance != null && GameManager.Instance.Essence != null)
    {
        GameManager.Instance.Essence.AddExternal(reward);
        SaveSystem.Save(GameManager.Instance); // optional: persist immediately
    }

    // 6) Exit back to Start after a short delay
    StartCoroutine(ReturnToStartAfterDelay());
    }

    private void Defeat()
    {
        OnInfoChanged?.Invoke("Defeat! Returning to tavern...");
        OnBattleEnded?.Invoke(false, 0);
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
