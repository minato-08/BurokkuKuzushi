using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// UIManager の SerializeField を新 UI Hierarchy (_UI/_CameraSpace/_Components/...) に自動バインドする
// BurokkuKuzushi > Setup UIManager Bindings で実行（冪等）
public static class SetupUIManager
{
    [MenuItem("BurokkuKuzushi/Setup UIManager Bindings")]
    public static void Execute()
    {
        var baseGO = GameObject.Find("_UI/_CameraSpace/_Base");
        if (baseGO == null)
        {
            Debug.LogError("[SetupUIManager] '_UI/_CameraSpace/_Base' が見つかりません");
            return;
        }

        var uim = baseGO.GetComponent<UIManager>();
        if (uim == null)
        {
            Debug.LogError("[SetupUIManager] UIManager コンポーネントが見つかりません");
            return;
        }

        Undo.RecordObject(uim, "Setup UIManager Bindings");
        var so = new SerializedObject(uim);

        const string P1 = "_UI/_CameraSpace/_Components/_P1Components";
        const string P2 = "_UI/_CameraSpace/_Components/_P2Components";

        // P1 HUD
        Bind(so, "p1HpFill",       FindComp<Image>           ($"{P1}/_P1HpIndicator/$P1HpFill"));
        Bind(so, "p1HpValue",      FindComp<TextMeshProUGUI>  ($"{P1}/_P1HpIndicator/$P1HpValue"));
        Bind(so, "p1HpMax",        FindComp<TextMeshProUGUI>  ($"{P1}/_P1HpIndicator/P1HpMax"));
        Bind(so, "p1ComboValue",   FindComp<TextMeshProUGUI>  ($"{P1}/_P1Combo/$P1ComboValue"));
        Bind(so, "p1ComboMax",     FindComp<TextMeshProUGUI>  ($"{P1}/_P1Combo/P1ComboMax"));
        Bind(so, "p1ScoreValue",   FindComp<TextMeshProUGUI>  ($"{P1}/_P1Score/$P1ScoreValue"));
        Bind(so, "p1ItemInfoRoot", FindGO                     ($"{P1}/_P1ItemInfo"));
        Bind(so, "p1ItemName",     FindComp<TextMeshProUGUI>  ($"{P1}/_P1ItemInfo/$P1ItemName"));
        Bind(so, "p1ItemDuration", FindComp<TextMeshProUGUI>  ($"{P1}/_P1ItemInfo/$P1ItemDuration"));

        // P2 HUD
        Bind(so, "p2HpFill",       FindComp<Image>           ($"{P2}/_P2HpIndicator/$P2HpFill"));
        Bind(so, "p2HpValue",      FindComp<TextMeshProUGUI>  ($"{P2}/_P2HpIndicator/$P2HpValue"));
        Bind(so, "p2HpMax",        FindComp<TextMeshProUGUI>  ($"{P2}/_P2HpIndicator/P2HpMax"));
        Bind(so, "p2ComboValue",   FindComp<TextMeshProUGUI>  ($"{P2}/_P2Combo/$P2ComboValue"));
        Bind(so, "p2ComboMax",     FindComp<TextMeshProUGUI>  ($"{P2}/_P2Combo/P2ComboMax"));
        Bind(so, "p2ScoreValue",   FindComp<TextMeshProUGUI>  ($"{P2}/_P2Score/$P2ScoreValue"));
        Bind(so, "p2ItemInfoRoot", FindGO                     ($"{P2}/_P2ItemInfo"));
        Bind(so, "p2ItemName",     FindComp<TextMeshProUGUI>  ($"{P2}/_P2ItemInfo/$P2ItemName"));
        Bind(so, "p2ItemDuration", FindComp<TextMeshProUGUI>  ($"{P2}/_P2ItemInfo/$P2ItemDuration"));

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(uim);
        EditorSceneManager.MarkSceneDirty(uim.gameObject.scene);

        Debug.Log("[SetupUIManager] UIManager バインド完了（[必須] セクション全フィールド）");
    }

    static T FindComp<T>(string path) where T : Component
    {
        var go = GameObject.Find(path);
        if (go == null)
        {
            Debug.LogWarning($"[SetupUIManager] 未検出: {path}");
            return null;
        }
        var comp = go.GetComponent<T>();
        if (comp == null)
            Debug.LogWarning($"[SetupUIManager] {typeof(T).Name} コンポーネントなし: {path}");
        return comp;
    }

    static Object FindGO(string path)
    {
        var go = GameObject.Find(path);
        if (go == null) Debug.LogWarning($"[SetupUIManager] 未検出: {path}");
        return go;
    }

    static void Bind(SerializedObject so, string fieldName, Object obj)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
            Debug.LogWarning($"[SetupUIManager] SerializeField 未検出: {fieldName}");
        else
            prop.objectReferenceValue = obj;
    }
}
