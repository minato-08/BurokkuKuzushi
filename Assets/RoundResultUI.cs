using TMPro;
using UnityEngine;

// ラウンド間の結果画面（GameState.RoundOver, マッチ未決着）。最終結果は MatchResultUI が担当。
// 数秒後に GameManager が自動で次ラウンドへ進める（入力は不要）。
// _Base にアタッチ。すべて null 安全（未バインドでも動く）。
//
// 表示:
//   p1RoundBanner / p2RoundBanner … そのラウンドの勝者側を表示（GameObject 切替）
//   p1WinsText / p2WinsText … 各プレイヤーの勝数（$P1RoundTally / $P2RoundTally）
//   tallyText … 勝数をまとめて出したい場合（任意, "P1 a - b P2"）
//   nextRoundTimeText … 次ラウンドまでの残り秒（$NextRoundTime, カウントダウン）
public class RoundResultUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;

    [Header("ラウンド勝者バナー（GameObject）")]
    [SerializeField] private GameObject p1RoundBanner;
    [SerializeField] private GameObject p2RoundBanner;

    [Header("勝数表示（TMP・任意）")]
    [SerializeField] private TextMeshProUGUI p1WinsText;  // $P1RoundTally
    [SerializeField] private TextMeshProUGUI p2WinsText;  // $P2RoundTally
    [SerializeField] private TextMeshProUGUI tallyText;   // 任意: "P1 a - b P2"

    [Header("今ラウンドの最大コンボ（TMP・任意, DESIGN.md 5.10）")]
    [SerializeField] private TextMeshProUGUI p1BestComboText;  // $P1RoundBestCombo
    [SerializeField] private TextMeshProUGUI p2BestComboText;  // $P2RoundBestCombo

    [Header("カウントダウン（TMP・任意）")]
    [SerializeField] private TextMeshProUGUI nextRoundTimeText; // $NextRoundTime

    private bool shown;

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isRoundOver = GameManager.Instance.GetCurrentState() == GameManager.GameState.RoundOver;
        if (isRoundOver && !shown)      Show();
        else if (!isRoundOver && shown) Hide();

        // 表示中はカウントダウンを毎フレーム更新
        if (shown && nextRoundTimeText != null)
        {
            int sec = Mathf.CeilToInt(Mathf.Max(0f, GameManager.Instance.RoundIntermissionRemaining));
            nextRoundTimeText.text = sec.ToString();
        }
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

        if (p1WinsText != null) p1WinsText.text = p1W.ToString();
        if (p2WinsText != null) p2WinsText.text = p2W.ToString();
        if (tallyText  != null) tallyText.text  = $"P1   {p1W} - {p2W}   P2";

        if (p1BestComboText != null) p1BestComboText.text = gm.GetMaxComboRound(1).ToString();
        if (p2BestComboText != null) p2BestComboText.text = gm.GetMaxComboRound(2).ToString();
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
