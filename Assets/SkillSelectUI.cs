using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 試合開始前のスキル選択画面（4 枚カード + P1/P2 独立カーソル, DESIGN.md 5.6）。
// GameState.SkillSelect のあいだ panel を表示し、両者が確定すると BeginMatch する。
// カードの並び順は AllSkills の index と一致させる（左→右で 0,1,2,3）。
// カーソル配列は未バインドでも安全に動作する（Figma 構築前でも入力・確定は機能する）。
public class SkillSelectUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;

    [Header("カードカーソル（index は AllSkills の並び順と一致させること）")]
    // 各プレイヤーが選択中のカードにのみ表示するカーソル/ハイライト。
    // 長さは AllSkills.Length（=4）に合わせてバインドする。
    [SerializeField] private GameObject[] cardP1Cursors;
    [SerializeField] private GameObject[] cardP2Cursors;

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
        SetCursors(cardP1Cursors, p1Index);
        SetCursors(cardP2Cursors, p2Index);

        if (p1StatusText != null) p1StatusText.text = p1Confirmed ? "Ready!" : "A / D  select     S  confirm";
        if (p2StatusText != null) p2StatusText.text = p2Confirmed ? "Ready!" : "J / L  select     K  confirm";
    }

    // 選択中のカードのみカーソルを表示。未バインド/null 要素は安全にスキップ。
    private static void SetCursors(GameObject[] cursors, int selected)
    {
        if (cursors == null) return;
        for (int i = 0; i < cursors.Length; i++)
        {
            if (cursors[i] == null) continue;
            bool show = (i == selected);
            if (cursors[i].activeSelf != show) cursors[i].SetActive(show);
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
