using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BlockSpawner : MonoBehaviour, IFreezable
{
    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    [Header("ブロック設定")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private int   blocksPerRow = 7;
    [SerializeField] private float blockWidth   = 1.5f;
    [SerializeField] private float blockGap     = 0.1f;
    [SerializeField] private float blockHeight  = 0.7f;

    [Header("スポーン・降下設定（ローカル座標）")]
    [SerializeField] private float spawnY         = 4.5f;
    [SerializeField] private float blockDeadZoneY = -4.5f; // ブロックがここを下回ったら破棄してダメージ

    [Header("Dynamic Escalation（DESIGN.md 5.4.1・ラウンド経過で増圧）")]
    [SerializeField] private float spawnIntervalBase         = 5.0f;  // ラウンド開始時のスポーン間隔
    [SerializeField] private float spawnIntervalDecayPerMin  = 0.2f;  // 1分ごとに縮む量
    [SerializeField] private float spawnIntervalMin          = 3.0f;  // 間隔の下限
    [SerializeField] private float descentSpeedBase          = 0.3f;  // ラウンド開始時の降下速度
    [SerializeField] private float descentSpeedGainPerMin    = 0.03f; // 1分ごとに増える量
    [SerializeField] private float descentSpeedMax           = 0.45f; // 降下速度の上限

    [Header("通常行のブロック種出現率")]
    [Range(0f, 1f)] [SerializeField] private float explosiveBlockChance = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float hardBlockChance      = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float itemBlockChance      = 0.08f; // 確定ドロップブロック（DESIGN.md 5.4/12.17）
    [SerializeField] private int hardBlockHp = 2;

    [Header("妨害行設定（Hard/Absorb）")]
    [Range(0f, 1f)] [SerializeField] private float sabotageHardRatio = 0.5f;
    [SerializeField] private int sabotageBlockHp = 2;

    [Header("妨害 Harden 設定")]
    [SerializeField] private int hardenCount    = 3;
    [SerializeField] private int hardenTargetHp = 3;

    [Header("ブロックDeadZone到達時ヒットストップ")]
    [SerializeField] private int  blockDeadZoneHitFrames = 5;
    [SerializeField] private bool blockDeadZoneHitShake  = true;

    [Header("妨害行 着弾演出（AttackAddRow, DESIGN.md 6.3）")]
    [SerializeField] private float addRowSlideDistance  = 6f;    // 上空からの落下投下距離
    [SerializeField] private float addRowSlideDuration  = 0.3f;  // 滑り込み秒
    [SerializeField] private int   addRowImpactFrames   = 2;     // 着弾ヒットストップ（フレーム）
    [SerializeField] private Color addRowImpactFlash    = Color.white; // 着弾点フラッシュ色
    [SerializeField] private float addRowImpactFlashSec = 0.1f;

    // スライドイン中のブロックは通常降下から除外する
    private readonly HashSet<Block> slidingBlocks = new HashSet<Block>();

    private List<Block> allBlocks = new List<Block>();
    private float spawnTimer = 0f;
    private float roundElapsedTime = 0f;  // ラウンド開始からの経過秒（Escalation 算出用）
    private int   pendingSabotageRows = 0;
    private bool  frozen = false;

    private enum RowType { Normal, Sabotage }

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
        SpawnRow();
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
            SpawnRow();
        }

        if (pendingSabotageRows > 0 && IsTopClear())
        {
            pendingSabotageRows--;
            int start = allBlocks.Count;
            SpawnRow(RowType.Sabotage);
            // 落下投下演出: 生成行を上空へずらしてスライドインさせる（着地で SE/フラッシュ/ヒットストップ）
            var row = new List<Block>();
            for (int i = start; i < allBlocks.Count; i++)
            {
                Block b = allBlocks[i];
                if (b == null) continue;
                Vector3 p = b.transform.localPosition;
                p.y += addRowSlideDistance;
                b.transform.localPosition = p;
                slidingBlocks.Add(b);
                row.Add(b);
            }
            StartCoroutine(SlideInSabotageRow(row));
        }

        DescendBlocks();
        CheckBottomReached();
    }

    private void SpawnRow(RowType rowType = RowType.Normal)
    {
        if (blockPrefab == null)
        {
            Debug.LogError("BlockPrefabが設定されていません！");
            return;
        }

        float spacing    = blockWidth + blockGap;
        float totalWidth = (blocksPerRow - 1) * spacing;
        float startX     = -totalWidth / 2f;

        for (int i = 0; i < blocksPerRow; i++)
        {
            float x = startX + i * spacing;
            Vector3 localPos = new Vector3(x, spawnY, 0f);

            GameObject blockGO = Instantiate(blockPrefab, transform);
            blockGO.transform.localPosition = localPos;

            Block blockScript = blockGO.GetComponent<Block>();
            if (blockScript == null) continue;

            switch (rowType)
            {
                case RowType.Sabotage: ApplySabotageRowSettings(blockScript); break;
                default:               ApplyNormalRowSettings(blockScript);   break;
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

    // 妨害行の落下投下演出（DESIGN.md 6.3）: 上空 → spawnY へ addRowSlideDuration 秒で滑り込み、
    // 着地で SE + 着弾フラッシュ + 小ヒットストップ。スライド中は DescendBlocks 対象外。
    private IEnumerator SlideInSabotageRow(List<Block> row)
    {
        int n = row.Count;
        float[] targetY = new float[n];
        for (int i = 0; i < n; i++)
            targetY[i] = row[i] != null ? row[i].transform.localPosition.y - addRowSlideDistance : 0f;

        float t = 0f;
        while (t < addRowSlideDuration)
        {
            float k = t / addRowSlideDuration;
            for (int i = 0; i < n; i++)
            {
                Block b = row[i];
                if (b == null) continue;
                Vector3 p = b.transform.localPosition;
                p.y = Mathf.Lerp(targetY[i] + addRowSlideDistance, targetY[i], k);
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
            b.FlashImpact(addRowImpactFlash, addRowImpactFlashSec);
        }
        if (anyAlive)
        {
            AudioManager.Instance?.PlayAddRowLand(playerIndex);                 // 着地 SE（DESIGN.md 10.4）
            GetArena()?.TriggerHitStop(addRowImpactFrames, shake: true);        // 小ヒットストップ
        }
    }

    private void ApplySabotageRowSettings(Block blockScript)
    {
        blockScript.blockType = Random.value < sabotageHardRatio
            ? BlockType.Hard
            : BlockType.Absorb;
        blockScript.hp = sabotageBlockHp;
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
            GetArena()?.TriggerHitStop(blockDeadZoneHitFrames, shake: blockDeadZoneHitShake);
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
        // BlockSpawner → Arena root → ArenaController（兄弟ノード）
        return transform.parent?.GetComponentInChildren<ArenaController>();
    }
}
