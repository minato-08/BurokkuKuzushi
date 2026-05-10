using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singletonパターン：ゲーム中にこのクラスは1つだけ存在する
    public static GameManager Instance { get; private set; }

    [Header("試合設定")]
    [SerializeField] private int maxLives = 3;      // 残機の初期値
    [SerializeField] private int roundsToWin = 1;   // 何本先取で勝利か
    [SerializeField] private float nextRoundDelay = 2f; // ラウンド終了から次ラウンド開始までの秒数

    [Header("PVP干渉")]
    [SerializeField] private int comboThreshold = 5; // 何個壊すと妨害行を送るか

    [Header("アリーナ参照")]
    [SerializeField] private ArenaController arena1;
    [SerializeField] private ArenaController arena2;

    // 各プレイヤーの状態
    private int p1Lives, p2Lives;
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
    }

    void Start()
    {
        // TODO: Phase 7でタイトル画面から呼ぶ形に変える
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
        p1Lives = maxLives;
        p2Lives = maxLives;
        p1DestroyedCount = 0;
        p2DestroyedCount = 0;

        Time.timeScale = 1f;
        currentState = GameState.Playing;
        // 初回はシーンに最初から配置されているため、アリーナリセットは呼ばない
    }

    // 次のラウンドを開始：アリーナをクリーンアップして再配置
    public void StartNextRound()
    {
        p1Lives = maxLives;
        p2Lives = maxLives;
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

        if (playerIndex == 1)
        {
            p1Lives--;
            if (p1Lives <= 0) EndRound(winner: 2);
        }
        else
        {
            p2Lives--;
            if (p2Lives <= 0) EndRound(winner: 1);
        }
    }

    public void OnBlocksReachedBottom(int playerIndex)
    {
        if (currentState != GameState.Playing) return;
        EndRound(winner: playerIndex == 1 ? 2 : 1);
    }

    public void AddScore(int playerIndex, int amount)
    {
        if (playerIndex == 1) p1Score += amount;
        else p2Score += amount;
    }

    // ブロック破壊数をカウント。閾値を超えたら相手に妨害行を送る
    public void RegisterBlockDestroyed(int playerIndex)
    {
        if (currentState != GameState.Playing) return;

        if (playerIndex == 1)
        {
            p1DestroyedCount++;
            if (p1DestroyedCount >= comboThreshold)
            {
                p1DestroyedCount = 0;
                SendSabotageTo(2);
            }
        }
        else
        {
            p2DestroyedCount++;
            if (p2DestroyedCount >= comboThreshold)
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

        Debug.Log($"P{targetPlayerIndex} に妨害行を送信！");
    }

    // =====================================================
    // ラウンド・試合終了
    // =====================================================

    private void EndRound(int winner)
    {
        if (winner == 1) p1RoundWins++;
        else p2RoundWins++;

        Debug.Log($"ラウンド終了！勝者: P{winner}（P1: {p1RoundWins} / P2: {p2RoundWins}）");

        // 試合終了か、もう1ラウンドあるかを判定
        if (p1RoundWins >= roundsToWin || p2RoundWins >= roundsToWin)
        {
            currentState = GameState.MatchOver;
            Time.timeScale = 0f;
            Debug.Log($"試合終了！勝者: P{winner}");
        }
        else
        {
            currentState = GameState.RoundOver;
            // 一定時間後に次ラウンド開始
            // WaitForSecondsRealtime を使えば Time.timeScale=0 にしても待てる
            StartCoroutine(NextRoundCoroutine());
        }
    }

    private IEnumerator NextRoundCoroutine()
    {
        yield return new WaitForSecondsRealtime(nextRoundDelay);
        StartNextRound();
    }

    // =====================================================
    // 外部からの情報取得（UIが使う）
    // =====================================================

    public int GetLives(int playerIndex)     => playerIndex == 1 ? p1Lives : p2Lives;
    public int GetScore(int playerIndex)     => playerIndex == 1 ? p1Score : p2Score;
    public int GetRoundWins(int playerIndex) => playerIndex == 1 ? p1RoundWins : p2RoundWins;
    public int GetCombo(int playerIndex)     => playerIndex == 1 ? p1DestroyedCount : p2DestroyedCount;
    public int GetComboThreshold()           => comboThreshold;
    public GameState GetCurrentState()       => currentState;
}
