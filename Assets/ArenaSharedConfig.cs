using System.Collections.Generic;
using UnityEngine;

// Arena1 / Arena2 で共通であるべきチューニング値を 1 箇所に集約する共有設定。
// シーンに 1 個だけ置き（例: GameManager や専用 _Config GameObject にアタッチ）、
// 各アリーナの PlayerController / BlockSpawner / BallScript / LaunchAimer /
// SkillController / ArenaController / DeadZone が初期化時に Instance を読んで自分へ適用する。
//
// 設計メモ（DESIGN.md 7.1 の方針変更, 2026-06-02）:
//   元は「ScriptableObject/Profile は使わず各コンポーネントが自分の SerializeField を持つ」方針だったが、
//   左右アリーナで同値であるべき設定の二重管理を避けるため「シーン内の共有 MonoBehaviour」を導入。
//   アセット(SO)ではなくシーン内コンポーネントなので「Profile/SO 不使用」の精神は維持。
//   per-arena 固有（playerIndex・各アリーナ子オブジェクトへの参照）は各コンポーネントが従来どおり保持する。
//
// null セーフ: この GameObject が存在しなければ Instance は null を返し、
// 各コンポーネントは従来どおり自前の SerializeField 値で動作する（段階移行可能）。
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
    [Header("パドル（PlayerController）")]
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
    [Header("ブロックスポーン（BlockSpawner）")]
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

    [Header("ブロック種別の出現確率 / HP")]
    [Range(0f, 1f)] public float explosiveBlockChance = 0.1f;
    [Range(0f, 1f)] public float hardBlockChance      = 0.2f;
    [Range(0f, 1f)] public float itemBlockChance      = 0.08f;
    [Range(0f, 1f)] public float specialRowChance     = 0.125f;
    public int hardBlockHp = 2;

    [Header("妨害行 / Harden")]
    [Range(0f, 1f)] public float sabotageHardRatio = 0.5f;
    public int sabotageBlockHp = 2;
    public int hardenCount     = 3;
    public int hardenTargetHp  = 3;

    [Header("底到達 / スライドイン演出")]
    public int   blockDeadZoneHitFrames = 5;
    public bool  blockDeadZoneHitShake  = true;
    public float normalSlideDistance    = 1.5f;
    public float normalSlideDuration    = 0.2f;
    public float addRowSlideDistance    = 6f;
    public float addRowSlideDuration    = 0.3f;
    public int   addRowImpactFrames     = 2;
    public Color addRowImpactFlash      = Color.white;
    public float addRowImpactFlashSec   = 0.1f;

    // ---------------- Ball ----------------
    [Header("ボール 速度 / 軌道")]
    public float ballSpeed = 7f;
    public Vector3 ballInitialLocalDirection = new Vector3(1f, 1f, 0f);
    public float relaunchAngleSpread = 3f;
    public float minAxisRatio        = 0.2f;
    public float timeAccelRate       = 0.05f;
    public float timeAccelMax        = 2.0f;
    public float boundX       = 7f;
    public float boundYTop    = 11f;
    public float boundYBottom = -13f;

    [Header("ボール 属性ダメージ / 半径 / 速度係数")]
    public int   normalDamage = 1;
    public int   iceDamage    = 2;
    public int   heavyDamage  = 3;
    public int   pierceDamage = 1;
    public float fireRadius    = 1.5f;
    public float thunderRadius = 2.5f;
    public float heavySpeedFactor = 0.7f;  // Heavy 属性中の速度倍率（DESIGN 5.2「速度0.7倍」）

    [Header("ボール ヒットストップ倍率（属性の手応え重み・壁/パドルの速度ゲート）")]
    public float hitStopSpeedThreshold = 1.5f;  // 壁/パドルの速度ゲート（baseSpeed の何倍超で発動）
    public float hitStopHeavyMul   = 3.0f;       // 属性の手応え重み（GetImpactFrames で使用）
    public float hitStopFireMul    = 1.2f;
    public float hitStopThunderMul = 1.1f;
    public float hitStopIceMul     = 1.2f;
    public int   wallBounceFrames  = 0;          // 壁バウンスの基準フレーム（速度倍率を乗算）

    // ---------------- ヒットストップ / カメラシェイク（手応え集約・Inspector 一元調整） ----------------
    [Header("手応え（ブロック衝突インパクト）")]
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

    [Header("カメラシェイク強度")]
    public float shakeIntensityNormal = 0.08f; // 通常シェイク振幅（ワールド単位）
    public float shakeIntensityStrong = 0.20f; // 強シェイク振幅（ラウンド/マッチ決着）

    [Header("スキル ヒットストップ")]
    // スキル発動時のシェイク演出フレーム数（現状 EXPLOSION の発動シェイクで使用, DESIGN.md 5.6）。
    // ※ serialize 名は旧 SkillPanic から踏襲（シーンの調整値を保つため改名しない）。
    public int   skillPanicHitStopFrames = 15;

    [Header("ボール 色 / Ball Heat / トレイル")]
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
    [Header("発射エイマー（LaunchAimer）")]
    public float indicatorLength      = 2.5f;
    public Color indicatorColor       = Color.yellow;
    public float metronomeAngleRange  = 60f;
    public float metronomePeriodSec   = 1.0f;

    // ---------------- Skill ----------------
    [Header("スキル（SkillController）")]
    public float maxEnergy = 10f;

    // ---------------- Arena ----------------
    [Header("アリーナ（ArenaController / DeadZone）")]
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

    [Header("アイテムアイコン（落下アイテム本体）")]
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
