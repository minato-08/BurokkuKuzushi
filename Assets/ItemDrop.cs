using UnityEngine;

public enum ItemType
{
    Fire, Ice, Thunder, Heavy, Pierce, // Ball attribute
    Enlarge, SpeedUp,                  // Good items
    Shrink, Hyper,                     // Bad items
    Heal                               // Recovery
}

public static class ItemDefinition
{
    public static Color GetColor(ItemType type) => type switch
    {
        ItemType.Fire    => new Color(1.000f, 0.478f, 0.239f), // #ff7a3d
        ItemType.Ice     => new Color(0.306f, 0.765f, 1.000f), // #4ec3ff
        ItemType.Thunder => new Color(1.000f, 0.847f, 0.290f), // #ffd84a
        ItemType.Heavy   => new Color(0.706f, 0.643f, 1.000f), // #b4a4ff lavender
        ItemType.Pierce  => new Color(0.635f, 1.000f, 0.878f), // #a2ffdf
        ItemType.Enlarge => new Color(0.482f, 0.878f, 0.482f), // #7be07b ok green
        ItemType.SpeedUp => new Color(0.306f, 0.765f, 1.000f), // #4ec3ff (blue like ice)
        ItemType.Shrink  => new Color(1.000f, 0.231f, 0.361f), // #ff3b5c warn red
        ItemType.Hyper   => new Color(1.000f, 0.847f, 0.290f), // #ffd84a accent yellow
        ItemType.Heal    => new Color(0.482f, 0.878f, 0.482f), // #7be07b ok green
        _                => Color.white
    };

    public static string GetName(ItemType type) => type switch
    {
        ItemType.Fire    => "FIRE",
        ItemType.Ice     => "ICE",
        ItemType.Thunder => "THUNDER",
        ItemType.Heavy   => "HEAVY",
        ItemType.Pierce  => "PIERCE",
        ItemType.Enlarge => "ENLARGE",
        ItemType.SpeedUp => "SPEED UP",
        ItemType.Shrink  => "SHRINK",
        ItemType.Hyper   => "HYPER",
        ItemType.Heal    => "HEAL",
        _                => "???"
    };
}

// ブロック破壊時に生成されるアイテムオブジェクト
// 落下しながらパドルへの接触を毎フレーム Physics.OverlapSphere で判定する
// （kinematic-kinematic 間では OnTriggerEnter が発火しないため）
public class ItemDrop : MonoBehaviour
{
    // ── 落下・消滅 ──────────────────────────────────────
    public float dropSpeed       = 2.5f;  // 落下速度
    public float detectionRadius = 0.5f;  // パドルとの接触判定半径
    public float bottomYOffset   = -20f;   // アリーナ下端からさらに何ユニット下で自然消滅するか

    // ── アイテム効果パラメータ ───────────────────────────
    public float attributeDuration  = 8f;   // 属性付与 (Fire/Ice/Thunder/Heavy) 持続時間
    public float paddleDuration     = 8f;   // パドル変化 (Enlarge/Shrink) 持続時間
    public float speedDuration      = 8f;   // 速度変化 (SpeedUp/Hyper) 持続時間
    public float enlargeMultiplier  = 1.5f; // パドル拡大倍率
    public float shrinkMultiplier   = 0.6f; // パドル縮小倍率
    public float speedUpMultiplier  = 1.4f; // 速度アップ倍率
    public float hyperMultiplier    = 1.8f; // ハイパー速度倍率
    public int   healAmount         = 50;   // 回復量

    private ItemType        itemType;
    private int             playerIndex;
    private ArenaController arena;
    private float           bottomWorldY;

    public void Setup(ItemType type, int pIndex, ArenaController a)
    {
        itemType    = type;
        playerIndex = pIndex;
        arena       = a;

        Transform arenaRoot = a.transform.parent ?? a.transform;
        bottomWorldY = arenaRoot.position.y - a.arenaHalfHeight + bottomYOffset;
    }

    void Update()
    {
        transform.position += Vector3.down * dropSpeed * Time.deltaTime;

        if (transform.position.y < bottomWorldY)
        {
            Destroy(gameObject);
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<PlayerController>() != null)
            {
                BuildEffect().Apply(playerIndex, arena);
                GameManager.Instance?.RegisterActiveItem(
                    playerIndex, ItemDefinition.GetName(itemType), GetActiveDuration());
                Destroy(gameObject);
                return;
            }
        }
    }

    // 効果の持続時間。Heal など瞬時アイテムは 0（=表示しない）
    private float GetActiveDuration() => itemType switch
    {
        ItemType.Fire or ItemType.Ice or ItemType.Thunder
            or ItemType.Heavy or ItemType.Pierce         => attributeDuration,
        ItemType.Enlarge or ItemType.Shrink              => paddleDuration,
        ItemType.SpeedUp or ItemType.Hyper               => speedDuration,
        _                                                => 0f
    };

    private EffectDefinition BuildEffect() => itemType switch
    {
        ItemType.Fire    => new EffectBallAttribute { Attr = BallAttribute.Fire,    Duration = attributeDuration },
        ItemType.Ice     => new EffectBallAttribute { Attr = BallAttribute.Ice,     Duration = attributeDuration },
        ItemType.Thunder => new EffectBallAttribute { Attr = BallAttribute.Thunder, Duration = attributeDuration },
        ItemType.Heavy   => new EffectBallAttribute { Attr = BallAttribute.Heavy,   Duration = attributeDuration },
        ItemType.Pierce  => new EffectBallAttribute { Attr = BallAttribute.Pierce,  Duration = attributeDuration },
        ItemType.Enlarge => new EffectPaddleScale   { Multiplier = enlargeMultiplier, Duration = paddleDuration },
        ItemType.Shrink  => new EffectPaddleScale   { Multiplier = shrinkMultiplier,  Duration = paddleDuration },
        ItemType.SpeedUp => new EffectBallSpeed     { Multiplier = speedUpMultiplier, Duration = speedDuration  },
        ItemType.Hyper   => new EffectBallSpeed     { Multiplier = hyperMultiplier,   Duration = speedDuration  },
        ItemType.Heal    => new EffectHeal          { Amount = healAmount },
        _                => new EffectHeal          { Amount = 0 }
    };
}
