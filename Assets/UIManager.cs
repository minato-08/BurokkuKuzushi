using System.Collections;
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

    private Coroutine p1OverlayRoutine;
    private Coroutine p2OverlayRoutine;

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
        if (comboValue != null) comboValue.text = gm.GetCombo(playerIndex).ToString();
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
        yield return new WaitForSecondsRealtime(1.5f);
        cg.alpha = 0f;
    }
}
