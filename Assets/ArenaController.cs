using UnityEngine;

// 1つのアリーナをまとめて管理するコントローラ
public class ArenaController : MonoBehaviour
{
    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    [Header("アリーナサイズ（SpawnItem の底面計算に使用）")]
    [SerializeField] public float arenaHalfWidth  = 5f;
    [SerializeField] public float arenaHalfHeight = 4.5f;

    [Header("アリーナ内の主要オブジェクト")]
    [SerializeField] private BallScript ball;
    [SerializeField] private BlockSpawner spawner;

    [Header("ボール初期位置オフセット（パドル上端からの距離）")]
    [SerializeField] private float ballSpawnOffsetY = 1f;

    [Header("カメラ・ヒットストップ")]
    [SerializeField] private Camera arenaCamera;

    [Header("メトロノーム発射")]
    [SerializeField] private LaunchAimer launchAimer;
    private HitStopController hitStop;
    private SkillController   skillController;
    private PlayerController  cachedPlayer;
    private UIManager         cachedUIManager;

    // ArenaController は Arena の子なので、兄弟オブジェクトには親から辿る
    private Transform ArenaRoot => transform.parent != null ? transform.parent : transform;

    public BlockSpawner  GetSpawner()          => spawner;
    public BallScript    GetBall()             => ball;
    public SkillController GetSkillController() => skillController;

    public void SpawnExtraBall(float duration)
    {
        if (ball == null) return;
        GameObject extra = Object.Instantiate(ball.gameObject, ball.transform.parent);
        extra.name = "Ball_Extra";
        BallScript bs = extra.GetComponent<BallScript>();
        bs.isExtraBall = true; // Start() の自動発射をスキップ → コルーチンで発射
        StartCoroutine(LaunchExtraBallRoutine(bs, duration));
    }

    private System.Collections.IEnumerator LaunchExtraBallRoutine(BallScript bs, float duration)
    {
        yield return null; // BallScript.Start() が実行されるまで1フレーム待つ
        if (bs == null) yield break;
        bs.LaunchInDirection(new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0f));
        yield return new WaitForSeconds(duration);
        if (bs != null) Object.Destroy(bs.gameObject);
    }

    public void SpawnItem(Vector3 worldPos, ItemType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Item_" + type;
        go.transform.SetParent(ArenaRoot, worldPositionStays: true);
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * 0.4f;

        // アイテムはキネマティック運動なので Rigidbody は不要
        Object.Destroy(go.GetComponent<Rigidbody>());

        // ボールが当たって跳ね返らないようにトリガーにする
        go.GetComponent<Collider>().isTrigger = true;

        go.GetComponent<Renderer>().material.color = ItemDefinition.GetColor(type);

        go.AddComponent<ItemDrop>().Setup(type, playerIndex, this);
    }

    public void TriggerHitStop(int frames, bool strong = false, bool shake = true)
    {
        hitStop?.TriggerHitStop(frames, strong, shake);
    }

    void Awake()
    {
        skillController = gameObject.GetComponent<SkillController>()
                       ?? gameObject.AddComponent<SkillController>();
        skillController.Initialize(playerIndex, this);

        // ArenaController は Arena の子なので、兄弟の Player を探すには親から検索する
        cachedPlayer    = ArenaRoot.GetComponentInChildren<PlayerController>();
        cachedUIManager = Object.FindFirstObjectByType<UIManager>();

        hitStop = GetComponentInChildren<HitStopController>();
        if (hitStop != null)
        {
            hitStop.SetCamera(arenaCamera);
            if (ball         != null) hitStop.RegisterFreezable(ball);
            if (spawner      != null) hitStop.RegisterFreezable(spawner);
            if (cachedPlayer != null) hitStop.RegisterFreezable(cachedPlayer);
        }

        if (launchAimer != null)
            launchAimer.Initialize(ball, playerIndex, this);
    }

    public void ResetForNewRound()
    {
        if (spawner != null)
            spawner.ClearAndRespawn();

        if (ball != null)
            ball.PrepareRespawn(GetBallSpawnLocalPos());

        foreach (var zone in ArenaRoot.GetComponentsInChildren<ZonePoison>())
            Object.Destroy(zone.gameObject);
        foreach (var zone in ArenaRoot.GetComponentsInChildren<ZoneSlow>())
            Object.Destroy(zone.gameObject);
    }

    public Vector3 GetBallSpawnLocalPos()
    {
        float paddleY = cachedPlayer != null ? cachedPlayer.transform.localPosition.y : -3.7f;
        return new Vector3(0f, paddleY + ballSpawnOffsetY, 0f);
    }

    public float GetPaddleWorldY()
    {
        float localY = cachedPlayer != null ? cachedPlayer.transform.localPosition.y : -8f;
        return ArenaRoot.position.y + localY;
    }

    public void SpawnZonePoison(Vector3 worldPos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ZonePoison";
        go.transform.SetParent(ArenaRoot, worldPositionStays: true);
        go.transform.position   = worldPos;
        go.transform.localScale = Vector3.one * 0.9f;

        Object.Destroy(go.GetComponent<Rigidbody>());
        go.GetComponent<Collider>().isTrigger = true;
        go.GetComponent<Renderer>().material.color = new Color(0.5f, 0f, 0.8f, 0.7f);

        go.AddComponent<ZonePoison>().Setup(playerIndex, GetPaddleWorldY() + 0.5f);
    }

    public Vector3 GetRandomFloorWorldPos()
    {
        float x = ArenaRoot.position.x + Random.Range(-arenaHalfWidth * 0.8f, arenaHalfWidth * 0.8f);
        return new Vector3(x, ArenaRoot.position.y + 6f, ArenaRoot.position.z);
    }

    public void SpawnZoneSlow(Vector3 worldPos)
    {
        // ZoneSlow はアリーナ中央付近（root Y）に着地させてボール飛行ラインを塞ぐ
        float targetY = ArenaRoot.position.y;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ZoneSlow";
        go.transform.SetParent(ArenaRoot, worldPositionStays: true);
        go.transform.position   = worldPos;
        go.transform.localScale = Vector3.one * 1.5f;

        Object.Destroy(go.GetComponent<Rigidbody>());
        go.GetComponent<Collider>().isTrigger = true;
        go.GetComponent<Renderer>().material.color = new Color(0f, 0.8f, 0.7f, 0.6f);  // シアン

        go.AddComponent<ZoneSlow>().Setup(targetY);
    }

    public void HardenBlocks()
    {
        spawner?.HardenRandomBlocks();
    }

    public void ShowInterferenceOverlay(string label)
    {
        cachedUIManager?.ShowInterferenceOverlay(playerIndex, label);
    }
}
