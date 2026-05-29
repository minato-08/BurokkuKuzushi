using UnityEngine;

public enum BlockType
{
    Normal,    // 通常：1撃で破壊
    Hard,      // 硬い：複数撃必要
    Absorb,    // 吸収：当たるとボール減速
    Explosive  // 爆発：破壊すると周囲ブロックのHPを増やす（妨害）
}

public class Block : MonoBehaviour
{
    [Header("ブロック設定")]
    [SerializeField] public BlockType blockType = BlockType.Normal;
    [SerializeField] public int hp = 1;

    [Header("スコア設定")]
    [SerializeField] private int normalScore = 10;
    [SerializeField] private int hardScore = 20;

    [Header("吸収設定")]
    [SerializeField] private float absorbSpeedMultiplier = 0.7f;

    [Header("爆発設定")]
    [SerializeField] private float explosionRadius    = 2f;
    [SerializeField] private int   explosionHpBuff    = 1;
    [SerializeField] private int   explosiveHitFrames = 6;  // 破壊時ヒットストップフレーム数

    [Header("衝突ヒットストップ（フレーム数・0=なし）")]
    [SerializeField] private int normalHitFrames = 0;   // Normal ブロック衝突時
    [SerializeField] private int hardHitFrames   = 0;   // Hard ブロック衝突時
    [SerializeField] private int absorbHitFrames = 0;   // Absorb ブロック衝突時
    // Explosive は破壊時に explosiveHitFrames を使用するため衝突時は 0 固定

    [Header("アイテムドロップ設定")]
    [SerializeField] private float baseDropChance = 0.15f;

    [Header("ブロック色設定")]
    [SerializeField] private Color normalColor    = new Color(0.490f, 0.639f, 1.000f); // #7da3ff 青
    [SerializeField] private Color hardColor      = new Color(0.753f, 0.769f, 0.816f); // #c0c4d0 グレー
    [SerializeField] private Color absorbColor    = new Color(0.616f, 0.427f, 1.000f); // #9d6dff 紫
    [SerializeField] private Color explosiveColor = new Color(1.000f, 0.690f, 0.290f); // #ffb04a オレンジ
    [SerializeField] private Color hardenedColor  = new Color(0.478f, 0.251f, 0.251f); // #7a4040 ダーク赤

    private int currentHp;
    private Renderer blockRenderer;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        currentHp = hp;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (blockRenderer == null) return;
        blockRenderer.material.color = blockType switch
        {
            BlockType.Hard      => hardColor,
            BlockType.Absorb    => absorbColor,
            BlockType.Explosive => explosiveColor,
            _                   => normalColor
        };
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("BallTag")) return;

        BallScript ball = collision.gameObject.GetComponent<BallScript>();

        // 吸収ブロック：ボールを減速
        if (blockType == BlockType.Absorb)
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity *= absorbSpeedMultiplier;
        }

        // 衝突ヒットストップ（Explosive は破壊時に処理、値0のときはスキップ）
        if (blockType != BlockType.Explosive)
        {
            int baseFrames = blockType switch
            {
                BlockType.Normal => normalHitFrames,
                BlockType.Hard   => hardHitFrames,
                BlockType.Absorb => absorbHitFrames,
                _ => 0
            };
            if (baseFrames > 0)
            {
                float mul = ball?.GetHitStopMultiplier() ?? 1f;
                GetArena()?.TriggerHitStop(Mathf.RoundToInt(baseFrames * mul));
            }
        }

        // ボールの属性に応じたダメージ量を取得
        int damage = ball != null ? ball.GetDamage() : 1;
        TakeDamage(damage, ball);

        // ボールの属性効果（炎の範囲ダメージ・雷の連鎖・重の貫通）を発動
        if (ball != null)
            ball.OnHitBlock(this);
    }

    // ダメージ処理（属性効果からも呼ばれる）
    public void TakeDamage(int damage, BallScript ball = null)
    {
        currentHp -= damage;

        if (currentHp <= 0)
            OnDestroyed(ball);
        // TODO: Phase 4でHPに応じた見た目の変化を追加
    }

    // HPを外部から増やす（爆発ブロック用）
    public void AddHp(int amount)
    {
        hp += amount;
        currentHp += amount;
    }

    private void OnDestroyed(BallScript ball)
    {
        // スコア加算＆コンボ通知
        if (ball != null && GameManager.Instance != null)
        {
            int score = blockType == BlockType.Hard ? hardScore : normalScore;
            GameManager.Instance.AddScore(ball.playerIndex, score);
            GameManager.Instance.RegisterBlockDestroyed(ball.playerIndex);
        }

        // 爆発ブロック：周囲のブロックのHPを増やして妨害 + ヒットストップ
        if (blockType == BlockType.Explosive)
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var col in nearby)
            {
                Block nearBlock = col.GetComponent<Block>();
                if (nearBlock != null && nearBlock != this)
                    nearBlock.AddHp(explosionHpBuff);
            }

            float mul = ball?.GetAttributeMultiplier() ?? 1f;
            GetArena()?.TriggerHitStop(Mathf.RoundToInt(explosiveHitFrames * mul), shake: true);
        }

        if (ball != null) TryDropItem(ball);
        Destroy(gameObject);
    }

    public void HardenToHp(int targetHp)
    {
        blockType = BlockType.Hard;
        hp        = targetHp;
        currentHp = targetHp;
        // 妨害 Harden で変換されたブロックは金色で通常 Hard と区別する
        if (blockRenderer != null)
            blockRenderer.material.color = hardenedColor;
    }

    private void TryDropItem(BallScript ball)
    {
        float dropChance = baseDropChance;
        if (GameManager.Instance != null)
            dropChance *= GameManager.Instance.GetCurrentBand(ball.playerIndex).itemDropMul;

        if (Random.value > dropChance) return;

        float bias = GameManager.Instance != null
            ? GameManager.Instance.GetCurrentBand(ball.playerIndex).goodItemBias
            : 0f;
        ItemType type = SelectRandomItemType(bias);

        GetArena()?.SpawnItem(transform.position, type);
    }

    // DESIGN.md 5.5.2: 強化 6 : 攻撃 4 が基本比率。HPStateBand.goodItemBias で
    // 強化偏重を加算 (劣勢時に強化が出やすくなる)
    private const float BASE_BUFF_WEIGHT = 0.6f;

    private static readonly ItemType[] BuffPool = {
        ItemType.Fire, ItemType.Ice, ItemType.Thunder, ItemType.Heavy, ItemType.Pierce,
        ItemType.Enlarge, ItemType.SpeedUp, ItemType.Heal
    };
    private static readonly ItemType[] AttackPool = {
        ItemType.AttackHarden, ItemType.AttackAddRow,
        ItemType.AttackPoison, ItemType.AttackSlow
    };

    private static ItemType SelectRandomItemType(float goodItemBias)
    {
        float buffWeight = Mathf.Clamp01(BASE_BUFF_WEIGHT + goodItemBias);
        ItemType[] pool  = Random.value < buffWeight ? BuffPool : AttackPool;
        return pool[Random.Range(0, pool.Length)];
    }

    private ArenaController GetArena()
    {
        // Block → BlockSpawner (parent) → Arena root (grandparent) → find ArenaController in children
        return transform.parent?.parent?.GetComponentInChildren<ArenaController>();
    }
}
