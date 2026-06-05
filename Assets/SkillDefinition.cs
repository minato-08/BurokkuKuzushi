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
public sealed class SkillHyper : SkillDefinition
{
    public float energyCost      = 6f;
    public float duration        = 6f;
    public float speedMultiplier = 5f;

    public override string  DisplayName => "HYPER";
    public override SkillId  Id          => SkillId.Hyper;
    public override float    EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        arena.GetBall()?.SetSpeedTemporary(speedMultiplier, duration);
        arena.SpawnHyperFloor(duration);
    }
}

// EXPLOSION: 盤面ブロックの一定割合をランダムに Explosive 化する
public sealed class SkillExplosion : SkillDefinition
{
    public float energyCost = 8f;
    public float fraction   = 0.3f; // 盤面ブロックのこの割合を Explosive 化（0〜1）

    public override string  DisplayName => "EXPLOSION";
    public override SkillId  Id          => SkillId.Explosion;
    public override float    EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        arena.GetSpawner()?.ConvertRandomToExplosive(fraction);

        // 発動の手応え＝シェイクのみ（ボール衝突ではないのでフリーズしない, DESIGN.md 5.x）
        int frames = ArenaSharedConfig.Instance != null
            ? ArenaSharedConfig.Instance.skillPanicHitStopFrames : 15;
        arena.TriggerHitStop(frames, strong: true, shake: true, freeze: false);
    }
}

// BURST: 発動すると shots 発のプレーンなボールを interval 秒間隔で自動連射する（プレイヤー操作には干渉しない）。
// 角度は鉛直上を 0° として +angle と -angle を交互に飛ばす（DESIGN.md 5.6）。
public sealed class SkillBurst : SkillDefinition
{
    public float energyCost   = 10f;
    public int   shots        = 10;     // 発射数
    public float interval     = 0.2f;   // 発射間隔（秒）
    public float angle        = 45f;    // 鉛直上(0°)からの発射角。+angle / -angle を交互に使う
    public float ballLifetime = 8f;     // 撃ったボールの寿命

    public override string  DisplayName => "BURST";
    public override SkillId  Id          => SkillId.Burst;
    public override float    EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        arena.BeginBurst(shots, interval, angle, ballLifetime);
    }
}

// GIANT: ボールを巨大化し Pierce 化する（巨大貫通弾）
public sealed class SkillGiant : SkillDefinition
{
    public float energyCost      = 5f;
    public float duration        = 6f;
    public float scaleMultiplier = 3f;

    public override string  DisplayName => "GIANT";
    public override SkillId  Id          => SkillId.Giant;
    public override float    EnergyCost  => energyCost;

    public override void Activate(int playerIndex, ArenaController arena)
    {
        BallScript ball = arena.GetBall();
        if (ball == null) return;
        ball.SetAttributeTemporary(BallAttribute.Pierce, duration);
        ball.SetScaleTemporary(scaleMultiplier, duration);
    }
}
