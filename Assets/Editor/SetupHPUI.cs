using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HP UI のフルセットアップを自動で行う。何度実行しても安全（冪等）。
// - CenterUI に UIManager コンポーネントがなければ自動追加
// - CenterUI 配下の既存テキスト（P1Score/P2Score/P1Combo/P2Combo/P1Wins/P2Wins/GameOverText）を
//   名前で探して UIManager にバインド
// - HPテキストは既存の P1HPText/P2HPText もしくは P1Lives/P2Lives を転用、なければ新規生成
// - HPバー（Image, Filled）は既存があれば再利用、なければ新規生成
// Unity メニュー [BurokkuKuzushi] > [Setup HP UI] から実行
public static class SetupHPUI
{
    private const string CENTER_UI_NAME = "CenterUI";
    private const float  HP_BAR_WIDTH   = 200f;
    private const float  HP_BAR_HEIGHT  = 14f;
    private const float  HP_TEXT_OFFSET_Y = -30f; // P1Score の下に HPText
    private const float  HP_BAR_OFFSET_Y  = -24f; // HPText の下に HPバー

    [MenuItem("BurokkuKuzushi/Setup HP UI")]
    public static void Setup()
    {
        // 1) CenterUI を探す
        GameObject centerUI = GameObject.Find(CENTER_UI_NAME);
        if (centerUI == null)
        {
            Debug.LogError($"[SetupHPUI] '{CENTER_UI_NAME}' が見つかりません。Canvas 構成を確認してください。");
            return;
        }

        // 2) UIManager コンポーネントを確保（なければ追加）
        UIManager ui = centerUI.GetComponent<UIManager>();
        if (ui == null)
        {
            ui = centerUI.AddComponent<UIManager>();
            Debug.Log("[SetupHPUI] UIManager コンポーネントを CenterUI に追加しました。");
        }

        // 3) 既存テキストを CenterUI 配下から名前で取得
        TextMeshProUGUI p1Score = FindText(centerUI, "P1Score");
        TextMeshProUGUI p2Score = FindText(centerUI, "P2Score");
        TextMeshProUGUI p1Combo = FindText(centerUI, "P1Combo");
        TextMeshProUGUI p2Combo = FindText(centerUI, "P2Combo");
        TextMeshProUGUI p1Wins  = FindText(centerUI, "P1Wins");
        TextMeshProUGUI p2Wins  = FindText(centerUI, "P2Wins");
        TextMeshProUGUI status  = FindText(centerUI, "GameOverText");

        // 4) HPテキスト：既存 P1HPText/P2HPText、なければ P1Lives/P2Lives を転用、それもなければ新規生成
        TextMeshProUGUI p1HPText = FindText(centerUI, "P1HPText") ?? TransferLivesText(centerUI, "P1Lives", "P1HPText");
        TextMeshProUGUI p2HPText = FindText(centerUI, "P2HPText") ?? TransferLivesText(centerUI, "P2Lives", "P2HPText");

        if (p1HPText == null) p1HPText = CreateHPText(centerUI, "P1HPText", p1Score);
        if (p2HPText == null) p2HPText = CreateHPText(centerUI, "P2HPText", p2Score);

        // 5) HPバー：既存があれば再利用、なければ HPテキストの下に新規生成
        Image p1Fill = FindImage(centerUI, "P1HPFill") ?? CreateHPBar(p1HPText, "P1HPFill");
        Image p2Fill = FindImage(centerUI, "P2HPFill") ?? CreateHPBar(p2HPText, "P2HPFill");

        // 6) UIManager に参照をバインド
        SerializedObject so = new SerializedObject(ui);
        BindProperty(so, "p1ScoreText",     p1Score);
        BindProperty(so, "p2ScoreText",     p2Score);
        BindProperty(so, "p1HPText",        p1HPText);
        BindProperty(so, "p2HPText",        p2HPText);
        BindProperty(so, "p1HPFill",        p1Fill);
        BindProperty(so, "p2HPFill",        p2Fill);
        BindProperty(so, "p1ComboText",     p1Combo);
        BindProperty(so, "p2ComboText",     p2Combo);
        BindProperty(so, "p1RoundWinsText", p1Wins);
        BindProperty(so, "p2RoundWinsText", p2Wins);
        BindProperty(so, "statusText",      status);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(ui);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);

        Debug.Log("[SetupHPUI] 完了。HP UI をセットアップしました。");
        Debug.Log("[SetupHPUI] HPバーの位置・サイズは初期値で配置しています。気になる場合は手動で調整してください。");
    }

    // =====================================================
    // ヘルパー：検索系
    // =====================================================

    private static TextMeshProUGUI FindText(GameObject root, string name)
    {
        Transform t = FindRecursive(root.transform, name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Image FindImage(GameObject root, string name)
    {
        Transform t = FindRecursive(root.transform, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    // =====================================================
    // ヘルパー：生成系
    // =====================================================

    // P1Lives / P2Lives が残っていれば名前を変えて HP テキストとして転用する
    private static TextMeshProUGUI TransferLivesText(GameObject centerUI, string oldName, string newName)
    {
        Transform t = FindRecursive(centerUI.transform, oldName);
        if (t == null) return null;

        TextMeshProUGUI text = t.GetComponent<TextMeshProUGUI>();
        if (text == null) return null;

        t.gameObject.name = newName;
        text.text = "HP 500 / 500";
        return text;
    }

    // 新規 HPテキストを生成（anchor となる既存テキストの直下に配置）
    private static TextMeshProUGUI CreateHPText(GameObject centerUI, string name, TextMeshProUGUI anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(centerUI.transform, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = "HP 500 / 500";
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Left;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (anchor != null)
        {
            RectTransform anchorRT = anchor.GetComponent<RectTransform>();
            rt.anchorMin = anchorRT.anchorMin;
            rt.anchorMax = anchorRT.anchorMax;
            rt.pivot     = anchorRT.pivot;
            rt.anchoredPosition = anchorRT.anchoredPosition + new Vector2(0f, HP_TEXT_OFFSET_Y);
            rt.sizeDelta        = new Vector2(HP_BAR_WIDTH, 30f);
        }
        else
        {
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(HP_BAR_WIDTH, 30f);
        }
        return text;
    }

    // 新規 HPバー（Image, FillType=Filled）を生成
    private static Image CreateHPBar(TextMeshProUGUI anchorText, string name)
    {
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(anchorText.transform.parent, false);

        RectTransform rt = bar.GetComponent<RectTransform>();
        RectTransform anchorRT = anchorText.GetComponent<RectTransform>();

        rt.anchorMin = anchorRT.anchorMin;
        rt.anchorMax = anchorRT.anchorMax;
        rt.pivot     = anchorRT.pivot;
        rt.anchoredPosition = anchorRT.anchoredPosition + new Vector2(0f, HP_BAR_OFFSET_Y);
        rt.sizeDelta        = new Vector2(HP_BAR_WIDTH, HP_BAR_HEIGHT);

        Image img = bar.GetComponent<Image>();
        // デフォルトの UI スプライトを設定（これがないと Filled モードで表示されない）
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        img.color      = new Color(0.4f, 1.0f, 0.4f);

        return img;
    }

    // =====================================================
    // ヘルパー：バインド
    // =====================================================

    private static void BindProperty(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogWarning($"[SetupHPUI] UIManager に '{propName}' フィールドが見つかりません。");
            return;
        }
        prop.objectReferenceValue = value;
    }
}
