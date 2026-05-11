using UnityEngine;

// ゲームバランスの全パラメータを集約する ScriptableObject
// プランナーはこのアセットを編集することで全体のバランスを調整できる
[CreateAssetMenu(menuName = "BurokkuKuzushi/GameBalanceProfile", fileName = "GameBalanceProfile")]
public class GameBalanceProfile : ScriptableObject
{
    public HPSettings hp = new HPSettings();
    public HPStateBand[] hpStateBands = new HPStateBand[]
    {
        new HPStateBand { thresholdPercent = 1.0f, gaugeRateMul = 1.0f, itemDropMul = 1.0f, scoreMul = 1.0f },
        new HPStateBand { thresholdPercent = 0.7f, gaugeRateMul = 1.3f, itemDropMul = 1.2f, scoreMul = 1.0f },
        new HPStateBand { thresholdPercent = 0.3f, gaugeRateMul = 1.6f, itemDropMul = 1.5f, scoreMul = 1.5f, goodItemBias = 0.7f },
        new HPStateBand { thresholdPercent = 0.1f, gaugeRateMul = 1.6f, itemDropMul = 1.5f, scoreMul = 1.5f, goodItemBias = 0.7f, panicMode = true },
    };
    public ComboSettings combo = new ComboSettings();
    public BallSettings ball = new BallSettings();
    public LaunchSettings launch = new LaunchSettings();
    public HitStopSettings hitStop = new HitStopSettings();
    public BlockSpawnSettings blockSpawn = new BlockSpawnSettings();

    // 現在のHP割合に応じた HPStateBand を返す
    // hpStateBands は thresholdPercent の降順で並んでいる前提
    public HPStateBand GetBandForRatio(float ratio)
    {
        if (hpStateBands == null || hpStateBands.Length == 0)
            return new HPStateBand();

        for (int i = 0; i < hpStateBands.Length; i++)
        {
            if (ratio >= hpStateBands[i].thresholdPercent)
                return hpStateBands[i];
        }
        return hpStateBands[hpStateBands.Length - 1];
    }
}

// =====================================================
// 以下、Profile に含まれるサブ設定群
// =====================================================

[System.Serializable]
public class HPSettings
{
    [Header("HP基本設定")]
    public int maxHP = 500;

    [Header("ダメージ量")]
    public int damageBallDrop          = 20;  // ボール落下時
    public int damageBlockReachBottom  = 10;  // ブロック1個底到達
    public int damageBlockSpike        = 30;  // 棘ブロック接触
    public int damagePoisonPerSec      = 5;   // 毒エリア滞在/秒
}

// HPの帯ごとに変動するパラメータ（カムバック機構の中核）
[System.Serializable]
public class HPStateBand
{
    [Range(0f, 1f)]
    public float thresholdPercent = 1.0f;  // この値以上のHP割合で適用

    public float gaugeRateMul = 1.0f;      // スキルゲージ蓄積倍率
    public float itemDropMul  = 1.0f;      // アイテムドロップ率倍率
    public float scoreMul     = 1.0f;      // スコア倍率
    [Range(0f, 1f)]
    public float goodItemBias = 0f;        // 有利アイテムへの偏重（0=均等、1=有利のみ）
    public bool  panicMode    = false;     // ピンチ専用スキル解禁などのフラグ
}

[System.Serializable]
public class ComboSettings
{
    public float comboTimeoutSec        = 2f;  // コンボ判定の連続時間（将来用）
    public int   interferenceTriggerCombo = 5; // 妨害送付に必要なコンボ数
}

[System.Serializable]
public class BallSettings
{
    [Header("ボール速度")]
    public float speed = 7f;

    [Header("軌道補正")]
    [Range(0f, 1f)]
    public float minAxisRatio = 0.2f;

    [Header("リスポーン")]
    public float relaunchAngleSpread = 0.5f;

    [Header("ループ脱出保険")]
    public float stuckTimeoutSec = 5f;  // この時間ブロック未ヒットで加速
    public float stuckSpeedMul   = 1.1f;

    [Header("ダメージ")]
    public int normalDamage = 1;
    public int iceDamage    = 2;
    public int heavyDamage  = 3;

    [Header("属性範囲")]
    public float fireRadius    = 1.5f;
    public float thunderRadius = 2.5f;
}

[System.Serializable]
public class LaunchSettings
{
    [Header("メトロノーム式発射（Phase B 以降）")]
    public float metronomeAngleRange = 60f; // 振れ幅 ±度
    public float metronomePeriodSec  = 1.0f;
}

[System.Serializable]
public class HitStopSettings
{
    // フレーム数（60fps想定、数値はそのまま固定フレーム使用）
    public int explosiveBlockFrames     = 6;
    public int spikeBlockFrames         = 4;
    public int paddleBounceFrames       = 1;
    public int interferenceTriggerFrames = 10;
    public int panicSkillFrames         = 15;
    public int roundEndFrames           = 30;
    public int matchEndFrames           = 60;
}

[System.Serializable]
public class BlockSpawnSettings
{
    [Header("レイアウト")]
    public int   blocksPerRow = 7;
    public float blockGap     = 0.1f;
    public float blockHeight  = 0.7f;

    [Header("スポーン・降下")]
    public float spawnInterval = 5f;
    public float descentSpeed  = 0.3f;

    [Header("通常行のブロック種出現率")]
    [Range(0f, 1f)] public float explosiveBlockChance = 0.1f;
    [Range(0f, 1f)] public float hardBlockChance      = 0.2f;
    public int hardBlockHp = 2;

    [Header("妨害行")]
    [Range(0f, 1f)] public float sabotageHardRatio = 0.5f;
    public int sabotageBlockHp = 2;
}
