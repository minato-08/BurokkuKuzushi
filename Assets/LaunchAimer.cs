using UnityEngine;
using UnityEngine.InputSystem;

// メトロノーム式発射インジケーターと入力ハンドリング
// ArenaController から Initialize() を呼ばれてセットアップされる
public class LaunchAimer : MonoBehaviour
{
    [Header("インジケーター設定")]
    [SerializeField] private float indicatorLength = 2.5f;
    [SerializeField] private Color indicatorColor  = Color.yellow;

    [Header("メトロノーム発射")]
    [SerializeField] private float metronomeAngleRange = 60f;
    [SerializeField] private float metronomePeriodSec  = 1.0f;

    private BallScript       ball;
    private int              playerIndex;
    private ArenaController  arena;
    private bool             isAiming;
    private float            metronomeTime;
    private float            currentAngleDeg;
    private LineRenderer     line;

    // BURST スキル（DESIGN.md 5.6）: 連射モード
    private bool  burstActive;
    private int   burstShotsLeft;
    private float burstTimer;
    private float burstBallLifetime;

    public void Initialize(BallScript b, int pIndex, ArenaController a)
    {
        ball        = b;
        playerIndex = pIndex;
        arena       = a;
    }

    // BURST 発動: shots 発・duration 秒の連射モードに入る
    public void BeginBurst(int shots, float duration, float ballLifetime)
    {
        burstActive       = true;
        burstShotsLeft    = shots;
        burstTimer        = duration;
        burstBallLifetime = ballLifetime;
    }

    private void EndBurst()
    {
        burstActive = false;
        StopAiming();
    }

    // 共有設定（ArenaSharedConfig）があれば左右共通のパラメータを自分へ適用（null セーフ）。
    private void ApplySharedConfig()
    {
        var c = ArenaSharedConfig.Instance;
        if (c == null) return;
        indicatorLength     = c.indicatorLength;
        indicatorColor      = c.indicatorColor;
        metronomeAngleRange = c.metronomeAngleRange;
        metronomePeriodSec  = c.metronomePeriodSec;
    }

    void Awake()
    {
        ApplySharedConfig();

        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = line.endWidth = 0.08f;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", indicatorColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", indicatorColor);
            line.material = mat;
        }
        line.startColor = line.endColor = Color.white;
        line.enabled = false;
    }

    void Update()
    {
        var state = GameManager.Instance?.GetCurrentState();

        if (state == GameManager.GameState.SkillSelect)
        {
            if (burstActive) EndBurst();
            else if (isAiming) StopAiming();
            return;
        }

        // BURST 連射モードは通常照準より優先（メインボール飛行中でも照準できる）
        if (burstActive) { UpdateBurst(state); return; }

        if (ball == null)
        {
            if (isAiming) StopAiming();
            return;
        }

        if (!ball.IsWaitingToLaunch)
        {
            if (isAiming) StopAiming();
            return;
        }

        // ボールが発射待ち → メトロノームモード
        if (!isAiming)
        {
            metronomeTime = 0f;
            isAiming      = true;
        }

        metronomeTime  += Time.deltaTime;
        currentAngleDeg = Mathf.Sin(metronomeTime * (2f * Mathf.PI / metronomePeriodSec))
                          * metronomeAngleRange;

        UpdateLineAt(ball.transform.position);

        // 発射は Playing 中のみ（カウントダウン中は S/K 無効, DESIGN.md 12.12）
        if (IsLaunchKeyPressed() && state == GameManager.GameState.Playing)
            Fire();
    }

    // BURST 連射モードの毎フレーム処理
    private void UpdateBurst(GameManager.GameState? state)
    {
        // Playing 以外（ラウンド間など）では時間・弾を消費しない（照準も止める）
        if (state != GameManager.GameState.Playing) { if (isAiming) StopAiming(); return; }

        burstTimer -= Time.deltaTime;
        if (burstTimer <= 0f || burstShotsLeft <= 0) { EndBurst(); return; }

        if (!isAiming) { metronomeTime = 0f; isAiming = true; }
        metronomeTime  += Time.deltaTime;
        currentAngleDeg = Mathf.Sin(metronomeTime * (2f * Mathf.PI / metronomePeriodSec))
                          * metronomeAngleRange;

        Vector3 originWorld = BurstOriginWorld();
        UpdateLineAt(originWorld);

        if (IsLaunchKeyPressed() && arena != null)
        {
            float rad        = currentAngleDeg * Mathf.Deg2Rad;
            Vector3 localDir = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
            arena.SpawnBurstBall(localDir, burstBallLifetime);
            AudioManager.Instance?.PlayBallLaunch(playerIndex);
            burstShotsLeft--;
        }
    }

    // BURST のインジケーター原点＝ボール生成位置（パドル上）のワールド座標
    private Vector3 BurstOriginWorld()
    {
        if (ball != null && ball.transform.parent != null && arena != null)
            return ball.transform.parent.TransformPoint(arena.GetBallSpawnLocalPos());
        return ball != null ? ball.transform.position : transform.position;
    }

    // ラウンド遷移でエイマーの位相をリセットする（ArenaController.ResetForNewRound から呼ばれる）。
    // ボールが発射待ちのままラウンドが終わると metronomeTime が引き継がれ、次ラウンドの初期角度が
    // 中央に戻らない問題を防ぐ。
    public void ResetAim()
    {
        metronomeTime   = 0f;
        currentAngleDeg = 0f;
        isAiming        = false;
        burstActive     = false; // BURST 連射中にラウンドが終わっても持ち越さない
    }

    private void UpdateLineAt(Vector3 origin)
    {
        if (ball == null) return;
        line.enabled = true;

        float rad        = currentAngleDeg * Mathf.Deg2Rad;
        Vector3 localDir = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
        Vector3 worldDir = ball.transform.parent != null
            ? ball.transform.parent.TransformDirection(localDir)
            : localDir;

        line.SetPosition(0, origin);
        line.SetPosition(1, origin + worldDir * indicatorLength);
    }

    private void StopAiming()
    {
        isAiming     = false;
        line.enabled = false;
    }

    private bool IsLaunchKeyPressed()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return playerIndex == 1
            ? kb.sKey.wasPressedThisFrame
            : kb.kKey.wasPressedThisFrame;
    }

    private void Fire()
    {
        float rad        = currentAngleDeg * Mathf.Deg2Rad;
        Vector3 localDir = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
        ball.LaunchInDirection(localDir);
        AudioManager.Instance?.PlayBallLaunch(playerIndex); // 発射確定 SE（DESIGN.md 10.4）
        StopAiming();
    }
}
