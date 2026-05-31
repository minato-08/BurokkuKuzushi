using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 設定画面（最小・先取数のみ, DESIGN.md 11.3）。スキル選択の前のステップ（GameState.Settings）。
// タイトルの START → この設定 → 確定で SkillSelect へ進む。
// 操作: A/D（←/→）で先取数 1〜5、Space/Enter で確定（→スキル選択）、Esc でタイトルへ戻る。
// PlayerPrefs "match.roundsToWin" に保存。roundsValueText 未バインドでも安全。
public class SettingsUI : MonoBehaviour
{
    private const string PrefKey = "match.roundsToWin";

    [SerializeField] private GameObject      panel;
    [SerializeField] private TextMeshProUGUI roundsValueText; // 先取数の数値表示（"3" 等）

    private int  rounds = 1;
    private bool prevSettings;

    void Update()
    {
        bool isSettings = GameManager.Instance?.GetCurrentState() == GameManager.GameState.Settings;

        if (panel != null && panel.activeSelf != isSettings) panel.SetActive(isSettings);

        if (!isSettings) { prevSettings = false; return; }

        // 設定に入った最初のフレームは現在値をロードして表示し、入力は処理しない
        // （タイトルで押した Space をこのフレームの「確定」として消費しないため）
        if (!prevSettings)
        {
            prevSettings = true;
            int def = GameManager.Instance != null ? GameManager.Instance.GetRoundsToWin() : 1;
            rounds = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, def), 1, 5);
            GameManager.Instance?.SetRoundsToWin(rounds);
            RefreshUI();
            return;
        }

        HandleInput();
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  SetRounds(rounds - 1);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) SetRounds(rounds + 1);

        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            GameManager.Instance?.ConfirmSettings();      // → スキル選択へ
        else if (kb.escapeKey.wasPressedThisFrame)
            GameManager.Instance?.ReturnToTitle();        // → タイトルへ戻る
    }

    private void SetRounds(int v)
    {
        rounds = Mathf.Clamp(v, 1, 5);
        GameManager.Instance?.SetRoundsToWin(rounds);
        PlayerPrefs.SetInt(PrefKey, rounds);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (roundsValueText != null) roundsValueText.text = rounds.ToString();
    }
}
