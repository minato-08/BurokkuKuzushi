using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// タイトル画面（最小・START のみ, DESIGN.md 11.2）。
// GameState.Title の間 panel を表示し、Space/Enter でゲーム開始（StartFromTitle → 設定 → SkillSelect）。
// メニュー（SETTINGS/QUIT）は持たない。panel 未バインドでも安全（Space で開始は機能）。
public class TitleUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Header("PRESS TO START 点滅")]
    [SerializeField] private TextMeshProUGUI pressToStartText; // _TitlePanel/_Title/Start
    [SerializeField] private float blinkPeriod   = 1.0f;       // 点滅周期（秒）
    [Range(0f, 1f)] [SerializeField] private float blinkMinAlpha = 0.15f; // 谷のアルファ

    private bool prevTitle;

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isTitle = GameManager.Instance.GetCurrentState() == GameManager.GameState.Title;
        if (panel != null && panel.activeSelf != isTitle) panel.SetActive(isTitle);
        if (!isTitle) { prevTitle = false; return; }

        // PRESS TO START を点滅（タイトルは timeScale=0 なので unscaledTime で駆動）
        if (pressToStartText != null)
        {
            float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.01f, blinkPeriod)) * 0.5f + 0.5f;
            Color c = pressToStartText.color;
            c.a = Mathf.Lerp(blinkMinAlpha, 1f, wave);
            pressToStartText.color = c;
        }

        // タイトルに入った最初のフレームは入力を処理しない。
        // 直前画面（MatchResult の「Menu」確定）で押した Space を、同フレームで
        // タイトルの「開始」として二重消費し設定画面へ飛んでしまうのを防ぐ。
        if (!prevTitle) { prevTitle = true; return; }

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
        {
            AudioManager.Instance?.PlayUiConfirm();
            GameManager.Instance.StartFromTitle();
        }
    }
}
