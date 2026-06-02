using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public static class SetupMatchResultUI
{
    // 旧 CenterUI 前提のレガシー処理。新 UI では実行しないため MenuItem は外している。
    public static void Setup()
    {
        var centerUI = GameObject.Find("CenterUI");
        if (centerUI == null)
        {
            Debug.LogError("[SetupMatchResultUI] CenterUI が見つかりません。");
            return;
        }

        // MatchResultPanel を冪等生成
        Transform existing = centerUI.transform.Find("MatchResultPanel");
        GameObject panel = existing != null ? existing.gameObject : CreatePanel(centerUI.transform);

        // MatchResultUI コンポーネントを CenterUI にバインド（冪等）
        MatchResultUI ui = centerUI.GetComponent<MatchResultUI>()
                        ?? centerUI.AddComponent<MatchResultUI>();

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("matchResultPanel").objectReferenceValue = panel;
        so.FindProperty("matchWinnerText").objectReferenceValue  =
            panel.transform.Find("MatchWinnerText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("scoreSummaryText").objectReferenceValue =
            panel.transform.Find("ScoreSummaryText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("winsSummaryText").objectReferenceValue  =
            panel.transform.Find("WinsSummaryText")?.GetComponent<TextMeshProUGUI>();

        Transform selPanel = panel.transform.Find("SelectionPanel");
        so.FindProperty("rematchText").objectReferenceValue =
            selPanel?.Find("RematchText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("menuText").objectReferenceValue    =
            selPanel?.Find("MenuText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("hintText").objectReferenceValue    =
            panel.transform.Find("HintText")?.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        panel.SetActive(false);

        EditorUtility.SetDirty(centerUI);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(centerUI.scene);
        Debug.Log("[SetupMatchResultUI] セットアップ完了。");
    }

    private static GameObject CreatePanel(Transform parent)
    {
        // 半透明背景パネル（画面中央）
        var panel = CreateGO("MatchResultPanel", parent);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.82f);
        SetAnchors(panel, new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.75f));

        // タイトル
        CreateTMP("MatchWinnerText",  panel.transform, new Vector2(0.5f, 0.8f),  new Vector2(600, 80),  "P1 WINS!", 52, TextAlignmentOptions.Center, Color.yellow);
        CreateTMP("ScoreSummaryText", panel.transform, new Vector2(0.5f, 0.62f), new Vector2(500, 40),  "P1: 0 pts    P2: 0 pts",   24, TextAlignmentOptions.Center, Color.white);
        CreateTMP("WinsSummaryText",  panel.transform, new Vector2(0.5f, 0.5f),  new Vector2(500, 40),  "P1: 0 wins    P2: 0 wins", 24, TextAlignmentOptions.Center, Color.white);

        // 選択肢エリア
        var selPanel = CreateGO("SelectionPanel", panel.transform);
        SetAnchorPoint(selPanel, new Vector2(0.5f, 0.3f), new Vector2(500, 50));
        var layout = selPanel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 60f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        CreateTMP("RematchText", selPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(180, 50), "[ Rematch ]", 30, TextAlignmentOptions.Center, Color.yellow);
        CreateTMP("MenuText",    selPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(260, 50), "[ Menu ]",    30, TextAlignmentOptions.Center, Color.white);

        // ヒント
        CreateTMP("HintText", panel.transform, new Vector2(0.5f, 0.12f), new Vector2(520, 34),
            "A / D  (or  J / L)  to select    Space to confirm",
            17, TextAlignmentOptions.Center, new Color(0.65f, 0.65f, 0.65f));

        return panel;
    }

    // ---- ヘルパー ----

    private static GameObject CreateGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // stretch アンカー（パネル全体に広げる）
    private static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // 単一アンカーポイント（中央揃え）
    private static void SetAnchorPoint(GameObject go, Vector2 anchor, Vector2 sizeDelta)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent,
        Vector2 anchor, Vector2 sizeDelta, string text,
        float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = CreateGO(name, parent);
        SetAnchorPoint(go, anchor, sizeDelta);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = align;
        tmp.color     = color;
        return tmp;
    }
}
