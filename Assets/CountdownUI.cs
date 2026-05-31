using TMPro;
using UnityEngine;

// ラウンド開始前のカウントダウン表示（3,2,1,GO!）。GameState.Countdown の間だけ表示。
// _Base にアタッチ。複数の表示先（各アリーナ等）に同じ値を出せる。
// countdownTexts の各要素を Countdown 中だけ表示し、GameManager.CountdownLabel を流し込む。null 安全。
public class CountdownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] countdownTexts; // 各アリーナの数字（複数可）

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isCountdown = GameManager.Instance.GetCurrentState() == GameManager.GameState.Countdown;
        string label = isCountdown ? GameManager.Instance.CountdownLabel : "";

        if (countdownTexts == null) return;
        for (int i = 0; i < countdownTexts.Length; i++)
        {
            var t = countdownTexts[i];
            if (t == null) continue;
            if (t.gameObject.activeSelf != isCountdown) t.gameObject.SetActive(isCountdown);
            if (isCountdown) t.text = label;
        }
    }
}
