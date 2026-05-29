using UnityEngine;

// スキル効果の抽象基底クラス
public abstract class SkillDefinition
{
    public abstract string DisplayName { get; }

    // 発動条件チェック（条件なしスキルは true を返す）
    public virtual bool CanActivate(int playerIndex) => true;

    public abstract void Activate(int playerIndex, ArenaController arena);
}

// =====================================================
// 具体的なスキル実装
// =====================================================

// パドルを一定時間拡大する
public sealed class SkillPaddle_Enlarge : SkillDefinition
{
    public float multiplier = 1.5f;
    public float duration   = 10f;

    public override string DisplayName => "Paddle Enlarge";

    public override void Activate(int playerIndex, ArenaController arena)
    {
        Transform root = arena.transform.parent ?? arena.transform;
        root.GetComponentInChildren<PlayerController>()?.SetWidthTemporary(multiplier, duration);
    }
}

// ボールを一定時間 Fire 属性にする
public sealed class SkillBall_Attribute_Fire : SkillDefinition
{
    public float duration = 10f;

    public override string DisplayName => "Fire Ball";

    public override void Activate(int playerIndex, ArenaController arena)
    {
        arena.GetBall()?.SetAttributeTemporary(BallAttribute.Fire, duration);
    }
}

// 一定時間、追加ボールをスポーンする
public sealed class SkillBall_Multi : SkillDefinition
{
    public float duration = 10f;

    public override string DisplayName => "Multi Ball";

    public override void Activate(int playerIndex, ArenaController arena)
    {
        arena.SpawnExtraBall(duration);
    }
}

// 上半分のブロックを全破壊。HP 1/3 以下のみ発動可
public sealed class SkillPanic_BlockClear : SkillDefinition
{
    public int   hitStopFrames = 15;

    public override string DisplayName => "Block Clear!";

    public override bool CanActivate(int playerIndex)
    {
        return GameManager.Instance != null
            && GameManager.Instance.GetCurrentState() == GameManager.GameState.Playing
            && GameManager.Instance.GetHPRatio(playerIndex) <= 1f / 3f;
    }

    public override void Activate(int playerIndex, ArenaController arena)
    {
        BlockSpawner spawner = arena.GetSpawner();
        if (spawner == null) return;

        foreach (Block b in spawner.GetComponentsInChildren<Block>())
        {
            if (b.transform.localPosition.y > 0f)
                Object.Destroy(b.gameObject);
        }

        arena.TriggerHitStop(hitStopFrames, strong: true, shake: true);
    }
}
