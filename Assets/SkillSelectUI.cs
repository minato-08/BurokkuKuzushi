using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 試合開始前のスキル選択画面
// GameState.SkillSelect のあいだ panel を表示し、両者が確定するとゲームを開始する
public class SkillSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject      panel;
    [SerializeField] private TextMeshProUGUI p1SkillText;
    [SerializeField] private TextMeshProUGUI p2SkillText;
    [SerializeField] private TextMeshProUGUI p1StatusText;
    [SerializeField] private TextMeshProUGUI p2StatusText;

    private int  p1Index, p2Index;
    private bool p1Confirmed, p2Confirmed;
    private bool prevSkillSelect;

    // 選択可能なスキル一覧
    private static readonly SkillDefinition[] AllSkills = new SkillDefinition[]
    {
        new SkillPaddle_Enlarge(),
        new SkillBall_Attribute_Fire(),
        new SkillBall_Multi(),
        new SkillPanic_BlockClear()
    };

    void Update()
    {
        bool isSkillSelect = GameManager.Instance?.GetCurrentState() == GameManager.GameState.SkillSelect;

        // SkillSelect に入った瞬間に確定状態をリセット
        if (isSkillSelect && !prevSkillSelect)
        {
            p1Confirmed = false;
            p2Confirmed = false;
        }
        prevSkillSelect = isSkillSelect;

        if (panel != null) panel.SetActive(isSkillSelect);
        if (!isSkillSelect) return;

        HandleInput();
        RefreshUI();

        if (p1Confirmed && p2Confirmed)
            ApplyAndBegin();
    }

    private void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 1P: A/D でサイクル、S で確定
        if (!p1Confirmed)
        {
            if (kb.aKey.wasPressedThisFrame)
                p1Index = (p1Index + AllSkills.Length - 1) % AllSkills.Length;
            if (kb.dKey.wasPressedThisFrame)
                p1Index = (p1Index + 1) % AllSkills.Length;
            if (kb.sKey.wasPressedThisFrame)
                p1Confirmed = true;
        }

        // 2P: J/L でサイクル、K で確定
        if (!p2Confirmed)
        {
            if (kb.jKey.wasPressedThisFrame)
                p2Index = (p2Index + AllSkills.Length - 1) % AllSkills.Length;
            if (kb.lKey.wasPressedThisFrame)
                p2Index = (p2Index + 1) % AllSkills.Length;
            if (kb.kKey.wasPressedThisFrame)
                p2Confirmed = true;
        }
    }

    private void RefreshUI()
    {
        if (p1SkillText  != null) p1SkillText.text  = $"< {AllSkills[p1Index].DisplayName} >";
        if (p2SkillText  != null) p2SkillText.text  = $"< {AllSkills[p2Index].DisplayName} >";
        if (p1StatusText != null) p1StatusText.text = p1Confirmed ? "Ready!" : "A / D  select     S  confirm";
        if (p2StatusText != null) p2StatusText.text = p2Confirmed ? "Ready!" : "J / L  select     K  confirm";
    }

    private void ApplyAndBegin()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.GetArena(1)?.GetSkillController()?.SetSkill(AllSkills[p1Index]);
        gm.GetArena(2)?.GetSkillController()?.SetSkill(AllSkills[p2Index]);
        gm.BeginMatch();
    }
}
