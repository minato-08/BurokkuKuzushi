using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 試合開始前のスキル選択画面（4 枚カード, DESIGN.md 5.6）。
// GameState.SkillSelect のあいだ panel を表示し、両者が確定すると BeginMatch する。
// カードの並び順は AllSkills の index と一致させる（左→右で 0,1,2,3）。
//
// 選択表現は「カードの色」で行う（別カーソル GameObject は置かない）:
//   各カードに P1 用 / P2 用のハイライト Image を 1 枚ずつ置き、
//   選択中カードのみ色を点灯（hoverColor）、未選択は透明（offColor）、確定後は confirmedColor。
// ハイライト配列は未バインドでも安全に動作する（入力・確定・BeginMatch は機能する）。
public class SkillSelectUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;

    [Header("カードハイライト Image（index は AllSkills の並び順 = 左→右 0..3）")]
    [SerializeField] private Image[] cardP1Highlights; // 長さ 4
    [SerializeField] private Image[] cardP2Highlights; // 長さ 4

    [Header("ハイライト色")]
    [SerializeField] private Color p1HoverColor     = new Color(0.306f, 0.765f, 1.000f, 0.85f); // 水色
    [SerializeField] private Color p2HoverColor     = new Color(1.000f, 0.306f, 0.455f, 0.85f); // 赤
    [SerializeField] private Color p1ConfirmedColor = new Color(0.306f, 0.765f, 1.000f, 1.000f);
    [SerializeField] private Color p2ConfirmedColor = new Color(1.000f, 0.306f, 0.455f, 1.000f);
    [SerializeField] private Color offColor         = new Color(1f, 1f, 1f, 0f);                // 透明

    [Header("操作ガイド / 状態テキスト")]
    [SerializeField] private TextMeshProUGUI p1StatusText;
    [SerializeField] private TextMeshProUGUI p2StatusText;

    private int  p1Index, p2Index;
    private bool p1Confirmed, p2Confirmed;
    private bool prevSkillSelect;

    // 選択可能なスキル一覧（順序 = カードの並び順）
    private static readonly SkillDefinition[] AllSkills =
    {
        new SkillPaddle_Enlarge(),       // 0
        new SkillBall_Attribute_Fire(),  // 1
        new SkillBall_Multi(),           // 2
        new SkillPanic_BlockClear()      // 3
    };

    void Update()
    {
        bool isSkillSelect = GameManager.Instance?.GetCurrentState() == GameManager.GameState.SkillSelect;

        // SkillSelect に入った瞬間に確定状態をリセット（index は前回の選択を保持）
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

        // 1P: A/D でカード移動、S で確定
        if (!p1Confirmed)
        {
            if (kb.aKey.wasPressedThisFrame) p1Index = Wrap(p1Index - 1);
            if (kb.dKey.wasPressedThisFrame) p1Index = Wrap(p1Index + 1);
            if (kb.sKey.wasPressedThisFrame) p1Confirmed = true;
        }

        // 2P: J/L でカード移動、K で確定
        if (!p2Confirmed)
        {
            if (kb.jKey.wasPressedThisFrame) p2Index = Wrap(p2Index - 1);
            if (kb.lKey.wasPressedThisFrame) p2Index = Wrap(p2Index + 1);
            if (kb.kKey.wasPressedThisFrame) p2Confirmed = true;
        }
    }

    private static int Wrap(int i) => (i + AllSkills.Length) % AllSkills.Length;

    private void RefreshUI()
    {
        SetHighlights(cardP1Highlights, p1Index, p1Confirmed, p1HoverColor, p1ConfirmedColor);
        SetHighlights(cardP2Highlights, p2Index, p2Confirmed, p2HoverColor, p2ConfirmedColor);

        if (p1StatusText != null) p1StatusText.text = p1Confirmed ? "READY!" : "A / D  SELECT     S  CONFIRM";
        if (p2StatusText != null) p2StatusText.text = p2Confirmed ? "READY!" : "J / L  SELECT     K  CONFIRM";
    }

    // 選択中カードのみ色を点灯。未バインド/null 要素は安全にスキップ。
    private void SetHighlights(Image[] highlights, int selected, bool confirmed, Color hover, Color confirmedColor)
    {
        if (highlights == null) return;
        for (int i = 0; i < highlights.Length; i++)
        {
            if (highlights[i] == null) continue;
            highlights[i].color = (i == selected) ? (confirmed ? confirmedColor : hover) : offColor;
        }
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
