using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HP / エネルギーゲージ UI のフルセットアップ（冪等）
// レイアウト方針: 左上 / 右上コーナーに縦 ~130px 以内に収め、
//   ブロックスポーン最上段との重なりを避ける
// 旧 CenterUI 前提のレガシー処理。新 UI では実行しないため MenuItem は外している。
public static class SetupHPUI
{
    private const string CENTER_UI_NAME = "CenterUI";

    public static void Setup()
    {
        // GameObject.Find は execute_script 環境では動かないため Canvas から探す
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.gameObject.name != CENTER_UI_NAME)
        {
            // name が違う場合は全 Canvas を検索
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            canvas = System.Array.Find(all, c => c.gameObject.name == CENTER_UI_NAME);
        }
        if (canvas == null)
        {
            Debug.LogError($"[SetupHPUI] '{CENTER_UI_NAME}' が見つかりません。");
            return;
        }
        GameObject centerUI = canvas.gameObject;

        UIManager ui = centerUI.GetComponent<UIManager>();
        if (ui == null) ui = centerUI.AddComponent<UIManager>();

        TMP_FontAsset latinFont = FindLatinFont();

        // ── P1（左上）──────────────────────────────────────────────
        // Score  |  Wins
        // [======HPBar=======]
        // HP xxx/xxx
        // [==EnergyBar==]  SkillName
        // Combo x/y
        var p1Score  = EnsureText(centerUI, "P1Score",
            anchor: new Vector2(0,1), pivot: new Vector2(0,1),
            pos: new Vector2(10,-8),  size: new Vector2(120,28), fontSize: 20, font: latinFont);

        var p1Wins   = EnsureText(centerUI, "P1Wins",
            anchor: new Vector2(0,1), pivot: new Vector2(0,1),
            pos: new Vector2(135,-8), size: new Vector2(75,28),  fontSize: 16, font: latinFont);

        var p1HPFill = EnsureBar(centerUI, "P1HPFill",
            anchor: new Vector2(0,1), pivot: new Vector2(0,1),
            pos: new Vector2(10,-40), size: new Vector2(200,12));

        var p1HPText = EnsureText(centerUI, "P1HPText",
            anchor: new Vector2(0,1), pivot: new Vector2(0,1),
            pos: new Vector2(10,-55), size: new Vector2(200,22), fontSize: 14, font: latinFont);

        var p1EnergyFill = EnsureBar(centerUI, "P1EnergyFill",
            anchor: new Vector2(0,1), pivot: new Vector2(0,1),
            pos: new Vector2(10,-82), size: new Vector2(120,8),
            color: new Color(0.3f, 0.7f, 1f));

        var p1SkillText = EnsureText(centerUI, "P1SkillText",
            anchor: new Vector2(0,1), pivot: new Vector2(0,1),
            pos: new Vector2(135,-78), size: new Vector2(75,18), fontSize: 12, font: latinFont);

        var p1Combo = EnsureText(centerUI, "P1Combo",
            anchor: new Vector2(0,1), pivot: new Vector2(0,1),
            pos: new Vector2(10,-96), size: new Vector2(200,22), fontSize: 13, font: latinFont);

        // ── P2（右上）──────────────────────────────────────────────
        var p2Score  = EnsureText(centerUI, "P2Score",
            anchor: new Vector2(1,1), pivot: new Vector2(1,1),
            pos: new Vector2(-10,-8),  size: new Vector2(120,28), fontSize: 20, font: latinFont);

        var p2Wins   = EnsureText(centerUI, "P2Wins",
            anchor: new Vector2(1,1), pivot: new Vector2(1,1),
            pos: new Vector2(-135,-8), size: new Vector2(75,28),  fontSize: 16, font: latinFont);

        var p2HPFill = EnsureBar(centerUI, "P2HPFill",
            anchor: new Vector2(1,1), pivot: new Vector2(1,1),
            pos: new Vector2(-10,-40), size: new Vector2(200,12));

        var p2HPText = EnsureText(centerUI, "P2HPText",
            anchor: new Vector2(1,1), pivot: new Vector2(1,1),
            pos: new Vector2(-10,-55), size: new Vector2(200,22), fontSize: 14, font: latinFont);

        var p2EnergyFill = EnsureBar(centerUI, "P2EnergyFill",
            anchor: new Vector2(1,1), pivot: new Vector2(1,1),
            pos: new Vector2(-10,-82), size: new Vector2(120,8),
            color: new Color(0.3f, 0.7f, 1f));

        var p2SkillText = EnsureText(centerUI, "P2SkillText",
            anchor: new Vector2(1,1), pivot: new Vector2(1,1),
            pos: new Vector2(-135,-78), size: new Vector2(75,18), fontSize: 12, font: latinFont);

        var p2Combo = EnsureText(centerUI, "P2Combo",
            anchor: new Vector2(1,1), pivot: new Vector2(1,1),
            pos: new Vector2(-10,-96), size: new Vector2(200,22), fontSize: 13, font: latinFont);

        // GameOverText（中央上）
        var status = EnsureText(centerUI, "GameOverText",
            anchor: new Vector2(0.5f,1f), pivot: new Vector2(0.5f,1f),
            pos: new Vector2(0,-8), size: new Vector2(300,36), fontSize: 24, font: latinFont);

        // ── 妨害通知オーバーレイ（左半分=P1、右半分=P2） ──────────
        var p1Overlay      = EnsureOverlayPanel(centerUI, "P1InterferenceOverlay",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.5f, 1f));
        var p1OverlayLabel = EnsureOverlayText(p1Overlay.gameObject, "P1InterferenceLabel", latinFont);

        var p2Overlay      = EnsureOverlayPanel(centerUI, "P2InterferenceOverlay",
            anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(1f, 1f));
        var p2OverlayLabel = EnsureOverlayText(p2Overlay.gameObject, "P2InterferenceLabel", latinFont);

        // オーバーレイは最前面に（最後の兄弟として配置）
        p1Overlay.transform.SetAsLastSibling();
        p2Overlay.transform.SetAsLastSibling();

        // ── UIManager にバインド ────────────────────────────────────
        SerializedObject so = new SerializedObject(ui);
        Bind(so, "p1ScoreText",             p1Score);
        Bind(so, "p2ScoreText",             p2Score);
        Bind(so, "p1HPText",                p1HPText);
        Bind(so, "p2HPText",                p2HPText);
        Bind(so, "p1HPFill",                p1HPFill);
        Bind(so, "p2HPFill",                p2HPFill);
        Bind(so, "p1ComboText",             p1Combo);
        Bind(so, "p2ComboText",             p2Combo);
        Bind(so, "p1RoundWinsText",         p1Wins);
        Bind(so, "p2RoundWinsText",         p2Wins);
        Bind(so, "p1EnergyFill",            p1EnergyFill);
        Bind(so, "p2EnergyFill",            p2EnergyFill);
        Bind(so, "p1SkillText",             p1SkillText);
        Bind(so, "p2SkillText",             p2SkillText);
        Bind(so, "statusText",              status);
        Bind(so, "p1InterferenceOverlay",   p1Overlay.GetComponent<CanvasGroup>());
        Bind(so, "p1InterferenceLabel",     p1OverlayLabel);
        Bind(so, "p2InterferenceOverlay",   p2Overlay.GetComponent<CanvasGroup>());
        Bind(so, "p2InterferenceLabel",     p2OverlayLabel);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(ui);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);

        Debug.Log("[SetupHPUI] 完了（エネルギーゲージ含む全 UI をセットアップ）。" +
                  (latinFont == null ? " ※フォント未検出：Inspector で手動設定してください。" : ""));
    }

    // ── ヘルパー ──────────────────────────────────────────────────

    private static TextMeshProUGUI EnsureText(GameObject parent, string name,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fontSize, TMP_FontAsset font)
    {
        Transform ex = parent.transform.Find(name);
        GameObject go = ex != null ? ex.gameObject : new GameObject(name);
        if (ex == null) go.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color     = Color.white;
        if (string.IsNullOrEmpty(tmp.text)) tmp.text = name;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return tmp;
    }

    private static Image EnsureBar(GameObject parent, string name,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        Color? color = null)
    {
        Transform ex = parent.transform.Find(name);
        GameObject go = ex != null ? ex.gameObject : new GameObject(name);
        if (ex == null) go.transform.SetParent(parent.transform, false);

        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite     = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        img.color      = color ?? new Color(0.4f, 1.0f, 0.4f);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return img;
    }

    private static void Bind(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
        else Debug.LogWarning($"[SetupHPUI] UIManager に '{prop}' が見つかりません。");
    }

    // 妨害通知用: 画面半分を覆う半透明パネル（CanvasGroup 付き、初期 alpha=0）
    private static CanvasGroup EnsureOverlayPanel(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        Transform ex = parent.transform.Find(name);
        GameObject go = ex != null ? ex.gameObject : new GameObject(name);
        if (ex == null) go.transform.SetParent(parent.transform, false);

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha          = 0f;
        cg.interactable   = false;
        cg.blocksRaycasts = false;

        // 背景 Image
        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color         = new Color(1f, 0.1f, 0.1f, 0.4f);
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;

        return cg;
    }

    // 妨害通知用テキスト（オーバーレイパネルの子、中央配置）
    private static TextMeshProUGUI EnsureOverlayText(GameObject overlayPanel,
        string name, TMP_FontAsset font)
    {
        Transform ex = overlayPanel.transform.Find(name);
        GameObject go = ex != null ? ex.gameObject : new GameObject(name);
        if (ex == null) go.transform.SetParent(overlayPanel.transform, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize  = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.text      = "";

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;

        return tmp;
    }

    private static TMP_FontAsset FindLatinFont()
    {
        string[] guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
        foreach (string g in guids)
        {
            TMP_FontAsset f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
            if (f != null) return f;
        }
        return null;
    }
}
