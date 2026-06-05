using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 落下アイテム本体（ArenaController.SpawnItem）用アイコンのセットアップ（冪等）。
//   1. Assets/UI/item-icons/*.png を Texture Type = Sprite に揃えて再インポート
//   2. ファイル名 → ItemType の対応でシーン内 ArenaSharedConfig.itemIcons[] を自動結線
//
// 何度実行しても安全。新しいアイコンを足したらメニューを再実行すれば取り込み直す。
public static class SetupItemIcons
{
    private const string IconDir = "Assets/UI/item-icons";

    // ファイル名（拡張子なし）→ ItemType。item-attack-direct は対応 ItemType が無いので未登録（スキップ）。
    private static readonly Dictionary<string, ItemType> NameToType = new Dictionary<string, ItemType>
    {
        { "item-buff-fire",      ItemType.Fire         },
        { "item-buff-ice",       ItemType.Ice          },
        { "item-buff-thunder",   ItemType.Thunder      },
        { "item-buff-heavy",     ItemType.Heavy        },
        { "item-buff-pierce",    ItemType.Pierce       },
        { "item-buff-enlarge",   ItemType.Enlarge      },
        { "item-buff-acceralate",ItemType.SpeedUp      },
        { "item-buff-heal",      ItemType.Heal         },
        { "item-attack-harden",  ItemType.AttackHarden },
        { "item-attack-addrow",  ItemType.AttackAddRow },
        { "item-attack-poison",  ItemType.AttackPoison },
        { "item-attack-slow",    ItemType.AttackSlow   },
        { "item-trap-shrink",    ItemType.Shrink       },
        { "item-trap-hyper",     ItemType.Hyper        },
        { "item-trap-reversed",  ItemType.Reversed     },
    };

    [MenuItem("BurokkuKuzushi/Setup Item Icons")]
    public static void Setup()
    {
        if (!Directory.Exists(IconDir))
        {
            Debug.LogWarning($"[SetupItemIcons] {IconDir} が見つかりません。");
            return;
        }

        // 1. PNG を Sprite として取り込み（Default で取り込まれていると Sprite 参照を張れないため）
        var entries = new List<ArenaSharedConfig.ItemIcon>();
        var missing = new List<ItemType>();
        var seen    = new HashSet<ItemType>();

        foreach (var pair in NameToType)
        {
            string path = $"{IconDir}/{pair.Key}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                missing.Add(pair.Value);
                continue;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) { missing.Add(pair.Value); continue; }

            entries.Add(new ArenaSharedConfig.ItemIcon { type = pair.Value, sprite = sprite });
            seen.Add(pair.Value);
        }

        // 2. シーン内 ArenaSharedConfig へ結線
        var config = Object.FindFirstObjectByType<ArenaSharedConfig>();
        if (config == null)
        {
            Debug.LogWarning("[SetupItemIcons] ArenaSharedConfig がシーンに見つかりません。" +
                             "スプライト取り込みのみ完了。GameObject に ArenaSharedConfig を付けてから再実行してください。");
            return;
        }

        config.itemIcons = entries.ToArray();
        EditorUtility.SetDirty(config);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(config.gameObject.scene);

        Debug.Log($"[SetupItemIcons] {entries.Count}/15 種のアイコンを ArenaSharedConfig に結線しました。" +
                  (missing.Count > 0 ? $" 取り込めなかった種別: {string.Join(", ", missing)}" : ""));

        // 全 ItemType がそろっているか確認（フォールバック=色付き球になる種別を警告）
        foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
            if (!seen.Contains(t))
                Debug.LogWarning($"[SetupItemIcons] {t} のアイコン未割り当て（落下時は色付き球で表示されます）。");
    }
}
