using UnityEngine;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    [Header("ブロック設定")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private int blocksPerRow = 7;
    [SerializeField] private float blockWidth = 1.5f;   // ブロック1個の横幅
    [SerializeField] private float blockGap   = 0.1f;   // ブロック間の隙間
    [SerializeField] private float blockHeight = 0.7f;

    [Header("スポーン・降下設定（ローカル座標）")]
    [SerializeField] private float spawnY = 4.5f;
    [SerializeField] private float bottomY = -4.5f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float descentSpeed = 0.3f;

    [Header("通常行のブロック種出現率")]
    [Range(0f, 1f)] [SerializeField] private float explosiveBlockChance = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float hardBlockChance = 0.2f;
    [SerializeField] private int hardBlockHp = 2;

    [Header("妨害行設定")]
    [Range(0f, 1f)] [SerializeField] private float sabotageHardRatio = 0.5f;
    [SerializeField] private int sabotageBlockHp = 2;

    private List<GameObject> allBlocks = new List<GameObject>();
    private float spawnTimer = 0f;
    private int pendingSabotageRows = 0; // 受信済みだがまだ生成していない妨害行の数

    void Start()
    {
        // ゲーム開始時に最初の行を生成
        SpawnRow();
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
        // 中心間距離 = ブロック幅 + 隙間
        float spacing   = blockWidth + blockGap;
        float totalWidth = (blocksPerRow - 1) * spacing;
        float startX    = -totalWidth / 2f;

        for (int i = 0; i < blocksPerRow; i++)
        {
            float x = startX + i * spacing;
            Vector3 localPos = new Vector3(x, spawnY, 0f);

            GameObject block = Instantiate(blockPrefab, transform);
            block.transform.localPosition = localPos;

            Block blockScript = block.GetComponent<Block>();
            if (blockScript == null) continue;

            if (isSabotage)
                ApplySabotageRowSettings(blockScript);
            else
                ApplyNormalRowSettings(blockScript);

            allBlocks.Add(block);
        }
    }

    // 通常行：確率で各ブロックの種類を決める
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
            blockScript.hp = hardBlockHp;
        }
        // それ以外はNormalのまま
    }

    // 妨害行：硬い or 吸収ブロックをランダムで配置
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

        // 後ろから走査することで、削除時にインデックスがずれない
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

    private void CheckBottomReached()
    {
        foreach (var block in allBlocks)
        {
            if (block == null) continue;
            if (block.transform.localPosition.y <= bottomY)
            {
                GameManager.Instance?.OnBlocksReachedBottom(playerIndex);
                return;
            }
        }
    }

    // ArenaController からサイズを受け取って自動設定する
    public void ConfigureFromArena(float halfWidth, float halfHeight)
    {
        spawnY  =  halfHeight;
        bottomY = -halfHeight;

        // ブロック幅をアリーナ幅から逆算
        // 全ブロックが halfWidth*2 に収まるように spacing を決める
        if (blocksPerRow > 1)
        {
            float spacing = (halfWidth * 2f) / (blocksPerRow - 1);
            blockWidth = Mathf.Max(0.1f, spacing - blockGap);
        }
    }

    // PVP干渉：相手から妨害行を受け取る
    // 即時生成ではなくキューに積む → スポーン位置が空いてから生成される
    public void ReceiveSabotageRow()
    {
        pendingSabotageRows++;
    }

    // スポーン位置（spawnY付近）にブロックがないか確認する
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
        SpawnRow();
    }
}
