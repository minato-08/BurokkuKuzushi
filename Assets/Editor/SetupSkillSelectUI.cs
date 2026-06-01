using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupSkillSelectUI
{
    // 旧 CenterUI 前提のレガシー処理。新 UI では実行しないため MenuItem は外している。
    public static void Setup()
    {
        Canvas centerUI = Object.FindFirstObjectByType<Canvas>();
        if (centerUI == null)
        {
            Debug.LogError("[SetupSkillSelectUI] Canvas が見つかりません。");
            return;
        }

        // ラテン文字を含む LiberationSans SDF をプロジェクト内から探す
        TMP_FontAsset latinFont = FindLatinFont();

        // SkillSelectUI コンポーネントを CenterUI に追加（冪等）
        SkillSelectUI ui = centerUI.GetComponent<SkillSelectUI>();
        if (ui == null) ui = centerUI.gameObject.AddComponent<SkillSelectUI>();

        // SkillSelectPanel を生成（冪等）
        Transform existing = centerUI.transform.Find("SkillSelectPanel");
        GameObject panel;
        if (existing != null)
        {
            panel = existing.gameObject;
        }
        else
        {
            panel = new GameObject("SkillSelectPanel");
            panel.transform.SetParent(centerUI.transform, false);
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        // ヘッダー
        CreateLabel(panel.transform, "HeaderText",
            "SELECT YOUR SKILL",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f), new Vector2(500f, 50f), 28, latinFont);

        // P1 側
        TextMeshProUGUI p1Skill = CreateLabel(panel.transform, "P1SkillText",
            "< Paddle Enlarge >",
            new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f),
            Vector2.zero, new Vector2(320f, 50f), 24, latinFont);

        TextMeshProUGUI p1Status = CreateLabel(panel.transform, "P1StatusText",
            "A / D  select     S  confirm",
            new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f),
            new Vector2(0f, -55f), new Vector2(320f, 36f), 16, latinFont);

        // P2 側
        TextMeshProUGUI p2Skill = CreateLabel(panel.transform, "P2SkillText",
            "< Paddle Enlarge >",
            new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f),
            Vector2.zero, new Vector2(320f, 50f), 24, latinFont);

        TextMeshProUGUI p2Status = CreateLabel(panel.transform, "P2StatusText",
            "J / L  select     K  confirm",
            new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f),
            new Vector2(0f, -55f), new Vector2(320f, 36f), 16, latinFont);

        // SkillSelectUI に参照をバインド
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue        = panel;
        so.FindProperty("p1SkillText").objectReferenceValue  = p1Skill;
        so.FindProperty("p2SkillText").objectReferenceValue  = p2Skill;
        so.FindProperty("p1StatusText").objectReferenceValue = p1Status;
        so.FindProperty("p2StatusText").objectReferenceValue = p2Status;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(centerUI.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SetupSkillSelectUI] 完了。" + (latinFont == null ? " ※フォント未検出：Inspector で手動設定してください。" : ""));
    }

    // プロジェクト内から LiberationSans SDF を探す（TMP 標準付属フォント）
    private static TMP_FontAsset FindLatinFont()
    {
        string[] guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return font;
        }
        return null;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, int fontSize,
        TMP_FontAsset font)
    {
        Transform ex = parent.Find(name);
        GameObject go;
        if (ex != null)
        {
            go = ex.gameObject;
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();

        if (font != null) tmp.font = font;
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        return tmp;
    }
}
