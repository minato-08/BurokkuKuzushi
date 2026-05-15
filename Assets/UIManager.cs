using System.Collections;
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

    [Header("エナジーゲージ UI")]
    [SerializeField] private Image           p1EnergyFill;
    [SerializeField] private Image           p2EnergyFill;
    [SerializeField] private TextMeshProUGUI p1SkillText;
    [SerializeField] private TextMeshProUGUI p2SkillText;

    [Header("試合状態 UI")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("妨害通知オーバーレイ")]
    [SerializeField] private CanvasGroup     p1InterferenceOverlay;
    [SerializeField] private TextMeshProUGUI p1InterferenceLabel;
    [SerializeField] private CanvasGroup     p2InterferenceOverlay;
    [SerializeField] private TextMeshProUGUI p2InterferenceLabel;

    private Coroutine p1OverlayRoutine;
    private Coroutine p2OverlayRoutine;

    void Update()
    {
        if (GameManager.Instance == null) return;

        UpdatePlayerStats(1, p1ScoreText, p1HPText, p1HPFill, p1ComboText, p1RoundWinsText);
        UpdatePlayerStats(2, p2ScoreText, p2HPText, p2HPFill, p2ComboText, p2RoundWinsText);
        UpdateEnergyUI();
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

    private void UpdateEnergyUI()
    {
        var gm = GameManager.Instance;
        if (p1EnergyFill != null) p1EnergyFill.fillAmount = gm.GetEnergyRatio(1);
        if (p2EnergyFill != null) p2EnergyFill.fillAmount = gm.GetEnergyRatio(2);
        if (p1SkillText  != null) p1SkillText.text  = gm.GetEquippedSkillName(1);
        if (p2SkillText  != null) p2SkillText.text  = gm.GetEquippedSkillName(2);
    }

    public void ShowInterferenceOverlay(int playerIndex, string label)
    {
        CanvasGroup     cg  = playerIndex == 1 ? p1InterferenceOverlay : p2InterferenceOverlay;
        TextMeshProUGUI txt = playerIndex == 1 ? p1InterferenceLabel   : p2InterferenceLabel;
        if (cg == null) return;

        ref Coroutine slot = ref (playerIndex == 1 ? ref p1OverlayRoutine : ref p2OverlayRoutine);
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(OverlayRoutine(cg, txt, label));
    }

    private IEnumerator OverlayRoutine(CanvasGroup cg, TextMeshProUGUI txt, string label)
    {
        if (txt != null) txt.text = $"妨害！\n{label}";
        cg.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.5f);
        cg.alpha = 0f;
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
        // MatchOver の表示は MatchResultUI が担当するためここでは非表示
        if (state == GameManager.GameState.RoundOver)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Round Over!";
        }
        else
        {
            statusText.gameObject.SetActive(false);
        }
    }
}
