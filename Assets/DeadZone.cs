using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [Header("プレイヤー紐付け")]
    [SerializeField] private int playerIndex = 1;

    [Header("ボール初期位置オフセット（パドル上端からの距離）")]
    [SerializeField] private float ballSpawnOffsetY = 1f;

    private void Awake()
    {
        // 共有設定があれば ballSpawnOffsetY を共通化（ArenaController と同値であるべき, null セーフ）
        var c = ArenaSharedConfig.Instance;
        if (c != null) ballSpawnOffsetY = c.ballSpawnOffsetY;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("BallTag")) return;

        BallScript ball = other.GetComponent<BallScript>();

        // 追加ボール（SkillBall_Multi）は落下してもペナルティなし
        if (ball != null && ball.isExtraBall)
        {
            Object.Destroy(ball.gameObject);
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnBallDropped(playerIndex);

        if (ball != null)
            ball.PrepareRespawn(GetRespawnPos());
    }

    // パドルのローカル位置から動的にリスポーン位置を計算する
    private Vector3 GetRespawnPos()
    {
        // DeadZone の親 = Arena ルート。その中から PlayerController を探す
        Transform arenaRoot = transform.parent ?? transform;
        PlayerController pc = arenaRoot.GetComponentInChildren<PlayerController>();
        float paddleY = pc != null ? pc.transform.localPosition.y : -3.7f;
        return new Vector3(0f, paddleY + ballSpawnOffsetY, 0f);
    }
}
