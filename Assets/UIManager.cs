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

    [Header("[演出] Danger Proximity（死線接近, DESIGN.md 5.4）")]
    [SerializeField] private SpriteRenderer p1BlockDeadLine;   // P1BlockDeadLine
    [SerializeField] private SpriteRenderer p2BlockDeadLine;   // P2BlockDeadLine
    [SerializeField] private float dangerRange         = 1.5f; // blockDeadZoneY + これ以内で点滅開始
    [SerializeField] private float dangerBlinkSlow     = 0.4f; // 入った瞬間の点滅周期（秒）
    [SerializeField] private float dangerBlinkFast     = 0.15f;// 死線直前の点滅周期（秒）
    [SerializeField] private Color dangerColor         = new Color(1f, 0.10f, 0.10f, 1f); // 点滅の色相
    [SerializeField] private float dangerIntensityMin  = 0.8f;  // 点滅 HDR Intensity 下限（太さは変えない）
    [SerializeField] private float dangerIntensityMax  = 4.5f;  // 点滅 HDR Intensity 上限（Bloom 発光）
    [SerializeField] private float deadLineFlashDuration   = 0.2f; // 底到達ペナルティ白フラッシュの片道秒
    [SerializeField] private float deadLineFlashThickenMul = 3.0f; // 白フラッシュ時の太さ倍率
    [SerializeField] private Color deadLineFlashColor      = Color.white; // フラッシュ色相
    [SerializeField] private float deadLineFlashIntensity  = 5.0f; // フラッシュ HDR Intensity
    [SerializeField] private string deadLineTintProperty   = "_TintColor"; // UI/HDRTint の HDR 色プロパティ

    [Header("[演出] Last Stand（HP10%, DESIGN.md 5.10）")]
    [SerializeField] private SpriteRenderer p1ArenaFrame;      // P1ArenaFrame（アラーム赤化）
    [SerializeField] private SpriteRenderer p2ArenaFrame;      // P2ArenaFrame
    [Range(0f, 1f)] [SerializeField] private float lastStandThreshold = 0.10f;
    [SerializeField] private float lastStandBlinkPeriod = 0.55f; // 明滅周期（消えかけ電球風・ゆっくりめ）
    [Range(0f, 1f)] [SerializeField] private float lastStandDimFloor = 0.28f; // 明るさを落とす下限
    [SerializeField] private string panicReadyLabel    = "PANIC READY";

    // HP バーは Sliced のまま RectTransform.width を HP 比率で縮める（fillAmount は Sliced で無効）。
    // pivot.x=0（左固定）なので幅を減らすと右から削れる。フル幅を Start でキャッシュ。
    private float p1HpFillMaxWidth, p2HpFillMaxWidth;

    // Danger / Last Stand のランタイム状態（元色・元スケール・インスタンスマテリアルのキャッシュ）
    private Color   p1FrameOrig, p2FrameOrig;
    private Vector3 p1DeadLineScale = Vector3.one, p2DeadLineScale = Vector3.one;
    private Material p1DeadLineMat, p2DeadLineMat;                       // sr.material（renderer 毎にインスタンス化）
    private Color   p1DeadLineTintOrig = Color.white, p2DeadLineTintOrig = Color.white;
    private bool  p1DeadLineFlashing, p2DeadLineFlashing;
    private Coroutine p1DeadLineFlashRoutine, p2DeadLineFlashRoutine;
    private bool  p1WasLastStand, p2WasLastStand;

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

        // HP バーのフル幅をキャッシュ（width で削る基準）
        if (p1HpFill != null) p1HpFillMaxWidth = p1HpFill.rectTransform.sizeDelta.x;
        if (p2HpFill != null) p2HpFillMaxWidth = p2HpFill.rectTransform.sizeDelta.x;

        // 死線ラインはインスタンスマテリアルと元 _TintColor・元スケールをキャッシュ
        // （sr.material は renderer 毎に複製されるので P1/P2 を独立に駆動できる）
        if (p1BlockDeadLine != null)
        {
            p1DeadLineMat   = p1BlockDeadLine.material;
            p1DeadLineScale = p1BlockDeadLine.transform.localScale;
            if (p1DeadLineMat.HasProperty(deadLineTintProperty)) p1DeadLineTintOrig = p1DeadLineMat.GetColor(deadLineTintProperty);
        }
        if (p2BlockDeadLine != null)
        {
            p2DeadLineMat   = p2BlockDeadLine.material;
            p2DeadLineScale = p2BlockDeadLine.transform.localScale;
            if (p2DeadLineMat.HasProperty(deadLineTintProperty)) p2DeadLineTintOrig = p2DeadLineMat.GetColor(deadLineTintProperty);
        }
        if (p1ArenaFrame != null) p1FrameOrig = p1ArenaFrame.color;
        if (p2ArenaFrame != null) p2FrameOrig = p2ArenaFrame.color;
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

        UpdateDangerLine(1);
        UpdateDangerLine(2);
        UpdateLastStand(1, p1HpFill, p1ArenaFrame);
        UpdateLastStand(2, p2HpFill, p2ArenaFrame);
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

        // HP（Sliced のまま width を縮める。pivot.x=0 なので右から削れる）
        if (hpFill != null)
        {
            float ratio = Mathf.Clamp01(gm.GetHPRatio(playerIndex));
            float maxW  = playerIndex == 1 ? p1HpFillMaxWidth : p2HpFillMaxWidth;
            RectTransform rt = hpFill.rectTransform;
            Vector2 sd = rt.sizeDelta;
            sd.x = maxW * ratio;
            rt.sizeDelta = sd;
            hpFill.color = GetHPColor(ratio);
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
            // 緊急スキル（SkillPanic_BlockClear）が発動可能になったら PANIC READY で上書き（DESIGN.md 5.10）
            if (gm.IsPanicReady(playerIndex))
            {
                skillName.text = panicReadyLabel;
            }
            else
            {
                string name = gm.GetEquippedSkillName(playerIndex);
                bool ready = gm.GetEnergyRatio(playerIndex) >= 1f;
                skillName.text = ready ? name + skillReadySuffix : name;
            }
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
    // Danger Proximity（死線接近で死線ラインを赤点滅, DESIGN.md 5.4）
    // =====================================================

    private void UpdateDangerLine(int playerIndex)
    {
        Material mat = playerIndex == 1 ? p1DeadLineMat : p2DeadLineMat;
        if (mat == null) return;
        // 底到達ペナルティの白フラッシュ中はそちらが色を制御するので触らない
        if (playerIndex == 1 ? p1DeadLineFlashing : p2DeadLineFlashing) return;

        var gm = GameManager.Instance;
        Color tintOrig = playerIndex == 1 ? p1DeadLineTintOrig : p2DeadLineTintOrig;

        float lowestY     = gm.GetLowestBlockY(playerIndex);
        float dangerStart = gm.GetBlockDeadZoneY(playerIndex) + dangerRange;

        if (gm.GetCurrentState() != GameManager.GameState.Playing || lowestY > dangerStart)
        {
            mat.SetColor(deadLineTintProperty, tintOrig); // 平常時の見た目へ
            return;
        }

        // 0 = 危険域に入った瞬間, 1 = 死線到達。近いほど点滅周期が短い（速い）
        float t      = Mathf.Clamp01((dangerStart - lowestY) / Mathf.Max(0.001f, dangerRange));
        float period = Mathf.Lerp(dangerBlinkSlow, dangerBlinkFast, t);
        float wave   = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.01f, period)) * 0.5f + 0.5f;

        // 太さは変えず、HDR _TintColor の Intensity（明るさ）だけ脈動させて Bloom で発光させる。
        // alpha も wave に連動させ、谷で完全に消える（0 まで下がる）on/off 点滅にする
        float intensity = Mathf.Lerp(dangerIntensityMin, dangerIntensityMax, wave);
        Color c = dangerColor * intensity;
        c.a = dangerColor.a * wave;
        mat.SetColor(deadLineTintProperty, c);
    }

    // 底到達でペナルティが発生した瞬間、死線ラインを 1s 白くフラッシュ（ArenaController 経由で呼ばれる）
    public void FlashDangerLine(int playerIndex)
    {
        SpriteRenderer line = playerIndex == 1 ? p1BlockDeadLine : p2BlockDeadLine;
        if (line == null) return;

        if (playerIndex == 1)
        {
            if (p1DeadLineFlashRoutine != null) StopCoroutine(p1DeadLineFlashRoutine);
            p1DeadLineFlashRoutine = StartCoroutine(DeadLineFlashRoutine(1, line));
        }
        else
        {
            if (p2DeadLineFlashRoutine != null) StopCoroutine(p2DeadLineFlashRoutine);
            p2DeadLineFlashRoutine = StartCoroutine(DeadLineFlashRoutine(2, line));
        }
    }

    private IEnumerator DeadLineFlashRoutine(int playerIndex, SpriteRenderer line)
    {
        Material mat      = playerIndex == 1 ? p1DeadLineMat      : p2DeadLineMat;
        Color    tintOrig = playerIndex == 1 ? p1DeadLineTintOrig : p2DeadLineTintOrig;
        Vector3  origScale= playerIndex == 1 ? p1DeadLineScale    : p2DeadLineScale;
        if (mat == null) yield break;
        if (playerIndex == 1) p1DeadLineFlashing = true; else p2DeadLineFlashing = true;

        Color flashTint = deadLineFlashColor * deadLineFlashIntensity; // HDR 白フラッシュ
        flashTint.a = deadLineFlashColor.a;

        float t = 0f;
        while (t < deadLineFlashDuration)
        {
            float k = 1f - (t / deadLineFlashDuration); // 1→0 でフラッシュ色から元色へ減衰
            mat.SetColor(deadLineTintProperty, Color.Lerp(tintOrig, flashTint, k));
            Vector3 s = origScale;
            s.y = origScale.y * Mathf.Lerp(1f, deadLineFlashThickenMul, k); // 太く→元の太さへ
            line.transform.localScale = s;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        mat.SetColor(deadLineTintProperty, tintOrig);
        line.transform.localScale = origScale;

        if (playerIndex == 1) { p1DeadLineFlashing = false; p1DeadLineFlashRoutine = null; }
        else                  { p2DeadLineFlashing = false; p2DeadLineFlashRoutine = null; }
    }

    // =====================================================
    // Last Stand（HP10% 以下でアリーナ枠と HP バーを赤アラーム, DESIGN.md 5.10）
    // =====================================================

    private void UpdateLastStand(int playerIndex, Image hpFill, SpriteRenderer frame)
    {
        var gm = GameManager.Instance;
        bool last = gm.GetCurrentState() == GameManager.GameState.Playing
                    && gm.GetHPRatio(playerIndex) <= lastStandThreshold;
        bool was  = playerIndex == 1 ? p1WasLastStand : p2WasLastStand;

        if (last)
        {
            float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.01f, lastStandBlinkPeriod)) * 0.5f + 0.5f;
            // 色は変えず明るさだけ周期的に落とす（消えかけの電球風）
            float bright = Mathf.Lerp(lastStandDimFloor, 1f, wave);

            if (frame != null)
            {
                // 枠は元の色のまま明滅（赤化しない）
                Color orig = playerIndex == 1 ? p1FrameOrig : p2FrameOrig;
                Color c = orig * bright;
                c.a = orig.a;
                frame.color = c;
            }
            if (hpFill != null)
            {
                // HP バーは低 HP の赤を保ったまま明滅
                Color c = hpColorLow * bright;
                c.a = hpColorLow.a;
                hpFill.color = c;
            }
        }
        else if (was && frame != null)
        {
            // 脱出フレームで枠を元色に戻す（HP バーは UpdatePlayerHUD が通常色へ戻す）
            frame.color = playerIndex == 1 ? p1FrameOrig : p2FrameOrig;
        }

        if (playerIndex == 1) p1WasLastStand = last; else p2WasLastStand = last;
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
        AudioManager.Instance?.PlayComboMilestone(milestone, playerIndex); // マイルストーン番号でピッチ可変（DESIGN.md 10.4）
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
