using UnityEngine;

// ブロックの種類を定義するenum
public enum BlockType
{
    Normal,    // 通常：1撃で破壊
    Hard,      // 硬い：複数撃必要
    Absorb,    // 吸収：当たるとボール減速
    Explosive, // 爆発：破壊すると周囲ブロックのHPを増やす（妨害）
    Spike      // 棘：ボール接触でHP減少、破壊時にZonePoisonを生成（妨害）
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

    private int currentHp;

    void Start()
    {
        currentHp = hp;
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

        // スパイクブロック：ボール接触でプレイヤーにHPダメージ
        if (blockType == BlockType.Spike && ball != null)
            GameManager.Instance?.OnSpikeHit(ball.playerIndex);

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

        // スパイクブロック：毒エリアを生成してアイテムドロップなし
        if (blockType == BlockType.Spike)
        {
            GetArena()?.SpawnZonePoison(transform.position);
            Destroy(gameObject);
            return;
        }

        if (ball != null) TryDropItem(ball);
        Destroy(gameObject);
    }

    // InterferenceHarden から呼ばれる: ブロックを Hard に変換してHPを直接設定する
    public void HardenToHp(int targetHp)
    {
        blockType = BlockType.Hard;
        hp        = targetHp;
        currentHp = targetHp;
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

    private static ItemType SelectRandomItemType(float goodItemBias)
    {
        var good = new[] { ItemType.Fire, ItemType.Ice, ItemType.Thunder, ItemType.Heavy, ItemType.Enlarge, ItemType.SpeedUp, ItemType.Heal };
        var all  = new[] { ItemType.Fire, ItemType.Ice, ItemType.Thunder, ItemType.Heavy, ItemType.Enlarge, ItemType.SpeedUp, ItemType.Heal, ItemType.Shrink, ItemType.Hyper };
        if (goodItemBias > 0f && Random.value < goodItemBias)
            return good[Random.Range(0, good.Length)];
        return all[Random.Range(0, all.Length)];
    }

    private ArenaController GetArena()
    {
        // Block → BlockSpawner (parent) → Arena root (grandparent) → find ArenaController in children
        return transform.parent?.parent?.GetComponentInChildren<ArenaController>();
    }
}
