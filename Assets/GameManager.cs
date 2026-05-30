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

    [Header("コンボ設定（DESIGN.md 5.8）")]
    [SerializeField] private float comboTimeout   = 6.0f;  // 最後の破壊からこの秒数でリセット
    [SerializeField] private int   comboScoreStep = 5;     // 何コンボ毎にスコア +10% するか
    [SerializeField] private int   comboGaugeStep = 5;     // 何コンボ毎にゲージ +5% するか
    [SerializeField] private int   comboItemStep  = 10;    // 何コンボ毎にドロップ +10% するか
    [SerializeField] private int[] comboMilestones = { 10, 20, 30 };  // 演出を出すコンボ数

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
    private int   p1Combo, p2Combo;
    private float p1ComboTimer, p2ComboTimer;  // 最後のブロック接触からの経過秒（combo>0 のとき加算）

    // アクティブ効果リスト（複数同時効果を追跡。HUD は当面 GetActiveItemName で最新1個のみ表示）
    public struct ActiveEffect
    {
        public ItemEffectSlot slot;
        public string         name;
        public float          endTime;
    }
    private readonly System.Collections.Generic.List<ActiveEffect> p1Active = new();
    private readonly System.Collections.Generic.List<ActiveEffect> p2Active = new();

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

    void Update()
    {
        if (currentState != GameState.Playing) return;
        TickComboTimer(ref p1Combo, ref p1ComboTimer);
        TickComboTimer(ref p2Combo, ref p2ComboTimer);
    }

    // 最後のブロック接触から comboTimeout 経過でコンボを 0 にする（DESIGN.md 5.8）
    private void TickComboTimer(ref int combo, ref float timer)
    {
        if (combo <= 0) return;
        timer += Time.deltaTime;
        if (timer >= comboTimeout) combo = 0;
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
        p1Combo = 0; p1ComboTimer = 0f;
        p2Combo = 0; p2ComboTimer = 0f;

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

        ClearActiveItems();
        if (arena1 != null) arena1.ResetForNewRound();
        if (arena2 != null) arena2.ResetForNewRound();
    }

    public void StartRematch() => StartSkillSelect(isNewMatch: true);

    public void StartNextRound()
    {
        p1HP.Reset();
        p2HP.Reset();
        p1Combo = 0; p1ComboTimer = 0f;
        p2Combo = 0; p2ComboTimer = 0f;

        arena1?.GetSkillController()?.ResetEnergy();
        arena2?.GetSkillController()?.ResetEnergy();

        ClearActiveItems();
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
        // ボール落下でコンボリセット（DESIGN.md 5.8 / 12.7）
        if (playerIndex == 1) { p1Combo = 0; p1ComboTimer = 0f; }
        else                  { p2Combo = 0; p2ComboTimer = 0f; }
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

    // スコア = baseScore × HP帯 scoreMul × コンボ scoreComboMul（いずれも乗算, DESIGN.md 5.8）
    // コンボは接触時 (RegisterBallHitBlock) に加算済みなので AddScore は更新後コンボで計算される
    public void AddScore(int playerIndex, int amount)
    {
        float mul    = GetCurrentBand(playerIndex).scoreMul * ScoreComboMul(playerIndex);
        int   gained = Mathf.RoundToInt(amount * mul);
        if (playerIndex == 1) p1Score += gained;
        else                  p2Score += gained;
    }

    // ボールがブロックに接触するたびに呼ばれる（破壊有無を問わない, DESIGN.md 5.8）。
    // コンボ加算 + タイマーリセット + マイルストーン演出を担う。
    public void RegisterBallHitBlock(int playerIndex)
    {
        if (currentState != GameState.Playing) return;

        // コンボ加算 + タイマーリセット（最後の接触起点で計測）
        int combo;
        if (playerIndex == 1) { combo = ++p1Combo; p1ComboTimer = 0f; }
        else                  { combo = ++p2Combo; p2ComboTimer = 0f; }

        // コンボマイルストーン演出（丁度その値に達した瞬間のみ。リセット後に再到達で再発火, DESIGN.md 12.10）
        if (System.Array.IndexOf(comboMilestones, combo) >= 0)
            GetArena(playerIndex)?.ShowComboMilestone(combo);
    }

    // ブロック破壊時にエナジー蓄積。コンボ加算は接触側 (RegisterBallHitBlock) に移譲。妨害送付はしない
    // （コンボ自動妨害は 2026-05-20 仕様刷新で撤廃。妨害は SendInterference 経由のみ）
    public void RegisterBlockDestroyed(int playerIndex)
    {
        if (currentState != GameState.Playing) return;

        // エナジー = energyPerBlock × HP帯 gaugeRateMul × コンボ gaugeComboMul
        float rateMul = GetCurrentBand(playerIndex).gaugeRateMul * GaugeComboMul(playerIndex);
        GetArena(playerIndex)?.GetSkillController()?.AddEnergy(energyPerBlock * rateMul);
    }

    // コンボ自己強化倍率（DESIGN.md 5.8、HP帯倍率と乗算される）
    // step は Mathf.Max(1, ...) で 0 除算（int 例外）を防ぐ
    private float ScoreComboMul(int playerIndex)    // 5 毎 +10%、上限 +100%
        => 1f + Mathf.Min(1.0f, 0.10f * (GetCombo(playerIndex) / Mathf.Max(1, comboScoreStep)));
    private float GaugeComboMul(int playerIndex)    // 5 毎 +5%、上限 +50%
        => 1f + Mathf.Min(0.5f, 0.05f * (GetCombo(playerIndex) / Mathf.Max(1, comboGaugeStep)));
    public  float GetItemDropComboMul(int playerIndex) // 10 毎 +10%、上限 +50%
        => 1f + Mathf.Min(0.5f, 0.10f * (GetCombo(playerIndex) / Mathf.Max(1, comboItemStep)));

    // 攻撃アイテム取得時の妨害送付窓口 (DESIGN.md 5.5.2 / 7.4)
    // EffectAttack.Apply から呼ばれる。targetPlayerIndex は受信側 (= 取得者の相手)
    public void SendInterference(int targetPlayerIndex, ItemType attackItem)
    {
        ArenaController target = GetArena(targetPlayerIndex);
        if (target == null) return;

        InterferenceType type  = AttackItemToInterference(attackItem);
        string           label = GetInterferenceLabel(type);
        ApplyInterference(target, type);
        target.TriggerHitStop(interferenceTriggerFrames);
        target.ShowInterferenceOverlay(label);
        target.PushIncoming(type);

        // 攻撃者（受信側の相手）の HUD に SENT → 表示
        int attackerIndex = targetPlayerIndex == 1 ? 2 : 1;
        GetArena(attackerIndex)?.ShowSentLabel($"P{targetPlayerIndex}: {label}");

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
    // アクティブ効果（複数同時。HUD は当面最新1個のみ表示）
    // =====================================================

    private System.Collections.Generic.List<ActiveEffect> ActiveList(int playerIndex)
        => playerIndex == 1 ? p1Active : p2Active;

    // 期限切れエントリを除去
    private void PruneActive(System.Collections.Generic.List<ActiveEffect> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i].endTime <= Time.time) list.RemoveAt(i);
    }

    // ItemDrop が効果適用と同時に呼ぶ。duration=0 のアイテム（Heal/Attack）は登録しない。
    // 同一スロットの効果は上書き（BallScript/PlayerController のコルーチン上書きと整合）。
    public void RegisterActiveItem(int playerIndex, ItemEffectSlot slot, string itemName, float duration)
    {
        if (duration <= 0f || slot == ItemEffectSlot.None) return;
        var list = ActiveList(playerIndex);
        PruneActive(list);

        var effect = new ActiveEffect { slot = slot, name = itemName, endTime = Time.time + duration };
        int idx = list.FindIndex(e => e.slot == slot);
        if (idx >= 0) list[idx] = effect;   // 同スロット上書き
        else          list.Add(effect);
    }

    // ラウンド遷移時にアクティブ効果表示をリセット（効果コルーチンはボール/パドル側で別途解除済み）
    public void ClearActiveItems()
    {
        p1Active.Clear();
        p2Active.Clear();
    }

    // 指定スロットの効果が現在有効か（ドロップ重複抑制用）
    public bool IsEffectSlotActive(int playerIndex, ItemEffectSlot slot)
    {
        if (slot == ItemEffectSlot.None) return false;
        var list = ActiveList(playerIndex);
        PruneActive(list);
        return list.Exists(e => e.slot == slot);
    }

    // 現在有効な効果のスナップショット（将来の HUD 複数表示用）。残り時間降順ではなく登録順。
    public System.Collections.Generic.List<ActiveEffect> GetActiveEffects(int playerIndex)
    {
        var list = ActiveList(playerIndex);
        PruneActive(list);
        return list;
    }

    // 最新（最後に登録された）有効効果の残り時間。なければ 0。
    public float GetActiveItemRemaining(int playerIndex)
    {
        var list = ActiveList(playerIndex);
        PruneActive(list);
        return list.Count > 0 ? Mathf.Max(0f, list[list.Count - 1].endTime - Time.time) : 0f;
    }

    // 最新の有効効果名。なければ null（UI 側で表示/非表示を判定）
    public string GetActiveItemName(int playerIndex)
    {
        var list = ActiveList(playerIndex);
        PruneActive(list);
        return list.Count > 0 ? list[list.Count - 1].name : null;
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
