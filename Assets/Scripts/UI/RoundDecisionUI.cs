using TMPro;
using UnityEngine;

// ラウンド決着の瞬間に各アリーナへ大見出し（ROUND WIN! / ROUND OVER）を出す。
// GameManager.P1/P2DecisionLabel を読む片方向ポーリング（CountdownUI と同方式）。
// _Base にアタッチ。TMP 未バインドでも null 安全。
public class RoundDecisionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI p1DecisionText; // P1 アリーナ側の大見出し
    [SerializeField] private TextMeshProUGUI p2DecisionText; // P2 アリーナ側の大見出し

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        Apply(p1DecisionText, gm.P1DecisionLabel);
        Apply(p2DecisionText, gm.P2DecisionLabel);
    }

    // ラベルが非空のときだけ表示。CountdownUI と同じ「activeSelf を必要時だけ切替」方式で無駄な SetActive を避ける。
    private static void Apply(TextMeshProUGUI t, string label)
    {
        if (t == null) return;
        bool show = !string.IsNullOrEmpty(label);
        if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
        if (show) t.text = label;
    }
}
