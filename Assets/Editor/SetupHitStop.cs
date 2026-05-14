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

        Camera cam1 = GameObject.Find("Camera1")?.GetComponent<Camera>();
        Camera cam2 = GameObject.Find("Camera2")?.GetComponent<Camera>();

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

            // ArenaController.arenaCamera SerializeField をカメラ参照で埋める
            Camera cam = arena.playerIndex == 1 ? cam1 : cam2;
            if (cam != null)
            {
                SerializedObject so = new SerializedObject(arena);
                SerializedProperty prop = so.FindProperty("arenaCamera");
                if (prop != null)
                {
                    prop.objectReferenceValue = cam;
                    so.ApplyModifiedProperties();
                    Debug.Log($"[SetupHitStop] {arena.name}.arenaCamera → {cam.name}");
                }
                else
                {
                    Debug.LogWarning($"[SetupHitStop] ArenaController に 'arenaCamera' フィールドが見つかりません。");
                }
            }
            else
            {
                Debug.LogWarning($"[SetupHitStop] playerIndex={arena.playerIndex} 用のカメラが見つかりません。");
            }

            EditorUtility.SetDirty(arena);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            arenas[0].gameObject.scene);

        Debug.Log("[SetupHitStop] セットアップ完了。");
    }
}
