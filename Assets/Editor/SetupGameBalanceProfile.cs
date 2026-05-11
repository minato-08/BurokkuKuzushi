using UnityEditor;
using UnityEngine;
using System.IO;

// GameBalanceProfile アセットを生成し、シーン内の GameManager にバインドする
// Unity メニュー [BurokkuKuzushi] > [Setup GameBalanceProfile] から実行
public static class SetupGameBalanceProfile
{
    private const string SETTINGS_DIR  = "Assets/Settings";
    private const string ASSET_PATH    = "Assets/Settings/GameBalanceProfile.asset";

    [MenuItem("BurokkuKuzushi/Setup GameBalanceProfile")]
    public static void Setup()
    {
        GameBalanceProfile profile = CreateOrLoadProfile();
        BindToGameManager(profile);

        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);

        Debug.Log("[SetupGameBalanceProfile] 完了。Profile アセットを作成・バインドしました。");
    }

    private static GameBalanceProfile CreateOrLoadProfile()
    {
        // Settings ディレクトリを保証
        if (!Directory.Exists(SETTINGS_DIR))
        {
            Directory.CreateDirectory(SETTINGS_DIR);
            AssetDatabase.Refresh();
        }

        // 既存があればロード、なければ新規作成
        GameBalanceProfile profile = AssetDatabase.LoadAssetAtPath<GameBalanceProfile>(ASSET_PATH);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<GameBalanceProfile>();
            AssetDatabase.CreateAsset(profile, ASSET_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupGameBalanceProfile] 新規作成: {ASSET_PATH}");
        }
        else
        {
            Debug.Log($"[SetupGameBalanceProfile] 既存アセットを使用: {ASSET_PATH}");
        }

        return profile;
    }

    private static void BindToGameManager(GameBalanceProfile profile)
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[SetupGameBalanceProfile] シーンに GameManager が見つかりません。手動で profile フィールドを設定してください。");
            return;
        }

        SerializedObject   so   = new SerializedObject(gm);
        SerializedProperty prop = so.FindProperty("profile");
        if (prop == null)
        {
            Debug.LogError("[SetupGameBalanceProfile] GameManager に 'profile' フィールドが見つかりません。");
            return;
        }

        prop.objectReferenceValue = profile;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);

        Debug.Log("[SetupGameBalanceProfile] GameManager.profile に Profile アセットをバインドしました。");
    }
}
