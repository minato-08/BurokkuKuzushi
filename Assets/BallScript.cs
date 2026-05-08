using UnityEngine;

public class BallScript : MonoBehaviour
{
    public float speed = 7f; // ボールの速さ
    private Rigidbody rb;

    void Start()
    {
        // コンポーネント「Rigidbody」を取得して代入
        rb = GetComponent<Rigidbody>();

        // 最初は斜め上（右斜め上など）に発射する
        // 3Dなので Vector3 (3次元のベクトル) を使い、Z軸は0にしておく
        Vector3 initialDirection = new Vector3(1f, 1f, 0f).normalized;

        // 速度（velocity）を設定してボールを動かし始める
        rb.velocity = initialDirection * speed;
    }

    // 物理演算に関わる処理は Update ではなく FixedUpdate に書くのがルール！
    void FixedUpdate()
    {
        // 現在の速度を取得
         rb.velocity = rb.velocity.normalized * speed;

    }
}