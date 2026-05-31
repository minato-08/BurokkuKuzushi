using UnityEngine;
using UnityEngine.InputSystem;

// タイトル画面（最小・START のみ, DESIGN.md 11.2）。
// GameState.Title の間 panel を表示し、Space/Enter でゲーム開始（StartFromTitle → SkillSelect）。
// メニュー（SETTINGS/QUIT）は持たない。panel 未バインドでも安全（Space で開始は機能）。
public class TitleUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    void Update()
    {
        if (GameManager.Instance == null) return;

        bool isTitle = GameManager.Instance.GetCurrentState() == GameManager.GameState.Title;
        if (panel != null && panel.activeSelf != isTitle) panel.SetActive(isTitle);
        if (!isTitle) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            GameManager.Instance.StartFromTitle();
    }
}
