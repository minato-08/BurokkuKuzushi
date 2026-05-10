using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SetupUIManager
{
    // レガシー Text を TMP に差し替えて UIManager の参照を設定する
    public static void Execute()
    {
        string[] statPaths = new[]
        {
            "CenterUI/P1Score", "CenterUI/P1Lives", "CenterUI/P1Combo", "CenterUI/P1Wins",
            "CenterUI/P2Score", "CenterUI/P2Lives", "CenterUI/P2Combo", "CenterUI/P2Wins"
        };

        // レガシー Text を削除して TextMeshProUGUI を追加
        foreach (string path in statPaths)
        {
            GameObject go = GameObject.Find(path);
            if (go == null) { Debug.LogError($"{path} が見つかりません"); continue; }

            Text legacy = go.GetComponent<Text>();
            string savedText = legacy != null ? legacy.text : "";
            if (legacy != null) Object.DestroyImmediate(legacy);

            if (go.GetComponent<TextMeshProUGUI>() == null)
            {
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text     = savedText;
                tmp.fontSize = 24;
                tmp.color    = Color.white;
                EditorUtility.SetDirty(go);
            }
        }

        // UIManager の参照を設定
        GameObject centerUI = GameObject.Find("CenterUI");
        UIManager uiManager = centerUI.GetComponent<UIManager>();
        SerializedObject so  = new SerializedObject(uiManager);

        so.FindProperty("p1ScoreText").objectReferenceValue     = GetTMP("CenterUI/P1Score");
        so.FindProperty("p1LivesText").objectReferenceValue     = GetTMP("CenterUI/P1Lives");
        so.FindProperty("p1ComboText").objectReferenceValue     = GetTMP("CenterUI/P1Combo");
        so.FindProperty("p1RoundWinsText").objectReferenceValue = GetTMP("CenterUI/P1Wins");
        so.FindProperty("p2ScoreText").objectReferenceValue     = GetTMP("CenterUI/P2Score");
        so.FindProperty("p2LivesText").objectReferenceValue     = GetTMP("CenterUI/P2Lives");
        so.FindProperty("p2ComboText").objectReferenceValue     = GetTMP("CenterUI/P2Combo");
        so.FindProperty("p2RoundWinsText").objectReferenceValue = GetTMP("CenterUI/P2Wins");
        so.FindProperty("statusText").objectReferenceValue      = GetTMP("CenterUI/GameOverText");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(uiManager);
        Debug.Log("UIManager の参照をすべて設定しました");
    }

    private static TextMeshProUGUI GetTMP(string path)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) { Debug.LogError($"{path} が見つかりません"); return null; }
        return go.GetComponent<TextMeshProUGUI>();
    }
}
