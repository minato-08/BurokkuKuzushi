using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// マッチ終了時の結果画面（GameObject 表示切替方式）。_UI/_CameraSpace/_Base にアタッチ。
// GameState.MatchOver でパネルを表示。A/D（J/L）で 再戦/メニュー を選択、Space/Enter で確定。
//
// 表示要素（すべて null 安全。未バインドでも動く）:
//   p1WinsBanner / p2WinsBanner … 勝者の「P{N} Wins!」を表示（敗者側は非表示）
//   p1ScoreText  / p2ScoreText  … 各プレイヤーの合計スコア（TMP）
//   p1TagWin/p1TagLose, p2TagWin/p2TagLose … 各プレイヤーの WIN / LOSE タグ
//   rematchSelected/rematchUnselect, menuSelected/menuUnselect … ボタンの選択状態
public class MatchResultUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject matchResultPanel;

    [Header("勝者バナー（GameObject）")]
    [SerializeField] private GameObject p1WinsBanner;
    [SerializeField] private GameObject p2WinsBanner;

    [Header("スコア（TMP）")]
    [SerializeField] private TextMeshProUGUI p1ScoreText;
    [SerializeField] private TextMeshProUGUI p2ScoreText;

    [Header("獲得ラウンド数（TMP・任意。先取数 > 1 で意味を持つ）")]
    [SerializeField] private TextMeshProUGUI p1RoundsWonText; // $P1RoundsWon
    [SerializeField] private TextMeshProUGUI p2RoundsWonText; // $P2RoundsWon

    [Header("マッチ統計（TMP・任意, DESIGN.md 5.10）")]
    [SerializeField] private TextMeshProUGUI p1BestComboText;    // マッチ全体の最大コンボ
    [SerializeField] private TextMeshProUGUI p2BestComboText;
    [SerializeField] private TextMeshProUGUI p1BlocksText;       // 総破壊ブロック数
    [SerializeField] private TextMeshProUGUI p2BlocksText;
    [SerializeField] private TextMeshProUGUI p1InterferenceText; // 受けた妨害数
    [SerializeField] private TextMeshProUGUI p2InterferenceText;

    [Header("WIN / LOSE タグ（GameObject）")]
    [SerializeField] private GameObject p1TagWin;
    [SerializeField] private GameObject p1TagLose;
    [SerializeField] private GameObject p2TagWin;
    [SerializeField] private GameObject p2TagLose;

    [Header("ボタン選択状態（GameObject）")]
    [SerializeField] private GameObject rematchSelected;
    [SerializeField] private GameObject rematchUnselect;
    [SerializeField] private GameObject menuSelected;
    [SerializeField] private GameObject menuUnselect;

    private int  selectedIndex; // 0=再戦, 1=メニュー
    private bool panelShown;

    void Start() => HidePanel(); // シーン既定で active 保存されていても起動時に隠す

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
        bool p1Win = p1W >= p2W;

        Set(p1WinsBanner,  p1Win);
        Set(p2WinsBanner, !p1Win);
        Set(p1TagWin,  p1Win);  Set(p1TagLose, !p1Win);
        Set(p2TagWin, !p1Win);  Set(p2TagLose,  p1Win);

        if (p1ScoreText != null) p1ScoreText.text = gm.GetScore(1).ToString("N0");
        if (p2ScoreText != null) p2ScoreText.text = gm.GetScore(2).ToString("N0");

        if (p1RoundsWonText != null) p1RoundsWonText.text = p1W.ToString();
        if (p2RoundsWonText != null) p2RoundsWonText.text = p2W.ToString();

        if (p1BestComboText != null) p1BestComboText.text = gm.GetMaxComboMatch(1).ToString();
        if (p2BestComboText != null) p2BestComboText.text = gm.GetMaxComboMatch(2).ToString();
        if (p1BlocksText != null) p1BlocksText.text = gm.GetBlocksDestroyed(1).ToString();
        if (p2BlocksText != null) p2BlocksText.text = gm.GetBlocksDestroyed(2).ToString();
        if (p1InterferenceText != null) p1InterferenceText.text = gm.GetInterferenceReceived(1).ToString();
        if (p2InterferenceText != null) p2InterferenceText.text = gm.GetInterferenceReceived(2).ToString();

        UpdateSelectionVisual();
    }

    private void HidePanel()
    {
        panelShown = false;
        if (matchResultPanel != null) matchResultPanel.SetActive(false);
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool left  = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame  || kb.jKey.wasPressedThisFrame;
        bool right = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame;
        bool confirm = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;

        if (left  && selectedIndex != 0) { selectedIndex = 0; UpdateSelectionVisual(); AudioManager.Instance?.PlayUiMove(); }
        if (right && selectedIndex != 1) { selectedIndex = 1; UpdateSelectionVisual(); AudioManager.Instance?.PlayUiMove(); }
        if (confirm) { AudioManager.Instance?.PlayUiConfirm(); Confirm(); }
    }

    private void UpdateSelectionVisual()
    {
        Set(rematchSelected, selectedIndex == 0);
        Set(rematchUnselect, selectedIndex != 0);
        Set(menuSelected,    selectedIndex == 1);
        Set(menuUnselect,    selectedIndex != 1);
    }

    private void Confirm()
    {
        HidePanel();
        if (selectedIndex == 0)
            GameManager.Instance.StartRematch(); // 再戦 → スキル選択へ
        else
            GameManager.Instance.ReturnToTitle(); // メニュー → タイトルへ（シーンはリロードしない）
    }

    private static void Set(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }
}
