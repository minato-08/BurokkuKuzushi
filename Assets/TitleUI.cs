using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// タイトル画面（最小, DESIGN.md 11.2）。GameState.Title の間 panel を表示し、START でゲーム開始。
// メニュー: 0=START / 1=SETTINGS / 2=QUIT。選択中項目のテキスト色を selectedColor にする（別カーソル不要）。
// 操作: W/S または ↑/↓ で項目移動、Space/Enter で確定。
// 各 SerializeField は未バインドでも安全に動作する（Figma/構築前でも START/確定は機能）。
public class TitleUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;

    [Header("メニュー項目テキスト（0 START / 1 SETTINGS / 2 QUIT）")]
    [SerializeField] private TextMeshProUGUI startText;
    [SerializeField] private TextMeshProUGUI settingsText;
    [SerializeField] private TextMeshProUGUI quitText;

    [Header("設定パネル連携")]
    [SerializeField] private SettingsUI settingsUI;

    [Header("選択肢カラー")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor   = Color.white;

    private const int MenuCount = 3;
    private int  index;
    private bool panelShown;

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isTitle      = GameManager.Instance.GetCurrentState() == GameManager.GameState.Title;
        bool settingsOpen = settingsUI != null && settingsUI.IsOpen;

        // タイトル中かつ設定を開いていない時だけパネル表示
        if (isTitle && !settingsOpen && !panelShown) ShowPanel();
        else if ((!isTitle || settingsOpen) && panelShown) HidePanel();

        if (!isTitle || settingsOpen) return;

        HandleInput();
    }

    private void ShowPanel()
    {
        panelShown = true;
        index = 0;
        if (panel != null) panel.SetActive(true);
        UpdateSelectionVisual();
    }

    private void HidePanel()
    {
        panelShown = false;
        if (panel != null) panel.SetActive(false);
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
        { index = (index + MenuCount - 1) % MenuCount; UpdateSelectionVisual(); }
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
        { index = (index + 1) % MenuCount; UpdateSelectionVisual(); }

        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            Confirm();
    }

    private void UpdateSelectionVisual()
    {
        if (startText    != null) startText.color    = index == 0 ? selectedColor : normalColor;
        if (settingsText != null) settingsText.color = index == 1 ? selectedColor : normalColor;
        if (quitText     != null) quitText.color     = index == 2 ? selectedColor : normalColor;
    }

    private void Confirm()
    {
        switch (index)
        {
            case 0: GameManager.Instance.StartFromTitle(); break; // START → スキル選択へ
            case 1: settingsUI?.Open();                     break; // SETTINGS → 設定パネル
            case 2: Quit();                                 break; // QUIT
        }
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
