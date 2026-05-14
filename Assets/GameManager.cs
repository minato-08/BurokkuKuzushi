using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singletonパターン：ゲーム中にこのクラスは1つだけ存在する
    public static GameManager Instance { get; private set; }

    [Header("バランス設定")]
    [SerializeField] private GameBalanceProfile profile;

    [Header("試合設定")]
    [SerializeField] private int   roundsToWin    = 1;   // 何本先取で勝利か
    [SerializeField] private float nextRoundDelay = 2f;  // ラウンド終了から次ラウンド開始までの秒数

    [Header("アリーナ参照")]
    [SerializeField] private ArenaController arena1;
    [SerializeField] private ArenaController arena2;

    // HPシステム（プレイヤーごと）
    private HPSystem p1HP;
    private HPSystem p2HP;

    // その他の状態
    private int p1Score, p2Score;
    private int p1RoundWins, p2RoundWins;
    private int p1DestroyedCount, p2DestroyedCount;

    public enum GameState
    {
        WaitingToStart,
        Playing,
        RoundOver,
        MatchOver
    }
    private GameState currentState = GameState.WaitingToStart;

    // 外部からプロファイルにアクセスするためのプロパティ
    public GameBalanceProfile Profile => profile;

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

        // HPシステム初期化（profile から maxHP を読む）
        int maxHP = profile != null ? profile.hp.maxHP : 500;
        p1HP = new HPSystem(maxHP);
        p2HP = new HPSystem(maxHP);
    }

    void Start()
    {
        // TODO: Phase A-3 でタイトル画面・即リスタート機構に置き換える
        StartNewMatch();
    }

    // =====================================================
    // 試合・ラウンド制御
    // =====================================================

    // 試合開始：すべての状態をリセット
    public void StartNewMatch()
    {
        p1RoundWins = 0;
        p2RoundWins = 0;
        p1Score = 0;
        p2Score = 0;
        p1DestroyedCount = 0;
        p2DestroyedCount = 0;

        int maxHP = profile != null ? profile.hp.maxHP : 500;
        p1HP.SetMaxHP(maxHP, refill: true);
        p2HP.SetMaxHP(maxHP, refill: true);

        Time.timeScale = 1f;
        currentState = GameState.Playing;
        // 初回はシーンに最初から配置されているため、アリーナリセットは呼ばない
    }

    // 次のラウンドを開始：アリーナをクリーンアップして再配置
    public void StartNextRound()
    {
        p1HP.Reset();
        p2HP.Reset();
        p1DestroyedCount = 0;
        p2DestroyedCount = 0;

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
        int dmg = profile != null ? profile.hp.damageBallDrop : 20;
        ApplyDamage(playerIndex, dmg);
    }

    // 1個以上のブロックが底に到達した時に呼ばれる
    public void OnBlocksReachedBottom(int playerIndex, int count = 1)
    {
        if (currentState != GameState.Playing) return;
        int perBlock = profile != null ? profile.hp.damageBlockReachBottom : 10;
        ApplyDamage(playerIndex, perBlock * count);
    }

    public void OnSpikeHit(int playerIndex)
    {
        if (currentState != GameState.Playing) return;
        int dmg = profile != null ? profile.hp.damageBlockSpike : 30;
        ApplyDamage(playerIndex, dmg);
    }

    public void OnPoisonTick(int playerIndex, float deltaTime)
    {
        if (currentState != GameState.Playing) return;
        int dmgPerSec = profile != null ? profile.hp.damagePoisonPerSec : 5;
        // 秒間ダメージを deltaTime で按分（整数四捨五入）
        int dmg = Mathf.RoundToInt(dmgPerSec * deltaTime);
        if (dmg > 0) ApplyDamage(playerIndex, dmg);
    }

    // ダメージ適用の共通処理。HP0 になったらラウンド終了
    private void ApplyDamage(int playerIndex, int amount)
    {
        HPSystem hp = playerIndex == 1 ? p1HP : p2HP;
        hp.TakeDamage(amount);
        if (!hp.IsAlive)
            EndRound(winner: playerIndex == 1 ? 2 : 1);
    }

    public void AddScore(int playerIndex, int amount)
    {
        // HP帯のスコア倍率を適用
        float mul = GetCurrentBand(playerIndex).scoreMul;
        int   gained = Mathf.RoundToInt(amount * mul);
        if (playerIndex == 1) p1Score += gained;
        else                  p2Score += gained;
    }

    // ブロック破壊数をカウント。閾値を超えたら相手に妨害を送る
    public void RegisterBlockDestroyed(int playerIndex)
    {
        if (currentState != GameState.Playing) return;

        int threshold = profile != null ? profile.combo.interferenceTriggerCombo : 5;

        if (playerIndex == 1)
        {
            p1DestroyedCount++;
            if (p1DestroyedCount >= threshold)
            {
                p1DestroyedCount = 0;
                SendSabotageTo(2);
            }
        }
        else
        {
            p2DestroyedCount++;
            if (p2DestroyedCount >= threshold)
            {
                p2DestroyedCount = 0;
                SendSabotageTo(1);
            }
        }
    }

    // 指定プレイヤーのアリーナに妨害行を追加
    private void SendSabotageTo(int targetPlayerIndex)
    {
        ArenaController target = targetPlayerIndex == 1 ? arena1 : arena2;
        if (target == null) return;

        BlockSpawner spawner = target.GetSpawner();
        if (spawner != null) spawner.ReceiveSabotageRow();

        int frames = profile != null ? profile.hitStop.interferenceTriggerFrames : 10;
        target.TriggerHitStop(frames);

        Debug.Log($"P{targetPlayerIndex} に妨害行を送信！");
    }

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
            int frames = profile != null ? profile.hitStop.matchEndFrames : 60;
            arena1?.TriggerHitStop(frames, strong: true);
            arena2?.TriggerHitStop(frames, strong: true);
            // HitStop が終わってから timeScale=0 にする（coroutine は unscaledDeltaTime を使うので問題なし）
            StartCoroutine(MatchOverCoroutine(frames));
            Debug.Log($"試合終了！勝者: P{winner}");
        }
        else
        {
            currentState = GameState.RoundOver;
            int frames = profile != null ? profile.hitStop.roundEndFrames : 30;
            arena1?.TriggerHitStop(frames, strong: true);
            arena2?.TriggerHitStop(frames, strong: true);
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

    public int   GetHP(int playerIndex)           => playerIndex == 1 ? p1HP.CurrentHP : p2HP.CurrentHP;
    public int   GetMaxHP(int playerIndex)        => playerIndex == 1 ? p1HP.MaxHP    : p2HP.MaxHP;
    public float GetHPRatio(int playerIndex)      => playerIndex == 1 ? p1HP.Ratio    : p2HP.Ratio;
    public int   GetScore(int playerIndex)        => playerIndex == 1 ? p1Score       : p2Score;
    public int   GetRoundWins(int playerIndex)    => playerIndex == 1 ? p1RoundWins   : p2RoundWins;
    public int   GetCombo(int playerIndex)        => playerIndex == 1 ? p1DestroyedCount : p2DestroyedCount;
    public int   GetComboThreshold()              => profile != null ? profile.combo.interferenceTriggerCombo : 5;
    public GameState GetCurrentState()            => currentState;

    // 現在のHP帯に応じた動的パラメータ参照
    public HPStateBand GetCurrentBand(int playerIndex)
    {
        if (profile == null) return new HPStateBand();
        float ratio = GetHPRatio(playerIndex);
        return profile.GetBandForRatio(ratio);
    }
}
