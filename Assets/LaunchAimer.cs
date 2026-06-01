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
    private bool             isAiming;
    private float            metronomeTime;
    private float            currentAngleDeg;
    private LineRenderer     line;

    public void Initialize(BallScript b, int pIndex, ArenaController a)
    {
        ball        = b;
        playerIndex = pIndex;
    }

    void Awake()
    {
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
        if (GameManager.Instance?.GetCurrentState() == GameManager.GameState.SkillSelect)
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
        }

        metronomeTime  += Time.deltaTime;
        currentAngleDeg = Mathf.Sin(metronomeTime * (2f * Mathf.PI / metronomePeriodSec))
                          * metronomeAngleRange;

        UpdateLine();

        // 発射は Playing 中のみ（カウントダウン中は S/K 無効, DESIGN.md 12.12）
        if (IsLaunchKeyPressed()
            && GameManager.Instance?.GetCurrentState() == GameManager.GameState.Playing)
            Fire();
    }

    // ラウンド遷移でエイマーの位相をリセットする（ArenaController.ResetForNewRound から呼ばれる）。
    // ボールが発射待ちのままラウンドが終わると metronomeTime が引き継がれ、次ラウンドの初期角度が
    // 中央に戻らない問題を防ぐ。
    public void ResetAim()
    {
        metronomeTime   = 0f;
        currentAngleDeg = 0f;
        isAiming        = false;
    }

    private void UpdateLine()
    {
        if (ball == null) return;
        line.enabled = true;

        float rad        = currentAngleDeg * Mathf.Deg2Rad;
        Vector3 localDir = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
        Vector3 worldDir = ball.transform.parent != null
            ? ball.transform.parent.TransformDirection(localDir)
            : localDir;

        Vector3 origin = ball.transform.position;
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
