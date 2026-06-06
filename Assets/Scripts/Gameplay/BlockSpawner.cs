using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BlockSpawner : MonoBehaviour, IFreezable
{
    [Header("Player / Prefab")]
    [SerializeField] public int playerIndex = 1;          // per-arena 固有
    [SerializeField] private GameObject blockPrefab;       // per-arena 参照

    // 以下のバランス値は ArenaSharedConfig で一元管理（ApplySharedConfig で取得）。未配置時は既定値。
    private int   blocksPerRow = 7;
    private float blockWidth   = 1.5f;
    private float blockGap     = 0.1f;
    private float blockHeight  = 0.7f;
    private float spawnY         = 4.5f;
    private float blockDeadZoneY = -4.5f; // ブロックがここを下回ったら破棄してダメージ
    private float spawnIntervalBase         = 5.0f;  // ラウンド開始時のスポーン間隔
    private float spawnIntervalDecayPerMin  = 0.2f;  // 1分ごとに縮む量
    private float spawnIntervalMin          = 3.0f;  // 間隔の下限
    private float descentSpeedBase          = 0.3f;  // ラウンド開始時の降下速度
    private float descentSpeedGainPerMin    = 0.03f; // 1分ごとに増える量
    private float descentSpeedMax           = 0.45f; // 降下速度の上限
    private float explosiveBlockChance = 0.1f;
    private float hardBlockChance      = 0.2f;
    private float itemBlockChance      = 0.08f; // 確定ドロップブロック（DESIGN.md 5.4/12.17）
    private float specialRowChance     = 0.125f;// スペシャル行（全Item/全Explosive/歯抜け, DESIGN.md 5.4）
    private int   hardBlockHp = 2;
    private int sabotageWeightNormal  = 2;
    private int sabotageWeightHardHp2 = 3;
    private int sabotageWeightHardHp3 = 4;
    private int sabotageWeightAbsorb  = 1;
    private int   hardenCount    = 3;
    private int   hardenTargetHp = 3;
    private int   blockDeadZoneHitFrames = 5;
    private bool  blockDeadZoneHitShake  = true;
    private float normalSlideDistance = 1.5f;  // 通常行 上からの滑り込み距離（小さめ）
    private float normalSlideDuration = 0.2f;  // 滑り込み秒
    private float addRowSlideDistance  = 6f;    // 妨害行 上空からの落下投下距離（大きめ）
    private float addRowSlideDuration  = 0.3f;  // 滑り込み秒
    private int   addRowImpactFrames   = 2;     // 着弾ヒットストップ（フレーム）
    private Color addRowImpactFlash    = Color.white; // 着弾点フラッシュ色
    private float addRowImpactFlashSec = 0.1f;

    // スライドイン中のブロックは通常降下から除外する
    private readonly HashSet<Block> slidingBlocks = new HashSet<Block>();

    private List<Block> allBlocks = new List<Block>();
    private float spawnTimer = 0f;
    private float roundElapsedTime = 0f;  // ラウンド開始からの経過秒（Escalation 算出用）
    private int   pendingSabotageRows = 0;
    private bool  frozen = false;

    private enum RowType { Normal, Sabotage }
    private enum SpecialKind { None, AllItem, AllExplosive, Gapped } // スペシャル行（DESIGN.md 5.4）

    // ラウンド経過時間から算出する実効値（毎フレーム再計算・基準値は上書きしない）
    private float CurrentSpawnInterval =>
        Mathf.Max(spawnIntervalMin, spawnIntervalBase - spawnIntervalDecayPerMin * (roundElapsedTime / 60f));
    private float CurrentDescentSpeed =>
        Mathf.Min(descentSpeedMax, descentSpeedBase + descentSpeedGainPerMin * (roundElapsedTime / 60f));

    public void Freeze()   => frozen = true;
    public void Unfreeze() => frozen = false;

    // LaunchAimer がブロック位置に基づいて自動発射時間を調整するために使用
    public float GetLowestBlockY()
    {
        float lowest = float.MaxValue;
        foreach (var block in allBlocks)
        {
            if (block == null) continue;
            float y = block.transform.localPosition.y;
            if (y < lowest) lowest = y;
        }
        return lowest == float.MaxValue ? spawnY : lowest;
    }

    public float GetSpawnY()        => spawnY;
    public float GetBlockDeadZoneY() => blockDeadZoneY;

    void Start()
    {
        ApplySharedConfig();
        SpawnRow();
    }

    // 共有設定（ArenaSharedConfig）があれば左右共通のパラメータを自分へ適用（null セーフ）。
    private void ApplySharedConfig()
    {
        var c = ArenaSharedConfig.Instance;
        if (c == null) return;
        blocksPerRow  = c.blocksPerRow;
        blockWidth    = c.blockWidth;
        blockGap      = c.blockGap;
        blockHeight   = c.blockHeight;
        spawnY        = c.spawnY;
        blockDeadZoneY = c.blockDeadZoneY;
        spawnIntervalBase        = c.spawnIntervalBase;
        spawnIntervalDecayPerMin = c.spawnIntervalDecayPerMin;
        spawnIntervalMin         = c.spawnIntervalMin;
        descentSpeedBase         = c.descentSpeedBase;
        descentSpeedGainPerMin   = c.descentSpeedGainPerMin;
        descentSpeedMax          = c.descentSpeedMax;
        explosiveBlockChance = c.explosiveBlockChance;
        hardBlockChance      = c.hardBlockChance;
        itemBlockChance      = c.itemBlockChance;
        specialRowChance     = c.specialRowChance;
        hardBlockHp          = c.hardBlockHp;
        sabotageWeightNormal  = c.sabotageWeightNormal;
        sabotageWeightHardHp2 = c.sabotageWeightHardHp2;
        sabotageWeightHardHp3 = c.sabotageWeightHardHp3;
        sabotageWeightAbsorb  = c.sabotageWeightAbsorb;
        hardenCount          = c.hardenCount;
        hardenTargetHp       = c.hardenTargetHp;
        blockDeadZoneHitFrames = c.blockDeadZoneHitFrames;
        blockDeadZoneHitShake  = c.blockDeadZoneHitShake;
        normalSlideDistance  = c.normalSlideDistance;
        normalSlideDuration  = c.normalSlideDuration;
        addRowSlideDistance  = c.addRowSlideDistance;
        addRowSlideDuration  = c.addRowSlideDuration;
        addRowImpactFrames   = c.addRowImpactFrames;
        addRowImpactFlash    = c.addRowImpactFlash;
        addRowImpactFlashSec = c.addRowImpactFlashSec;
    }

    void Update()
    {
        if (frozen) return;
        if (GameManager.Instance != null &&
            GameManager.Instance.GetCurrentState() != GameManager.GameState.Playing)
            return;

        roundElapsedTime += Time.deltaTime;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= CurrentSpawnInterval)
        {
            spawnTimer = 0f;
            // スペシャル行は妨害行予約が無いときのみ抽選（妨害優先, DESIGN.md 5.4）
            SpecialKind kind = (pendingSabotageRows == 0 && Random.value < specialRowChance)
                ? PickSpecialKind() : SpecialKind.None;
            // 通常/スペシャル行: 控えめにスライドイン（着弾演出なし）で「湧き」感を軽減
            SpawnRowWithSlide(RowType.Normal, normalSlideDistance, normalSlideDuration, impact: false, special: kind);
            if (kind != SpecialKind.None) AudioManager.Instance?.PlaySpecialRow(playerIndex);
        }

        if (pendingSabotageRows > 0 && IsTopClear())
        {
            pendingSabotageRows--;
            // 妨害行: 派手な落下投下（着地でフラッシュ/ヒットストップ/SE）で差別化
            SpawnRowWithSlide(RowType.Sabotage, addRowSlideDistance, addRowSlideDuration, impact: true);
        }

        DescendBlocks();
        CheckBottomReached();
    }

    private SpecialKind PickSpecialKind()
    {
        switch (Random.Range(0, 3))
        {
            case 0:  return SpecialKind.AllItem;
            case 1:  return SpecialKind.AllExplosive;
            default: return SpecialKind.Gapped;
        }
    }

    private void SpawnRow(RowType rowType = RowType.Normal, SpecialKind special = SpecialKind.None)
    {
        if (blockPrefab == null)
        {
            Debug.LogError("BlockPrefabが設定されていません！");
            return;
        }

        float spacing    = blockWidth + blockGap;
        float totalWidth = (blocksPerRow - 1) * spacing;
        float startX     = -totalWidth / 2f;

        // 歯抜け行: スキップする列を 2 つ選ぶ
        HashSet<int> gaps = null;
        if (special == SpecialKind.Gapped)
        {
            gaps = new HashSet<int>();
            int gapCount = Mathf.Min(2, blocksPerRow - 1);
            while (gaps.Count < gapCount) gaps.Add(Random.Range(0, blocksPerRow));
        }

        for (int i = 0; i < blocksPerRow; i++)
        {
            if (gaps != null && gaps.Contains(i)) continue; // 歯抜け

            float x = startX + i * spacing;
            Vector3 localPos = new Vector3(x, spawnY, 0f);

            GameObject blockGO = Instantiate(blockPrefab, transform);
            blockGO.transform.localPosition = localPos;

            Block blockScript = blockGO.GetComponent<Block>();
            if (blockScript == null) continue;

            switch (special)
            {
                case SpecialKind.AllItem:      blockScript.blockType = BlockType.Item;      blockScript.hp = 1; break;
                case SpecialKind.AllExplosive: blockScript.blockType = BlockType.Explosive;                     break;
                default:
                    if (rowType == RowType.Sabotage) ApplySabotageRowSettings(blockScript);
                    else                             ApplyNormalRowSettings(blockScript);
                    break;
            }

            allBlocks.Add(blockScript);
        }
    }

    private void ApplyNormalRowSettings(Block blockScript)
    {
        float rand = Random.value;
        if (rand < explosiveBlockChance)
        {
            blockScript.blockType = BlockType.Explosive;
        }
        else if (rand < explosiveBlockChance + hardBlockChance)
        {
            blockScript.blockType = BlockType.Hard;
            blockScript.hp        = hardBlockHp;
        }
        else if (rand < explosiveBlockChance + hardBlockChance + itemBlockChance)
        {
            blockScript.blockType = BlockType.Item; // HP1・破壊で確定ドロップ
            blockScript.hp        = 1;
        }
    }

    // 行を spawnY に生成 → 上空へずらして SlideInRow でスライドインさせる。
    // impact=true（妨害行）は着地でフラッシュ/ヒットストップ/SE。impact=false（通常行）は控えめ。
    private void SpawnRowWithSlide(RowType type, float distance, float duration, bool impact, SpecialKind special = SpecialKind.None)
    {
        int start = allBlocks.Count;
        SpawnRow(type, special);
        var row = new List<Block>();
        for (int i = start; i < allBlocks.Count; i++)
        {
            Block b = allBlocks[i];
            if (b == null) continue;
            Vector3 p = b.transform.localPosition;
            p.y += distance;
            b.transform.localPosition = p;
            slidingBlocks.Add(b);
            row.Add(b);
        }
        StartCoroutine(SlideInRow(row, distance, duration, impact));
    }

    // 上空 → spawnY へ duration 秒で ease-out 滑り込み（スライド中は DescendBlocks 対象外）。
    private IEnumerator SlideInRow(List<Block> row, float distance, float duration, bool impact)
    {
        int n = row.Count;
        float[] targetY = new float[n];
        for (int i = 0; i < n; i++)
            targetY[i] = row[i] != null ? row[i].transform.localPosition.y - distance : 0f;

        float t = 0f;
        while (t < duration)
        {
            float k = t / duration;
            float e = 1f - (1f - k) * (1f - k); // ease-out（着地に向けて減速＝「収まる」感）
            for (int i = 0; i < n; i++)
            {
                Block b = row[i];
                if (b == null) continue;
                Vector3 p = b.transform.localPosition;
                p.y = Mathf.Lerp(targetY[i] + distance, targetY[i], e);
                b.transform.localPosition = p;
            }
            t += Time.deltaTime;
            yield return null;
        }

        bool anyAlive = false;
        for (int i = 0; i < n; i++)
        {
            Block b = row[i];
            if (b == null) continue;
            anyAlive = true;
            Vector3 p = b.transform.localPosition; p.y = targetY[i]; b.transform.localPosition = p;
            slidingBlocks.Remove(b);
            if (impact) b.FlashImpact(addRowImpactFlash, addRowImpactFlashSec);
        }
        if (anyAlive && impact)
        {
            AudioManager.Instance?.PlayAddRowLand(playerIndex);                 // 着地 SE（DESIGN.md 10.4）
            // ボール衝突でない演出なのでフリーズせずシェイクのみ（飛行中ボールを止めない, DESIGN.md 5.x）
            GetArena()?.TriggerHitStop(addRowImpactFrames, shake: true, freeze: false);
        }
    }

    // 妨害行の 1 ブロックを重み付き抽選で決める（Normal / Hard HP2 / Hard HP3 / Absorb）。
    private void ApplySabotageRowSettings(Block blockScript)
    {
        int total = sabotageWeightNormal + sabotageWeightHardHp2
                  + sabotageWeightHardHp3 + sabotageWeightAbsorb;
        if (total <= 0) { blockScript.blockType = BlockType.Hard; blockScript.hp = 2; return; } // 安全フォールバック

        int r = Random.Range(0, total); // 0..total-1
        if      ((r -= sabotageWeightNormal)  < 0) { blockScript.blockType = BlockType.Normal; blockScript.hp = 1; }
        else if ((r -= sabotageWeightHardHp2) < 0) { blockScript.blockType = BlockType.Hard;   blockScript.hp = 2; }
        else if ((r -= sabotageWeightHardHp3) < 0) { blockScript.blockType = BlockType.Hard;   blockScript.hp = 3; }
        else                                       { blockScript.blockType = BlockType.Absorb; blockScript.hp = 1; }
    }

    private void DescendBlocks()
    {
        float step = CurrentDescentSpeed * Time.deltaTime;

        for (int i = allBlocks.Count - 1; i >= 0; i--)
        {
            if (allBlocks[i] == null)
            {
                allBlocks.RemoveAt(i);
                continue;
            }
            if (slidingBlocks.Contains(allBlocks[i])) continue; // スライドイン中は降下しない
            allBlocks[i].transform.localPosition -= new Vector3(0f, step, 0f);
        }
    }

    private void CheckBottomReached()
    {
        int reachedCount = 0;
        for (int i = allBlocks.Count - 1; i >= 0; i--)
        {
            Block block = allBlocks[i];
            if (block == null)
            {
                allBlocks.RemoveAt(i);
                continue;
            }

            if (block.transform.localPosition.y <= blockDeadZoneY)
            {
                reachedCount++;
                Destroy(block.gameObject);
                allBlocks.RemoveAt(i);
            }
        }

        if (reachedCount > 0)
        {
            GameManager.Instance?.OnBlocksReachedBottom(playerIndex, reachedCount);
            AudioManager.Instance?.PlayBlockBottom(playerIndex);  // 自ブロック底到達（被ペナルティ）SE
            // ボール衝突でないペナルティ演出なのでフリーズせずシェイクのみ（飛行中ボールを止めない, DESIGN.md 5.x）
            GetArena()?.TriggerHitStop(blockDeadZoneHitFrames, shake: blockDeadZoneHitShake, freeze: false);
        }
    }

    public void ReceiveSabotageRow() => pendingSabotageRows++;

    public void HardenRandomBlocks()
    {
        Block[] candidates = allBlocks
            .Where(b => b != null && b.blockType == BlockType.Normal)
            .OrderBy(_ => Random.value)
            .Take(hardenCount)
            .ToArray();

        foreach (Block b in candidates)
            b.HardenToHp(hardenTargetHp);
    }

    // Explosive の巻き込みダメージを delay 秒遅らせて適用する（Block.OnDestroyed から呼ばれる, DESIGN.md 5.4）。
    // 破壊される Block 自身ではなく永続する Spawner で走らせる。ClearAndRespawn の StopAllCoroutines で
    // ラウンド遷移時に自動キャンセルされるので、跨いだ爆発は残らない。
    public void ScheduleExplosion(Vector3 worldPos, float radius, int damage, BallScript ball, float delay)
    {
        StartCoroutine(ExplosionRoutine(worldPos, radius, damage, ball, delay));
    }

    private System.Collections.IEnumerator ExplosionRoutine(Vector3 worldPos, float radius, int damage, BallScript ball, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Collider[] nearby = Physics.OverlapSphere(worldPos, radius);
        foreach (var col in nearby)
        {
            if (col == null) continue;
            Block b = col.GetComponent<Block>();
            if (b != null && !b.IsDestroyed)
                b.TakeDamage(damage, ball); // Explosive ならここで OnDestroyed → 再び遅延スケジュールして連鎖が伝播
        }
    }

    // EXPLOSION スキル: 盤面の現在ブロック数の fraction 割合をランダムに Explosive へ変換する（DESIGN.md 5.6）。
    // 既に Explosive のブロックも母集団に含む（選ばれても同状態への再設定で無害）。
    public void ConvertRandomToExplosive(float fraction)
    {
        Block[] living = allBlocks.Where(b => b != null).ToArray();
        int count = Mathf.RoundToInt(living.Length * Mathf.Clamp01(fraction));
        if (count <= 0) return;

        foreach (Block b in living.OrderBy(_ => Random.value).Take(count))
            b.ConvertToExplosive();
    }

    private bool IsTopClear()
    {
        foreach (var block in allBlocks)
        {
            if (block == null) continue;
            if (block.transform.localPosition.y > spawnY - blockHeight)
                return false;
        }
        return true;
    }

    public void ClearAndRespawn()
    {
        StopAllCoroutines();       // 進行中のスライドイン演出を停止
        slidingBlocks.Clear();
        foreach (var block in allBlocks)
        {
            if (block != null) Destroy(block.gameObject);
        }
        allBlocks.Clear();
        spawnTimer          = 0f;
        roundElapsedTime    = 0f;  // Escalation をラウンドごとにリセット
        pendingSabotageRows = 0;
        SpawnRow();
    }

    private ArenaController GetArena()
    {
        // Arena ルート（transform.root）配下から ArenaController を探す。
        // ShakeRoot を挟んでも壊れないよう、親階層の深さに依存しない（2026-06-03）。
        return transform.root.GetComponentInChildren<ArenaController>();
    }
}
