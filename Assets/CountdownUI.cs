using TMPro;
using UnityEngine;

// ラウンド開始前のカウントダウン表示（3,2,1,GO!）。GameState.Countdown の間だけ表示。
// _Base にアタッチ。GameManager.CountdownLabel を毎フレーム流し込む。null 安全。
public class CountdownUI : MonoBehaviour
{
    [SerializeField] private GameObject      root;          // 表示/非表示の親（任意。無ければ countdownText を出し入れ）
    [SerializeField] private TextMeshProUGUI countdownText; // "3" / "2" / "1" / "GO!"

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isCountdown = GameManager.Instance.GetCurrentState() == GameManager.GameState.Countdown;

        GameObject toggle = root != null ? root
                          : (countdownText != null ? countdownText.gameObject : null);
        if (toggle != null && toggle.activeSelf != isCountdown) toggle.SetActive(isCountdown);

        if (isCountdown && countdownText != null)
            countdownText.text = GameManager.Instance.CountdownLabel;
    }
}
