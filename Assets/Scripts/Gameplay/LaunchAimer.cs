using System.Collections.Generic;
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
    private Color rangeArcColor        = new Color(1f, 1f, 1f, 0.22f);
    private float centerThresholdDeg   = 10f;
    private Color centerColor          = Color.cyan;
    private int   trajectoryMaxBounces = 3;
    private float trajectoryMaxDist    = 30f;
    private float trajectoryTotalLength = 14f;
    private float trajectoryDashPeriod  = 0.5f;
    private Color trajectoryColor      = new Color(1f, 1f, 1f, 0.45f);

    private BallScript       ball;
    private int              playerIndex;
    private ArenaController  arena;
    private bool             isAiming;
    private float            metronomeTime;
    private float            currentAngleDeg;
    private float            prevAngleDeg;      // 前フレームのエイマー角度（センター通過検出用）
    private LineRenderer     line;            // 現在角度のインジケーター
    private LineRenderer     rangeLine;       // 振れ幅の扇形ガイド（輪郭線）
    private LineRenderer     trajectoryLine;  // 予想軌道（壁反射を辿った折れ線）

    private float            ballRadius = 0.18f;          // SphereCast 用のボール半径（Awake で算出）
    private readonly List<Vector3> trajPoints = new();    // 軌道の頂点（GC 回避で使い回す）
    private Material         trajectoryMat;               // 点線マテリアル（mainTextureScale を毎フレ更新）

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
        rangeArcColor        = c.rangeArcColor;
        centerThresholdDeg   = c.centerThresholdDeg;
        centerColor          = c.centerColor;
        trajectoryMaxBounces = c.trajectoryMaxBounces;
        trajectoryMaxDist    = c.trajectoryMaxDist;
        trajectoryTotalLength = c.trajectoryTotalLength;
        trajectoryDashPeriod  = c.trajectoryDashPeriod;
        trajectoryColor      = c.trajectoryColor;
    }

    void Awake()
    {
        ApplySharedConfig();

        // 角度インジケーターは LaunchAimer 本体に、扇形と軌道は子オブジェクトに描く
        // （1 つの GameObject には LineRenderer は 1 個しか付けられないため）。
        line           = CreateLine(gameObject, 0.08f, Color.white);
        rangeLine      = CreateLine(NewChild("RangeArc"),   0.04f, rangeArcColor);
        trajectoryLine = CreateLine(NewChild("Trajectory"), 0.05f, trajectoryColor);
        SetupDashedMaterial(trajectoryLine); // 軌道だけ点線マテリアルへ差し替え
    }

    // 軌道線を「点線」にする。線の長さ方向(U)に半分不透明・半分透明のテクスチャを Repeat で貼り、
    // Stretch モードで U=0→1 を線全体に対応させ、毎フレーム mainTextureScale で繰り返し回数を決める。
    private void SetupDashedMaterial(LineRenderer lr)
    {
        // 破線テクスチャ（横 8px: 前半不透明 / 後半透明 = デューティ 50%）。
        const int w = 8;
        var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point,
        };
        var px = new Color[w];
        for (int i = 0; i < w; i++)
            px[i] = i < w / 2 ? Color.white : new Color(1f, 1f, 1f, 0f);
        tex.SetPixels(px);
        tex.Apply();

        Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        // URP/Unlit を透過サーフェスに切り替える（テクスチャのアルファで点線のすき間を抜く）。
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f); // 0=Opaque / 1=Transparent
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", trajectoryColor);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", trajectoryColor);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        else mat.mainTexture = tex;

        lr.material       = mat;
        trajectoryMat     = mat;                   // 毎フレ mainTextureScale を更新するためキャッシュ
        lr.textureMode    = LineTextureMode.Stretch; // U を線全体に張り、_ST(mainTextureScale) で繰り返す
        lr.numCapVertices = 0;                      // 丸キャップだと破線端がにじむので無効
    }

    void Start()
    {
        // SphereCast 用にボールの実半径（ワールド）を求めておく。
        if (ball != null && ball.TryGetComponent<SphereCollider>(out var sc))
            ballRadius = sc.radius * Mathf.Abs(ball.transform.lossyScale.x);
    }

    // 子の空 GameObject を作って返す（LineRenderer をぶら下げる入れ物）。
    private GameObject NewChild(string childName)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go;
    }

    // 共通設定の LineRenderer を生成する。色は startColor/endColor で指定（マテリアルは白の Unlit を共有）。
    private LineRenderer CreateLine(GameObject host, float width, Color color)
    {
        var lr = host.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.numCapVertices = 2;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            lr.material = mat;
        }
        lr.startColor = lr.endColor = color;
        lr.enabled = false;
        return lr;
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

        Vector3 origin   = ball.transform.position;
        Vector3 worldDir = LocalAngleToWorldDir(currentAngleDeg);
        UpdateLineAt(origin, worldDir);
        UpdateRangeArc(origin);
        UpdateTrajectory(origin, worldDir);

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

    // 角度（度・真上=0°）をアリーナのローカル→ワールド方向ベクトルに変換する。
    private Vector3 LocalAngleToWorldDir(float angleDeg)
    {
        float rad        = angleDeg * Mathf.Deg2Rad;
        Vector3 localDir = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
        return ball != null && ball.transform.parent != null
            ? ball.transform.parent.TransformDirection(localDir)
            : localDir;
    }

    private void UpdateLineAt(Vector3 origin, Vector3 worldDir)
    {
        if (ball == null) return;
        line.enabled = true;

        // センター通過ビジュアル（DESIGN 5.3）: 真上 ±centerThresholdDeg に入ると色を切替。
        Color c = Mathf.Abs(currentAngleDeg) <= centerThresholdDeg ? centerColor : Color.white;
        line.startColor = line.endColor = c;

        line.SetPosition(0, origin);
        line.SetPosition(1, origin + worldDir * indicatorLength);
    }

    // 振れ幅の扇形ガイド（DESIGN 5.3）: 原点→左端→円弧→右端→原点 を 1 本の折れ線で描く輪郭扇形。
    private void UpdateRangeArc(Vector3 origin)
    {
        const int arcSegments = 16;
        float r = indicatorLength;

        // 頂点数 = 原点 + 円弧上の (arcSegments+1) 点 + 原点で閉じる 1 点
        rangeLine.positionCount = arcSegments + 3;
        rangeLine.SetPosition(0, origin);
        for (int i = 0; i <= arcSegments; i++)
        {
            // -metronomeAngleRange から +metronomeAngleRange まで等間隔に走査
            float t   = (float)i / arcSegments;
            float deg = Mathf.Lerp(-metronomeAngleRange, metronomeAngleRange, t);
            rangeLine.SetPosition(i + 1, origin + LocalAngleToWorldDir(deg) * r);
        }
        rangeLine.SetPosition(arcSegments + 2, origin); // 右端から原点へ戻して閉じる
        rangeLine.enabled = true;
    }

    private void StopAiming()
    {
        isAiming = false;
        line.enabled           = false;
        rangeLine.enabled      = false;
        trajectoryLine.enabled = false;
    }

    // 予想軌道（DESIGN 5.3）: SphereCast でボール半径ぶんの球を発射方向へ飛ばし、
    // 壁に当たれば反射して継続、ブロック等に当たれば停止する折れ線を組み立てる。
    private void UpdateTrajectory(Vector3 originWorld, Vector3 dirWorld)
    {
        trajPoints.Clear();
        trajPoints.Add(originWorld);

        // ボール自身のコライダーと重ならないよう、半径ぶん前進した位置から走査を始める。
        Vector3 pos = originWorld + dirWorld.normalized * (ballRadius + 0.01f);
        Vector3 dir = dirWorld.normalized;
        float remaining = trajectoryTotalLength; // 全体の長さ予算（尽きたら途中でも打ち切る）

        for (int bounce = 0; bounce <= trajectoryMaxBounces && remaining > 0f; bounce++)
        {
            // 残り予算と 1 セグメント上限の小さい方までしか探索しない。
            float castDist = Mathf.Min(trajectoryMaxDist, remaining);

            // トリガー（DeadZone / ZonePoison / ZoneSlow）は無視し、実コライダーの壁・ブロックだけ拾う。
            if (Physics.SphereCast(pos, ballRadius, dir, out RaycastHit hit, castDist,
                                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                trajPoints.Add(hit.point); // 軌道の折れ目（壁でもブロックでもまず描点を打つ）
                remaining -= hit.distance;

                if (IsWall(hit.collider))
                {
                    // 壁 → 面の法線で鏡面反射して継続。
                    dir = Vector3.Reflect(dir, hit.normal).normalized;
                    // 反射後の方向へ少し進めておかないと、次の SphereCast が同じ壁を
                    // 即再ヒットして同じ場所で跳ね続けてしまう。
                    pos = hit.point + dir * (ballRadius + 0.01f);
                }
                else
                {
                    break; // ブロック等に当たって止まる → 軌道終了
                }
            }
            else
            {
                // 予算ぶん進んでも何にも当たらなければ、その地点で打ち切る。
                trajPoints.Add(pos + dir * castDist);
                break;
            }
        }

        trajectoryLine.positionCount = trajPoints.Count;
        trajectoryLine.SetPositions(trajPoints.ToArray());
        trajectoryLine.enabled = trajPoints.Count >= 2;

        // 点線の密度を実長に比例させる（mainTextureScale.x = 実長 / 1 周期）。
        // これで反射で折れても破線の間隔が一定に見える。
        if (trajectoryLine.enabled && trajectoryMat != null && trajectoryDashPeriod > 0f)
        {
            float len = 0f;
            for (int i = 1; i < trajPoints.Count; i++)
                len += Vector3.Distance(trajPoints[i - 1], trajPoints[i]);
            trajectoryMat.mainTextureScale = new Vector2(len / trajectoryDashPeriod, 1f);
        }
    }

    // 当たった相手が「壁」か（＝Block でも Player でもボールでもない静的コライダー）。
    private bool IsWall(Collider c)
    {
        return c.GetComponent<Block>() == null
            && c.GetComponent<PlayerController>() == null
            && c.GetComponent<BallScript>() == null;
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
