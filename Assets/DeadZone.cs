using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [Header("プレイヤー紐付け")]
    [SerializeField] private int playerIndex = 1;

    [Header("ボールリスポーン設定")]
    // ボールが復活する位置（アリーナのローカル座標）
    [SerializeField] private Vector3 ballRespawnLocalPos = new Vector3(0f, -2f, 0f);
    // ボールが落ちてから再発射までの待機秒数
    [SerializeField] private float relaunchDelay = 1f;

    // ArenaController からサイズを受け取って自動設定する
    public void ConfigureFromArena(float halfHeight, Vector3 respawnPos)
    {
        // デッドゾーンをアリーナ下端の少し下に配置
        Vector3 pos = transform.localPosition;
        pos.y = -halfHeight - 0.5f;
        transform.localPosition = pos;

        ballRespawnLocalPos = respawnPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("BallTag")) return;

        // ペナルティ通知
        if (GameManager.Instance != null)
            GameManager.Instance.OnBallDropped(playerIndex);

        // ボールをアリーナ中央付近に戻して停止させる
        Rigidbody rb = other.GetComponent<Rigidbody>();
        BallScript ball = other.GetComponent<BallScript>();
        if (rb != null && ball != null)
        {
            other.transform.localPosition = ballRespawnLocalPos;
            rb.linearVelocity = Vector3.zero;

            // 一定秒数後にRelaunch()を呼ぶ
            ball.Invoke(nameof(BallScript.Relaunch), relaunchDelay);
        }
    }
}
