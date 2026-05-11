using UnityEngine;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    [Header("ブロック設定")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private int   blocksPerRow = 7;
    [SerializeField] private float blockWidth   = 1.5f;   // ブロック1個の横幅
    [SerializeField] private float blockGap     = 0.1f;   // ブロック間の隙間
    [SerializeField] private float blockHeight  = 0.7f;

    [Header("スポーン・降下設定（ローカル座標）")]
    [SerializeField] private float spawnY        = 4.5f;
    [SerializeField] private float bottomY       = -4.5f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float descentSpeed  = 0.3f;

    [Header("通常行のブロック種出現率")]
    [Range(0f, 1f)] [SerializeField] private float explosiveBlockChance = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float hardBlockChance      = 0.2f;
    [SerializeField] private int hardBlockHp = 2;

    [Header("妨害行設定")]
    [Range(0f, 1f)] [SerializeField] private float sabotageHardRatio = 0.5f;
    [SerializeField] private int sabotageBlockHp = 2;

    private List<GameObject> allBlocks = new List<GameObject>();
    private float spawnTimer = 0f;
    private int   pendingSabotageRows = 0; // 受信済みだがまだ生成していない妨害行の数

    void Start()
    {
        // ArenaController から再度サイズ取得して、Profile 反映後の値で再計算する
        // （Awake 順序によっては ConfigureFromArena 時点で GameManager.Instance が
        //   まだ null の可能性があるため、Start で確実にやり直す）
        ArenaController arena = GetComponentInParent<ArenaController>();
        if (arena != null)
        {
            ConfigureFromArena(arena.arenaHalfWidth, arena.arenaHalfHeight);
        }
        else
        {
            ApplyProfile();
        }

        // ゲーム開始時に最初の行を生成
        SpawnRow();
    }

    // GameBalanceProfile の BlockSpawnSettings を読み込んで自身のフィールドに反映する
    private void ApplyProfile()
    {
        var profile = GameManager.Instance?.Profile;
        if (profile == null) return;

        var bs = profile.blockSpawn;
        blocksPerRow         = bs.blocksPerRow;
        blockGap             = bs.blockGap;
        blockHeight          = bs.blockHeight;
        spawnInterval        = bs.spawnInterval;
        descentSpeed         = bs.descentSpeed;
        explosiveBlockChance = bs.explosiveBlockChance;
        hardBlockChance      = bs.hardBlockChance;
        hardBlockHp          = bs.hardBlockHp;
        sabotageHardRatio    = bs.sabotageHardRatio;
        sabotageBlockHp      = bs.sabotageBlockHp;
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.GetCurrentState() != GameManager.GameState.Playing)
            return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRow();
        }

        // 妨害行はスポーン位置が空いてから1行ずつ生成する
        if (pendingSabotageRows > 0 && IsTopClear())
        {
            pendingSabotageRows--;
            SpawnRow(isSabotage: true);
        }

        DescendBlocks();
        CheckBottomReached();
    }

    private void SpawnRow(bool isSabotage = false)
    {
        if (blockPrefab == null)
        {
            Debug.LogError("BlockPrefabが設定されていません！");
            return;
        }

        // blocksPerRow個のブロックをX方向に均等配置
        float spacing    = blockWidth + blockGap;
        float totalWidth = (blocksPerRow - 1) * spacing;
        float startX     = -totalWidth / 2f;

        for (int i = 0; i < blocksPerRow; i++)
        {
            float x = startX + i * spacing;
            Vector3 localPos = new Vector3(x, spawnY, 0f);

            GameObject block = Instantiate(blockPrefab, transform);
            block.transform.localPosition = localPos;

            Block blockScript = block.GetComponent<Block>();
            if (blockScript == null) continue;

            if (isSabotage) ApplySabotageRowSettings(blockScript);
            else            ApplyNormalRowSettings(blockScript);

            allBlocks.Add(block);
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
        // それ以外は Normal のまま
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
        float step = descentSpeed * Time.deltaTime;

        for (int i = allBlocks.Count - 1; i >= 0; i--)
        {
            if (allBlocks[i] == null)
            {
                allBlocks.RemoveAt(i);
                continue;
            }
            allBlocks[i].transform.localPosition -= new Vector3(0f, step, 0f);
        }
    }

    // 底に到達したブロックを破棄し、その数を集計して GameManager に通知する
    // HP制移行に伴い「1個でも到達したら即ラウンド終了」から「到達数に応じてダメージ」に変更
    private void CheckBottomReached()
    {
        int reachedCount = 0;
        for (int i = allBlocks.Count - 1; i >= 0; i--)
        {
            GameObject blockObj = allBlocks[i];
            if (blockObj == null)
            {
                allBlocks.RemoveAt(i);
                continue;
            }

            if (blockObj.transform.localPosition.y <= bottomY)
            {
                reachedCount++;
                Destroy(blockObj);
                allBlocks.RemoveAt(i);
            }
        }

        if (reachedCount > 0)
        {
            GameManager.Instance?.OnBlocksReachedBottom(playerIndex, reachedCount);
        }
    }

    // ArenaController からサイズを受け取って自動設定する
    public void ConfigureFromArena(float halfWidth, float halfHeight)
    {
        // GameManager がいれば Profile を先に反映してから blockWidth を計算する
        ApplyProfile();

        spawnY  =  halfHeight;
        bottomY = -halfHeight;

        if (blocksPerRow > 1)
        {
            float spacing = (halfWidth * 2f) / (blocksPerRow - 1);
            blockWidth = Mathf.Max(0.1f, spacing - blockGap);
        }
    }

    // PVP干渉：相手から妨害行を受け取る（即時生成ではなくキューに積む）
    public void ReceiveSabotageRow()
    {
        pendingSabotageRows++;
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

    // 全ブロックを消去して最初の行を再生成（ラウンド開始時に使う）
    public void ClearAndRespawn()
    {
        foreach (var block in allBlocks)
        {
            if (block != null) Destroy(block);
        }
        allBlocks.Clear();
        spawnTimer = 0f;
        pendingSabotageRows = 0;
        SpawnRow();
    }
}
