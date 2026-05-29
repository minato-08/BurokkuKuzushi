using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 新 UI Hierarchy (_UI/_CameraSpace/_Components/_P1Components/...) に合わせた UIManager。
// GameManager を毎フレームポーリングして $P1XXX / $P2XXX 要素を更新する。
//
// SerializeField は次の 3 区分:
//   [必須]      新 UI に既に存在する要素（HP / Combo / Score / ActiveItem）
//   [任意・将来] まだ UI 要素が作られていないもの（Energy / Skill / Round / 妨害 / Status）
//                バインドされたときだけ更新される（null セーフ）
//   [演出]      色閾値などのパラメータ
public class UIManager : MonoBehaviour
{
    // =====================================================
    // 必須セクション（新 UI で既に配置済み）
    // =====================================================

    [Header("[必須] P1 HUD")]
    [SerializeField] private Image           p1HpFill;        // $P1HpFill (Image Sliced + Horizontal Fill)
    [SerializeField] private TextMeshProUGUI p1HpValue;       // $P1HpValue  ← 数字のみ
    [SerializeField] private TextMeshProUGUI p1HpMax;         // P1HpMax    ← "/500" 静的ラベル。Start() で実値に合わせる
    [SerializeField] private TextMeshProUGUI p1ComboValue;    // $P1ComboValue ← 数字のみ
    [SerializeField] private TextMeshProUGUI p1ComboMax;      // P1ComboMax ← "× /15" 静的ラベル。Start() で実値に合わせる
    [SerializeField] private TextMeshProUGUI p1ScoreValue;    // $P1ScoreValue ← "1,000" 形式
    [SerializeField] private GameObject      p1ItemInfoRoot;  // _P1ItemInfo（アイテム表示時のみ active）
    [SerializeField] private TextMeshProUGUI p1ItemName;      // $P1ItemName
    [SerializeField] private TextMeshProUGUI p1ItemDuration;  // $P1ItemDuration

    [Header("[必須] P2 HUD")]
    [SerializeField] private Image           p2HpFill;
    [SerializeField] private TextMeshProUGUI p2HpValue;
    [SerializeField] private TextMeshProUGUI p2HpMax;
    [SerializeField] private TextMeshProUGUI p2ComboValue;
    [SerializeField] private TextMeshProUGUI p2ComboMax;
    [SerializeField] private TextMeshProUGUI p2ScoreValue;
    [SerializeField] private GameObject      p2ItemInfoRoot;
    [SerializeField] private TextMeshProUGUI p2ItemName;
    [SerializeField] private TextMeshProUGUI p2ItemDuration;

    // =====================================================
    // 任意セクション（UI 要素が未配置。あとから追加バインド）
    // =====================================================

    [Header("[任意] エナジー / スキル / ラウンド")]
    [SerializeField] private Image           p1EnergyFill;
    [SerializeField] private Image           p2EnergyFill;
    [SerializeField] private TextMeshProUGUI p1SkillName;
    [SerializeField] private TextMeshProUGUI p2SkillName;
    [SerializeField] private TextMeshProUGUI p1RoundWins;
    [SerializeField] private TextMeshProUGUI p2RoundWins;

    [Header("[任意] 試合状態テキスト（Round Over 等）")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("[任意] 妨害通知オーバーレイ")]
    [SerializeField] private CanvasGroup     p1InterferenceOverlay;
    [SerializeField] private TextMeshProUGUI p1InterferenceLabel;
    [SerializeField] private CanvasGroup     p2InterferenceOverlay;
    [SerializeField] private TextMeshProUGUI p2InterferenceLabel;

    [Header("[任意] 攻撃送付ラベル（攻撃者 HUD に SENT → 表示）")]
    [SerializeField] private TextMeshProUGUI p1SentLabel;
    [SerializeField] private TextMeshProUGUI p2SentLabel;

    [Header("[任意] コンボマイルストーン演出")]
    [SerializeField] private CanvasGroup     p1ComboMilestoneOverlay;
    [SerializeField] private TextMeshProUGUI p1ComboMilestoneLabel;
    [SerializeField] private CanvasGroup     p2ComboMilestoneOverlay;
    [SerializeField] private TextMeshProUGUI p2ComboMilestoneLabel;

    [Header("[任意] Victory Bar（中央・優勢可視化）")]
    [SerializeField] private Image           victoryBar;  // $VictoryBar fillAmount = P1HP/(P1HP+P2HP)

    [Header("[任意] Incoming インジケータ（妨害予約キュー）")]
    [SerializeField] private TextMeshProUGUI[] p1IncomingSlots;  // 左列=P1への予約（最大3, [0]=最古）
    [SerializeField] private TextMeshProUGUI[] p2IncomingSlots;  // 右列=P2への予約
    [SerializeField] private float           incomingDisplaySec = 3.0f;

    // =====================================================
    // 演出パラメータ
    // =====================================================

    [Header("HP バー色")]
    [SerializeField] private Color hpColorFull = new Color(0.910f, 0.902f, 0.875f);
    [SerializeField] private Color hpColorMid  = new Color(1.000f, 0.847f, 0.290f);
    [SerializeField] private Color hpColorLow  = new Color(1.000f, 0.231f, 0.361f);
    [Range(0f, 1f)] [SerializeField] private float midThreshold = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float lowThreshold = 0.3f;

    [Header("スキル READY 表示")]
    [SerializeField] private string skillReadySuffix = " · READY";

    [Header("演出時間（秒）")]
    [SerializeField] private float overlayFlashDuration   = 1.5f;  // 妨害受信フラッシュ
    [SerializeField] private float sentLabelDuration      = 1.5f;  // 攻撃送付ラベル
    [SerializeField] private float comboMilestoneDuration = 1.2f;  // コンボマイルストーン

    private Coroutine p1OverlayRoutine;
    private Coroutine p2OverlayRoutine;
    private Coroutine p1SentRoutine;
    private Coroutine p2SentRoutine;
    private Coroutine p1MilestoneRoutine;
    private Coroutine p2MilestoneRoutine;

    // Incoming キュー（FIFO・最大3・incomingDisplaySec で自動失効）
    private struct IncomingEntry { public string symbol; public float expireTime; }
    private readonly List<IncomingEntry> p1Incoming = new List<IncomingEntry>();
    private readonly List<IncomingEntry> p2Incoming = new List<IncomingEntry>();

    // =====================================================
    // 初期化（静的ラベルを GameManager 実値に合わせる）
    // =====================================================

    void Start()
    {
        if (GameManager.Instance == null) return;

        int maxHP        = GameManager.Instance.GetMaxHP(1); // 両プレイヤー同値

        if (p1HpMax    != null) p1HpMax.text    = $"/{maxHP}";
        if (p2HpMax    != null) p2HpMax.text    = $"/{maxHP}";
        // コンボは上限なし（DESIGN.md 5.8）。旧「/15」しきい値表示は撤廃し
        // 「×」のみ表示する（コンボ数値は $ComboValue 側で更新）
        if (p1ComboMax != null) p1ComboMax.text = "×";
        if (p2ComboMax != null) p2ComboMax.text = "×";
    }

    // =====================================================
    // 更新ループ
    // =====================================================

    void Update()
    {
        if (GameManager.Instance == null) return;

        UpdatePlayerHUD(1, p1HpFill, p1HpValue, p1ComboValue, p1ScoreValue,
                            p1ItemInfoRoot, p1ItemName, p1ItemDuration,
                            p1EnergyFill, p1SkillName, p1RoundWins);
        UpdatePlayerHUD(2, p2HpFill, p2HpValue, p2ComboValue, p2ScoreValue,
                            p2ItemInfoRoot, p2ItemName, p2ItemDuration,
                            p2EnergyFill, p2SkillName, p2RoundWins);

        UpdateStatusText();
        UpdateVictoryBar();
        UpdateIncoming();
    }

    // P1HP/(P1HP+P2HP) を毎フレーム反映。左に傾く=P1優勢（DESIGN.md 12.5）
    private void UpdateVictoryBar()
    {
        if (victoryBar == null) return;
        var gm = GameManager.Instance;
        float p1 = gm.GetHP(1);
        float p2 = gm.GetHP(2);
        float total = p1 + p2;
        victoryBar.fillAmount = total > 0f ? p1 / total : 0.5f;
    }

    private void UpdatePlayerHUD(int playerIndex,
                                 Image hpFill, TextMeshProUGUI hpValue,
                                 TextMeshProUGUI comboValue, TextMeshProUGUI scoreValue,
                                 GameObject itemRoot, TextMeshProUGUI itemName, TextMeshProUGUI itemDuration,
                                 Image energyFill, TextMeshProUGUI skillName, TextMeshProUGUI roundWins)
    {
        var gm = GameManager.Instance;

        // HP
        if (hpFill != null)
        {
            float ratio = gm.GetHPRatio(playerIndex);
            hpFill.fillAmount = ratio;
            hpFill.color      = GetHPColor(ratio);
        }
        if (hpValue   != null) hpValue.text   = gm.GetHP(playerIndex).ToString();
        // コンボは UI 上 99 で表示頭打ち（内部値は維持, DESIGN.md 5.8）
        if (comboValue != null) comboValue.text = Mathf.Min(99, gm.GetCombo(playerIndex)).ToString();
        if (scoreValue != null) scoreValue.text = gm.GetScore(playerIndex).ToString("N0");

        // Active Item
        UpdateActiveItem(playerIndex, itemRoot, itemName, itemDuration);

        // 任意セクション
        if (energyFill != null) energyFill.fillAmount = gm.GetEnergyRatio(playerIndex);
        if (skillName != null)
        {
            string name = gm.GetEquippedSkillName(playerIndex);
            bool ready = gm.GetEnergyRatio(playerIndex) >= 1f;
            skillName.text = ready ? name + skillReadySuffix : name;
        }
        if (roundWins != null) roundWins.text = gm.GetRoundWins(playerIndex).ToString();
    }

    private void UpdateActiveItem(int playerIndex,
                                  GameObject itemRoot,
                                  TextMeshProUGUI itemName,
                                  TextMeshProUGUI itemDuration)
    {
        var gm = GameManager.Instance;
        string name      = gm.GetActiveItemName(playerIndex);
        float  remaining = gm.GetActiveItemRemaining(playerIndex);
        bool   active    = name != null && remaining > 0f;

        if (itemRoot != null && itemRoot.activeSelf != active)
            itemRoot.SetActive(active);

        if (active)
        {
            if (itemName     != null) itemName.text     = name;
            if (itemDuration != null) itemDuration.text = remaining.ToString("0.0") + "s";
        }
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        var state = GameManager.Instance.GetCurrentState();
        // MatchOver は MatchResultUI が担当
        if (state == GameManager.GameState.RoundOver)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Round Over!";
        }
        else
        {
            statusText.gameObject.SetActive(false);
        }
    }

    private Color GetHPColor(float ratio)
    {
        if (ratio <= lowThreshold) return hpColorLow;
        if (ratio <= midThreshold) return hpColorMid;
        return hpColorFull;
    }

    // =====================================================
    // 妨害オーバーレイ（任意・GameManager から呼ばれる）
    // =====================================================

    public void ShowInterferenceOverlay(int playerIndex, string label)
    {
        CanvasGroup     cg  = playerIndex == 1 ? p1InterferenceOverlay : p2InterferenceOverlay;
        TextMeshProUGUI txt = playerIndex == 1 ? p1InterferenceLabel   : p2InterferenceLabel;
        if (cg == null) return;

        ref Coroutine slot = ref (playerIndex == 1 ? ref p1OverlayRoutine : ref p2OverlayRoutine);
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(OverlayRoutine(cg, txt, label));
    }

    private IEnumerator OverlayRoutine(CanvasGroup cg, TextMeshProUGUI txt, string label)
    {
        if (txt != null) txt.text = $"妨害！\n{label}";
        cg.alpha = 1f;
        yield return new WaitForSecondsRealtime(overlayFlashDuration);
        cg.alpha = 0f;
    }

    // =====================================================
    // 攻撃送付ラベル（攻撃者 HUD に SENT → 表示。GameManager から呼ばれる）
    // =====================================================

    public void ShowSentLabel(int attackerIndex, string interferenceLabel)
    {
        TextMeshProUGUI txt = attackerIndex == 1 ? p1SentLabel : p2SentLabel;
        if (txt == null) return;

        ref Coroutine slot = ref (attackerIndex == 1 ? ref p1SentRoutine : ref p2SentRoutine);
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(SentLabelRoutine(txt, interferenceLabel));
    }

    private IEnumerator SentLabelRoutine(TextMeshProUGUI txt, string label)
    {
        txt.text = $"SENT → {label}";
        txt.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(sentLabelDuration);
        txt.gameObject.SetActive(false);
    }

    // =====================================================
    // コンボマイルストーン演出（10/20/30 到達時。GameManager から呼ばれる）
    // =====================================================

    public void ShowComboMilestone(int playerIndex, int milestone)
    {
        CanvasGroup     cg  = playerIndex == 1 ? p1ComboMilestoneOverlay : p2ComboMilestoneOverlay;
        TextMeshProUGUI txt = playerIndex == 1 ? p1ComboMilestoneLabel   : p2ComboMilestoneLabel;
        if (cg == null) return;

        ref Coroutine slot = ref (playerIndex == 1 ? ref p1MilestoneRoutine : ref p2MilestoneRoutine);
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(ComboMilestoneRoutine(cg, txt, milestone));
    }

    private IEnumerator ComboMilestoneRoutine(CanvasGroup cg, TextMeshProUGUI txt, int milestone)
    {
        if (txt != null) txt.text = $"{milestone} COMBO!!";
        cg.alpha = 1f;
        yield return new WaitForSecondsRealtime(comboMilestoneDuration);
        cg.alpha = 0f;
    }

    // =====================================================
    // Incoming インジケータ（妨害予約キュー。GameManager から呼ばれる）
    //   targetPlayerIndex = 妨害を受ける側。左列=P1, 右列=P2
    //   FIFO 最大 3、incomingDisplaySec 経過で自動失効、Playing 以外で全消去
    // =====================================================

    public void PushIncoming(int targetPlayerIndex, GameManager.InterferenceType type)
    {
        var list = targetPlayerIndex == 1 ? p1Incoming : p2Incoming;
        list.Add(new IncomingEntry { symbol = IncomingSymbol(type),
                                     expireTime = Time.unscaledTime + incomingDisplaySec });
        while (list.Count > 3) list.RemoveAt(0); // 4個目到着で最古を押し出す
    }

    private static string IncomingSymbol(GameManager.InterferenceType type) => type switch
    {
        GameManager.InterferenceType.Harden => "⬛HARD",
        GameManager.InterferenceType.AddRow => "↓ROW",
        GameManager.InterferenceType.Poison => "☣PSION",
        GameManager.InterferenceType.Slow   => "🐌SLOW",
        _                                   => "?"
    };

    private void UpdateIncoming()
    {
        bool playing = GameManager.Instance.GetCurrentState() == GameManager.GameState.Playing;
        if (!playing)
        {
            if (p1Incoming.Count > 0) p1Incoming.Clear();
            if (p2Incoming.Count > 0) p2Incoming.Clear();
        }
        RenderIncoming(p1Incoming, p1IncomingSlots);
        RenderIncoming(p2Incoming, p2IncomingSlots);
    }

    private void RenderIncoming(List<IncomingEntry> list, TextMeshProUGUI[] slots)
    {
        // 失効したエントリを除去（新しいほど後ろ）
        for (int i = list.Count - 1; i >= 0; i--)
            if (Time.unscaledTime >= list[i].expireTime) list.RemoveAt(i);

        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            bool show = i < list.Count;
            if (slots[i].gameObject.activeSelf != show) slots[i].gameObject.SetActive(show);
            if (show) slots[i].text = list[i].symbol;
        }
    }
}
