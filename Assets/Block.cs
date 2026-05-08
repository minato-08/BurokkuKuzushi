using UnityEngine;

public class Block : MonoBehaviour
{
    // ボールがぶつかったときに呼ばれる関数
    private void OnCollisionEnter(Collision collision)
    {
        // ぶつかってきた相手（collision.gameObject）のタグをチェック！
        if (collision.gameObject.tag == "BallTag")
        {
            // タグが「BallTag」だったら、自分自身（gameObject）を破棄する
            Destroy(gameObject);
        }
    }
}