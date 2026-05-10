using UnityEngine;

public enum BallAttribute
{
    Normal, // 通常（属性なし）
    Fire,   // 炎：周囲のブロックも巻き込む
    Thunder,// 雷：同種ブロックに連鎖
    Ice,    // 氷：高ダメージ（2HPブロックを1撃）
    Heavy   // 重：貫通する
}

public class BallScript : MonoBehaviour
{
    [Header("ボール設定")]
    [SerializeField] public float speed = 7f;

    [Header("発射設定")]
    [SerializeField] private Vector3 initialLocalDirection = new Vector3(1f, 1f, 0f);
    [SerializeField] private float relaunchAngleSpread = 0.5f;

    [Header("軌道補正")]
    // X・Y それぞれの最小成分比率（0.2 = 約11度以上の角度を保証）
    // 小さすぎると壁沿いの上下・左右ループが発生する
    [SerializeField] private float minAxisRatio = 0.2f;

    [Header("属性設定")]
    [SerializeField] public BallAttribute attribute = BallAttribute.Normal;

    [Header("属性パラメータ")]
    [SerializeField] private int normalDamage = 1;
    [SerializeField] private int iceDamage = 2;
    [SerializeField] private int heavyDamage = 3;
    [SerializeField] private float fireRadius = 1.5f;     // 炎の巻き込み半径
    [SerializeField] private float thunderRadius = 2.5f;  // 雷の連鎖範囲

    [Header("属性別カラー")]
    [SerializeField] private Color normalColor  = Color.white;
    [SerializeField] private Color fireColor    = new Color(1.0f, 0.3f, 0.1f);
    [SerializeField] private Color thunderColor = new Color(1.0f, 0.9f, 0.2f);
    [SerializeField] private Color iceColor     = new Color(0.4f, 0.8f, 1.0f);
    [SerializeField] private Color heavyColor   = new Color(0.6f, 0.3f, 0.8f);

    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    private Rigidbody rb;
    // Heavy属性で「衝突直前の速度」を復元するために前フレームの速度を保持
    private Vector3 lastVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ApplyAttributeColor();
        Launch(initialLocalDirection);
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity != Vector3.zero)
        {
            // 速度の大きさをスピードに固定（スピードが変わらないようにする）
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
            lastVelocity = rb.linearVelocity;
        }
    }

    // 衝突直後（反射後）に角度を補正する
    // FixedUpdateより確実：反射計算が終わった直後に実行されるため
    // lastVelocity は更新しない → Heavy属性の「衝突前に戻す」処理を壊さないため
    private void OnCollisionEnter(Collision collision)
    {
        if (rb.linearVelocity.sqrMagnitude < 0.01f) return;
        rb.linearVelocity = ClampAngle(rb.linearVelocity.normalized) * speed;
    }

    // X・Y どちらかが minAxisRatio 未満なら補正して再正規化する
    // 例: dir.x が 0.05 → 0.2 に引き上げてから normalized で長さ1に戻す
    private Vector3 ClampAngle(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) < minAxisRatio)
            dir.x = dir.x >= 0f ? minAxisRatio : -minAxisRatio;
        if (Mathf.Abs(dir.y) < minAxisRatio)
            dir.y = dir.y >= 0f ? minAxisRatio : -minAxisRatio;
        return dir.normalized;
    }

    public void Relaunch()
    {
        float randomX = Random.Range(-relaunchAngleSpread, relaunchAngleSpread);
        Launch(new Vector3(randomX, 1f, 0f));
    }

    // 属性に応じたダメージ量を返す（Block.cs から呼ばれる）
    public int GetDamage()
    {
        return attribute switch
        {
            BallAttribute.Ice   => iceDamage,
            BallAttribute.Heavy => heavyDamage,
            _ => normalDamage
        };
    }

    // ブロックに当たった瞬間の追加効果（Block.cs から呼ばれる）
    public void OnHitBlock(Block hitBlock)
    {
        switch (attribute)
        {
            case BallAttribute.Heavy:
                // 衝突で曲がった速度を直前の速度に戻して貫通
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

    // 周囲のブロックにダメージを与える共通処理
    // sameTypeOnly=true なら、衝突したブロックと同じBlockTypeのものだけが対象
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

    // 属性に応じたボールの色を反映する
    private void ApplyAttributeColor()
    {
        Color color = attribute switch
        {
            BallAttribute.Fire    => fireColor,
            BallAttribute.Thunder => thunderColor,
            BallAttribute.Ice     => iceColor,
            BallAttribute.Heavy   => heavyColor,
            _ => normalColor
        };

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;
    }

    // ローカル方向 → ワールド方向に変換して発射
    private void Launch(Vector3 localDirection)
    {
        Vector3 dir = localDirection.normalized;
        if (transform.parent != null)
            dir = transform.parent.TransformDirection(dir);

        rb.linearVelocity = dir * speed;
        lastVelocity = rb.linearVelocity;
    }
}
