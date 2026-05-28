using UnityEngine;

// アイテム・スキル効果の抽象基底クラス（Phase D でスキルシステムと統合予定）
public abstract class EffectDefinition
{
    public abstract void Apply(int playerIndex, ArenaController arena);
}

public sealed class EffectBallAttribute : EffectDefinition
{
    public BallAttribute Attr;
    public float         Duration;

    public override void Apply(int playerIndex, ArenaController arena)
        => arena.GetBall()?.SetAttributeTemporary(Attr, Duration);
}

public sealed class EffectPaddleScale : EffectDefinition
{
    public float Multiplier;
    public float Duration;

    public override void Apply(int playerIndex, ArenaController arena)
    {
        // ArenaController は Arena の子。PlayerController は Arena の別の子なので親から検索
        Transform arenaRoot = arena.transform.parent ?? arena.transform;
        arenaRoot.GetComponentInChildren<PlayerController>()?.SetWidthTemporary(Multiplier, Duration);
    }
}

public sealed class EffectBallSpeed : EffectDefinition
{
    public float Multiplier;
    public float Duration;

    public override void Apply(int playerIndex, ArenaController arena)
        => arena.GetBall()?.SetSpeedTemporary(Multiplier, Duration);
}

public sealed class EffectHeal : EffectDefinition
{
    public int Amount;

    public override void Apply(int playerIndex, ArenaController arena)
        => GameManager.Instance?.Heal(playerIndex, Amount);
}

// 攻撃アイテム取得時に発火する効果。playerIndex は取得した側。
// GameManager 経由で相手アリーナへ妨害を送付する。
public sealed class EffectAttack : EffectDefinition
{
    public ItemType AttackItem;

    public override void Apply(int playerIndex, ArenaController arena)
    {
        int opponent = playerIndex == 1 ? 2 : 1;
        GameManager.Instance?.SendInterference(opponent, AttackItem);
    }
}
