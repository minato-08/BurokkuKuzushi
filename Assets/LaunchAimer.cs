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

    [Header("自動発射")]
    [SerializeField] private float autoLaunchSec    = 3f;   // 通常時の自動発射待ち時間
    [SerializeField] private float minAutoLaunchSec = 0.5f; // ブロック最低位置での最短自動発射時間

    [Header("強制リスポーン")]
    // 飛行中に発射キー(S/K)を押すとボールをリスポーンさせる。HP ペナルティは GameManager で設定。
    // このフィールドはメモ用（実際のペナルティ量は GameManager.damageForceRespawn）

    private BallScript       ball;
    private int              playerIndex;
    private ArenaController  arena;
    private bool             isAiming;
    private float            metronomeTime;
    private float            aimingTime;
    private float            currentAngleDeg;
    private LineRenderer     line;

    public void Initialize(BallScript b, int pIndex, ArenaController a)
    {
        ball        = b;
        playerIndex = pIndex;
        arena       = a;
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

        // 飛行中に発射キー → 強制リスポーン（HP ペナルティあり）
        if (!ball.IsWaitingToLaunch)
        {
            if (GameManager.Instance?.GetCurrentState() == GameManager.GameState.Playing
                && IsLaunchKeyPressed())
                ForceRespawn();
            if (isAiming) StopAiming();
            return;
        }

        // ボールが発射待ち → メトロノームモード
        if (!isAiming)
        {
            metronomeTime = 0f;
            aimingTime    = 0f;
            isAiming      = true;
        }

        metronomeTime  += Time.deltaTime;
        aimingTime     += Time.deltaTime;
        currentAngleDeg = Mathf.Sin(metronomeTime * (2f * Mathf.PI / metronomePeriodSec))
                          * metronomeAngleRange;

        UpdateLine();

        if (IsLaunchKeyPressed() || aimingTime >= GetEffectiveAutoLaunchSec())
            Fire();
    }

    // ブロックが低いほど自動発射時間を短縮する
    private float GetEffectiveAutoLaunchSec()
    {
        BlockSpawner spawner = arena?.GetSpawner();
        if (spawner == null) return autoLaunchSec;

        float lowestY = spawner.GetLowestBlockY();
        float spawnY  = spawner.GetSpawnY();
        float bottomY = spawner.GetBlockDeadZoneY();

        float range = spawnY - bottomY;
        if (range <= 0f) return autoLaunchSec;

        float danger = Mathf.Clamp01((spawnY - lowestY) / range);
        return Mathf.Lerp(autoLaunchSec, minAutoLaunchSec, danger);
    }

    // 飛行中のボールを強制リスポーンする（HP ペナルティあり）
    private void ForceRespawn()
    {
        GameManager.Instance?.OnForceRespawn(playerIndex);
        Vector3 spawnPos = arena?.GetBallSpawnLocalPos() ?? Vector3.zero;
        ball.PrepareRespawn(spawnPos);
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
        StopAiming();
    }
}
