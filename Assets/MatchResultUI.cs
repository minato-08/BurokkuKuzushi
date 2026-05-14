using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// マッチ終了時の結果画面と入力ハンドリング
// CenterUI Canvas にアタッチする
public class MatchResultUI : MonoBehaviour
{
    [Header("マッチ結果パネル")]
    [SerializeField] private GameObject matchResultPanel;
    [SerializeField] private TextMeshProUGUI matchWinnerText;
    [SerializeField] private TextMeshProUGUI scoreSummaryText;
    [SerializeField] private TextMeshProUGUI winsSummaryText;
    [SerializeField] private TextMeshProUGUI rematchText;
    [SerializeField] private TextMeshProUGUI menuText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("選択肢カラー")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor   = Color.white;

    private int  selectedIndex  = 0; // 0=再戦, 1=メニューへ戻る
    private bool panelShown     = false;

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isMatchOver = GameManager.Instance.GetCurrentState() == GameManager.GameState.MatchOver;

        if (isMatchOver && !panelShown)
            ShowPanel();
        else if (!isMatchOver && panelShown)
            HidePanel();

        if (isMatchOver && panelShown)
            HandleInput();
    }

    private void ShowPanel()
    {
        panelShown = true;
        selectedIndex = 0;
        if (matchResultPanel != null) matchResultPanel.SetActive(true);

        var gm = GameManager.Instance;
        int p1W = gm.GetRoundWins(1);
        int p2W = gm.GetRoundWins(2);
        int winner = p1W >= p2W ? 1 : 2;

        if (matchWinnerText  != null) matchWinnerText.text  = $"P{winner} WINS!";
        if (scoreSummaryText != null) scoreSummaryText.text = $"P1: {gm.GetScore(1)} pts    P2: {gm.GetScore(2)} pts";
        if (winsSummaryText  != null) winsSummaryText.text  = $"P1: {p1W} wins    P2: {p2W} wins";
        if (hintText         != null) hintText.text         = "A / D  (or  J / L)  to select    Space to confirm";

        UpdateSelectionVisual();
    }

    private void HidePanel()
    {
        panelShown = false;
        if (matchResultPanel != null) matchResultPanel.SetActive(false);
    }

    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        bool moveLeft  = Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame
                      || Keyboard.current.jKey.wasPressedThisFrame;
        bool moveRight = Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame
                      || Keyboard.current.lKey.wasPressedThisFrame;
        bool confirm   = Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;

        if (moveLeft  && selectedIndex != 0) { selectedIndex = 0; UpdateSelectionVisual(); }
        if (moveRight && selectedIndex != 1) { selectedIndex = 1; UpdateSelectionVisual(); }
        if (confirm) Confirm();
    }

    private void UpdateSelectionVisual()
    {
        if (rematchText != null) rematchText.color = selectedIndex == 0 ? selectedColor : normalColor;
        if (menuText    != null) menuText.color    = selectedIndex == 1 ? selectedColor : normalColor;
    }

    private void Confirm()
    {
        HidePanel();
        if (selectedIndex == 0)
        {
            // 再戦: アリーナをリセットしてマッチ開始
            GameManager.Instance.StartRematch();
        }
        else
        {
            // メニューへ戻る: 現フェーズではシーンをリロード
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
