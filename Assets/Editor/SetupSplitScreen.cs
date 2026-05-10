using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class SetupSplitScreen
{
    // Arena1 は (-17, 0, 0)、Arena2 は (+17, 0, 0) を想定
    private const float ARENA1_X  = -17f;
    private const float ARENA2_X  =  17f;
    private const float CAM_Y     =   0f;
    private const float CAM_Z     = -15f;  // アリーナ手前15ユニット
    private const float FOV       =  45f;

    public static void Execute()
    {
        // ── Camera 1（既存の Main Camera）────────────────────────
        Camera cam1 = Camera.main;
        if (cam1 == null) { Debug.LogError("Main Camera が見つかりません"); return; }

        cam1.transform.position   = new Vector3(ARENA1_X, CAM_Y, CAM_Z);
        cam1.transform.rotation   = Quaternion.identity;
        cam1.orthographic         = false;
        cam1.fieldOfView          = FOV;
        cam1.rect                 = new Rect(0f, 0f, 0.5f, 1f);
        cam1.depth                = 0;
        EditorUtility.SetDirty(cam1);

        // ── Camera 2（新規作成）──────────────────────────────────
        // 既存の Camera2 があれば再利用
        GameObject cam2Go = GameObject.Find("Camera2");
        if (cam2Go == null)
        {
            cam2Go = new GameObject("Camera2");
            cam2Go.AddComponent<Camera>();
            cam2Go.AddComponent<UniversalAdditionalCameraData>();
        }

        Camera cam2 = cam2Go.GetComponent<Camera>();
        cam2.transform.position = new Vector3(ARENA2_X, CAM_Y, CAM_Z);
        cam2.transform.rotation = Quaternion.identity;
        cam2.orthographic       = false;
        cam2.fieldOfView        = FOV;
        cam2.rect               = new Rect(0.5f, 0f, 0.5f, 1f);
        cam2.clearFlags         = CameraClearFlags.Skybox;
        cam2.depth              = 0;

        // AudioListener は1つだけにする
        AudioListener al = cam2Go.GetComponent<AudioListener>();
        if (al != null) Object.DestroyImmediate(al);

        EditorUtility.SetDirty(cam2Go);

        // シーン保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("スプリットスクリーン設定完了：Camera1（左）/ Camera2（右）");
    }
}
