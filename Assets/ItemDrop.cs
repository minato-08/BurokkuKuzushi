using UnityEngine;

public enum ItemType
{
    // Buff: Ball attribute
    Fire, Ice, Thunder, Heavy, Pierce,
    // Buff: Paddle / Recovery
    Enlarge, SpeedUp, Heal,
    // Attack (相手アリーナへ妨害送付)
    AttackHarden, AttackAddRow, AttackPoison, AttackSlow,
    // Trap (取得回避が戦略)
    Shrink, Hyper, Reversed
}

public enum ItemCategory { Buff, Attack, Trap }

// 持続効果が占有する「スロット」。同一スロットの効果は重ね掛けで上書きされる
// （BallScript / PlayerController の各コルーチンが StopCoroutine で上書きする単位と一致）。
// None = 持続効果を持たない（Heal の即時回復 / Attack の妨害送付）。
public enum ItemEffectSlot { None, BallAttribute, BallSpeed, PaddleScale, PaddleSpeed, InputReverse }

public static class ItemDefinition
{
    public static Color GetColor(ItemType type) => type switch
    {
        ItemType.Fire         => new Color(1.000f, 0.478f, 0.239f), // #ff7a3d
        ItemType.Ice          => new Color(0.306f, 0.765f, 1.000f), // #4ec3ff
        ItemType.Thunder      => new Color(1.000f, 0.847f, 0.290f), // #ffd84a
        ItemType.Heavy        => new Color(0.706f, 0.643f, 1.000f), // #b4a4ff lavender
        ItemType.Pierce       => new Color(0.635f, 1.000f, 0.878f), // #a2ffdf
        ItemType.Enlarge      => new Color(0.482f, 0.878f, 0.482f), // #7be07b ok green
        ItemType.SpeedUp      => new Color(0.306f, 0.765f, 1.000f), // #4ec3ff
        ItemType.Heal         => new Color(0.482f, 0.878f, 0.482f), // #7be07b ok green
        // Attack 系: 赤系オーラ
        ItemType.AttackHarden => new Color(1.000f, 0.745f, 0.196f), // #ffbe32 gold-red
        ItemType.AttackAddRow => new Color(1.000f, 0.298f, 0.235f), // #ff4c3c crimson
        ItemType.AttackPoison => new Color(0.741f, 0.227f, 0.812f), // #bd3aCF purple-red
        ItemType.AttackSlow   => new Color(1.000f, 0.439f, 0.290f), // #ff704a orange-red
        // Trap: 紫系
        ItemType.Shrink       => new Color(0.792f, 0.286f, 0.851f), // #ca49d9 purple
        ItemType.Hyper        => new Color(0.659f, 0.345f, 0.961f), // #a858f5 deep purple
        ItemType.Reversed     => new Color(0.561f, 0.302f, 0.812f), // #8f4dCF dark purple
        _                     => Color.white
    };

    public static string GetName(ItemType type) => type switch
    {
        ItemType.Fire         => "FIRE",
        ItemType.Ice          => "ICE",
        ItemType.Thunder      => "THUNDER",
        ItemType.Heavy        => "HEAVY",
        ItemType.Pierce       => "PIERCE",
        ItemType.Enlarge      => "ENLARGE",
        ItemType.SpeedUp      => "SPEED UP",
        ItemType.Heal         => "HEAL",
        ItemType.AttackHarden => "HARDEN",
        ItemType.AttackAddRow => "ADD ROW",
        ItemType.AttackPoison => "POISON",
        ItemType.AttackSlow   => "SLOW",
        ItemType.Shrink       => "SHRINK",
        ItemType.Hyper        => "HYPER",
        ItemType.Reversed     => "REVERSED",
        _                     => "???"
    };

    public static ItemCategory GetCategory(ItemType type) => type switch
    {
        ItemType.AttackHarden or ItemType.AttackAddRow or
        ItemType.AttackPoison or ItemType.AttackSlow    => ItemCategory.Attack,
        ItemType.Shrink or ItemType.Hyper or
        ItemType.Reversed                               => ItemCategory.Trap,
        _                                               => ItemCategory.Buff
    };

    // 持続効果スロット（重複抑制・アクティブ効果リスト用）。
    public static ItemEffectSlot GetEffectSlot(ItemType type) => type switch
    {
        ItemType.Fire or ItemType.Ice or ItemType.Thunder or
        ItemType.Heavy or ItemType.Pierce               => ItemEffectSlot.BallAttribute,
        ItemType.SpeedUp                                => ItemEffectSlot.PaddleSpeed,
        ItemType.Hyper                                  => ItemEffectSlot.BallSpeed,
        ItemType.Enlarge or ItemType.Shrink             => ItemEffectSlot.PaddleScale,
        ItemType.Reversed                               => ItemEffectSlot.InputReverse,
        _                                               => ItemEffectSlot.None, // Heal / Attack*
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

    // ── アイテム効果パラメータ（DESIGN.md 5.5）───────────
    // ※ アイテムは AddComponent 生成のため、ここの初期値がそのまま使われる
    // 属性付与の持続時間（属性ごとに異なる, DESIGN.md 5.5 効果持続表）
    public float fireDuration    = 5f;   // Fire
    public float thunderDuration = 3f;   // Thunder
    public float iceDuration     = 8f;   // Ice
    public float heavyDuration   = 8f;   // Heavy
    public float pierceDuration  = 3f;   // Pierce
    public float paddleDuration     = 10f;  // パドル変化 (Enlarge/Shrink) 持続時間
    public float speedDuration      = 10f;  // 速度変化 (SpeedUp) 持続時間
    public float hyperDuration      = 3f;   // TrapBall_Hyperspeed 持続時間
    public float reversedDuration   = 5f;   // TrapBall_Reversed 持続時間
    public float enlargeMultiplier  = 1.5f; // パドル拡大倍率
    public float shrinkMultiplier   = 0.7f; // パドル縮小倍率 (Trap)
    public float speedUpMultiplier  = 1.3f; // パドル移動速度 +30%（DESIGN.md 5.5 BuffPaddle_SpeedUp）
    public float hyperMultiplier    = 4f;   // ハイパー速度倍率 (Trap, 制御困難化)
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
            PlayerController paddle = hit.GetComponent<PlayerController>();
            if (paddle != null)
            {
                paddle.OnItemPickup(ItemDefinition.GetCategory(itemType));
                BuildEffect().Apply(playerIndex, arena);
                GameManager.Instance?.RegisterActiveItem(
                    playerIndex, ItemDefinition.GetEffectSlot(itemType),
                    ItemDefinition.GetName(itemType), GetActiveDuration());
                Destroy(gameObject);
                return;
            }
        }
    }

    // 属性付与アイテムの持続時間（DESIGN.md 5.5）。属性以外は 0。
    private float AttributeDuration(ItemType t) => t switch
    {
        ItemType.Fire    => fireDuration,
        ItemType.Ice     => iceDuration,
        ItemType.Thunder => thunderDuration,
        ItemType.Heavy   => heavyDuration,
        ItemType.Pierce  => pierceDuration,
        _                => 0f
    };

    // 効果の持続時間。Heal や Attack 等の瞬時アイテムは 0（=表示しない）
    private float GetActiveDuration() => itemType switch
    {
        ItemType.Fire or ItemType.Ice or ItemType.Thunder
            or ItemType.Heavy or ItemType.Pierce         => AttributeDuration(itemType),
        ItemType.Enlarge or ItemType.Shrink              => paddleDuration,
        ItemType.SpeedUp                                 => speedDuration,
        ItemType.Hyper                                   => hyperDuration,
        ItemType.Reversed                                => reversedDuration,
        _                                                => 0f
    };

    private EffectDefinition BuildEffect() => itemType switch
    {
        ItemType.Fire    => new EffectBallAttribute { Attr = BallAttribute.Fire,    Duration = fireDuration    },
        ItemType.Ice     => new EffectBallAttribute { Attr = BallAttribute.Ice,     Duration = iceDuration     },
        ItemType.Thunder => new EffectBallAttribute { Attr = BallAttribute.Thunder, Duration = thunderDuration },
        ItemType.Heavy   => new EffectBallAttribute { Attr = BallAttribute.Heavy,   Duration = heavyDuration   },
        ItemType.Pierce  => new EffectBallAttribute { Attr = BallAttribute.Pierce,  Duration = pierceDuration  },
        ItemType.Enlarge => new EffectPaddleScale   { Multiplier = enlargeMultiplier, Duration = paddleDuration },
        ItemType.Shrink  => new EffectPaddleScale   { Multiplier = shrinkMultiplier,  Duration = paddleDuration },
        ItemType.SpeedUp => new EffectPaddleSpeed   { Multiplier = speedUpMultiplier, Duration = speedDuration  },
        ItemType.Hyper   => new EffectBallSpeed     { Multiplier = hyperMultiplier,   Duration = hyperDuration  },
        ItemType.Reversed => new EffectInputReverse { Duration = reversedDuration },
        ItemType.Heal    => new EffectHeal          { Amount = healAmount },
        ItemType.AttackHarden or ItemType.AttackAddRow
            or ItemType.AttackPoison or ItemType.AttackSlow
                         => new EffectAttack        { AttackItem = itemType },
        _                => new EffectHeal          { Amount = 0 }
    };
}
