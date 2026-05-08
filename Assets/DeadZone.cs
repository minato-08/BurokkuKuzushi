using UnityEngine;

public class DeadZone : MonoBehaviour
{
    // Inspectorから表示させたいCanvasをドラッグ&ドロップするための変数
    [SerializeField] private GameObject gameOverCanvas;

    void Start()
    {
        // ゲーム開始時はCanvasを非表示にする
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }
    }
    // 他のオブジェクトがこのセンサーに触れた瞬間に呼ばれる
    private void OnTriggerEnter(Collider other)
    {
        // 触れた相手のタグが「BallTag」なら実行
        if (other.CompareTag("BallTag"))
        {
            // Canvasを表示する
            if (gameOverCanvas != null)
            {
                gameOverCanvas.SetActive(true);
            }

            Time.timeScale = 0f; // ゲーム内の時間を止める
        }
    }
}