using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// カメラの Viewport Rect をデザイン仕様に合わせてセットアップする
// Camera1: 左 42%  (0, 0, 0.42, 1)
// Camera2: 右 42%  (0.58, 0, 0.42, 1)
// 中央 16% は Canvas (Screen Space Overlay) の UI 専用エリアになる
public static class SetupCameraViewports
{
    [MenuItem("BurokkuKuzushi/Setup Camera Viewports")]
    public static void Setup()
    {
        Camera cam1 = GameObject.Find("Camera1")?.GetComponent<Camera>();
        Camera cam2 = GameObject.Find("Camera2")?.GetComponent<Camera>();

        if (cam1 == null || cam2 == null)
        {
            Debug.LogWarning("[SetupCameraViewports] Camera1 または Camera2 が見つかりません。");
            return;
        }

        Undo.RecordObject(cam1, "Setup Camera1 Viewport");
        Undo.RecordObject(cam2, "Setup Camera2 Viewport");

        cam1.rect = new Rect(0f,    0f, 0.42f, 1f);
        cam2.rect = new Rect(0.58f, 0f, 0.42f, 1f);

        EditorUtility.SetDirty(cam1);
        EditorUtility.SetDirty(cam2);
        EditorSceneManager.MarkSceneDirty(cam1.gameObject.scene);

        Debug.Log("[SetupCameraViewports] Camera1 → (0, 0, 0.42, 1) / Camera2 → (0.58, 0, 0.42, 1)");
    }

    [MenuItem("BurokkuKuzushi/Reset Camera Viewports (50/50)")]
    public static void Reset()
    {
        Camera cam1 = GameObject.Find("Camera1")?.GetComponent<Camera>();
        Camera cam2 = GameObject.Find("Camera2")?.GetComponent<Camera>();

        if (cam1 == null || cam2 == null)
        {
            Debug.LogWarning("[SetupCameraViewports] Camera1 または Camera2 が見つかりません。");
            return;
        }

        Undo.RecordObject(cam1, "Reset Camera1 Viewport");
        Undo.RecordObject(cam2, "Reset Camera2 Viewport");

        cam1.rect = new Rect(0f,   0f, 0.5f, 1f);
        cam2.rect = new Rect(0.5f, 0f, 0.5f, 1f);

        EditorUtility.SetDirty(cam1);
        EditorUtility.SetDirty(cam2);
        EditorSceneManager.MarkSceneDirty(cam1.gameObject.scene);

        Debug.Log("[SetupCameraViewports] Camera1 → (0, 0, 0.5, 1) / Camera2 → (0.5, 0, 0.5, 1)");
    }
}
