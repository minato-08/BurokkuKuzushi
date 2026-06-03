using UnityEngine;

public enum BallAttribute
{
    Normal,
    Fire,
    Thunder,
    Ice,
    Heavy,
    Pierce  // 貫通: ブロックを反射なしで通り抜ける
}

public class BallScript : MonoBehaviour, IFreezable
{
    [Header("ボール設定")]
    [SerializeField] public float speed = 7f;

    [Header("発射設定")]
    [SerializeField] private Vector3 initialLocalDirection = new Vector3(1f, 1f, 0f);
    [SerializeField] private float relaunchAngleSpread = 3f;

    [Header("軌道補正（最小軸成分比率）")]
    [SerializeField] private float minAxisRatio = 0.2f;

    [Header("時間加速（メインボールのみ）")]
    [SerializeField] private float timeAccelRate = 0.05f;  // 1秒あたりの速度増加（baseSpeed単位）
    [SerializeField] private float timeAccelMax  = 2.0f;   // 上限: baseSpeed × この倍率

    [Header("コリジョン抜け対策（アリーナローカル座標）")]
    [SerializeField] private float boundX       = 7f;
    [SerializeField] private float boundYTop    = 11f;
    [SerializeField] private float boundYBottom = -13f;

    [Header("属性設定")]
    [SerializeField] public BallAttribute attribute = BallAttribute.Normal;

    [Header("属性パラメータ")]
    [SerializeField] private int normalDamage = 1;
    [SerializeField] private int iceDamage    = 2;
    [SerializeField] private int heavyDamage  = 3;
    [SerializeField] private int pierceDamage = 1;
    [SerializeField] private float fireRadius    = 1.5f;
    [SerializeField] private float thunderRadius = 2.5f;

    [Header("ヒットストップ係数")]
    [SerializeField] private float hitStopSpeedThreshold = 1.5f; // baseSpeed の何倍超えで発動
    [SerializeField] private float hitStopHeavyMul   = 1.5f;
    [SerializeField] private float hitStopFireMul    = 1.2f;
    [SerializeField] private float hitStopThunderMul = 1.1f;
    [SerializeField] private float hitStopIceMul     = 1.2f;

    [Header("壁バウンスヒットストップ（フレーム数・0=なし）")]
    [SerializeField] private int wallBounceFrames = 0;

    [Header("属性別カラー")]
    [SerializeField] private Color normalColor  = Color.white;
    [SerializeField] private Color fireColor    = new Color(1.0f, 0.478f, 0.239f); // #ff7a3d
    [SerializeField] private Color thunderColor = new Color(1.0f, 0.847f, 0.290f); // #ffd84a
    [SerializeField] private Color iceColor     = new Color(0.306f, 0.765f, 1.0f); // #4ec3ff
    [SerializeField] private Color heavyColor   = new Color(0.706f, 0.643f, 1.0f); // #b4a4ff lavender
    [SerializeField] private Color pierceColor  = new Color(0.635f, 1.0f, 0.878f); // #a2ffdf

    [Header("コンボ熱表示 (Ball Heat, DESIGN.md 5.3)")]
    // 属性が Normal のとき、コンボ段階でボール色を 白→クリーム→橙→赤 に Lerp（純演出）
    [SerializeField] private int   heatStage1 = 10;  // この値以上でクリーム
    [SerializeField] private int   heatStage2 = 20;  // この値以上で温かいオレンジ
    [SerializeField] private int   heatStage3 = 30;  // この値以上で深い赤
    [SerializeField] private Color heatColorLow  = new Color(1.0f, 0.949f, 0.690f); // #fff2b0 クリーム
    [SerializeField] private Color heatColorMid  = new Color(1.0f, 0.690f, 0.290f); // #ffb04a オレンジ
    [SerializeField] private Color heatColorHigh = new Color(1.0f, 0.290f, 0.200f); // #ff4a33 赤
    [SerializeField] private float heatLerpSpeed = 6f; // 色追従の速さ（unscaled）

    [Header("軌跡設定")]
    [SerializeField] private float trailTime       = 0.18f;
    [SerializeField] private float trailStartWidth = 0.22f;

    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    private Rigidbody rb;
    private Vector3 lastVelocity;
    private TrailRenderer trail;
    private Renderer cachedRenderer;
    private Collider cachedCollider;

    // Pierce（貫通）中に物理反発を無効化したブロック群。反発で軌道が折れて
    // トレイルがカクつくのを防ぐため、検出したブロックは IgnoreCollision で素通りさせ、
    // ダメージは衝突ではなくオーバーラップ経由で1回だけ与える。
    [SerializeField] private float pierceDetectMargin = 0.12f; // 貫通検出オーバーラップの余白
    private readonly System.Collections.Generic.HashSet<Block> pierceIgnored
        = new System.Collections.Generic.HashSet<Block>();
    private static readonly Collider[] pierceBuf = new Collider[16];

    private bool frozen = false;
    private Vector3 frozenVelocity;

    public bool IsWaitingToLaunch { get; private set; }

    // 速度の2層管理:
    //   naturalSpeed  = baseSpeed + 時間加速（メインボールのみ連続更新）
    //   speedMultiplier = アイテム効果（SpeedUp/Hyper コルーチンで一時変更）
    //   slowZoneMul   = ZoneSlow が毎フレーム書き込む（ゾーン離脱時に ZoneSlow が 1 に戻す）
    //   実効速度 = naturalSpeed * speedMultiplier * slowZoneMul
    private float baseSpeed;
    private float naturalSpeed;
    private float speedMultiplier = 1f;
    private float arenaDwellTime  = 0f;  // リスポーンでリセットするアリーナ滞在時間

    public float slowZoneMul = 1f;  // ZoneSlow から書き換える。リスポーン時に 1 にリセット

    private Coroutine attributeRoutine;
    private Coroutine speedRoutine;

    // SkillBall_Multi で生成された追加ボール（時間加速なし、落下ペナルティなし）
    public bool isExtraBall = false;

    public void Freeze()
    {
        if (rb == null) return;
        frozen = true;
        frozenVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector3.zero;
        // TrailRenderer はワールド座標に履歴を保持するため、親アリーナを揺らすと履歴だけが
        // 置き去りになって裂ける。ヒットストップ中は履歴を消して非表示にする。
        SetTrailVisible(false, clear: true);
    }

    public void Unfreeze()
    {
        frozen = false;
        if (rb == null) return;
        rb.linearVelocity = frozenVelocity;
        lastVelocity = frozenVelocity;
        // 再開直後のシェイク最終位置と通常位置をつなぐ線が出ないよう、空の履歴から再開する。
        SetTrailVisible(!IsWaitingToLaunch, clear: true);
    }

    // 共有設定（ArenaSharedConfig）があれば左右共通のパラメータを自分へ適用（null セーフ）。
    private void ApplySharedConfig()
    {
        var c = ArenaSharedConfig.Instance;
        if (c == null) return;
        speed                 = c.ballSpeed;
        initialLocalDirection = c.ballInitialLocalDirection;
        relaunchAngleSpread   = c.relaunchAngleSpread;
        minAxisRatio          = c.minAxisRatio;
        timeAccelRate         = c.timeAccelRate;
        timeAccelMax          = c.timeAccelMax;
        boundX       = c.boundX;
        boundYTop    = c.boundYTop;
        boundYBottom = c.boundYBottom;
        normalDamage = c.normalDamage;
        iceDamage    = c.iceDamage;
        heavyDamage  = c.heavyDamage;
        pierceDamage = c.pierceDamage;
        fireRadius    = c.fireRadius;
        thunderRadius = c.thunderRadius;
        hitStopSpeedThreshold = c.hitStopSpeedThreshold;
        hitStopHeavyMul   = c.hitStopHeavyMul;
        hitStopFireMul    = c.hitStopFireMul;
        hitStopThunderMul = c.hitStopThunderMul;
        hitStopIceMul     = c.hitStopIceMul;
        wallBounceFrames  = c.wallBounceFrames;
        normalColor  = c.ballNormalColor;
        fireColor    = c.ballFireColor;
        thunderColor = c.ballThunderColor;
        iceColor     = c.ballIceColor;
        heavyColor   = c.ballHeavyColor;
        pierceColor  = c.ballPierceColor;
        heatStage1 = c.heatStage1;
        heatStage2 = c.heatStage2;
        heatStage3 = c.heatStage3;
        heatColorLow  = c.heatColorLow;
        heatColorMid  = c.heatColorMid;
        heatColorHigh = c.heatColorHigh;
        heatLerpSpeed = c.heatLerpSpeed;
        trailTime       = c.trailTime;
        trailStartWidth = c.trailStartWidth;
    }

    void Start()
    {
        ApplySharedConfig(); // baseSpeed=speed の前に共通設定を反映

        rb = GetComponent<Rigidbody>();
        cachedRenderer = GetComponent<Renderer>();
        cachedCollider = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        baseSpeed    = speed;
        naturalSpeed = baseSpeed;

        trail = GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
        trail.time             = trailTime;
        trail.startWidth       = trailStartWidth;
        trail.endWidth         = 0f;
        trail.minVertexDistance = 0.05f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows    = false;
        Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                          ?? Shader.Find("Sprites/Default");
        if (trailShader != null) trail.material = new Material(trailShader);
        SetTrailVisible(!isExtraBall, clear: true);

        ApplyAttributeColor();

        if (isExtraBall) return;
        Launch(initialLocalDirection);
    }

    void FixedUpdate()
    {
        if (frozen || IsWaitingToLaunch) return;

        // 時間加速（メインボールのみ。アリーナ滞在への報酬）
        if (!isExtraBall)
        {
            arenaDwellTime += Time.fixedDeltaTime;
            naturalSpeed = Mathf.Min(baseSpeed * timeAccelMax,
                                     baseSpeed + timeAccelRate * arenaDwellTime);
        }

        float effectiveSpeed = naturalSpeed * speedMultiplier * slowZoneMul;
        if (rb.linearVelocity != Vector3.zero)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * effectiveSpeed;
            lastVelocity = rb.linearVelocity;
        }

        // 貫通中はブロックを物理的に素通りさせる（反発による軌道の折れ＝トレイルのカクつき防止）。
        if (attribute == BallAttribute.Pierce) PierceThroughBlocks();
        else if (pierceIgnored.Count > 0)      RestorePierceCollisions();

        CheckBounds();
    }

    // 接近したブロックとの衝突を無効化して直進させ、ダメージはここで1回だけ与える。
    // 物理ステップ前（FixedUpdate）に IgnoreCollision を立てるので、その後の衝突解決では反発しない。
    private void PierceThroughBlocks()
    {
        if (cachedCollider == null) return;
        float radius = cachedCollider.bounds.extents.x + pierceDetectMargin;
        int n = Physics.OverlapSphereNonAlloc(transform.position, radius, pierceBuf);
        for (int i = 0; i < n; i++)
        {
            Block b = pierceBuf[i].GetComponent<Block>();
            if (b == null || pierceIgnored.Contains(b)) continue;
            Physics.IgnoreCollision(cachedCollider, pierceBuf[i], true);
            pierceIgnored.Add(b);
            b.TakeDamage(GetDamage(), this); // 衝突経由でないのでここでダメージ
        }
    }

    private void RestorePierceCollisions()
    {
        if (cachedCollider != null)
        {
            foreach (var b in pierceIgnored)
            {
                if (b == null) continue;
                Collider bc = b.GetComponent<Collider>();
                if (bc != null) Physics.IgnoreCollision(cachedCollider, bc, false);
            }
        }
        pierceIgnored.Clear();
    }

    // Ball Heat（DESIGN.md 5.3）: 属性 Normal のときコンボ段階でボール色を Lerp。
    // ヒットストップ中（timeScale=0）も継続させるため Update + unscaledDeltaTime で駆動。
    void Update()
    {
        if (cachedRenderer == null) return;
        if (attribute != BallAttribute.Normal) return; // 属性カラーが Ball Heat に優先

        int combo = GameManager.Instance != null ? GameManager.Instance.GetCombo(playerIndex) : 0;
        Color target = GetHeatColor(combo);
        Color c = Color.Lerp(
            cachedRenderer.material.color, target, heatLerpSpeed * Time.unscaledDeltaTime);
        cachedRenderer.material.color = c;
        SetTrailColor(c); // トレイルもヒート色に追従
    }

    private Color GetHeatColor(int combo)
    {
        if (combo >= heatStage3) return heatColorHigh;
        if (combo >= heatStage2) return heatColorMid;
        if (combo >= heatStage1) return heatColorLow;
        return normalColor;
    }

    // コリジョン抜けでアリーナ外に出た場合の安全網
    private void CheckBounds()
    {
        Vector3 lp = transform.localPosition;
        bool escaped = Mathf.Abs(lp.x) > boundX || lp.y > boundYTop || lp.y < boundYBottom;
        if (!escaped) return;

        if (isExtraBall)
        {
            Destroy(gameObject);
            return;
        }
        // ペナルティなしでリスポーン（プレイヤーのミスではないため）
        PrepareRespawn(GetArena()?.GetBallSpawnLocalPos() ?? new Vector3(0f, -6f, 0f));
    }

    // 衝突直後（反射後）に角度を補正する
    // lastVelocity は更新しない → Heavy/Pierce 属性の「衝突前速度を復元」処理を守るため
    private void OnCollisionEnter(Collision collision)
    {
        if (rb.linearVelocity.sqrMagnitude < 0.01f) return;
        float effectiveSpeed = naturalSpeed * speedMultiplier * slowZoneMul;
        rb.linearVelocity = ClampAngle(rb.linearVelocity.normalized) * effectiveSpeed;

        // 壁判定（Block・PlayerController 以外への衝突 = 壁）
        bool isWall = collision.gameObject.GetComponent<Block>() == null
                   && collision.gameObject.GetComponent<PlayerController>() == null;
        if (isWall)
        {
            // 壁反射 SE（ピッチを速度層で可変, DESIGN.md 10.4）
            AudioManager.Instance?.PlayBallWall(baseSpeed > 0f ? naturalSpeed / baseSpeed : 1f, playerIndex);

            // 壁バウンスヒットストップ
            if (wallBounceFrames > 0)
            {
                float mul = GetHitStopMultiplier();
                if (mul > 0f)
                    GetArena()?.TriggerHitStop(Mathf.RoundToInt(wallBounceFrames * mul), shake: true);
            }
        }
    }

    private Vector3 ClampAngle(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) < minAxisRatio)
            dir.x = dir.x >= 0f ? minAxisRatio : -minAxisRatio;
        if (Mathf.Abs(dir.y) < minAxisRatio)
            dir.y = dir.y >= 0f ? minAxisRatio : -minAxisRatio;
        return dir.normalized;
    }

    // DeadZone / ArenaController / LaunchAimer から呼ばれる
    public void PrepareRespawn(Vector3 localPos)
    {
        if (attributeRoutine != null) { StopCoroutine(attributeRoutine); attributeRoutine = null; }
        if (speedRoutine     != null) { StopCoroutine(speedRoutine);     speedRoutine = null; }
        CancelInvoke();

        // 貫通中に無効化したブロック衝突を元に戻す（次ラウンドへ持ち越さない）
        RestorePierceCollisions();

        frozen          = false;
        speedMultiplier = 1f;
        naturalSpeed    = baseSpeed;
        arenaDwellTime  = 0f;
        slowZoneMul     = 1f;
        IsWaitingToLaunch = true;

        attribute = BallAttribute.Normal;
        ApplyAttributeColor();
        SetTrailVisible(false, clear: true);

        transform.localPosition = localPos;
        transform.localRotation = Quaternion.identity; // 回転（スピン）もリセット
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        GetComponent<Collider>().enabled = false;
    }

    // LaunchAimer から呼ばれる
    public void LaunchInDirection(Vector3 localDir)
    {
        GetComponent<Collider>().enabled = true;
        IsWaitingToLaunch = false;
        frozen = false;
        SetTrailVisible(true, clear: true);
        Launch(localDir);
    }

    public void Relaunch()
    {
        float randomX = Random.Range(-relaunchAngleSpread, relaunchAngleSpread);
        LaunchInDirection(new Vector3(randomX, 1f, 0f));
    }

    public void SetAttributeTemporary(BallAttribute attr, float duration)
    {
        if (attributeRoutine != null) StopCoroutine(attributeRoutine);
        attributeRoutine = StartCoroutine(AttributeRoutine(attr, duration));
    }

    private System.Collections.IEnumerator AttributeRoutine(BallAttribute attr, float duration)
    {
        BallAttribute prev = attribute;
        attribute = attr;
        ApplyAttributeColor();
        yield return new WaitForSeconds(duration);
        attribute = prev;
        ApplyAttributeColor();
        attributeRoutine = null;
    }

    public void SetSpeedTemporary(float multiplier, float duration)
    {
        if (speedRoutine != null) StopCoroutine(speedRoutine);
        speedRoutine = StartCoroutine(SpeedRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator SpeedRoutine(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
        speedRoutine = null;
    }

    // 速度が hitStopSpeedThreshold 倍を超えた場合のみ 0→1 を返す
    // ブロック衝突・壁バウンスのフレーム数にそのまま乗算する（上限がフレーム数そのもの）
    public float GetHitStopMultiplier()
    {
        float ratio = naturalSpeed / baseSpeed;
        if (ratio < hitStopSpeedThreshold) return 0f;
        float range = timeAccelMax - hitStopSpeedThreshold;
        if (range <= 0f) return 1f;
        return Mathf.Clamp01((ratio - hitStopSpeedThreshold) / range);
    }

    // 属性倍率のみ（Explosive 破壊など、速度閾値によらず掛けたい場合に使う）
    public float GetAttributeMultiplier()
    {
        return attribute switch
        {
            BallAttribute.Heavy   => hitStopHeavyMul,
            BallAttribute.Fire    => hitStopFireMul,
            BallAttribute.Thunder => hitStopThunderMul,
            BallAttribute.Ice     => hitStopIceMul,
            BallAttribute.Pierce  => 0f,   // 貫通中はヒットストップなし
            _ => 1f
        };
    }

    public int GetDamage()
    {
        return attribute switch
        {
            BallAttribute.Ice    => iceDamage,
            BallAttribute.Heavy  => heavyDamage,
            BallAttribute.Pierce => pierceDamage,
            _ => normalDamage
        };
    }

    public void OnHitBlock(Block hitBlock)
    {
        switch (attribute)
        {
            case BallAttribute.Pierce:
                // 高速で PierceThroughBlocks の検出より先に衝突した場合のフォールバック:
                // 衝突前の速度ベクトルを復元して直進を維持し、以後はこのブロックを素通りさせる。
                // すでに当該ブロックは衝突経由でダメージ済みなので、ここでは重複ダメージを与えない
                // （overlap 経路が pierceIgnored で二重加算しないよう登録だけ行う）。
                rb.linearVelocity = lastVelocity;
                if (cachedCollider != null && hitBlock != null && !pierceIgnored.Contains(hitBlock))
                {
                    Collider bc = hitBlock.GetComponent<Collider>();
                    if (bc != null) Physics.IgnoreCollision(cachedCollider, bc, true);
                    pierceIgnored.Add(hitBlock);
                }
                break;
            case BallAttribute.Fire:
                ApplyAreaDamage(hitBlock, fireRadius, sameTypeOnly: false);
                break;
            case BallAttribute.Thunder:
                ApplyAreaDamage(hitBlock, thunderRadius, sameTypeOnly: true);
                break;
        }
    }

    private void ApplyAreaDamage(Block centerBlock, float radius, bool sameTypeOnly)
    {
        Collider[] nearby = Physics.OverlapSphere(centerBlock.transform.position, radius);
        foreach (var col in nearby)
        {
            Block other = col.GetComponent<Block>();
            if (other == null || other == centerBlock) continue;
            if (sameTypeOnly && other.blockType != centerBlock.blockType) continue;
            other.TakeDamage(normalDamage, this);
        }
    }

    private void ApplyAttributeColor()
    {
        Color color = attribute switch
        {
            BallAttribute.Fire    => fireColor,
            BallAttribute.Thunder => thunderColor,
            BallAttribute.Ice     => iceColor,
            BallAttribute.Heavy   => heavyColor,
            BallAttribute.Pierce  => pierceColor,
            _ => normalColor
        };
        if (cachedRenderer == null) cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null) cachedRenderer.material.color = color;

        SetTrailColor(color);
    }

    // トレイルの色を更新（Ball Heat / 属性カラー共通）。
    // 毎フレーム Update から呼ばれるため Gradient とキー配列を再利用して GC を避ける。
    private Gradient trailGradient;
    private readonly GradientColorKey[] trailColorKeys = new GradientColorKey[2];
    private readonly GradientAlphaKey[] trailAlphaKeys = {
        new GradientAlphaKey(0.85f, 0f),
        new GradientAlphaKey(0f,    1f)
    };

    private void SetTrailColor(Color color)
    {
        if (trail == null) return;
        if (trailGradient == null) trailGradient = new Gradient();
        trailColorKeys[0] = new GradientColorKey(color, 0f);
        trailColorKeys[1] = new GradientColorKey(color, 1f);
        trailGradient.SetKeys(trailColorKeys, trailAlphaKeys);
        trail.colorGradient = trailGradient;
    }

    private void SetTrailVisible(bool visible, bool clear)
    {
        if (trail == null) return;
        trail.emitting = visible;
        if (clear) trail.Clear();
        trail.enabled = visible;
    }

    private void Launch(Vector3 localDirection)
    {
        Vector3 dir = localDirection.normalized;
        if (transform.parent != null)
            dir = transform.parent.TransformDirection(dir);
        rb.linearVelocity = dir * naturalSpeed;
        lastVelocity = rb.linearVelocity;
    }

    private ArenaController GetArena()
    {
        return transform.parent?.GetComponentInChildren<ArenaController>();
    }
}
