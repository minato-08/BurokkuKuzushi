using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 設定画面（最小・先取数のみ, DESIGN.md 11.3）。タイトルの SETTINGS から Open() で開く。
// 先取数 1〜5 を A/D（←/→）で増減して $RoundsValue に反映。Esc/Space/Enter で閉じる。
// PlayerPrefs "match.roundsToWin" に保存し、起動時に GameManager へ適用する。
// roundsValueText が未バインドでも安全に動作する。
public class SettingsUI : MonoBehaviour
{
    private const string PrefKey = "match.roundsToWin";

    [SerializeField] private GameObject      panel;
    [SerializeField] private TextMeshProUGUI roundsValueText; // 先取数の数値表示（"3" 等）

    private int rounds = 1;
    public bool IsOpen { get; private set; }

    void Start()
    {
        // 保存値をロードして GameManager に適用（Instance は Awake 済みで非 null）
        int def = GameManager.Instance != null ? GameManager.Instance.GetRoundsToWin() : 1;
        rounds  = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, def), 1, 5);
        GameManager.Instance?.SetRoundsToWin(rounds);

        IsOpen = false;
        if (panel != null) panel.SetActive(false);
        RefreshUI();
    }

    public void Open()
    {
        IsOpen = true;
        if (panel != null) panel.SetActive(true);
        RefreshUI();
    }

    public void Close()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  SetRounds(rounds - 1);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) SetRounds(rounds + 1);

        if (kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            Close();
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
