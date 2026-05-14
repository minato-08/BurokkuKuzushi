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
