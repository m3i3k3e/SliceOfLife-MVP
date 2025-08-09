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

    /// <summary>
    /// Single entry point for playing a card. UI (2D/3D) should call this,
    /// not the individual PlayerAttack/Guard/Mend, so we can swap implementations later.
    /// </summary>
    public void PlayCard(CardSO card)
    {
        if (card == null || !_playerTurn) return;

        PlayAction(card.action);
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
        OnInfoChanged?.Invoke("Your turn");

        _playerTurn = true;
    }

    // --- Public API called by UI buttons ---

    /// <summary>Attack does flat damage to the enemy.</summary>
    public void PlayerAttack()
    {
        if (!_playerTurn) return;
        _enemyHP -= Mathf.Max(0, config.attackDamage);
        PushEnemyStats();

        EndPlayerTurn();
    }

    /// <summary>Guard grants temporary armor. Armor is consumed by the next enemy hit (one or multiple hits, up to its amount).</summary>
    public void PlayerGuard()
    {
        if (!_playerTurn) return;
        _playerArmor += Mathf.Max(0, config.guardBlock);
        PushPlayerStats();

        EndPlayerTurn();
    }

    /// <summary>Mend heals HP but has limited uses (defaults to 1 for MVP).</summary>
    public void PlayerMend()
    {
        if (!_playerTurn) return;
        if (_playerMendUsesLeft <= 0) return;

        _playerHP = Mathf.Min(config.playerMaxHP, _playerHP + Mathf.Max(0, config.mendHeal));
        _playerMendUsesLeft--;
        PushPlayerStats();

        EndPlayerTurn();
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
        StartCoroutine(EnemyTurn());
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
                int dmg = _nextEnemyIntent.magnitude;
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

        _playerTurn = true;
        OnInfoChanged?.Invoke("Your turn");
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
