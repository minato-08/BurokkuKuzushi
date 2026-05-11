using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 中央UIを管理するスクリプト
// GameManagerの状態を毎フレーム読み取ってテキストを更新する
public class UIManager : MonoBehaviour
{
    [Header("プレイヤー1 UI")]
    [SerializeField] private TextMeshProUGUI p1ScoreText;
    [SerializeField] private TextMeshProUGUI p1HPText;       // HP数値表示
    [SerializeField] private Image           p1HPFill;       // HPバー（Image Type=Filled 想定）
    [SerializeField] private TextMeshProUGUI p1ComboText;
    [SerializeField] private TextMeshProUGUI p1RoundWinsText;

    [Header("プレイヤー2 UI")]
    [SerializeField] private TextMeshProUGUI p2ScoreText;
    [SerializeField] private TextMeshProUGUI p2HPText;
    [SerializeField] private Image           p2HPFill;
    [SerializeField] private TextMeshProUGUI p2ComboText;
    [SerializeField] private TextMeshProUGUI p2RoundWinsText;

    [Header("HPバー演出")]
    [SerializeField] private Color hpColorFull   = new Color(0.4f, 1.0f, 0.4f); // 緑
    [SerializeField] private Color hpColorMid    = new Color(1.0f, 0.9f, 0.3f); // 黄
    [SerializeField] private Color hpColorLow    = new Color(1.0f, 0.4f, 0.3f); // 赤
    [Range(0f, 1f)] [SerializeField] private float midThreshold = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float lowThreshold = 0.3f;

    [Header("試合状態 UI")]
    [SerializeField] private TextMeshProUGUI statusText;

    void Update()
    {
        if (GameManager.Instance == null) return;

        UpdatePlayerStats(1, p1ScoreText, p1HPText, p1HPFill, p1ComboText, p1RoundWinsText);
        UpdatePlayerStats(2, p2ScoreText, p2HPText, p2HPFill, p2ComboText, p2RoundWinsText);
        UpdateStatusText();
    }

    private void UpdatePlayerStats(int playerIndex,
                                   TextMeshProUGUI score,
                                   TextMeshProUGUI hpText,
                                   Image           hpFill,
                                   TextMeshProUGUI combo,
                                   TextMeshProUGUI rounds)
    {
        var gm = GameManager.Instance;
        int   currentHP = gm.GetHP(playerIndex);
        int   maxHP     = gm.GetMaxHP(playerIndex);
        float ratio     = gm.GetHPRatio(playerIndex);

        if (score  != null) score.text  = $"{gm.GetScore(playerIndex)}";
        if (hpText != null) hpText.text = $"HP {currentHP} / {maxHP}";
        if (hpFill != null)
        {
            hpFill.fillAmount = ratio;
            hpFill.color      = GetHPColor(ratio);
        }
        if (combo  != null) combo.text  = $"Combo {gm.GetCombo(playerIndex)}/{gm.GetComboThreshold()}";
        if (rounds != null) rounds.text = $"Wins: {gm.GetRoundWins(playerIndex)}";
    }

    private Color GetHPColor(float ratio)
    {
        if (ratio <= lowThreshold) return hpColorLow;
        if (ratio <= midThreshold) return hpColorMid;
        return hpColorFull;
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
