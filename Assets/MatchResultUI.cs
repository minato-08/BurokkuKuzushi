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

        if (left  && selectedIndex != 0) { selectedIndex = 0; UpdateSelectionVisual(); }
        if (right && selectedIndex != 1) { selectedIndex = 1; UpdateSelectionVisual(); }
        if (confirm) Confirm();
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
