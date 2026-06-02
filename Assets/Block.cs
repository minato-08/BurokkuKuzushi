using UnityEngine;

public enum BlockType
{
    Normal,    // 通常：1撃で破壊
    Hard,      // 硬い：複数撃必要
    Absorb,    // 吸収：当たるとボール減速
    Explosive, // 爆発：破壊すると周囲ブロックに巻き込みダメージ（同 Explosive は連鎖爆発, DESIGN.md 5.4）
    Item       // アイテム：HP1。破壊で確定 1 個ドロップ（DESIGN.md 5.4/12.17）
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
    [SerializeField] private int   explosionDamage    = 1;   // 爆発の巻き込みダメージ。Explosive 含む周囲ブロックへ適用（同 Explosive は連鎖発火）
    [SerializeField] private int   explosiveHitFrames = 6;  // 破壊時ヒットストップフレーム数

    [Header("衝突ヒットストップ（フレーム数・0=なし）")]
    [SerializeField] private int normalHitFrames = 0;   // Normal ブロック衝突時
    [SerializeField] private int hardHitFrames   = 0;   // Hard ブロック衝突時
    [SerializeField] private int absorbHitFrames = 0;   // Absorb ブロック衝突時
    // Explosive は破壊時に explosiveHitFrames を使用するため衝突時は 0 固定

    [Header("アイテムドロップ設定")]
    [SerializeField] private float baseDropChance = 0.15f;
    // 強化枠が出る際、この確率で「強化に偽装した罠」に置き換える（DESIGN.md 5.5.3 の紛らわしさ）
    [Range(0f, 1f)] [SerializeField] private float trapDisguiseChance = 0.1f;
    // 抽選した持続効果が既に有効なスロットだった場合の再抽選上限。
    // 上限まで再抽選しても同スロットなら、そのドロップはスキップする（DESIGN.md 5.5 ドロップ過多抑制）
    [SerializeField] private int maxSlotRerolls = 2;

    [Header("ブロック色設定")]
    [SerializeField] private Color normalColor    = new Color(0.490f, 0.639f, 1.000f); // #7da3ff 青
    [SerializeField] private Color hardColor      = new Color(0.753f, 0.769f, 0.816f); // #c0c4d0 グレー
    [SerializeField] private Color absorbColor    = new Color(0.616f, 0.427f, 1.000f); // #9d6dff 紫
    [SerializeField] private Color explosiveColor = new Color(1.000f, 0.690f, 0.290f); // #ffb04a オレンジ
    [SerializeField] private Color hardenedColor  = new Color(0.478f, 0.251f, 0.251f); // #7a4040 ダーク赤
    [SerializeField] private Color itemColor      = new Color(0.290f, 1.000f, 0.627f); // #4affa0 緑（報酬感）

    [Header("HP pip（残耐久ドット, DESIGN.md 5.4。HP>1 のみ表示）")]
    [SerializeField] private bool  showHpPips      = true;
    [SerializeField] private float pipWorldSize    = 0.12f; // ワールド換算のドット径
    [SerializeField] private float pipWorldSpacing = 0.18f; // ドット間隔（ワールド）
    [SerializeField] private Vector3 pipWorldOffset = new Vector3(0f, 0.16f, -0.55f); // ブロック中心からのワールドオフセット（z<0=手前）
    [SerializeField] private Color pipColor        = new Color(0.06f, 0.06f, 0.09f, 1f); // 暗色ドット

    private int currentHp;
    private Renderer blockRenderer;
    private bool destroyed;   // 多重破壊ガード（Destroy は遅延実行なので同フレームの追撃で二重発火するのを防ぐ）
    private GameObject[] hpPips;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        currentHp = hp;
        RefreshColor();
        BuildHpPips();
    }

    // HP>1 ブロックの残耐久ドット（●●●）を子に生成。親が非一様スケール(1.3,0.5,1)なので
    // ワールド指定値を親スケールで割って localPosition/localScale に換算する（DESIGN.md 5.4）。
    private void BuildHpPips()
    {
        ClearHpPips();
        if (!showHpPips || hp <= 1 || blockType == BlockType.Item) return;

        Vector3 s = transform.localScale;
        if (s.x == 0f || s.y == 0f || s.z == 0f) return;
        hpPips = new GameObject[hp];
        float totalW = (hp - 1) * pipWorldSpacing;
        for (int i = 0; i < hp; i++)
        {
            GameObject pip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pip.name = "HpPip";
            Collider col = pip.GetComponent<Collider>();
            if (col != null) Destroy(col);
            pip.transform.SetParent(transform, false);
            float wx = -totalW * 0.5f + i * pipWorldSpacing + pipWorldOffset.x;
            pip.transform.localPosition = new Vector3(wx / s.x, pipWorldOffset.y / s.y, pipWorldOffset.z / s.z);
            pip.transform.localScale    = new Vector3(pipWorldSize / s.x, pipWorldSize / s.y, (pipWorldSize * 0.4f) / s.z);
            Renderer r = pip.GetComponent<Renderer>();
            if (r != null) r.material.color = pipColor;
            hpPips[i] = pip;
        }
        UpdateHpPips();
    }

    private void UpdateHpPips()
    {
        if (hpPips == null) return;
        for (int i = 0; i < hpPips.Length; i++)
            if (hpPips[i] != null && hpPips[i].activeSelf != (i < currentHp))
                hpPips[i].SetActive(i < currentHp);
    }

    private void ClearHpPips()
    {
        if (hpPips == null) return;
        foreach (var p in hpPips) if (p != null) Destroy(p);
        hpPips = null;
    }

    private void RefreshColor()
    {
        if (blockRenderer == null) return;
        blockRenderer.material.color = blockType switch
        {
            BlockType.Hard      => hardColor,
            BlockType.Absorb    => absorbColor,
            BlockType.Explosive => explosiveColor,
            BlockType.Item      => itemColor,
            _                   => normalColor
        };
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("BallTag")) return;

        BallScript ball = collision.gameObject.GetComponent<BallScript>();

        // コンボは破壊時に加算（DESIGN.md 5.8, OnDestroyed → RegisterBlockDestroyed）。接触では加算しない。

        // ブロック衝突 SE（アリーナごと 50ms クールダウン, DESIGN.md 10.4）
        if (ball != null)
            AudioManager.Instance?.PlayBlockHit((int)blockType, ball.playerIndex);

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
        UpdateHpPips();

        if (currentHp <= 0)
            OnDestroyed(ball);
        // TODO: Phase 4でHPに応じた見た目の変化を追加
    }

    private void OnDestroyed(BallScript ball)
    {
        // 同一フレーム内の追撃（マルチボール/貫通/連鎖）で二重発火するとコンボ・スコア・破壊数・
        // ドロップ・爆発が重複する。Destroy は遅延実行なのでフラグで一度だけに保証する。
        if (destroyed) return;
        destroyed = true;

        // ブロック破壊 SE（Explosive は専用音, DESIGN.md 10.4）
        AudioManager.Instance?.PlayBlockBreak(blockType == BlockType.Explosive, ball != null ? ball.playerIndex : 0);

        // コンボ加算 → スコア加算の順（AddScore が更新後コンボで scoreComboMul を計算する）
        if (ball != null && GameManager.Instance != null)
        {
            int score = blockType == BlockType.Hard ? hardScore : normalScore;
            GameManager.Instance.RegisterBlockDestroyed(ball.playerIndex);
            GameManager.Instance.AddScore(ball.playerIndex, score);
        }

        // 爆発ブロック：周囲のブロックに巻き込みダメージ（DESIGN.md 5.4）。
        // 巻き込まれた Block が HP0 になると OnDestroyed が走り、それが Explosive なら
        // 同期的に連鎖爆発する（各ブロックは destroyed フラグで一度だけ処理）。
        // 巻き込みで倒したブロックのスコア/コンボは、各 OnDestroyed が個別に加算する。
        if (blockType == BlockType.Explosive)
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var col in nearby)
            {
                Block nearBlock = col.GetComponent<Block>();
                if (nearBlock != null && nearBlock != this && !nearBlock.destroyed)
                    nearBlock.TakeDamage(explosionDamage, ball);
            }

            float mul = ball?.GetAttributeMultiplier() ?? 1f;
            GetArena()?.TriggerHitStop(Mathf.RoundToInt(explosiveHitFrames * mul), shake: true);
        }

        if (ball != null) TryDropItem(ball);
        Destroy(gameObject);
    }

    // 妨害行の着弾フラッシュ（BlockSpawner の AttackAddRow 演出から呼ばれる, DESIGN.md 6.3）
    private Coroutine impactRoutine;
    public void FlashImpact(Color color, float duration)
    {
        if (blockRenderer == null) return;
        if (impactRoutine != null) StopCoroutine(impactRoutine);
        impactRoutine = StartCoroutine(ImpactRoutine(color, duration));
    }
    private System.Collections.IEnumerator ImpactRoutine(Color color, float duration)
    {
        blockRenderer.material.color = color;
        yield return new WaitForSeconds(duration);
        RefreshColor();
        impactRoutine = null;
    }

    public void HardenToHp(int targetHp)
    {
        blockType = BlockType.Hard;
        hp        = targetHp;
        currentHp = targetHp;
        // 妨害 Harden で変換されたブロックは金色で通常 Hard と区別する
        if (blockRenderer != null)
            blockRenderer.material.color = hardenedColor;
        BuildHpPips(); // 硬化で HP>1 になったので残耐久ドットを生成
    }

    private void TryDropItem(BallScript ball)
    {
        // BlockItem は確定ドロップ（確率判定をスキップ, DESIGN.md 12.17）
        bool guaranteed = blockType == BlockType.Item;

        if (!guaranteed)
        {
            float dropChance = baseDropChance;
            if (GameManager.Instance != null)
                dropChance *= GameManager.Instance.GetCurrentBand(ball.playerIndex).itemDropMul
                            * GameManager.Instance.GetItemDropComboMul(ball.playerIndex);
            if (Random.value > dropChance) return;
        }

        float bias = GameManager.Instance != null
            ? GameManager.Instance.GetCurrentBand(ball.playerIndex).goodItemBias
            : 0f;
        ItemType type = SelectRandomItemType(bias, trapDisguiseChance);

        // ドロップ過多抑制: 抽選結果の持続効果スロットが既に有効なら再抽選。
        // maxSlotRerolls 回試しても解消しなければ、通常ブロックはスキップ。
        // ただし BlockItem は「確定で 1 個」なのでスキップせず最後の抽選結果を出す（DESIGN.md 12.17）。
        // Heal / Attack 系はスロット None なので抑制対象外。
        if (GameManager.Instance != null)
        {
            int rerolls = 0;
            while (GameManager.Instance.IsEffectSlotActive(ball.playerIndex, ItemDefinition.GetEffectSlot(type)))
            {
                if (++rerolls > maxSlotRerolls)
                {
                    if (guaranteed) break;   // 確定ドロップはスキップしない
                    return;
                }
                type = SelectRandomItemType(bias, trapDisguiseChance);
            }
        }

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
    private static readonly ItemType[] TrapPool = {
        ItemType.Shrink, ItemType.Hyper, ItemType.Reversed
    };

    private static ItemType SelectRandomItemType(float goodItemBias, float trapDisguiseChance)
    {
        float buffWeight = Mathf.Clamp01(BASE_BUFF_WEIGHT + goodItemBias);
        if (Random.value < buffWeight)
        {
            // 強化枠だが、一部は「強化に偽装した罠」として出る（DESIGN.md 5.5.3）
            ItemType[] pool = Random.value < trapDisguiseChance ? TrapPool : BuffPool;
            return pool[Random.Range(0, pool.Length)];
        }
        return AttackPool[Random.Range(0, AttackPool.Length)];
    }

    private ArenaController GetArena()
    {
        // Block → BlockSpawner (parent) → Arena root (grandparent) → find ArenaController in children
        return transform.parent?.parent?.GetComponentInChildren<ArenaController>();
    }
}
