using System.Collections;
using UnityEngine;

// HP帯別パラメータ（HP割合に応じて動的に変わるゲームパラメータ）
// hpStateBands 配列は Inspector で thresholdPercent 降順に設定すること
[System.Serializable]
public class HPStateBand
{
    [Range(0f, 1f)]
    public float thresholdPercent = 1.0f;  // この値以上のHP割合で適用

    public float gaugeRateMul = 1.0f;      // スキルゲージ蓄積倍率
    public float itemDropMul  = 1.0f;      // アイテムドロップ率倍率
    public float scoreMul     = 1.0f;      // スコア倍率
    [Range(0f, 1f)]
    public float goodItemBias = 0f;        // 有利アイテムへの偏重（0=均等、1=有利のみ）
    public bool  panicMode    = false;     // ピンチ専用スキル解禁などのフラグ
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("試合設定")]
    [SerializeField] private int   roundsToWin    = 1;
    [SerializeField] private float nextRoundDelay = 2f;

    [Header("HP設定")]
    [SerializeField] private int maxHP                  = 500;
    [SerializeField] private int damageBallDrop         = 5;
    [SerializeField] private int damageBlockReachBottom = 10;
    [SerializeField] private int damagePoisonPerSec     = 5;

    [Header("スキル・エナジー設定")]
    [SerializeField] private float energyPerBlock = 1f;

    [Header("強制リスポーン設定")]

    [Header("ヒットストップ設定（フレーム数）")]
    [SerializeField] private int interferenceTriggerFrames = 10;
    [SerializeField] private int roundEndFrames            = 30;
    [SerializeField] private int matchEndFrames            = 60;

    [Header("HP帯別パラメータ（thresholdPercent 降順で設定）")]
    [SerializeField] private HPStateBand[] hpStateBands;

    [Header("アリーナ参照")]
    [SerializeField] private ArenaController arena1;
    [SerializeField] private ArenaController arena2;

    private HPSystem p1HP;
    private HPSystem p2HP;

    private int p1Score, p2Score;
    private int p1RoundWins, p2RoundWins;
    private int p1Combo, p2Combo;

    // アクティブアイテム表示用（最後に取得した1個のみ、コルーチン上書きと整合）
    private string p1ActiveItemName, p2ActiveItemName;
    private float  p1ActiveItemEnd,  p2ActiveItemEnd;

    public enum GameState
    {
        WaitingToStart,
        SkillSelect,
        Playing,
        RoundOver,
        MatchOver
    }

    public enum InterferenceType { AddRow, Harden, Poison, Slow }
    private GameState currentState = GameState.WaitingToStart;

    // =====================================================
    // Unity ライフサイクル
    // =====================================================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        p1HP = new HPSystem(maxHP);
        p2HP = new HPSystem(maxHP);
    }

    void Start()
    {
        StartSkillSelect(isNewMatch: true);
    }

    // =====================================================
    // 試合・ラウンド制御
    // =====================================================

    private void StartSkillSelect(bool isNewMatch)
    {
        if (isNewMatch)
        {
            p1RoundWins = 0;
            p2RoundWins = 0;
            p1Score     = 0;
            p2Score     = 0;
        }
        p1Combo = 0;
        p2Combo = 0;

        p1HP.SetMaxHP(maxHP, refill: true);
        p2HP.SetMaxHP(maxHP, refill: true);

        arena1?.GetSkillController()?.ResetEnergy();
        arena2?.GetSkillController()?.ResetEnergy();

        Time.timeScale = 0f;
        currentState = GameState.SkillSelect;
    }

    public void BeginMatch()
    {
        if (currentState != GameState.SkillSelect) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;

        if (arena1 != null) arena1.ResetForNewRound();
        if (arena2 != null) arena2.ResetForNewRound();
    }

    public void StartRematch() => StartSkillSelect(isNewMatch: true);

    public void StartNextRound()
    {
        p1HP.Reset();
        p2HP.Reset();
        p1Combo = 0;
        p2Combo = 0;

        arena1?.GetSkillController()?.ResetEnergy();
        arena2?.GetSkillController()?.ResetEnergy();

        if (arena1 != null) arena1.ResetForNewRound();
        if (arena2 != null) arena2.ResetForNewRound();

        Time.timeScale = 1f;
        currentState = GameState.Playing;
    }

    // =====================================================
    // ゲーム中のイベント
    // =====================================================

    public void OnBallDropped(int playerIndex)
    {
        if (currentState != GameState.Playing) return;
        ApplyDamage(playerIndex, damageBallDrop);
    }

    public void OnBlocksReachedBottom(int playerIndex, int count = 1)
    {
        if (currentState != GameState.Playing) return;
        ApplyDamage(playerIndex, damageBlockReachBottom * count);
    }

    public void OnPoisonTick(int playerIndex, float deltaTime)
    {
        if (currentState != GameState.Playing) return;
        int dmg = Mathf.RoundToInt(damagePoisonPerSec * deltaTime);
        if (dmg > 0) ApplyDamage(playerIndex, dmg);
    }

    private void ApplyDamage(int playerIndex, int amount)
    {
        HPSystem hp = playerIndex == 1 ? p1HP : p2HP;
        hp.TakeDamage(amount);
        if (!hp.IsAlive)
            EndRound(winner: playerIndex == 1 ? 2 : 1);
    }

    public void AddScore(int playerIndex, int amount)
    {
        float mul    = GetCurrentBand(playerIndex).scoreMul;
        int   gained = Mathf.RoundToInt(amount * mul);
        if (playerIndex == 1) p1Score += gained;
        else                  p2Score += gained;
    }

    // ブロック破壊時にコンボ加算 + エナジー蓄積。妨害送付はしない
    // （コンボ自動妨害は 2026-05-20 仕様刷新で撤廃。妨害は SendInterference 経由のみ）
    public void RegisterBlockDestroyed(int playerIndex)
    {
        if (currentState != GameState.Playing) return;

        float rateMul = GetCurrentBand(playerIndex).gaugeRateMul;
        GetArena(playerIndex)?.GetSkillController()?.AddEnergy(energyPerBlock * rateMul);

        if (playerIndex == 1) p1Combo++;
        else                  p2Combo++;
    }

    // 攻撃アイテム取得時の妨害送付窓口 (DESIGN.md 5.5.2 / 7.4)
    // EffectAttack.Apply から呼ばれる。targetPlayerIndex は受信側 (= 取得者の相手)
    public void SendInterference(int targetPlayerIndex, ItemType attackItem)
    {
        ArenaController target = GetArena(targetPlayerIndex);
        if (target == null) return;

        InterferenceType type = AttackItemToInterference(attackItem);
        ApplyInterference(target, type);
        target.TriggerHitStop(interferenceTriggerFrames);
        target.ShowInterferenceOverlay(GetInterferenceLabel(type));

        Debug.Log($"P{targetPlayerIndex} に妨害送信: {type} (attack={attackItem})");
    }

    private static InterferenceType AttackItemToInterference(ItemType item) => item switch
    {
        ItemType.AttackHarden => InterferenceType.Harden,
        ItemType.AttackAddRow => InterferenceType.AddRow,
        ItemType.AttackPoison => InterferenceType.Poison,
        ItemType.AttackSlow   => InterferenceType.Slow,
        _                     => InterferenceType.AddRow
    };

    private void ApplyInterference(ArenaController target, InterferenceType type)
    {
        switch (type)
        {
            case InterferenceType.AddRow:
                target.GetSpawner()?.ReceiveSabotageRow();
                break;
            case InterferenceType.Harden:
                target.HardenBlocks();
                break;
            case InterferenceType.Poison:
                target.SpawnZonePoison(target.GetRandomFloorWorldPos());
                break;
            case InterferenceType.Slow:
                target.SpawnZoneSlow(target.GetRandomFloorWorldPos());
                break;
        }
    }

    private static string GetInterferenceLabel(InterferenceType type) => type switch
    {
        InterferenceType.AddRow  => "妨害行",
        InterferenceType.Harden  => "ブロック硬化",
        InterferenceType.Poison  => "毒エリア",
        InterferenceType.Slow    => "速度低下",
        _ => type.ToString()
    };

    // =====================================================
    // ラウンド・試合終了
    // =====================================================

    private void EndRound(int winner)
    {
        if (winner == 1) p1RoundWins++;
        else             p2RoundWins++;

        Debug.Log($"ラウンド終了！勝者: P{winner}（P1: {p1RoundWins} / P2: {p2RoundWins}）");

        if (p1RoundWins >= roundsToWin || p2RoundWins >= roundsToWin)
        {
            currentState = GameState.MatchOver;
            // 勝者はフリーズのみ、敗者にのみカメラシェイクを適用する
            ArenaController matchWinnerArena = winner == 1 ? arena1 : arena2;
            ArenaController matchLoserArena  = winner == 1 ? arena2 : arena1;
            matchLoserArena?.TriggerHitStop(matchEndFrames,  strong: true, shake: true);
            matchWinnerArena?.TriggerHitStop(matchEndFrames, strong: true, shake: false);
            StartCoroutine(MatchOverCoroutine(matchEndFrames));
            Debug.Log($"試合終了！勝者: P{winner}");
        }
        else
        {
            currentState = GameState.RoundOver;
            ArenaController loserArena  = winner == 1 ? arena2 : arena1;
            ArenaController winnerArena = winner == 1 ? arena1 : arena2;
            loserArena?.TriggerHitStop(roundEndFrames,  strong: true,  shake: true);
            winnerArena?.TriggerHitStop(roundEndFrames, strong: false, shake: false);
            StartCoroutine(NextRoundCoroutine());
        }
    }

    private IEnumerator MatchOverCoroutine(int hitStopFrames)
    {
        yield return new WaitForSecondsRealtime(hitStopFrames / 60f);
        Time.timeScale = 0f;
    }

    private IEnumerator NextRoundCoroutine()
    {
        yield return new WaitForSecondsRealtime(nextRoundDelay);
        StartNextRound();
    }

    // =====================================================
    // 外部からの情報取得（UIなどが使う）
    // =====================================================

    public ArenaController GetArena(int playerIndex)        => playerIndex == 1 ? arena1 : arena2;
    public float  GetEnergyRatio(int playerIndex)           => GetArena(playerIndex)?.GetSkillController()?.EnergyRatio ?? 0f;
    public string GetEquippedSkillName(int playerIndex)     => GetArena(playerIndex)?.GetSkillController()?.SkillName    ?? "---";

    public void Heal(int playerIndex, int amount)
    {
        if (currentState != GameState.Playing) return;
        HPSystem hp = playerIndex == 1 ? p1HP : p2HP;
        hp.Heal(amount);
    }

    public int   GetHP(int playerIndex)        => playerIndex == 1 ? p1HP.CurrentHP : p2HP.CurrentHP;
    public int   GetMaxHP(int playerIndex)     => playerIndex == 1 ? p1HP.MaxHP     : p2HP.MaxHP;
    public float GetHPRatio(int playerIndex)   => playerIndex == 1 ? p1HP.Ratio     : p2HP.Ratio;
    public int   GetScore(int playerIndex)     => playerIndex == 1 ? p1Score        : p2Score;
    public int   GetRoundWins(int playerIndex) => playerIndex == 1 ? p1RoundWins    : p2RoundWins;
    public int   GetCombo(int playerIndex)     => playerIndex == 1 ? p1Combo : p2Combo;
    public GameState GetCurrentState()         => currentState;

    // =====================================================
    // アクティブアイテム（最後に取った1個だけ表示）
    // =====================================================

    // ItemDrop が効果適用と同時に呼ぶ。duration=0 のアイテム（Heal 等）は表示しない
    public void RegisterActiveItem(int playerIndex, string itemName, float duration)
    {
        if (duration <= 0f) return;
        float end = Time.time + duration;
        if (playerIndex == 1) { p1ActiveItemName = itemName; p1ActiveItemEnd = end; }
        else                  { p2ActiveItemName = itemName; p2ActiveItemEnd = end; }
    }

    public float GetActiveItemRemaining(int playerIndex)
    {
        float end = playerIndex == 1 ? p1ActiveItemEnd : p2ActiveItemEnd;
        return Mathf.Max(0f, end - Time.time);
    }

    // 残り 0 のときは null を返す（UI 側で表示/非表示を判定）
    public string GetActiveItemName(int playerIndex)
    {
        return GetActiveItemRemaining(playerIndex) > 0f
             ? (playerIndex == 1 ? p1ActiveItemName : p2ActiveItemName)
             : null;
    }

    // 現在のHP割合に応じた HPStateBand を返す（配列が空なら等倍のデフォルトを返す）
    public HPStateBand GetCurrentBand(int playerIndex)
    {
        if (hpStateBands == null || hpStateBands.Length == 0) return new HPStateBand();
        float ratio = GetHPRatio(playerIndex);
        for (int i = 0; i < hpStateBands.Length; i++)
        {
            if (ratio >= hpStateBands[i].thresholdPercent)
                return hpStateBands[i];
        }
        return hpStateBands[hpStateBands.Length - 1];
    }
}
