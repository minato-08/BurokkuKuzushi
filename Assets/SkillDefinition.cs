using UnityEngine;

// スキルの種別 ID（アイコン配列・UI で型安全に引くためのキー）。
// 並び順は SkillSelectUI.AllSkills と一致させる（0 Hyper / 1 Explosion / 2 Burst / 3 Giant）。
// アイコン配列（UIManager.skillIconsReady / skillIconsUnavailable）はこの index で引く。
public enum SkillId { Hyper = 0, Explosion = 1, Burst = 2, Giant = 3 }

// スキル効果の抽象基底クラス（2026-06-05 刷新, DESIGN.md 5.6）
// 性能差は EnergyCost（必要ゲージ量）で差別化する。
public abstract class SkillDefinition
{
    public abstract string DisplayName { get; }

    // 種別 ID（UI がアイコンを選ぶのに使う。名前文字列ではなく enum で引く＝タイポ/改名に強い）
    public abstract SkillId Id { get; }

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
// パラメータは ArenaSharedConfig で一元調整（未配置時はコード既定値にフォールバック）。
public sealed class SkillHyper : SkillDefinition
{
    public override string  DisplayName => "HYPER";
    public override SkillId  Id          => SkillId.Hyper;
    public override float    EnergyCost  => ArenaSharedConfig.Instance?.hyperEnergyCost ?? 6f;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        var c = ArenaSharedConfig.Instance;
        float duration = c != null ? c.hyperDuration : 6f;
        float speedMul = c != null ? c.hyperSpeedMul : 5f;
        arena.GetBall()?.SetSpeedTemporary(speedMul, duration);
        arena.SpawnHyperFloor(duration);
    }
}

// EXPLOSION: 盤面ブロックの一定割合をランダムに Explosive 化する
public sealed class SkillExplosion : SkillDefinition
{
    public override string  DisplayName => "EXPLOSION";
    public override SkillId  Id          => SkillId.Explosion;
    public override float    EnergyCost  => ArenaSharedConfig.Instance?.explosionEnergyCost ?? 8f;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        var c = ArenaSharedConfig.Instance;
        float fraction = c != null ? c.explosionFraction : 0.3f;
        arena.GetSpawner()?.ConvertRandomToExplosive(fraction);

        // 発動の手応え＝シェイクのみ（ボール衝突ではないのでフリーズしない, DESIGN.md 5.x）
        int frames = c != null ? c.skillPanicHitStopFrames : 15;
        arena.TriggerHitStop(frames, strong: true, shake: true, freeze: false);
    }
}

// BURST: 発動すると shots 発のプレーンなボールを interval 秒間隔で自動連射する（プレイヤー操作には干渉しない）。
// 角度は鉛直上を 0° として +angle と -angle を交互に飛ばす（DESIGN.md 5.6）。
public sealed class SkillBurst : SkillDefinition
{
    public override string  DisplayName => "BURST";
    public override SkillId  Id          => SkillId.Burst;
    public override float    EnergyCost  => ArenaSharedConfig.Instance?.burstEnergyCost ?? 10f;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        var c = ArenaSharedConfig.Instance;
        int   shots    = c != null ? c.burstShots        : 10;
        float interval = c != null ? c.burstInterval     : 0.2f;
        float angle    = c != null ? c.burstAngle        : 45f;
        float lifetime = c != null ? c.burstBallLifetime : 8f;
        arena.BeginBurst(shots, interval, angle, lifetime);
    }
}

// GIANT: ボールを巨大化し Pierce 化する（巨大貫通弾）
public sealed class SkillGiant : SkillDefinition
{
    public override string  DisplayName => "GIANT";
    public override SkillId  Id          => SkillId.Giant;
    public override float    EnergyCost  => ArenaSharedConfig.Instance?.giantEnergyCost ?? 5f;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        BallScript ball = arena.GetBall();
        if (ball == null) return;
        var c = ArenaSharedConfig.Instance;
        float duration = c != null ? c.giantDuration   : 6f;
        float scaleMul = c != null ? c.giantScaleMul   : 3f;
        ball.SetAttributeTemporary(BallAttribute.Pierce, duration);
        ball.SetScaleTemporary(scaleMul, duration);
    }
}
