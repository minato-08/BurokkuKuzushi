using UnityEngine;

// スキル効果の抽象基底クラス（2026-06-05 刷新, DESIGN.md 5.6）
// 性能差は EnergyCost（必要ゲージ量）で差別化する。
public abstract class SkillDefinition
{
    public abstract string DisplayName { get; }

    // 発動に必要なエナジー量（スキルごとに差別化。SkillController が EnergyRatio / 発動判定に使う）
    public abstract float EnergyCost { get; }

    // 発動条件チェック（条件なしスキルは true を返す）
    public virtual bool CanActivate(int playerIndex) => true;

    public abstract void Activate(int playerIndex, ArenaController arena);
}

// =====================================================
// 具体的なスキル実装（すべて自己強化 / 盤面有利。攻撃系は持たない）
// パラメータは public フィールドの既定値で調整する。
// =====================================================

// HYPER: ボールを高速化し、Dead Zone 付近に一時的な床を出して暴れさせる
public sealed class SkillHyper : SkillDefinition
{
    public float energyCost      = 6f;
    public float duration        = 6f;
    public float speedMultiplier = 5f;

    public override string DisplayName => "HYPER";
    public override float  EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        arena.GetBall()?.SetSpeedTemporary(speedMultiplier, duration);
        arena.SpawnHyperFloor(duration);
    }
}

// EXPLOSION: 自陣のブロックをランダムに複数 Explosive 化する
public sealed class SkillExplosion : SkillDefinition
{
    public float energyCost = 8f;
    public int   minCount   = 10;
    public int   maxCount   = 20;

    public override string DisplayName => "EXPLOSION";
    public override float  EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        int count = Random.Range(minCount, maxCount + 1);
        arena.GetSpawner()?.ConvertRandomToExplosive(count);

        // 発動の手応え＝シェイクのみ（ボール衝突ではないのでフリーズしない, DESIGN.md 5.x）
        int frames = ArenaSharedConfig.Instance != null
            ? ArenaSharedConfig.Instance.skillPanicHitStopFrames : 15;
        arena.TriggerHitStop(frames, strong: true, shake: true, freeze: false);
    }
}

// BURST: 発動中、最大 shots 発のボールを連射できる（撃ち切る or 時間切れで終了）
public sealed class SkillBurst : SkillDefinition
{
    public float energyCost   = 10f;
    public float duration     = 5f;
    public int   shots        = 10;
    public float ballLifetime = 8f; // 撃ったボールの寿命

    public override string DisplayName => "BURST";
    public override float  EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        arena.BeginBurst(shots, duration, ballLifetime);
    }
}

// GIANT: ボールを巨大化し Pierce 化する（巨大貫通弾）
public sealed class SkillGiant : SkillDefinition
{
    public float energyCost      = 5f;
    public float duration        = 6f;
    public float scaleMultiplier = 3f;

    public override string DisplayName => "GIANT";
    public override float  EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        BallScript ball = arena.GetBall();
        if (ball == null) return;
        ball.SetAttributeTemporary(BallAttribute.Pierce, duration);
        ball.SetScaleTemporary(scaleMultiplier, duration);
    }
}
