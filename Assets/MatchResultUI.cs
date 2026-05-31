using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// マッチ終了時の結果画面（サマリー版）。_UI/_CameraSpace/_Base にアタッチ。
// GameState.MatchOver を検出してパネルを表示。A/D（J/L）で 再戦/メニュー を選択、Space 確定。
// 動的要素は全て null セーフ（未バインドでも動作する）。
public class MatchResultUI : MonoBehaviour
{
    [Header("マッチ結果パネル")]
    [SerializeField] private GameObject matchResultPanel;
    [SerializeField] private TextMeshProUGUI matchWinnerText;   // "P{N} WINS!"
    [SerializeField] private TextMeshProUGUI scoreSummaryText;  // "P1: x pts    P2: y pts"
    [SerializeField] private TextMeshProUGUI winsSummaryText;   // "P1: a wins    P2: b wins"
    [SerializeField] private TextMeshProUGUI rematchText;
    [SerializeField] private TextMeshProUGUI menuText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("選択肢カラー")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor   = Color.white;

    private int  selectedIndex; // 0=再戦, 1=メニューへ戻る
    private bool panelShown;

    void Start()
    {
        // シーン既定で active 保存されていても起動時に確実に隠す（panelShown 初期 false 対策）
        HidePanel();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isMatchOver = GameManager.Instance.GetCurrentState() == GameManager.GameState.MatchOver;

        if (isMatchOver && !panelShown)      ShowPanel();
        else if (!isMatchOver && panelShown) HidePanel();

        if (isMatchOver && panelShown) HandleInput();
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
        if (hintText         != null) hintText.text         = "A / D ( J / L ) 選択   Space 決定";

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
            // 再戦: スキル選択へ戻る
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
