using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle UI glue.
///
/// Wiring guidelines:
/// - Place this component on the <see cref="BattleManager"/> GameObject or assign the field manually.
/// - If left unassigned, the script will walk up the hierarchy to locate a <see cref="BattleManager"/>.
/// - No global FindAnyObjectByType fallback; missing wiring will log an error in play mode.
///
/// Responsibilities:
/// - Updates labels AND health bars when stats change.
/// - Forwards button clicks to the BattleManager.
/// </summary>
[RequireComponent(typeof(BattleManager))]
public class BattleUI : MonoBehaviour
{
    [Header("Scene References (drag BattleManager here or keep default to auto-find parent)")]
    [SerializeField] private BattleManager battle; // Serialized for manual wiring; defaults to same object/parent

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI playerText;
    [SerializeField] private TextMeshProUGUI enemyText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI playerArmorText; // optional, can be left null

    [Header("Status Labels")]
    [SerializeField] private TextMeshProUGUI playerStatusText;
    [SerializeField] private TextMeshProUGUI enemyStatusText;


    [Header("HP Bars")]
    [SerializeField] private Slider playerHPBar;              // new
    [SerializeField] private Slider enemyHPBar;               // new

    [Header("Buttons")]
    [SerializeField] private UnityEngine.UI.Button endTurnButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button guardButton;
    [SerializeField] private Button mendButton;

    [Header("Energy")]
    [SerializeField] private TextMeshProUGUI energyText; // new label "Energy: X/Y"
    [SerializeField] private CardHandUI handUI;           // to refresh affordability



    private void Awake()
    {
        AutoWireBattle();
    }

    /// <summary>
    /// Attempts to resolve the <see cref="BattleManager"/> reference if none was wired in the Inspector.
    /// We intentionally avoid expensive global searches (FindObjectByType) and only look on the
    /// current GameObject or its parents. This keeps the hierarchy explicit and predictable.
    /// </summary>
    private void AutoWireBattle()
    {
        if (battle) return; // Field already assigned via Inspector

        // First, check the same GameObject. RequireComponent ensures this succeeds
        // when BattleUI lives on the BattleManager root.
        battle = GetComponent<BattleManager>();

        // If the script is placed on a child object, walk up the hierarchy.
        if (!battle)
        {
            battle = GetComponentInParent<BattleManager>();
        }

        // Still missing? Developer forgot to wire it.
        if (!battle)
        {
            Debug.LogError(
                "BattleUI: BattleManager reference missing. " +
                "Assign the 'battle' field or place this component under a BattleManager.",
                this);
        }
    }

    private void OnEnable()
    {
        AutoWireBattle();
        if (!battle) { enabled = false; return; }

        battle.OnPlayerStatsChanged += HandlePlayerStats;
        battle.OnEnemyStatsChanged  += HandleEnemyStats;
        battle.OnInfoChanged        += HandleInfo;
        battle.OnEnemyIntentChanged += HandleIntent;
        battle.OnBattleEnded        += HandleBattleEnded;
        battle.OnPlayerStatusChanged += HandlePlayerStatus;
        battle.OnEnemyStatusChanged  += HandleEnemyStatus;
        battle.OnEnergyChanged      += HandleEnergyChanged;
        battle.OnHandChanged        += HandleHandChanged;


        // Keep legacy buttons optional (only wire if assigned)
        if (endTurnButton) endTurnButton.onClick.AddListener(battle.EndTurn);
        if (attackButton) attackButton.onClick.AddListener(battle.PlayerAttack);
        if (guardButton)  guardButton.onClick.AddListener(battle.PlayerGuard);
        if (mendButton)   mendButton.onClick.AddListener(battle.PlayerMend);
    }

    private void OnDisable()
    {
        if (battle)
        {
            battle.OnPlayerStatsChanged -= HandlePlayerStats;
            battle.OnEnemyStatsChanged -= HandleEnemyStats;
            battle.OnInfoChanged -= HandleInfo;
            battle.OnEnemyIntentChanged -= HandleIntent;
            battle.OnBattleEnded -= HandleBattleEnded;
            battle.OnPlayerStatusChanged -= HandlePlayerStatus;
            battle.OnEnemyStatusChanged -= HandleEnemyStatus;
            battle.OnEnergyChanged -= HandleEnergyChanged;
            battle.OnHandChanged -= HandleHandChanged;

        }

        if (endTurnButton) endTurnButton.onClick.RemoveListener(battle.EndTurn);
        if (attackButton) attackButton.onClick.RemoveAllListeners();
        if (guardButton)  guardButton.onClick.RemoveAllListeners();
        if (mendButton)   mendButton.onClick.RemoveAllListeners();
    }
    private void HandleHandChanged(System.Collections.Generic.IReadOnlyList<CardSO> cards)
    {
        if (handUI) handUI.PopulateHand(cards);
        // Re-apply affordability using the last-known energy
        // BattleManager will also fire OnEnergyChanged in BeginPlayerTurn, but this keeps mid-turn removals tidy.
        // If you want this to be exact, you could cache current energy here; for now EnergyChanged will follow shortly.
    }

    private void HandleEnergyChanged(int current, int max)
    {
        if (energyText) energyText.text = $"Energy: {current}/{max}";
        if (handUI)     handUI.RefreshAffordability(current);
    }

    // --- Event handlers update texts AND bars ---
    private void HandlePlayerStatus(string s)
    {
        if (playerStatusText) playerStatusText.text = s;
    }

    private void HandleEnemyStatus(string s)
    {
        if (enemyStatusText) enemyStatusText.text = s;
    }

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
