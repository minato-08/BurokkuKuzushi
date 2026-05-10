using TMPro;
using UnityEngine;

// 中央UIを管理するスクリプト
// GameManagerの状態を毎フレーム読み取ってテキストを更新する
public class UIManager : MonoBehaviour
{
    [Header("プレイヤー1 UI")]
    [SerializeField] private TextMeshProUGUI p1ScoreText;
    [SerializeField] private TextMeshProUGUI p1LivesText;
    [SerializeField] private TextMeshProUGUI p1ComboText;
    [SerializeField] private TextMeshProUGUI p1RoundWinsText;

    [Header("プレイヤー2 UI")]
    [SerializeField] private TextMeshProUGUI p2ScoreText;
    [SerializeField] private TextMeshProUGUI p2LivesText;
    [SerializeField] private TextMeshProUGUI p2ComboText;
    [SerializeField] private TextMeshProUGUI p2RoundWinsText;

    [Header("試合状態 UI")]
    // ラウンド終了・試合終了時に表示するテキスト
    [SerializeField] private TextMeshProUGUI statusText;

    void Update()
    {
        if (GameManager.Instance == null) return;

        UpdatePlayerStats(1, p1ScoreText, p1LivesText, p1ComboText, p1RoundWinsText);
        UpdatePlayerStats(2, p2ScoreText, p2LivesText, p2ComboText, p2RoundWinsText);
        UpdateStatusText();
    }

    private void UpdatePlayerStats(int playerIndex,
                                   TextMeshProUGUI score,
                                   TextMeshProUGUI lives,
                                   TextMeshProUGUI combo,
                                   TextMeshProUGUI rounds)
    {
        var gm = GameManager.Instance;
        if (score  != null) score.text  = $"{gm.GetScore(playerIndex)}";
        if (lives  != null) lives.text  = $"♥ {gm.GetLives(playerIndex)}";
        if (combo  != null) combo.text  = $"Combo {gm.GetCombo(playerIndex)}/{gm.GetComboThreshold()}";
        if (rounds != null) rounds.text = $"Wins: {gm.GetRoundWins(playerIndex)}";
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        var state = GameManager.Instance.GetCurrentState();
        switch (state)
        {
            case GameManager.GameState.RoundOver:
                statusText.gameObject.SetActive(true);
                statusText.text = "Round Over!";
                break;

            case GameManager.GameState.MatchOver:
                statusText.gameObject.SetActive(true);
                int p1W = GameManager.Instance.GetRoundWins(1);
                int p2W = GameManager.Instance.GetRoundWins(2);
                int winner = p1W > p2W ? 1 : 2;
                statusText.text = $"P{winner} WINS!";
                break;

            default:
                statusText.gameObject.SetActive(false);
                break;
        }
    }
}
