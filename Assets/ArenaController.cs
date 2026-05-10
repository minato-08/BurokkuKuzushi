using UnityEngine;

// 1つのアリーナをまとめて管理するコントローラ
// ここのサイズ設定を変えるだけで、ブロック・パドル・デッドゾーンが全部追従する
public class ArenaController : MonoBehaviour
{
    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    [Header("アリーナサイズ ← ここを変えれば全体が追従")]
    [SerializeField] public float arenaHalfWidth  = 5f;   // 中心から左右の壁まで
    [SerializeField] public float arenaHalfHeight = 4.5f; // 中心から上壁・デッドゾーンまで
    [SerializeField] private float paddleMargin   = 0.8f; // パドルを下端から何ユニット上に置くか

    [Header("壁（設定すると位置が自動調整される）")]
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;
    [SerializeField] private Transform topWall;

    [Header("アリーナ内の主要オブジェクト")]
    [SerializeField] private BallScript ball;
    [SerializeField] private BlockSpawner spawner;

    [Header("ボールリスポーン設定")]
    [SerializeField] private float launchDelay = 1f;

    // ボールリスポーン位置（ConfigureArena で自動計算）
    private Vector3 ballSpawnLocalPos;

    public BlockSpawner GetSpawner() => spawner;

    void Awake()
    {
        ConfigureArena();
    }

    // アリーナサイズを全コンポーネントに反映する
    private void ConfigureArena()
    {
        // 壁の位置を自動設定（参照が設定されている場合のみ）
        if (leftWall)  leftWall.localPosition  = new Vector3(-arenaHalfWidth, leftWall.localPosition.y,  leftWall.localPosition.z);
        if (rightWall) rightWall.localPosition = new Vector3( arenaHalfWidth, rightWall.localPosition.y, rightWall.localPosition.z);
        if (topWall)   topWall.localPosition   = new Vector3(topWall.localPosition.x, arenaHalfHeight,   topWall.localPosition.z);

        // ボールリスポーン位置 = パドルの1ユニット上
        float paddleY     = -(arenaHalfHeight - paddleMargin);
        ballSpawnLocalPos = new Vector3(0f, paddleY + 1f, 0f);

        // BlockSpawner に伝える
        if (spawner != null)
            spawner.ConfigureFromArena(arenaHalfWidth, arenaHalfHeight);

        // PlayerController に伝える
        PlayerController pc = GetComponentInChildren<PlayerController>();
        if (pc != null)
            pc.ConfigureFromArena(arenaHalfWidth, arenaHalfHeight, paddleMargin);

        // DeadZone に伝える
        DeadZone dz = GetComponentInChildren<DeadZone>();
        if (dz != null)
            dz.ConfigureFromArena(arenaHalfHeight, ballSpawnLocalPos);
    }

    // ラウンド開始時に呼ばれるリセット処理
    public void ResetForNewRound()
    {
        if (spawner != null)
            spawner.ClearAndRespawn();

        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            ball.transform.localPosition = ballSpawnLocalPos;
            if (rb != null) rb.linearVelocity = Vector3.zero;

            ball.CancelInvoke();
            ball.Invoke(nameof(BallScript.Relaunch), launchDelay);
        }
    }
}
