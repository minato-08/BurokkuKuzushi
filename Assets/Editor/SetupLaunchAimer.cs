using UnityEditor;
using UnityEngine;

public static class SetupLaunchAimer
{
    [MenuItem("BurokkuKuzushi/Setup LaunchAimer")]
    public static void Setup()
    {
        // ArenaController コンポーネントをシーン全体から探す（パスに依存しない）
        var controllers = Object.FindObjectsByType<ArenaController>(FindObjectsSortMode.None);
        if (controllers.Length == 0)
        {
            Debug.LogError("[SetupLaunchAimer] ArenaController が見つかりません。");
            return;
        }

        foreach (var ac in controllers)
        {
            // LaunchAimer を ArenaController の子 GameObject に冪等生成
            Transform existing = ac.transform.Find("LaunchAimer");
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject("LaunchAimer");

            if (existing == null)
                go.transform.SetParent(ac.transform, false);

            if (go.GetComponent<LaunchAimer>() == null)
                go.AddComponent<LaunchAimer>();

            LaunchAimer aimer = go.GetComponent<LaunchAimer>();

            SerializedObject so = new SerializedObject(ac);
            so.FindProperty("launchAimer").objectReferenceValue = aimer;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(ac);
            EditorUtility.SetDirty(go);
            Debug.Log($"[SetupLaunchAimer] {ac.gameObject.name} に LaunchAimer を設定");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SetupLaunchAimer] 完了。");
    }
}
