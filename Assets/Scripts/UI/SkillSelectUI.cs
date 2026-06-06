using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 試合開始前のスキル選択画面（4 枚カード, DESIGN.md 5.6）。
// GameState.SkillSelect のあいだ panel を表示し、両者が確定すると BeginMatch する。
// カードの並び順は AllSkills の index と一致させる（左→右で 0,1,2,3）:
//   0 HYPER / 1 EXPLOSION / 2 BURST / 3 GIANT
//
// 選択表現は GameObject の表示切替（SetActive）:
//   選択中カード … cardP{N}Cursors[i] を表示（他は非表示）
//   確定後      … cardP{N}Cursors を消し cardP{N}Ready[i] を表示
// 配列は未バインドでも安全に動作する（入力・確定・BeginMatch は機能する）。
public class SkillSelectUI : MonoBehaviour
{
    [Header("パネル")]
    [SerializeField] private GameObject panel;

    [Header("カーソル（選択中ホバー。index = AllSkills 並び順 0..3）")]
    [SerializeField] private GameObject[] cardP1Cursors;
    [SerializeField] private GameObject[] cardP2Cursors;

    [Header("Ready（確定後。index = AllSkills 並び順 0..3）")]
    [SerializeField] private GameObject[] cardP1Ready;
    [SerializeField] private GameObject[] cardP2Ready;

    [Header("操作ガイド / 状態テキスト（任意）")]
    [SerializeField] private TextMeshProUGUI p1StatusText;
    [SerializeField] private TextMeshProUGUI p2StatusText;

    private int  p1Index, p2Index;
    private bool p1Confirmed, p2Confirmed;
    private bool prevSkillSelect;

    // 選択可能なスキル一覧（順序 = カードの並び順）
    private static readonly SkillDefinition[] AllSkills =
    {
        new SkillHyper(),       // 0 HYPER
        new SkillExplosion(),   // 1 EXPLOSION
        new SkillBurst(),       // 2 BURST
        new SkillGiant()        // 3 GIANT
    };

    void Update()
    {
        bool isSkillSelect = GameManager.Instance?.GetCurrentState() == GameManager.GameState.SkillSelect;

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

        if (!p1Confirmed)
        {
            if (kb.aKey.wasPressedThisFrame) { p1Index = Wrap(p1Index - 1); AudioManager.Instance?.PlayUiMove(1); }
            if (kb.dKey.wasPressedThisFrame) { p1Index = Wrap(p1Index + 1); AudioManager.Instance?.PlayUiMove(1); }
            if (kb.sKey.wasPressedThisFrame) { p1Confirmed = true; AudioManager.Instance?.PlayUiConfirm(1); }
        }
        if (!p2Confirmed)
        {
            if (kb.jKey.wasPressedThisFrame) { p2Index = Wrap(p2Index - 1); AudioManager.Instance?.PlayUiMove(2); }
            if (kb.lKey.wasPressedThisFrame) { p2Index = Wrap(p2Index + 1); AudioManager.Instance?.PlayUiMove(2); }
            if (kb.kKey.wasPressedThisFrame) { p2Confirmed = true; AudioManager.Instance?.PlayUiConfirm(2); }
        }
    }

    private static int Wrap(int i) => (i + AllSkills.Length) % AllSkills.Length;

    private void RefreshUI()
    {
        // 確定前はカーソルを選択カードに、確定後はカーソルを消して Ready を選択カードに
        SetOnly(cardP1Cursors, p1Confirmed ? -1 : p1Index);
        SetOnly(cardP2Cursors, p2Confirmed ? -1 : p2Index);
        SetOnly(cardP1Ready,   p1Confirmed ? p1Index : -1);
        SetOnly(cardP2Ready,   p2Confirmed ? p2Index : -1);

        if (p1StatusText != null) p1StatusText.text = p1Confirmed ? "READY!" : "A / D  SELECT     S  CONFIRM";
        if (p2StatusText != null) p2StatusText.text = p2Confirmed ? "READY!" : "J / L  SELECT     K  CONFIRM";
    }

    // index == active のものだけ SetActive(true)、他は false。active<0 で全消灯。null 安全。
    private static void SetOnly(GameObject[] arr, int active)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) continue;
            bool show = (i == active);
            if (arr[i].activeSelf != show) arr[i].SetActive(show);
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
