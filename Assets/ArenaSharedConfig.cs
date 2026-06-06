using System.Collections.Generic;
using UnityEngine;

// Arena1 / Arena2 で共通であるべきチューニング値を 1 箇所に集約する共有設定。
// シーンに 1 個だけ置き（例: GameManager や専用 _Config GameObject にアタッチ）、
// 各アリーナの PlayerController / BlockSpawner / BallScript / LaunchAimer /
// SkillController / ArenaController / DeadZone / Block / ItemDrop / Zone /
// GameManager が初期化時に Instance を読んで自分へ適用する。
//
// 設計メモ（DESIGN.md 7.1 の方針変更, 2026-06-02 / 2026-06-06 全バランス一元化）:
//   元は「ScriptableObject/Profile は使わず各コンポーネントが自分の SerializeField を持つ」方針だったが、
//   左右アリーナで同値であるべき設定の二重管理を避けるため「シーン内の共有 MonoBehaviour」を導入。
//   さらに 2026-06-06 に「全ゲームバランスをここから調整する」方針へ拡張し、スキル/アイテム/
//   試合(HP/ダメージ/コンボ)/ブロック挙動/妨害ゾーンのパラメータも集約した。
//   各コンポーネントは ApplySharedConfig 相当で初期化時に値を読む。Inspector で散らばらないよう、
//   バランス系の SerializeField は各コンポーネントから外し、ここを唯一の調整面とする。
//
// null セーフ: この GameObject が存在しなければ Instance は null を返し、
// 各コンポーネントは従来どおり自前のコード既定値で動作する（段階移行可能）。
public class ArenaSharedConfig : MonoBehaviour
{
    private static ArenaSharedConfig _instance;
    // Awake 順に依存せず取得できるよう、未解決なら都度 Find する（シーンに 1 個前提）。
    public static ArenaSharedConfig Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<ArenaSharedConfig>();
            return _instance;
        }
    }

    void Awake()      { _instance = this; }
    void OnDestroy()  { if (_instance == this) _instance = null; }

    // ---------------- Paddle (PlayerController) ----------------
    [Header("Paddle (PlayerController)")]
    public float paddleSpeed        = 10f;
    public float paddleXLimit       = 5.5f;
    public float paddleLocalY       = -5f;
    public float paddleLocalZ       = 0f;
    public int   paddleBounceFrames = 0;
    public Color paddleBuffFlash    = new Color(0.306f, 0.765f, 1.000f); // Cyan
    public Color paddleAttackFlash  = new Color(1.000f, 0.298f, 0.235f); // Red
    public Color paddleTrapFlash    = new Color(0.792f, 0.286f, 0.851f); // Purple
    public float pickupFlashDuration = 0.1f;

    // ---------------- Block spawner ----------------
    [Header("Block Spawner")]
    public int   blocksPerRow = 7;
    public float blockWidth   = 1.5f;
    public float blockGap     = 0.1f;
    public float blockHeight  = 0.7f;
    public float spawnY         = 4.5f;
    public float blockDeadZoneY = -4.5f;

    [Header("Dynamic Escalation")]
    public float spawnIntervalBase        = 5.0f;
    public float spawnIntervalDecayPerMin = 0.2f;
    public float spawnIntervalMin         = 3.0f;
    public float descentSpeedBase         = 0.3f;
    public float descentSpeedGainPerMin   = 0.03f;
    public float descentSpeedMax          = 0.45f;

    [Header("Block Type Spawn Chances / HP")]
    [Range(0f, 1f)] public float explosiveBlockChance = 0.1f;
    [Range(0f, 1f)] public float hardBlockChance      = 0.2f;
    [Range(0f, 1f)] public float itemBlockChance      = 0.08f;
    [Range(0f, 1f)] public float specialRowChance     = 0.125f;
    public int hardBlockHp = 2;

    [Header("Sabotage Row / Harden")]
    [Range(0f, 1f)] public float sabotageHardRatio = 0.5f;
    public int sabotageBlockHp = 2;
    public int hardenCount     = 3;
    public int hardenTargetHp  = 3;

    [Header("Bottom Reach / Slide-In FX")]
    public int   blockDeadZoneHitFrames = 5;
    public bool  blockDeadZoneHitShake  = true;
    public float normalSlideDistance    = 1.5f;
    public float normalSlideDuration    = 0.2f;
    public float addRowSlideDistance    = 6f;
    public float addRowSlideDuration    = 0.3f;
    public int   addRowImpactFrames     = 2;
    public Color addRowImpactFlash      = Color.white;
    public float addRowImpactFlashSec   = 0.1f;

    // ---------------- Block behavior (Block prefab) ----------------
    [Header("Block Behavior (drop / explosion)")]
    public float baseDropChance      = 0.15f; // 通常ブロックのアイテムドロップ確率
    [Range(0f, 1f)] public float trapDisguiseChance = 0.1f;  // 罠アイテムが強化枠に偽装する確率
    public float explosionRadius     = 2f;    // Explosive 破壊の巻き込み半径
    public int   explosionDamage     = 1;     // 爆発の巻き込みダメージ
    public float explosionChainDelay = 0.07f; // 連鎖爆発が一拍ずつ広がる遅延（秒）

    // ---------------- Ball ----------------
    [Header("Ball Speed / Trajectory")]
    public float ballSpeed = 7f;
    public Vector3 ballInitialLocalDirection = new Vector3(1f, 1f, 0f);
    public float relaunchAngleSpread = 3f;
    public float minAxisRatio        = 0.2f;
    public float timeAccelRate       = 0.05f;
    public float timeAccelMax        = 2.0f;
    public float boundX       = 7f;
    public float boundYTop    = 11f;
    public float boundYBottom = -13f;

    [Header("Ball Attribute Damage / Radius / Speed Factor")]
    public int   normalDamage = 1;
    public int   iceDamage    = 2;
    public int   heavyDamage  = 3;
    public int   pierceDamage = 1;
    public float fireRadius    = 1.5f;
    public float thunderRadius = 2.5f;
    public float heavySpeedFactor = 0.7f;  // Heavy 属性中の速度倍率（DESIGN 5.2「速度0.7倍」）

    [Header("Ball HitStop Multipliers (attr weight / speed gate)")]
    public float hitStopSpeedThreshold = 1.5f;  // 壁/パドルの速度ゲート（baseSpeed の何倍超で発動）
    public float hitStopHeavyMul   = 3.0f;       // 属性の手応え重み（GetImpactFrames で使用）
    public float hitStopFireMul    = 1.2f;
    public float hitStopThunderMul = 1.1f;
    public float hitStopIceMul     = 1.2f;
    public int   wallBounceFrames  = 0;          // 壁バウンスの基準フレーム（速度倍率を乗算）

    // ---------------- ヒットストップ / カメラシェイク（手応え集約・Inspector 一元調整） ----------------
    [Header("Impact Feel (block hit hitstop)")]
    // ブロック衝突の停止フレーム = clamp(round(impactBaseFrames × impact), 1, impactMaxFrames)
    //   impact = speedTerm × attackWeight,  speedTerm = 1 + impactSpeedWeight×(naturalSpeed/baseSpeed − 1)
    //   attackWeight = 属性倍率（Normal1.0 / Ice・Fire1.2 / Thunder1.1 / Heavy3.0 / Pierce0）
    //   impact < impactThreshold は 0（軽い当たりは止めずテンポ維持）
    public int   impactBaseFrames  = 2;    // 標準的な一撃の基準フレーム
    public float impactSpeedWeight = 0.6f; // 速度寄与の強さ（0=速度無視 / 1=線形）
    public float impactThreshold   = 1.4f; // これ未満は手応えを出さない（0フレーム）
    public int   impactMaxFrames   = 10;   // 一撃の停止フレーム上限
    public int   explosiveHitFrames = 6;   // Explosive 破壊の最低保証フレーム（手応えがこれ未満でも下限）
    // 実効速度が baseSpeed の何倍を超えたらブロック衝突を「フリーズせずシェイクのみ」にするか（HYPER 等の高速時）。
    // フリーズで止まらない＝爽快さ維持＋トレイルが途切れない。0 以下なら無効（常にフリーズ）, DESIGN.md 5.2/5.6。
    public float freezeSkipSpeedFactor = 2.5f;

    [Header("Camera Shake Intensity")]
    public float shakeIntensityNormal = 0.08f; // 通常シェイク振幅（ワールド単位）
    public float shakeIntensityStrong = 0.20f; // 強シェイク振幅（ラウンド/マッチ決着）

    [Header("Skill HitStop")]
    // スキル発動時のシェイク演出フレーム数（現状 EXPLOSION の発動シェイクで使用, DESIGN.md 5.6）。
    // ※ serialize 名は旧 SkillPanic から踏襲（シーンの調整値を保つため改名しない）。
    public int   skillPanicHitStopFrames = 15;

    [Header("Ball Color / Ball Heat / Trail")]
    public Color ballNormalColor  = Color.white;
    public Color ballFireColor    = new Color(1.0f, 0.478f, 0.239f);
    public Color ballThunderColor = new Color(1.0f, 0.847f, 0.290f);
    public Color ballIceColor     = new Color(0.306f, 0.765f, 1.0f);
    public Color ballHeavyColor   = new Color(0.706f, 0.643f, 1.0f);
    public Color ballPierceColor  = new Color(0.635f, 1.0f, 0.878f);
    public int   heatStage1 = 10;
    public int   heatStage2 = 20;
    public int   heatStage3 = 30;
    public Color heatColorLow  = new Color(1.0f, 0.949f, 0.690f);
    public Color heatColorMid  = new Color(1.0f, 0.690f, 0.290f);
    public Color heatColorHigh = new Color(1.0f, 0.290f, 0.200f);
    public float heatLerpSpeed = 6f;
    public float trailTime       = 0.18f;
    public float trailStartWidth = 0.22f;

    // ---------------- LaunchAimer ----------------
    [Header("Launch Aimer")]
    public float indicatorLength      = 2.5f;
    public Color indicatorColor       = Color.yellow;
    public float metronomeAngleRange  = 60f;
    public float metronomePeriodSec   = 1.0f;

    // ---------------- Skill ----------------
    [Header("Skill (SkillController)")]
    public float maxEnergy = 10f;
    public float skillChargeLockSeconds = 10f; // スキル発動後ゲージが溜まらない時間（クールダウン）

    [Header("Skill - HYPER")]
    public float hyperEnergyCost = 6f;
    public float hyperDuration   = 6f;
    public float hyperSpeedMul   = 5f;

    [Header("Skill - EXPLOSION")]
    public float explosionEnergyCost = 8f;
    [Range(0f, 1f)] public float explosionFraction = 0.3f; // 盤面ブロックのこの割合を Explosive 化

    [Header("Skill - BURST")]
    public float burstEnergyCost   = 10f;
    public int   burstShots        = 10;
    public float burstInterval     = 0.2f;
    public float burstAngle        = 45f;
    public float burstBallLifetime = 8f;

    [Header("Skill - GIANT")]
    public float giantEnergyCost = 5f;
    public float giantDuration   = 6f;
    public float giantScaleMul   = 3f;

    // ---------------- Item effects (ItemDrop) ----------------
    [Header("Item - Drop / Durations")]
    public float itemDropSpeed   = 2.5f; // 落下速度
    public float fireDuration    = 5f;
    public float thunderDuration = 3f;
    public float iceDuration     = 8f;
    public float heavyDuration   = 8f;
    public float pierceDuration  = 3f;
    public float paddleDuration  = 10f;  // Enlarge/Shrink 持続
    public float speedDuration   = 10f;  // SpeedUp 持続
    public float itemHyperDuration   = 3f;   // TrapBall_Hyperspeed 持続
    public float reversedDuration    = 5f;   // TrapBall_Reversed 持続

    [Header("Item - Effect Magnitudes")]
    public float enlargeMultiplier = 1.5f; // パドル拡大倍率
    public float shrinkMultiplier  = 0.7f; // パドル縮小倍率 (Trap)
    public float speedUpMultiplier = 1.3f; // パドル移動速度倍率
    public float itemHyperMultiplier = 4f;   // ハイパー速度倍率 (Trap)
    public int   healAmount          = 50;   // 回復量

    // ---------------- Match balance (GameManager) ----------------
    [Header("Match - HP / Damage")]
    public int maxHP                  = 500;
    public int damageBallDrop         = 0;   // ボール落下のペナルティ（現行シーン値=0）
    public int damageBlockReachBottom = 10;  // ブロック底到達のペナルティ
    public int damagePoisonPerSec     = 5;   // 毒ゾーンの秒間ダメージ

    [Header("Match - Energy / Combo")]
    public float energyPerBlock = 1f;    // ブロック破壊ごとのエナジー
    public float comboTimeout   = 6.0f;  // 最後の破壊からこの秒数でコンボリセット
    public int   comboScoreStep = 5;     // 何コンボ毎にスコア +10% するか
    public int   comboGaugeStep = 5;     // 何コンボ毎にゲージ +5% するか
    public int   comboItemStep  = 20;    // 何コンボ毎にドロップ +10% するか（現行シーン値=20）
    public int[] comboMilestones = { 10, 20, 30 };  // 演出を出すコンボ数

    [Header("Match - HP State Bands (threshold 降順)")]
    public HPStateBand[] hpStateBands;   // 空なら全倍率 1.0（逆転ボーナス無し）

    // ---------------- Interference zones ----------------
    [Header("Interference - Poison Zone")]
    public float poisonZoneDuration = 5f;
    public float poisonZoneRadius   = 1.0f;

    [Header("Interference - Slow Zone")]
    public float slowZoneFallSpeed = 8f;
    public float slowZoneDuration  = 8f;
    public float slowZoneRadius    = 1.5f;
    public float slowZoneFactor    = 0.5f; // 内部のボール速度倍率

    // ---------------- Arena ----------------
    [Header("Arena (ArenaController / DeadZone)")]
    public float arenaHalfWidth   = 5f;
    public float arenaHalfHeight  = 4.5f;
    public float ballSpawnOffsetY = 1f;   // ArenaController と DeadZone で同値であるべき

    // ---------------- Item icons ----------------
    // 落下中アイテム本体（ArenaController.SpawnItem）に表示するスプライト。
    // ItemType ごとに Inspector で割り当てる（順不同・未割り当ては従来の色付き球にフォールバック）。
    [System.Serializable]
    public struct ItemIcon
    {
        public ItemType type;
        public Sprite   sprite;
    }

    [Header("Item Icons (falling item body)")]
    public ItemIcon[] itemIcons;
    public float      itemIconWorldSize = 0.9f;  // 表示するアイコンのワールド高さ（スプライト bounds 基準）
    // Bloom 発光の強さ。Custom/HDRSprite のマテリアル _Color(HDR) RGB に載る → 1 超で Bloom Threshold(1.0) を越えて発光。
    // （SpriteRenderer.color は Color32 にクランプされるため発光に使えない点に注意）
    // 1 で発光なし（等倍）、1.5〜2.5 で明部がにじむ。0 でアイコン非表示になるので注意。
    public float      itemIconGlow = 1.8f;

    private Dictionary<ItemType, Sprite> _itemIconMap;

    // ItemType → Sprite。未割り当て / 未配置なら null（呼び出し側が球にフォールバック）。
    public Sprite GetItemIcon(ItemType type)
    {
        if (_itemIconMap == null)
        {
            _itemIconMap = new Dictionary<ItemType, Sprite>();
            if (itemIcons != null)
                foreach (var e in itemIcons)
                    if (e.sprite != null) _itemIconMap[e.type] = e.sprite;
        }
        return _itemIconMap.TryGetValue(type, out var s) ? s : null;
    }
}
