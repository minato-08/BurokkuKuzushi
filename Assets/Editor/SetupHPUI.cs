using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 既存の P1Lives/P2Lives テキスト UI を HP表示用に再構成する
// - P1Lives/P2Lives テキストは HPテキストとして再利用
// - 同位置に HPバー（Image, FillType=Filled）を新規追加
// - UIManager の参照を再バインド
// Unity メニュー [BurokkuKuzushi] > [Setup HP UI] から実行
public static class SetupHPUI
{
    [MenuItem("BurokkuKuzushi/Setup HP UI")]
    public static void Setup()
    {
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (ui == null)
        {
            // UIManager コンポーネントが見つからない場合、CenterUI に自動アタッチする
            GameObject centerUI = GameObject.Find("CenterUI");
            if (centerUI == null)
            {
                Debug.LogError("[SetupHPUI] シーンに UIManager も CenterUI も見つかりません。Canvas 構成を確認してください。");
                return;
            }
            ui = centerUI.AddComponent<UIManager>();
            Debug.Log("[SetupHPUI] UIManager コンポーネントを CenterUI に自動アタッチしました。");
        }

        // P1Lives と P2Lives を見つけて、HP表示に転用する
        GameObject p1Lives = GameObject.Find("P1Lives");
        GameObject p2Lives = GameObject.Find("P2Lives");
        if (p1Lives == null || p2Lives == null)
        {
            Debug.LogError("[SetupHPUI] P1Lives もしくは P2Lives が見つかりません。CenterUI 配下にあるか確認してください。");
            return;
        }

        // HPテキストとして使うのでリネーム
        p1Lives.name = "P1HPText";
        p2Lives.name = "P2HPText";

        // HPバー（Image）を生成
        Image p1Fill = CreateHPBar(p1Lives, "P1HPFill", new Color(0.4f, 1.0f, 0.4f));
        Image p2Fill = CreateHPBar(p2Lives, "P2HPFill", new Color(0.4f, 1.0f, 0.4f));

        // UIManager の参照を再バインド
        SerializedObject so = new SerializedObject(ui);

        // p1HPText / p2HPText に転用したテキストを設定
        TextMeshProUGUI p1Text = p1Lives.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI p2Text = p2Lives.GetComponent<TextMeshProUGUI>();
        BindProperty(so, "p1HPText", p1Text);
        BindProperty(so, "p2HPText", p2Text);
        BindProperty(so, "p1HPFill", p1Fill);
        BindProperty(so, "p2HPFill", p2Fill);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);

        Debug.Log("[SetupHPUI] 完了。P1/P2 の残機UIを HP表示に転用しました。");
    }

    private static Image CreateHPBar(GameObject anchorText, string name, Color color)
    {
        // すでに同名Imageがあれば再利用
        Transform existing = anchorText.transform.parent.Find(name);
        if (existing != null)
        {
            Image existingImg = existing.GetComponent<Image>();
            if (existingImg != null) return existingImg;
        }

        // HPバー本体（横長Image）を anchorText の親に追加
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(anchorText.transform.parent, false);

        RectTransform rt = bar.GetComponent<RectTransform>();
        RectTransform anchorRT = anchorText.GetComponent<RectTransform>();

        // anchorText のすぐ下に表示。サイズは width 200, height 14
        rt.anchorMin = anchorRT.anchorMin;
        rt.anchorMax = anchorRT.anchorMax;
        rt.pivot     = anchorRT.pivot;
        rt.anchoredPosition = anchorRT.anchoredPosition + new Vector2(0f, -24f);
        rt.sizeDelta = new Vector2(200f, 14f);

        Image img = bar.GetComponent<Image>();
        img.color  = color;
        img.type   = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;

        // テキストと重ならないよう描画順を後ろに
        bar.transform.SetSiblingIndex(anchorText.transform.GetSiblingIndex() + 1);

        return img;
    }

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
