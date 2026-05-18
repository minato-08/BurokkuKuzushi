using UnityEditor;
using UnityEngine;

public static class SetupHitStop
{
    [MenuItem("BurokkuKuzushi/Setup HitStop")]
    public static void Setup()
    {
        var arenas = Object.FindObjectsByType<ArenaController>(FindObjectsSortMode.None);
        if (arenas.Length == 0)
        {
            Debug.LogWarning("[SetupHitStop] ArenaController が見つかりません。シーンを開いてから実行してください。");
            return;
        }

        foreach (var arena in arenas)
        {
            // HitStopController を子に生成（冪等）
            HitStopController hitStop = arena.GetComponentInChildren<HitStopController>();
            if (hitStop == null)
            {
                var go = new GameObject("HitStopController");
                go.transform.SetParent(arena.transform, false);
                hitStop = go.AddComponent<HitStopController>();
                Debug.Log($"[SetupHitStop] {arena.name} に HitStopController を生成しました。");
            }

            // シェイク対象は ArenaController.Awake() が ArenaRoot を自動バインドするので
            // Editor 側での追加バインドは不要

            EditorUtility.SetDirty(arena);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            arenas[0].gameObject.scene);

        Debug.Log("[SetupHitStop] セットアップ完了。");
    }
}
