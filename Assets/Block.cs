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
    [SerializeField] private int absorbScore = 25;     // DESIGN.md 5.x スコア表
    [SerializeField] private int explosiveScore = 30;  // DESIGN.md 5.4 L517（周囲巻き込みは各 OnDestroyed が別途加算）

    [Header("吸収設定")]
    [SerializeField] private float absorbSpeedMultiplier = 0.7f;

    [Header("爆発設定")]
    [SerializeField] private float explosionRadius    = 2f;
    [SerializeField] private int   explosionDamage    = 1;   // 爆発の巻き込みダメージ。Explosive 含む周囲ブロックへ適用（同 Explosive は連鎖発火）
    [SerializeField] private float explosionChainDelay = 0.07f; // 爆発が周囲へ伝播するまでの遅延（秒）。連鎖が一拍ずつ広がって見える（DESIGN.md 5.4）
    [SerializeField] private int   explosiveHitFrames = 6;  // Explosive 破壊の最低保証フレーム（ArenaSharedConfig で一元調整）
    // 通常衝突（Normal/Hard/Absorb）のヒットストップは BallScript.GetImpactFrames()（速度×攻撃力）に一本化。
    // 以前の per-type フレーム（全て0で死にパラメータ）は撤去（2026-06-03）。

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
    public bool IsDestroyed => destroyed; // 遅延爆発コルーチンが破壊済みブロックをスキップするために参照
    private GameObject[] hpPips;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
        ApplySharedConfig();
    }

    void Start()
    {
        currentHp = hp;
        RefreshColor();
        BuildHpPips();
    }

    // 共有設定があればヒットストップ系を一元値で上書き（null セーフ）。
    private void ApplySharedConfig()
    {
        var c = ArenaSharedConfig.Instance;
        if (c == null) return;
        explosiveHitFrames = c.explosiveHitFrames;
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

        // 属性ダメージ量と、この一撃でとどめ（破壊）になるか
        int damage = ball != null ? ball.GetDamage() : 1;
        bool willBreak = currentHp - damage <= 0;

        // ブロック衝突 SE。とどめの一撃は破壊音と競合するため鳴らさない（アリーナごと 50ms クールダウン, DESIGN.md 10.4）
        if (ball != null && !willBreak)
            AudioManager.Instance?.PlayBlockHit((int)blockType, ball.playerIndex);

        // 吸収ブロック：ボールを減速
        if (blockType == BlockType.Absorb)
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity *= absorbSpeedMultiplier;
        }

        // 衝突ヒットストップ（手応え＝速度×攻撃力。Explosive は破壊時に別途処理）
        if (blockType != BlockType.Explosive)
        {
            int frames = ball != null ? ball.GetImpactFrames() : 0;
            // 高速時（HYPER 等）はフリーズせずシェイクのみ＝止まらない爽快さ＋トレイルが途切れない
            if (frames > 0) GetArena()?.TriggerHitStop(frames, shake: true, freeze: ball.ShouldFreezeOnImpact());
        }

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
        AudioManager.Instance?.PlayBlockBreak((int)blockType, ball != null ? ball.playerIndex : 0);

        // コンボ加算 → スコア加算の順（AddScore が更新後コンボで scoreComboMul を計算する）
        if (ball != null && GameManager.Instance != null)
        {
            int score = blockType switch
            {
                BlockType.Hard      => hardScore,
                BlockType.Absorb    => absorbScore,
                BlockType.Explosive => explosiveScore,
                _                   => normalScore
            };
            GameManager.Instance.RegisterBlockDestroyed(ball.playerIndex);
            GameManager.Instance.AddScore(ball.playerIndex, score);
        }

        // 爆発ブロック：周囲のブロックに巻き込みダメージ（DESIGN.md 5.4）。
        // 巻き込みダメージは explosionChainDelay 秒「遅らせて」適用する＝連鎖が一拍ずつ外へ広がって見える。
        // 巻き込まれた Block が Explosive なら、その OnDestroyed が再び遅延スケジュールして波が伝播する
        // （各ブロックは destroyed フラグで一度だけ処理。倒したブロックのスコア/コンボは各 OnDestroyed が個別加算）。
        // コルーチンは破壊されるこのブロックではなく、永続する BlockSpawner 上で走らせる。
        if (blockType == BlockType.Explosive)
        {
            BlockSpawner spawner = GetComponentInParent<BlockSpawner>();
            if (spawner != null)
                spawner.ScheduleExplosion(transform.position, explosionRadius, explosionDamage, ball, explosionChainDelay);
            else
            {
                // スポーナー未取得時のフォールバック（即時）
                Collider[] nearby = Physics.OverlapSphere(transform.position, explosionRadius);
                foreach (var col in nearby)
                {
                    Block nearBlock = col.GetComponent<Block>();
                    if (nearBlock != null && nearBlock != this && !nearBlock.destroyed)
                        nearBlock.TakeDamage(explosionDamage, ball);
                }
            }

            // 破壊の手応え（速度×攻撃力）。最低でも explosiveHitFrames は確保してドラマ性を残す。
            int frames = ball != null
                ? Mathf.Max(ball.GetImpactFrames(), explosiveHitFrames)
                : explosiveHitFrames;
            // 高速時（HYPER 等）はフリーズせずシェイクのみ
            bool freeze = ball == null || ball.ShouldFreezeOnImpact();
            GetArena()?.TriggerHitStop(frames, shake: true, freeze: freeze);
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

    // EXPLOSION スキルで実行時に Explosive へ変換する（DESIGN.md 5.6）。
    // 爆発挙動・連鎖は既存の BlockType.Explosive 経路（OnDestroyed）をそのまま使う。
    public void ConvertToExplosive()
    {
        if (destroyed) return;
        blockType = BlockType.Explosive;
        hp        = 1;
        currentHp = 1;
        if (blockRenderer != null)
            blockRenderer.material.color = explosiveColor;
        BuildHpPips(); // HP1 なのでドットは消える
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
        // Arena ルート（transform.root）配下から ArenaController を探す。
        // ShakeRoot を挟んでも壊れないよう、親階層の深さに依存しない（2026-06-03）。
        return transform.root.GetComponentInChildren<ArenaController>();
    }
}
