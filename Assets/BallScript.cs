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
    [SerializeField] private float heavySpeedFactor = 0.7f; // Heavy 属性中の速度倍率（DESIGN 5.2）

    [Header("ヒットストップ係数")]
    [SerializeField] private float hitStopSpeedThreshold = 1.5f; // baseSpeed の何倍超えで発動
    [SerializeField] private float hitStopHeavyMul   = 1.5f;
    [SerializeField] private float hitStopFireMul    = 1.2f;
    [SerializeField] private float hitStopThunderMul = 1.1f;
    [SerializeField] private float hitStopIceMul     = 1.2f;

    [Header("壁バウンスヒットストップ（フレーム数・0=なし）")]
    [SerializeField] private int wallBounceFrames = 0;

    [Header("手応え（ブロック衝突インパクト）— ArenaSharedConfig で一元調整")]
    [SerializeField] private int   impactBaseFrames  = 2;    // 標準的な一撃の基準フレーム
    [SerializeField] private float impactSpeedWeight = 0.6f; // 速度寄与の強さ
    [SerializeField] private float impactThreshold   = 1.4f; // これ未満は手応えを出さない
    [SerializeField] private int   impactMaxFrames   = 10;   // 停止フレーム上限
    [SerializeField] private float freezeSkipSpeedFactor = 2.5f; // 実効速度がこの倍率超でブロック衝突はフリーズせずシェイクのみ（HYPER 等）

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
    private static readonly Collider[] pierceBuf = new Collider[32]; // GIANT で多数のブロックに重なるため広め

    private bool frozen = false;
    private Vector3 frozenVelocity;

    public bool IsWaitingToLaunch { get; private set; }

    // 速度の2層管理:
    //   naturalSpeed  = baseSpeed + 時間加速（メインボールのみ連続更新）
    //   speedMultiplier = アイテム効果（Hyper コルーチンで一時変更。SpeedUp はパドル速度なので無関係）
    //   slowZoneMul   = ZoneSlow が毎フレーム書き込む（ゾーン離脱時に ZoneSlow が 1 に戻す）
    //   実効速度 = naturalSpeed * speedMultiplier * slowZoneMul * 属性速度係数(Heavy=0.7)
    private float baseSpeed;
    private float naturalSpeed;
    private float speedMultiplier = 1f;
    private float arenaDwellTime  = 0f;  // リスポーンでリセットするアリーナ滞在時間

    public float slowZoneMul = 1f;  // ZoneSlow から書き換える。リスポーン時に 1 にリセット

    private Coroutine attributeRoutine;
    private Coroutine speedRoutine;
    private Coroutine scaleRoutine;
    private Vector3   baseScale = Vector3.one; // Start で実スケールをキャプチャ（GIANT の復元用）

    // メインボールの基準スケール（GIANT 拡大前）。BURST が追加ボールを素のサイズで生成するのに使う。
    public Vector3 BaseScale => baseScale;

    // BURST 等で生成された追加ボール（時間加速なし、落下ペナルティなし）
    public bool isExtraBall = false;

    public void Freeze()
    {
        if (rb == null) return;
        frozen = true;
        frozenVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector3.zero;
        // ボールは ShakeRoot の外（Arena 直下）なのでシェイクで動かず、履歴は置き去りにならない＝裂けない。
        // よってフリーズ中は履歴を消さず、新規頂点の追加だけ止める（emitting=false / 描画は継続）。
        // 旧実装は毎フリーズで Clear+非表示にしていたため、HYPER 等で頻繁にヒットストップが起きると
        // トレイルが毎回消えて見えなくなっていた（2026-06-05 修正）。再開後は履歴が残るので連続して見える。
        if (trail != null) trail.emitting = false;
    }

    public void Unfreeze()
    {
        frozen = false;
        if (rb == null) return;
        rb.linearVelocity = frozenVelocity;
        lastVelocity = frozenVelocity;
        // 履歴は保ったまま発射中なら再び新規頂点を出す（Clear しない＝フリーズ前の軌跡と連続する）。
        if (trail != null) trail.emitting = !IsWaitingToLaunch;
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
        heavySpeedFactor = c.heavySpeedFactor;
        hitStopSpeedThreshold = c.hitStopSpeedThreshold;
        hitStopHeavyMul   = c.hitStopHeavyMul;
        hitStopFireMul    = c.hitStopFireMul;
        hitStopThunderMul = c.hitStopThunderMul;
        hitStopIceMul     = c.hitStopIceMul;
        wallBounceFrames  = c.wallBounceFrames;
        impactBaseFrames  = c.impactBaseFrames;
        impactSpeedWeight = c.impactSpeedWeight;
        impactThreshold   = c.impactThreshold;
        impactMaxFrames   = c.impactMaxFrames;
        freezeSkipSpeedFactor = c.freezeSkipSpeedFactor;
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
        baseScale    = transform.localScale; // GIANT の一時拡大からの復元基準

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

        float effectiveSpeed = EffectiveSpeed();
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
        float effectiveSpeed = EffectiveSpeed();
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
        if (scaleRoutine     != null) { StopCoroutine(scaleRoutine);     scaleRoutine = null; }
        transform.localScale = baseScale; // GIANT の一時拡大を次ラウンドへ持ち越さない
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

    // GIANT スキル: ボールを一定時間巨大化する（Pierce 検出半径は bounds 由来なので薙ぎ払い幅も自動拡大）
    public void SetScaleTemporary(float multiplier, float duration)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator ScaleRoutine(float multiplier, float duration)
    {
        transform.localScale = baseScale * multiplier;
        yield return new WaitForSeconds(duration);
        transform.localScale = baseScale;
        scaleRoutine = null;
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

    // 属性倍率のみ（手応えの「攻撃力」重み。Normal1.0 / Ice・Fire1.2 / Thunder1.1 / Heavy3.0 / Pierce0）
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

    // ブロック衝突の「手応え」フレーム数（速度 × 攻撃力）。
    //   impact = speedTerm × attackWeight
    //     speedTerm   = 1 + impactSpeedWeight × (naturalSpeed/baseSpeed − 1)   ← 速いほど大
    //     attackWeight = GetAttributeMultiplier()                              ← 強属性ほど大
    //   impact < impactThreshold は 0（軽い当たりは止めずテンポ維持）、以上は base×impact を上限クランプ。
    // 仕様（DESIGN 5.2）の「速い/攻撃力が高いほど手応え」を 1 本化したもの。Pierce は 0。
    public int GetImpactFrames()
    {
        float attackWeight = GetAttributeMultiplier();
        if (attackWeight <= 0f) return 0; // Pierce
        // 実効速度（時間加速 × アイテム加減速 × ZoneSlow）で見る＝速い当たりほど手応え、
        // 遅延ゾーンで減速した当たりは弱くなる。
        float effectiveSpeed = EffectiveSpeed();
        float speedFactor = baseSpeed > 0f ? effectiveSpeed / baseSpeed : 1f;
        float speedTerm   = 1f + impactSpeedWeight * (speedFactor - 1f);
        float impact      = speedTerm * attackWeight;
        if (impact < impactThreshold) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(impactBaseFrames * impact), 1, impactMaxFrames);
    }

    // ブロック衝突でボールをフリーズ（一時停止）すべきか。実効速度が freezeSkipSpeedFactor 倍を
    // 超える高速時（HYPER 等）は false＝止めずシェイクのみ（爽快さ維持＋トレイルが途切れない, DESIGN.md 5.2/5.6）。
    public bool ShouldFreezeOnImpact()
    {
        if (freezeSkipSpeedFactor <= 0f) return true; // 機能無効＝常にフリーズ
        float speedFactor = baseSpeed > 0f ? EffectiveSpeed() / baseSpeed : 1f;
        return speedFactor < freezeSkipSpeedFactor;
    }

    // 実効速度 = 自然速度 × アイテム加減速 × ZoneSlow × 属性速度係数。FixedUpdate での正規化・
    // 衝突時の角度補正・手応え算出で共通利用する。
    private float EffectiveSpeed()
        => naturalSpeed * speedMultiplier * slowZoneMul * AttributeSpeedFactor();

    // 属性による速度倍率。Heavy は重い分だけ遅い（DESIGN 5.2「速度0.7倍」）。
    private float AttributeSpeedFactor()
        => attribute == BallAttribute.Heavy ? heavySpeedFactor : 1f;

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
