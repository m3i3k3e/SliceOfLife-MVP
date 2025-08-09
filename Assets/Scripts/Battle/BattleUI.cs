using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle UI glue:
/// - Auto-finds BattleManager (or you can assign it)
/// - Updates labels AND health bars when stats change
/// - Forwards button clicks to the BattleManager
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("Scene References (assign if you want; auto-wire will backfill)")]
    [SerializeField] private BattleManager battle;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private TextMeshProUGUI enemyText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI playerArmorText; // optional, can be left null

    [Header("HP Bars")]
    [SerializeField] private Slider playerHPBar;              // new
    [SerializeField] private Slider enemyHPBar;               // new

    [Header("Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button guardButton;
    [SerializeField] private Button mendButton;

    private void Awake()
    {
        AutoWireBattle();
    }

    private void AutoWireBattle()
    {
        if (battle) return;

        // Look for a BattleManager under the same prefab root first
        var root = transform.root;
        battle = root.GetComponentInChildren<BattleManager>(true);

#if UNITY_600_OR_NEWER
        if (!battle) battle = FindAnyObjectByType<BattleManager>(FindObjectsInactive.Include);
#else
        if (!battle) battle = FindFirstObjectByType<BattleManager>();
#endif
        if (!battle)
        {
            Debug.LogError("BattleUI: Couldn't find BattleManager. Drag it into the 'battle' field or ensure it exists in the scene.", this);
        }
    }

    private void OnEnable()
    {
        AutoWireBattle();
        if (!battle)
        {
            enabled = false;
            return;
        }

        // Subscribe to manager events so UI updates reactively
        battle.OnPlayerStatsChanged += HandlePlayerStats;
        battle.OnEnemyStatsChanged  += HandleEnemyStats;
        battle.OnInfoChanged        += HandleInfo;
        battle.OnEnemyIntentChanged += HandleIntent;
        battle.OnBattleEnded        += HandleBattleEnded;

        // Hook up button clicks
        attackButton.onClick.AddListener(battle.PlayerAttack);
        guardButton.onClick.AddListener(battle.PlayerGuard);
        mendButton.onClick.AddListener(battle.PlayerMend);
    }

    private void OnDisable()
    {
        if (battle)
        {
            battle.OnPlayerStatsChanged -= HandlePlayerStats;
            battle.OnEnemyStatsChanged  -= HandleEnemyStats;
            battle.OnInfoChanged        -= HandleInfo;
            battle.OnEnemyIntentChanged -= HandleIntent;
            battle.OnBattleEnded        -= HandleBattleEnded;

            attackButton?.onClick.RemoveListener(battle.PlayerAttack);
            guardButton?.onClick.RemoveListener(battle.PlayerGuard);
            mendButton?.onClick.RemoveListener(battle.PlayerMend);
        }
        else
        {
            attackButton?.onClick.RemoveAllListeners();
            guardButton?.onClick.RemoveAllListeners();
            mendButton?.onClick.RemoveAllListeners();
        }
    }

    // --- Event handlers update texts AND bars ---

    private void HandlePlayerStats(int hp, int maxHp, int armor)
    {
        // Update text
        if (playerText) playerText.text = $"Player HP: {hp}/{maxHp}";
        if (playerArmorText) playerArmorText.text = armor > 0 ? $"Armor: {armor}" : string.Empty;

        // Initialize bar bounds once (safe to set every time)
        if (playerHPBar)
        {
            playerHPBar.minValue = 0;
            playerHPBar.maxValue = maxHp;
            playerHPBar.value    = Mathf.Clamp(hp, 0, maxHp);
        }
    }

    private void HandleEnemyStats(int hp, int maxHp)
    {
        // Update text
        if (enemyText) enemyText.text = $"Enemy HP: {hp}/{maxHp}";

        // Update bar
        if (enemyHPBar)
        {
            enemyHPBar.minValue = 0;
            enemyHPBar.maxValue = maxHp;
            enemyHPBar.value    = Mathf.Clamp(hp, 0, maxHp);
        }
    }

    private void HandleInfo(string msg)
    {
        if (infoText) infoText.text = msg;
    }

    private void HandleIntent(EnemyIntent intent)
    {
        // Append preview as a second line under the enemy text
        if (enemyText) enemyText.text += $"\nNext: {intent.label}";
    }

    private void HandleBattleEnded(bool victory, int reward)
    {
        // Prevent further clicks during the return delay
        if (attackButton) attackButton.interactable = false;
        if (guardButton)  guardButton.interactable  = false;
        if (mendButton)   mendButton.interactable   = false;
    }
}
