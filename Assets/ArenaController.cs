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

    public BlockSpawner  GetSpawner()        => spawner;
    public BallScript    GetBall()           => ball;
    public SkillController GetSkillController() => skillController;

    // SkillBall_Multi 用：既存ボールを複製して追加ボールを生成する
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
        Transform arenaRoot = transform.parent ?? transform;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Item_" + type;
        go.transform.SetParent(arenaRoot, worldPositionStays: true);
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
        // SkillController を自動生成（同 GameObject 上）
        skillController = gameObject.GetComponent<SkillController>()
                       ?? gameObject.AddComponent<SkillController>();
        skillController.Initialize(playerIndex, this);

        hitStop = GetComponentInChildren<HitStopController>();
        if (hitStop != null)
        {
            hitStop.SetCamera(arenaCamera);
            if (ball    != null) hitStop.RegisterFreezable(ball);
            if (spawner != null) hitStop.RegisterFreezable(spawner);
            // ArenaController は Arena の子なので、兄弟の Player を探すには親から検索する
            Transform arenaRoot = transform.parent ?? transform;
            PlayerController pc = arenaRoot.GetComponentInChildren<PlayerController>();
            if (pc != null) hitStop.RegisterFreezable(pc);
        }

        if (launchAimer != null)
            launchAimer.Initialize(ball, playerIndex, this);
    }

    // ラウンド開始時に呼ばれるリセット処理
    public void ResetForNewRound()
    {
        if (spawner != null)
            spawner.ClearAndRespawn();

        if (ball != null)
            ball.PrepareRespawn(GetBallSpawnLocalPos());
    }

    // パドルのローカル位置から動的にボール初期位置を計算する
    public Vector3 GetBallSpawnLocalPos()
    {
        Transform arenaRoot = transform.parent ?? transform;
        PlayerController pc = arenaRoot.GetComponentInChildren<PlayerController>();
        float paddleY = pc != null ? pc.transform.localPosition.y : -3.7f;
        return new Vector3(0f, paddleY + ballSpawnOffsetY, 0f);
    }
}
