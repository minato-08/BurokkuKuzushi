using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// マッチ終了時の結果画面（Result A · 左右対称＋勝者特大, docs/design_handoff_versus_screens）。
// _UI/_CameraSpace/_Base にアタッチ。GameState.MatchOver を検出してパネルを表示。
// A/D（J/L）で REMATCH/MENU 選択、Space で確定。
// 動的要素は全て null セーフ（未バインドでも動作する）。
public class MatchResultUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject matchResultPanel;

    [Header("勝者")]
    [SerializeField] private TextMeshProUGUI matchWinnerText;   // "P{N} WINS!"（勝者色）

    [Header("スコア対比")]
    [SerializeField] private TextMeshProUGUI p1ScoreText;       // 数値
    [SerializeField] private TextMeshProUGUI p2ScoreText;
    [SerializeField] private TextMeshProUGUI p1ScoreTagText;    // "P1 · WIN" / "P1"
    [SerializeField] private TextMeshProUGUI p2ScoreTagText;

    [Header("勝数（best-of ピップ）")]
    [SerializeField] private Image[] p1WinPips;                 // 長さ最大本数（5）。先頭から total 個を使用
    [SerializeField] private Image[] p2WinPips;
    [SerializeField] private TextMeshProUGUI bestOfText;        // "BEST OF 3"
    [SerializeField] private Color pipEmptyColor = new Color(0.31f, 0.32f, 0.38f, 1f); // line2

    [Header("選択肢")]
    [SerializeField] private TextMeshProUGUI rematchText;
    [SerializeField] private TextMeshProUGUI menuText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("色")]
    [SerializeField] private Color p1Color       = new Color(0.306f, 0.765f, 1.000f); // #4EC3FF
    [SerializeField] private Color p2Color       = new Color(1.000f, 0.306f, 0.455f); // #FF4E74
    [SerializeField] private Color winnerScore   = Color.white;
    [SerializeField] private Color loserScore    = new Color(0.604f, 0.627f, 0.706f); // #9AA0B4
    [SerializeField] private Color selectedColor  = new Color(0.925f, 0.788f, 0.184f); // #ECC92F
    [SerializeField] private Color normalColor    = new Color(0.604f, 0.627f, 0.706f);

    private int  selectedIndex; // 0=再戦, 1=メニュー
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

        if (isMatchOver && !panelShown)       ShowPanel();
        else if (!isMatchOver && panelShown)  HidePanel();

        if (isMatchOver && panelShown)        HandleInput();
    }

    private void ShowPanel()
    {
        panelShown    = true;
        selectedIndex = 0;
        if (matchResultPanel != null) matchResultPanel.SetActive(true);

        var gm     = GameManager.Instance;
        int p1W    = gm.GetRoundWins(1);
        int p2W    = gm.GetRoundWins(2);
        int winner = p1W >= p2W ? 1 : 2;
        Color winColor = winner == 1 ? p1Color : p2Color;

        if (matchWinnerText != null)
        {
            matchWinnerText.text  = $"P{winner} WINS!";
            matchWinnerText.color = winColor;
        }

        int s1 = gm.GetScore(1), s2 = gm.GetScore(2);
        SetScore(p1ScoreText, p1ScoreTagText, "P1", s1, winner == 1);
        SetScore(p2ScoreText, p2ScoreTagText, "P2", s2, winner == 2);

        // best-of ピップ: 必要勝利数 r から best-of = 2r-1
        int rounds = gm.GetRoundsToWin();
        int total  = Mathf.Max(1, 2 * rounds - 1);
        if (bestOfText != null) bestOfText.text = $"BEST OF {total}";
        SetPips(p1WinPips, p1W, total, p1Color);
        SetPips(p2WinPips, p2W, total, p2Color);

        if (hintText != null) hintText.text = "A / D ( J / L ) SELECT   SPACE CONFIRM";

        UpdateSelectionVisual();
    }

    private void SetScore(TextMeshProUGUI scoreText, TextMeshProUGUI tagText, string tag, int score, bool isWinner)
    {
        if (scoreText != null)
        {
            scoreText.text  = score.ToString("N0");
            scoreText.color = isWinner ? winnerScore : loserScore;
        }
        if (tagText != null) tagText.text = isWinner ? $"{tag} · WIN" : tag;
    }

    // 先頭 total 個を表示し、i < wins を該当色で塗り、残りは空色。total 超は非表示。
    private void SetPips(Image[] pips, int wins, int total, Color color)
    {
        if (pips == null) return;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            bool used = i < total;
            if (pips[i].gameObject.activeSelf != used) pips[i].gameObject.SetActive(used);
            if (used) pips[i].color = i < wins ? color : pipEmptyColor;
        }
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
            GameManager.Instance.StartRematch(); // スキル選択へ戻る
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 現フェーズはシーンリロード
        }
    }
}
