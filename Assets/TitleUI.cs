using UnityEngine;
using UnityEngine.InputSystem;

// タイトル画面（最小, DESIGN.md 11.2）。GameState.Title の間 panel を表示し、START でゲーム開始。
// メニュー: 0=START / 1=SETTINGS / 2=QUIT。選択中項目の menuCursors[i] のみ SetActive(true)。
// 操作: W/S または ↑/↓ で項目移動、Space/Enter で確定。
// カーソル配列・settingsUI は未バインドでも安全に動作する（Figma 構築前でも START/確定は機能）。
public class TitleUI : MonoBehaviour
{
    [SerializeField] private GameObject   panel;
    [SerializeField] private GameObject[] menuCursors; // 0:START 1:SETTINGS 2:QUIT
    [SerializeField] private SettingsUI   settingsUI;  // SETTINGS で開く

    private const int MenuCount = 3;
    private int index;

    void Update()
    {
        bool isTitle      = GameManager.Instance?.GetCurrentState() == GameManager.GameState.Title;
        bool settingsOpen = settingsUI != null && settingsUI.IsOpen;

        if (panel != null) panel.SetActive(isTitle && !settingsOpen);
        if (!isTitle || settingsOpen) return;

        HandleInput();
        RefreshUI();
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            index = (index + MenuCount - 1) % MenuCount;
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            index = (index + 1) % MenuCount;

        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            Confirm();
    }

    private void Confirm()
    {
        switch (index)
        {
            case 0: GameManager.Instance?.StartFromTitle(); break;
            case 1: settingsUI?.Open();                     break;
            case 2: Quit();                                 break;
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

    private void RefreshUI()
    {
        if (menuCursors == null) return;
        for (int i = 0; i < menuCursors.Length; i++)
        {
            if (menuCursors[i] == null) continue;
            bool show = (i == index);
            if (menuCursors[i].activeSelf != show) menuCursors[i].SetActive(show);
        }
    }
}
