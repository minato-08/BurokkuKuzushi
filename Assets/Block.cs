using UnityEngine;

// ブロックの種類を定義するenum
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
    // 周囲ブロックを巻き込む半径
    [SerializeField] private float explosionRadius = 2f;
    // 巻き込んだブロックのHPを何増やすか
    [SerializeField] private int explosionHpBuff = 1;

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

        // 爆発ブロック：周囲のブロックのHPを増やして妨害
        if (blockType == BlockType.Explosive)
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var col in nearby)
            {
                Block nearBlock = col.GetComponent<Block>();
                if (nearBlock != null && nearBlock != this)
                    nearBlock.AddHp(explosionHpBuff);
            }
        }

        Destroy(gameObject);
    }
}
