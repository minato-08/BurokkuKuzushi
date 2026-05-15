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
    [SerializeField] private Color fireColor    = new Color(1.0f, 0.3f, 0.1f);
    [SerializeField] private Color thunderColor = new Color(1.0f, 0.9f, 0.2f);
    [SerializeField] private Color iceColor     = new Color(0.4f, 0.8f, 1.0f);
    [SerializeField] private Color heavyColor   = new Color(0.6f, 0.3f, 0.8f);
    [SerializeField] private Color pierceColor  = new Color(0.6f, 1.0f, 1.0f);

    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    private Rigidbody rb;
    private Vector3 lastVelocity;

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
    }

    public void Unfreeze()
    {
        frozen = false;
        if (rb == null) return;
        rb.linearVelocity = frozenVelocity;
        lastVelocity = frozenVelocity;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        baseSpeed    = speed;
        naturalSpeed = baseSpeed;
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

        CheckBounds();
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

        // 壁バウンスヒットストップ（Block・PlayerController 以外への衝突 = 壁）
        if (wallBounceFrames > 0
            && collision.gameObject.GetComponent<Block>() == null
            && collision.gameObject.GetComponent<PlayerController>() == null)
        {
            float mul = GetHitStopMultiplier();
            if (mul > 0f)
                GetArena()?.TriggerHitStop(Mathf.RoundToInt(wallBounceFrames * mul), shake: true);
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

        frozen          = false;
        speedMultiplier = 1f;
        naturalSpeed    = baseSpeed;
        arenaDwellTime  = 0f;
        slowZoneMul     = 1f;
        IsWaitingToLaunch = true;

        attribute = BallAttribute.Normal;
        ApplyAttributeColor();

        transform.localPosition = localPos;
        rb.linearVelocity = Vector3.zero;
        GetComponent<Collider>().enabled = false;
    }

    // LaunchAimer から呼ばれる
    public void LaunchInDirection(Vector3 localDir)
    {
        GetComponent<Collider>().enabled = true;
        IsWaitingToLaunch = false;
        frozen = false;
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
            case BallAttribute.Heavy:
            case BallAttribute.Pierce:
                // 衝突前の速度ベクトルを復元して貫通
                rb.linearVelocity = lastVelocity;
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
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = color;
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
