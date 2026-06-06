using UnityEngine;
using UnityEngine.InputSystem;

// メトロノーム式発射インジケーターと入力ハンドリング
// ArenaController から Initialize() を呼ばれてセットアップされる
public class LaunchAimer : MonoBehaviour
{
    // バランス値は ArenaSharedConfig で一元管理（ApplySharedConfig で取得）。未配置時は既定値。
    private float indicatorLength = 2.5f;
    private Color indicatorColor  = Color.yellow;
    private float metronomeAngleRange = 60f;
    private float metronomePeriodSec  = 1.0f;

    private BallScript       ball;
    private int              playerIndex;
    private ArenaController  arena;
    private bool             isAiming;
    private float            metronomeTime;
    private float            currentAngleDeg;
    private float            prevAngleDeg;      // 前フレームのエイマー角度（センター通過検出用）
    private LineRenderer     line;

    public void Initialize(BallScript b, int pIndex, ArenaController a)
    {
        ball        = b;
        playerIndex = pIndex;
        arena       = a;
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
            if (isAiming) StopAiming();
            return;
        }

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
            prevAngleDeg  = 0f;
        }

        metronomeTime  += Time.deltaTime;
        currentAngleDeg = Mathf.Sin(metronomeTime * (2f * Mathf.PI / metronomePeriodSec))
                          * metronomeAngleRange;

        CheckCenterPass();
        UpdateLineAt(ball.transform.position);

        // 発射は Playing 中のみ（カウントダウン中は S/K 無効, DESIGN.md 12.12）
        if (IsLaunchKeyPressed() && state == GameManager.GameState.Playing)
            Fire();
    }

    // ラウンド遷移でエイマーの位相をリセットする（ArenaController.ResetForNewRound から呼ばれる）。
    // ボールが発射待ちのままラウンドが終わると metronomeTime が引き継がれ、次ラウンドの初期角度が
    // 中央に戻らない問題を防ぐ。
    public void ResetAim()
    {
        metronomeTime   = 0f;
        currentAngleDeg = 0f;
        prevAngleDeg    = 0f;
        isAiming        = false;
    }

    // メトロノームのインジケーターが真上（0°）を横切った瞬間に「ティック」SE を 1 回鳴らす（DESIGN 5.3）。
    // 発射タイミングの耳コピを可能にする。sin 波なので角度は半周期ごとに 0° を通過する。
    private void CheckCenterPass()
    {

        if (prevAngleDeg*currentAngleDeg < 0) {
            AudioManager.Instance?.PlayCenterTick(playerIndex);
        }

        prevAngleDeg = currentAngleDeg;
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
