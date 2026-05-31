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

        // ラベルが出ている間だけ表示（GO! 表示中は Playing でも出し続ける）
        string label = GameManager.Instance.CountdownLabel;
        bool show = !string.IsNullOrEmpty(label);

        if (countdownTexts == null) return;
        for (int i = 0; i < countdownTexts.Length; i++)
        {
            var t = countdownTexts[i];
            if (t == null) continue;
            if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
            if (show) t.text = label;
        }
    }
}
