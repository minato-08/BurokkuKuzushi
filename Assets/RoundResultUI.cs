using TMPro;
using UnityEngine;

// ラウンド間の結果画面（GameState.RoundOver, マッチ未決着）。最終結果は MatchResultUI が担当。
// 数秒後に GameManager が自動で次ラウンドへ進める（入力は不要）。
// _Base にアタッチ。すべて null 安全（未バインドでも動く）。
//
// 表示:
//   p1RoundBanner / p2RoundBanner … そのラウンドの勝者側を表示（GameObject 切替）
//   tallyText … 現在の勝数（既定 "P1  a - b  P2" 形式）
//   p1WinsText / p2WinsText … 各プレイヤーの勝数を個別に出したい場合（任意）
public class RoundResultUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;

    [Header("ラウンド勝者バナー（GameObject）")]
    [SerializeField] private GameObject p1RoundBanner;
    [SerializeField] private GameObject p2RoundBanner;

    [Header("勝数表示（TMP・任意）")]
    [SerializeField] private TextMeshProUGUI tallyText;   // "P1  a - b  P2"
    [SerializeField] private TextMeshProUGUI p1WinsText;  // "a"
    [SerializeField] private TextMeshProUGUI p2WinsText;  // "b"

    private bool shown;

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isRoundOver = GameManager.Instance.GetCurrentState() == GameManager.GameState.RoundOver;
        if (isRoundOver && !shown)      Show();
        else if (!isRoundOver && shown) Hide();
    }

    private void Show()
    {
        shown = true;
        if (panel != null) panel.SetActive(true);

        var gm = GameManager.Instance;
        int p1W = gm.GetRoundWins(1);
        int p2W = gm.GetRoundWins(2);
        bool p1Won = gm.LastRoundWinner == 1;

        Set(p1RoundBanner,  p1Won);
        Set(p2RoundBanner, !p1Won);

        if (tallyText  != null) tallyText.text  = $"P1   {p1W} - {p2W}   P2";
        if (p1WinsText != null) p1WinsText.text = p1W.ToString();
        if (p2WinsText != null) p2WinsText.text = p2W.ToString();
    }

    private void Hide()
    {
        shown = false;
        if (panel != null) panel.SetActive(false);
    }

    private static void Set(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }
}
