# BurokkuKuzushi アーキテクチャ資料

最終更新: 2026-05-15

このドキュメントはコードを読まなくても実装の全体像を把握できることを目的とする。  
ゲームルール・仕様は [`DESIGN.md`](./DESIGN.md)、開発進捗は [`ROADMAP.md`](./ROADMAP.md) を参照。

---

## 目次

1. [プロジェクト技術スタック](#1-プロジェクト技術スタック)
2. [シーン構造](#2-シーン構造)
3. [アーキテクチャの核心：イベントハブパターン](#3-アーキテクチャの核心イベントハブパターン)
4. [ゲームステート管理](#4-ゲームステート管理)
5. [スクリプト一覧と依存関係](#5-スクリプト一覧と依存関係)
6. [各スクリプト詳細](#6-各スクリプト詳細)
7. [主要データフロー](#7-主要データフロー)
8. [ヒットストップシステム](#8-ヒットストップシステム)
9. [ボール速度の3層管理](#9-ボール速度の3層管理)
10. [アイテム・スキルの共通設計](#10-アイテムスキルの共通設計)
11. [妨害システム](#11-妨害システム)
12. [座標系と2アリーナ分離](#12-座標系と2アリーナ分離)
13. [パラメータ管理方針](#13-パラメータ管理方針)
14. [設計上の重要な判断とその理由](#14-設計上の重要な判断とその理由)
15. [エディタ拡張](#15-エディタ拡張)

---

## 1. プロジェクト技術スタック

| 項目 | 内容 |
|---|---|
| エンジン | Unity 6 + URP (Universal Render Pipeline) |
| 言語 | C# 9 (Unity 2022相当) |
| 物理 | Unity 3D Physics (Rigidbody CCD) |
| UI | TextMeshPro + Unity UI (UGUI) |
| 入力 | Unity Input System (`Keyboard.current`) |
| バージョン管理 | Git / GitHub |

---

## 2. シーン構造

アクティブシーン: `Assets/Scenes/SampleScene.unity`

```
SampleScene
├── EventSystem
├── GameManager                    ← Singleton。全ゲームロジックの唯一の窓口
├── CenterUI  (Canvas)             ← Screen Space Overlay。両カメラに重なる
│   ├── P1HPText / P1HPFill
│   ├── P1Score / P1Combo / P1Wins
│   ├── P1EnergyFill / P1SkillText
│   ├── P2HPText / P2HPFill
│   ├── P2Score / P2Combo / P2Wins
│   ├── P2EnergyFill / P2SkillText
│   ├── GameOverText
│   ├── MatchResultPanel           ← MatchResultUI がアタッチ
│   ├── SkillSelectPanel           ← SkillSelectUI がアタッチ
│   ├── P1InterferenceOverlay      ← 妨害通知フラッシュ（CanvasGroup）
│   └── P2InterferenceOverlay
│
├── Arena1  (world pos: 0, 0, 0)   ← 1Pのアリーナ
│   ├── Camera1                    ← Arena1の子。localPos(-0.3, 0, -25) FOV 45°
│   ├── TopWall / LeftWall / RightWall
│   ├── Ball                       ← BallScript, Rigidbody, Collider, "BallTag"タグ必須
│   ├── Player                     ← PlayerController, Rigidbody(isKinematic)
│   ├── DeadZone                   ← localPos(0, -11, 0)。落下検知トリガー
│   ├── BlockSpawner               ← ブロックを子として動的生成・管理
│   └── ArenaController            ← Arena内の司令塔（ArenaControllerは Arena1の子）
│       ├── HitStopController      ← Setup HitStop で自動生成
│       └── LaunchAimer            ← Setup LaunchAimer で自動生成
│
└── Arena2  (world pos: 50, 0, 0)  ← 2Pのアリーナ（Arena1と完全に同構成）
    ├── Camera2                    ← localPos(0.2, 0, -25) FOV 45°。AudioListener なし
    └── （Arena1と同構成）
```

### なぜ Arena2 は X=50 にオフセットするのか

カメラが Arena の**子**として配置されているため、Arena ごとオフセットするだけで完全に独立した3D空間が成立する。Arena1 と Arena2 のオブジェクトは同一シーン内に共存しているが、それぞれのカメラが独自の Viewport を持つため、プレイヤーからは相手のアリーナは見えない。この設計により、座標変換・シーン分割・特殊ローディングなしで2画面を実現している。

---

## 3. アーキテクチャの核心：イベントハブパターン

**すべてのゲームイベントは `GameManager.Instance` を経由する。** 各コンポーネントは直接 HP を操作したり、相手アリーナのオブジェクトを参照したりしない。

```
                   ┌─────────────────────────────┐
                   │        GameManager           │
                   │  (Singleton, 唯一の審判)     │
                   └──────────────┬──────────────┘
          ↑ 通知・要求              │ 指示・状態提供
          │                        ↓
  Block / BallScript          ArenaController
  DeadZone / ZonePoison       UIManager（ポーリング）
  LaunchAimer / SkillController
```

### メリット

- 相手アリーナへの直接参照が不要 → コンポーネント間の結合が疎
- HP操作・妨害送付・ラウンド終了など競合しやすい処理が1か所に集約される
- テスト・デバッグ時に GameManager のログだけで試合全体のイベントを追える

---

## 4. ゲームステート管理

`GameManager.GameState` enum で管理する。

```
WaitingToStart
    ↓ Start()
SkillSelect  ← Time.timeScale = 0
    ↓ BeginMatch()（両プレイヤーがスキル確定）
Playing      ← Time.timeScale = 1
    ↓ どちらかの HP = 0
RoundOver    ← NextRoundCoroutine（WaitForSecondsRealtime）
    ↓ nextRoundDelay 秒後 StartNextRound()
Playing（次ラウンド）
    ↓ 先取条件達成
MatchOver    ← Time.timeScale = 0（MatchOverCoroutine 後）
    ↓ 再戦 → StartRematch() → SkillSelect
```

`WaitForSecondsRealtime` を使用するため、`Time.timeScale = 0` の状態でもコルーチンが進む。

### 各状態での動作制限

| コンポーネント | SkillSelect | Playing | RoundOver | MatchOver |
|---|---|---|---|---|
| BlockSpawner | 停止（GameState判定） | 動作 | 動作 | 停止 |
| BallScript | 停止（timeScale=0） | 動作 | 動作 | 停止 |
| LaunchAimer | 表示停止 | 動作 | 動作 | 停止 |
| SkillController | 停止（GameState判定） | 動作 | 停止 | 停止 |

---

## 5. スクリプト一覧と依存関係

### 依存関係図（→ は「参照する」）

```
GameManager
  → HPSystem（保持、純粋C#クラス）
  → ArenaController（arena1 / arena2）
  → ArenaController.GetSkillController()

ArenaController
  → BallScript（SerializeField）
  → BlockSpawner（SerializeField）
  → HitStopController（GetComponentInChildren）
  → LaunchAimer（SerializeField）
  → PlayerController（cachedPlayer: Awakeでキャッシュ）
  → UIManager（cachedUIManager: Awakeでキャッシュ）

BlockSpawner
  → Block（List<Block> allBlocks で管理）
  → ArenaController（GetArena()で取得）
  → GameManager.Instance

BallScript
  → ArenaController（GetArena()で取得）
  → Block（OnHitBlock: 属性効果対象）

Block
  → BallScript（OnCollisionEnter: GetDamage/OnHitBlock）
  → ArenaController（GetArena()で取得）
  → GameManager.Instance

PlayerController
  → ArenaController（GetArena()で取得）
  → SkillController（OnCollisionEnter: ForceCatch確認）

DeadZone → GameManager.Instance / BallScript
ZonePoison → GameManager.Instance
ZoneSlow → BallScript（slowZoneMul直接書き込み）

ItemDrop
  → EffectDefinition（BuildEffect()で生成）
  → PlayerController（接触判定）

SkillController
  → SkillDefinition（equippedSkill）
  → EnergySystem（保持、純粋C#クラス）
  → ArenaController（Activate呼び出し）

UIManager → GameManager.Instance（毎フレームポーリング）
MatchResultUI → GameManager.Instance
SkillSelectUI → GameManager.Instance / SkillController
```

### ファイル別スクリプト種別

| ファイル | 種別 | 役割 |
|---|---|---|
| `GameManager.cs` | MonoBehaviour (Singleton) | 試合制御全般 |
| `HPSystem.cs` | 純粋C#クラス | HP計算のみ |
| `EnergySystem.cs` | 純粋C#クラス | エナジーゲージ計算のみ |
| `IFreezable.cs` | インターフェース | ヒットストップ対象の契約 |
| `ArenaController.cs` | MonoBehaviour | アリーナ内の生成・橋渡し |
| `HitStopController.cs` | MonoBehaviour | Freeze/Unfreeze + カメラシェイク |
| `BallScript.cs` | MonoBehaviour + IFreezable | ボール挙動・属性 |
| `BlockSpawner.cs` | MonoBehaviour + IFreezable | ブロック生成・降下・底到達 |
| `Block.cs` | MonoBehaviour | ブロック単体の衝突・破壊 |
| `PlayerController.cs` | MonoBehaviour + IFreezable | パドル操作 |
| `DeadZone.cs` | MonoBehaviour | ボール落下検知 |
| `LaunchAimer.cs` | MonoBehaviour | メトロノーム発射インジケーター |
| `ZonePoison.cs` | MonoBehaviour | 毒エリア（Phase E） |
| `ZoneSlow.cs` | MonoBehaviour | 減速エリア（Phase E） |
| `EffectDefinition.cs` | 純粋C#クラス群 | アイテム効果の抽象定義 |
| `ItemDrop.cs` | MonoBehaviour + static class | アイテム落下・効果適用 |
| `SkillController.cs` | MonoBehaviour | スキルゲージ・発動入力 |
| `SkillDefinition.cs` | 純粋C#クラス群 | スキル効果の抽象定義 |
| `UIManager.cs` | MonoBehaviour | 毎フレームUI更新 |
| `MatchResultUI.cs` | MonoBehaviour | マッチ結果画面 |
| `SkillSelectUI.cs` | MonoBehaviour | スキル選択画面 |

---

## 6. 各スクリプト詳細

### GameManager

ゲーム全体の唯一の審判。Singleton。

**保持するデータ:**
```
HPSystem p1HP, p2HP
int p1Score, p2Score
int p1RoundWins, p2RoundWins
int p1DestroyedCount, p2DestroyedCount  ← 次の妨害送付までのカウント
GameState currentState
```

**HP帯別パラメータ（HPStateBand[]）:**  
Inspector で `thresholdPercent` 降順の配列を設定する。`GetCurrentBand(playerIndex)` が現在HPに応じたバンドを線形探索で返す。配列が空なら全倍率1.0のデフォルトを返す。

```
HP 100-70%: gaugeRateMul=1.0 / itemDropMul=1.0 / scoreMul=1.0
HP  70-30%: gaugeRateMul=1.3 / itemDropMul=1.2
HP  30-10%: gaugeRateMul=1.6 / itemDropMul=1.5 / goodItemBias有効
HP  10%以下: panicMode=true → SkillPanic_BlockClear 解禁
```

**主要メソッド:**
| メソッド | 呼び出し元 | 処理 |
|---|---|---|
| `OnBallDropped(pi)` | DeadZone | damageBallDrop を ApplyDamage |
| `OnBlocksReachedBottom(pi, count)` | BlockSpawner | damageBlockReachBottom × count |
| `OnSpikeHit(pi)` | Block | damageBlockSpike を ApplyDamage |
| `OnPoisonTick(pi, dt)` | ZonePoison | damagePoisonPerSec × dt を ApplyDamage |
| `OnForceRespawn(pi)` | LaunchAimer | damageForceRespawn を ApplyDamage |
| `RegisterBlockDestroyed(pi)` | Block | コンボ++、閾値でSendSabotageTo |
| `Heal(pi, amount)` | EffectHeal経由 | HPSystem.Heal |
| `AddScore(pi, amount)` | Block | scoreMul を乗算して加算 |

---

### HPSystem（純粋C#クラス）

GameManager が P1/P2 ぶんインスタンスを保持する。MonoBehaviour ではない。

```csharp
public int   CurrentHP { get; }
public int   MaxHP     { get; }
public float Ratio     { get; }  // CurrentHP / MaxHP
public bool  IsAlive   { get; }  // CurrentHP > 0

public int TakeDamage(int amount)  // 戻り値: 実際に減った量
public int Heal(int amount)        // 戻り値: 実際に回復した量
public void Reset()                // ラウンド開始時リセット
public void SetMaxHP(int newMaxHP, bool refill = false)
```

---

### ArenaController

Arena 内のファサード（Facade）。GameManager と各コンポーネントの橋渡し。  
`ArenaController` は `Arena1/Arena2` の子として配置されているため、Arena ルートは `transform.parent`。

**Awake でのキャッシュ:**
```csharp
cachedPlayer    = ArenaRoot.GetComponentInChildren<PlayerController>();
cachedUIManager = Object.FindFirstObjectByType<UIManager>();
hitStop         = GetComponentInChildren<HitStopController>();
// hitStop に ball / spawner / cachedPlayer を RegisterFreezable
// launchAimer.Initialize(ball, playerIndex, this)
// skillController を AddComponent して Initialize
```

**公開 API:**
| メソッド | 用途 |
|---|---|
| `TriggerHitStop(frames, strong, shake)` | Block/BallScript/GameManager から呼ぶ統一口 |
| `SpawnItem(worldPos, type)` | Block.TryDropItem から呼ぶ |
| `SpawnZonePoison(worldPos)` | Block(Spike破壊) / GameManager(Interference) から呼ぶ |
| `SpawnZoneSlow(worldPos)` | GameManager(Interference) から呼ぶ |
| `HardenBlocks()` | GameManager(Interference) から呼ぶ |
| `ShowInterferenceOverlay(label)` | GameManager から呼ぶ |
| `ResetForNewRound()` | GameManager から呼ぶ。ブロック/ゾーン全消去 + ボールリスポーン準備 |
| `SpawnExtraBall(duration)` | SkillBall_Multi から呼ぶ |
| `GetBall() / GetSpawner() / GetSkillController()` | 各システムが参照 |
| `GetBallSpawnLocalPos() / GetPaddleWorldY()` | LaunchAimer / DeadZone / ZonePoison が使用 |
| `GetRandomFloorWorldPos()` | Interference で ZonePoison/ZoneSlow をランダム位置に生成 |

---

### BallScript

`IFreezable` 実装。ヒットストップ中は `rb.linearVelocity = Vector3.zero`。

**速度の3層管理:**
```
naturalSpeed  = baseSpeed + 時間加速（isExtraBall=false のメインボールのみ）
                Mathf.Min(baseSpeed × timeAccelMax, baseSpeed + timeAccelRate × arenaDwellTime)
speedMultiplier = アイテム効果（SetSpeedTemporary コルーチン。1.0 がデフォルト）
slowZoneMul   = ZoneSlow が毎フレーム書き込む（ZoneSlow がゾーン離脱/Destroy 時に 1 に戻す）

実効速度 = naturalSpeed × speedMultiplier × slowZoneMul
```
`FixedUpdate` で毎フレーム `rb.linearVelocity = 正規化 × 実効速度` に強制補正する。

**属性別の動作:**
| 属性 | GetDamage | GetAttributeMultiplier | OnHitBlock |
|---|---|---|---|
| Normal | 1 | 1.0 | 何もしない |
| Fire | 1 | 1.2 | 周囲ブロックに範囲ダメージ |
| Thunder | 1 | 1.1 | 周囲の同種ブロックに連鎖ダメージ |
| Ice | 2 | 1.2 | 何もしない |
| Heavy | 3 | 1.5 | `rb.linearVelocity = lastVelocity`（貫通） |
| Pierce | 1 | 0.0 | `rb.linearVelocity = lastVelocity`（貫通・ヒットストップなし） |

**ヒットストップ係数（GetHitStopMultiplier）:**  
`naturalSpeed / baseSpeed` が `hitStopSpeedThreshold` (デフォルト 1.5) 未満なら 0 を返す。これ以上なら 0→1 の線形スケール。ブロック衝突・壁バウンスのフレーム数に乗算するため、低速時はヒットストップが発動しない。

**状態遷移:**
```
通常飛行
  PrepareRespawn() → IsWaitingToLaunch=true, Collider無効, velocity=0
      LaunchInDirection() → Collider有効, IsWaitingToLaunch=false, 発射
```

**GetArena():**
```csharp
transform.parent?.GetComponentInChildren<ArenaController>()
// Ball → Arena ルート（Ballの親）→ ArenaController
```

---

### BlockSpawner

`IFreezable` 実装。ブロック降下はヒットストップ中停止する。

**ブロック管理:**
```csharp
private List<Block> allBlocks;  // ブロックの実体。DestroyされるとGOは削除されるがnullが残る
                                // DescendBlocks/CheckBottomReached でnull除去
```

**Update の処理順序（毎フレーム）:**
1. `spawnTimer` 加算 → 閾値で `SpawnRow(Normal)`
2. `pendingSabotageRows > 0 && IsTopClear()` → `SpawnRow(Sabotage)`
3. `pendingSpikeRows > 0 && IsTopClear()` → `SpawnRow(Spike)`
4. `DescendBlocks()` — 全ブロックを `descentSpeed × dt` だけ下に移動
5. `CheckBottomReached()` — `blockDeadZoneY` 以下のブロックを破棄 + GameManager 通知

**SpawnRow の種別:**
| RowType | ブロック構成 |
|---|---|
| Normal | Explosive(10%) / Hard(20%) / Normal(70%)。確率は SerializeField |
| Sabotage | Hard or Absorb 50:50（`sabotageHardRatio`） |
| Spike | 全マス Spike（`spikeBlockHp` = 1） |

`IsTopClear()`: spawnY 付近にブロックが存在しない場合 true。妨害行はトップが空いてからスポーン。

**HardenRandomBlocks():**
```csharp
Block[] candidates = allBlocks
    .Where(b => b != null && b.blockType == BlockType.Normal)
    .OrderBy(_ => Random.value)
    .Take(hardenCount)
    .ToArray();
foreach (Block b in candidates)
    b.HardenToHp(hardenTargetHp);
```

**GetArena():**
```csharp
transform.parent?.GetComponentInChildren<ArenaController>()
// BlockSpawner → Arena ルート → ArenaController
```

---

### Block

`blockType` と `hp` は `public` フィールド。`BlockSpawner` が `Instantiate` 直後に直接代入して種別を設定する。色は `Start()` の `RefreshColor()` で適用される（Awake後・Start前に BlockSpawner が代入するため、Start時点で正しい blockType になっている）。

**衝突処理（OnCollisionEnter）:**
1. `"BallTag"` タグを持つオブジェクトのみ処理
2. Absorb なら `rb.linearVelocity *= absorbSpeedMultiplier`
3. Explosive 以外の衝突ヒットストップ（normalHitFrames / hardHitFrames / absorbHitFrames、デフォルト0）
4. Spike なら `GameManager.OnSpikeHit(ball.playerIndex)`
5. `ball.GetDamage()` でダメージ量取得 → `TakeDamage(damage, ball)`
6. `ball.OnHitBlock(this)` で属性効果発動

**破壊処理（OnDestroyed）:**
- スコア加算 + `RegisterBlockDestroyed`
- Explosive: 周囲ブロックに `AddHp(explosionHpBuff)` + ヒットストップ（`GetAttributeMultiplier()` でスケール）
- Spike: `SpawnZonePoison(transform.position)` → Destroy（`TryDropItem` スキップ）
- その他: `TryDropItem` → Destroy

**HardenToHp(int targetHp):**
```csharp
blockType = BlockType.Hard;
hp = currentHp = targetHp;
blockRenderer.material.color = hardenedColor;  // 金色で通常 Hard と区別
```

**GetArena():**
```csharp
transform.parent?.parent?.GetComponentInChildren<ArenaController>()
// Block → BlockSpawner → Arena ルート → ArenaController
```

**ブロック種別カラー（デフォルト値）:**
| 種別 | カラー | RGB概算 |
|---|---|---|
| Normal | 水色 | (0.6, 0.8, 1.0) |
| Hard | オレンジ | (1.0, 0.5, 0.1) |
| Absorb | 青紫 | (0.5, 0.4, 0.9) |
| Explosive | 赤 | (1.0, 0.2, 0.1) |
| Spike | 濃紫 | (0.5, 0.0, 0.5) |
| Hardened（妨害変換） | 金色 | (1.0, 0.8, 0.0) |

---

### HitStopController

`Time.timeScale` を使わず `IFreezable` 経由で個別制御する。  
`ArenaController.Awake()` で `RegisterFreezable(ball/spawner/player)` されることで管理対象が確定する。

**TriggerHitStop の内部処理:**
```
1. 前のコルーチンがあれば強制終了 + UnfreezeAll
2. HitStopRoutine コルーチン開始
   a. FreezeAll（全 IFreezable の Freeze() を呼ぶ）
   b. カメラ localPos をキャッシュ
   c. Time.unscaledDeltaTime でカウント（timeScale=0でも動く）
   d. 毎フレームカメラをランダムオフセット（intensity > 0 の場合）
   e. 時間経過後: カメラ位置を戻し UnfreezeAll
```

**シェイク強度:**
| shake | strong | intensity |
|---|---|---|
| false | - | 0（シェイクなし） |
| true | false | shakeIntensityNormal = 0.08 |
| true | true | shakeIntensityStrong = 0.20 |

---

### LaunchAimer

`ArenaController.Awake()` → `launchAimer.Initialize(ball, playerIndex, arena)` で初期化される。

**Update の処理:**
```
GameState == SkillSelect → StopAiming して return

ball.IsWaitingToLaunch == false（飛行中）:
  発射キー押下 → ForceRespawn()
  return

ball.IsWaitingToLaunch == true（発射待ち）:
  metronomeTime += dt
  currentAngleDeg = sin(metronomeTime × 2π / period) × range
  UpdateLine() → LineRenderer を更新
  発射キー押下 or aimingTime >= GetEffectiveAutoLaunchSec() → Fire()
```

**自動発射時間の短縮:**  
ブロックの最下端Y座標が底に近づくほど `autoLaunchSec` → `minAutoLaunchSec` に線形補間（`Lerp`）。プレイヤーが操作しなくてもピンチ時には素早く自動発射される。

**ForceRespawn（強制リスポーン）:**
```csharp
GameManager.Instance?.OnForceRespawn(playerIndex);  // HPペナルティ
ball.PrepareRespawn(arena.GetBallSpawnLocalPos());   // ボールをパドル上に戻す
```

---

### PlayerController

`rb.isKinematic = true`。物理計算はせず `transform.localPosition` を直接操作する。

**IFreezable 実装:**  
`frozen = true` で `Update()` の入力処理全体をスキップ。

**SetWidthTemporary（コルーチン）:**
```csharp
transform.localScale = new Vector3(originalScale.x * multiplier, originalScale.y, originalScale.z);
yield return new WaitForSeconds(duration);
transform.localScale = originalScale;
```

**ForceCatch（SkillForceCatch）の検出:**  
`OnCollisionEnter` でボールとの衝突時に `SkillController.IsForceCatchActive` を確認。有効なら `ball.PrepareRespawn(パドル上の位置)` でキャッチ扱いにしてリスポーン待機状態にする。

---

### DeadZone

Arena ルートの子として `localPos(0, -11, 0)` に固定配置。`isTrigger = true` のコライダー。

**OnTriggerEnter:**
- `"BallTag"` タグ以外は無視
- `isExtraBall == true` → ペナルティなしで Destroy
- メインボール → `GameManager.OnBallDropped(pi)` → `ball.PrepareRespawn(GetRespawnPos())`

`GetRespawnPos()`: `ArenaRoot.GetComponentInChildren<PlayerController>()` でパドルの現在 localY を取得し、`ballSpawnOffsetY` を加算。パドルが移動していても正しいリスポーン位置になる。

---

### ZonePoison

BlockSpike 破壊時または InterferencePoison で `ArenaController.SpawnZonePoison()` から生成される。  
`Setup(playerIndex, targetWorldY)` で落下目標Y（パドルWorldY + 0.5）を受け取る。

**ライフサイクル:**
```
生成 → fallSpeed で落下 → targetWorldY に到達したら着地
  → Destroy(gameObject, duration) で duration 秒後に自動消滅
  → 毎フレーム OverlapSphereNonAlloc でパドル接触判定
  → 接触中: GameManager.OnPoisonTick(playerIndex, Time.deltaTime)
```

ゼロアロケーション: `private readonly Collider[] _overlapBuffer = new Collider[4]` を使い回す。

---

### ZoneSlow

InterferenceSlow で `ArenaController.SpawnZoneSlow()` から生成される。  
`Setup(targetWorldY)` でアリーナ中央の着地 Y（`ArenaRoot.position.y`）を受け取る。

**ボール減速の仕組み（"リセット再適用"パターン）:**
```
毎フレーム:
  1. 前フレームの slowedBalls 全員に slowZoneMul = 1f を書き込む
  2. slowedBalls.Clear()
  3. OverlapSphereNonAlloc でボールを検出
  4. 検出したボールに slowZoneMul = slowFactor を書き込む
  5. slowedBalls に追加
```
ゾーン外に出たボールは次フレームのステップ1で自動的に 1.0 に戻る。別途「ゾーン離脱検知」ロジックは不要。

**OnDestroy:** `slowedBalls` 全員に `slowZoneMul = 1f` を書き込む（`ResetForNewRound` で即時 Destroy された場合の安全網）。

---

### EffectDefinition（純粋C#クラス群）

アイテムとスキルの効果を統一インターフェースで表現する。

```csharp
public abstract class EffectDefinition
{
    public abstract void Apply(int playerIndex, ArenaController arena);
}
```

| 実装クラス | フィールド | Apply の処理 |
|---|---|---|
| `EffectBallAttribute` | Attr, Duration | `ball.SetAttributeTemporary(Attr, Duration)` |
| `EffectPaddleScale` | Multiplier, Duration | `player.SetWidthTemporary(Multiplier, Duration)` |
| `EffectBallSpeed` | Multiplier, Duration | `ball.SetSpeedTemporary(Multiplier, Duration)` |
| `EffectHeal` | Amount | `GameManager.Heal(playerIndex, Amount)` |

`ItemDrop.BuildEffect()` がアイテム種別に応じた EffectDefinition を生成し `.Apply()` を呼ぶ。新しいアイテムを追加するには ItemType enum と BuildEffect の case を追加するだけでよい。

---

### ItemDrop

`ArenaController.SpawnItem()` から `AddComponent` で生成（Prefab なし）。

**接触判定に OnTriggerEnter を使わない理由:**  
パドルは `isKinematic = true`。Unity の物理エンジンでは kinematic と kinematic の衝突で `OnTriggerEnter` は発火しないため、毎フレーム `Physics.OverlapSphere` でポーリングする。

**ドロップ率の計算:**
```csharp
float dropChance = baseDropChance * GetCurrentBand(ball.playerIndex).itemDropMul;
if (Random.value > dropChance) return;
```
HP帯が低いほど `itemDropMul` が高くなるため、ピンチ時にアイテムが出やすい。

**アイテムプール:**
```
有利: Fire / Ice / Thunder / Heavy / Pierce / Enlarge / SpeedUp / Heal
不利: Shrink / Hyper
goodItemBias > 0 の時は有利プールのみから抽選
```

---

### SkillController

`ArenaController.Awake()` で自動的に `AddComponent` → `Initialize()` される。Prefab 不要。

```csharp
// Update の発動条件
GameState == Playing
  && equippedSkill != null
  && energy.IsFull
  && IsSkillKeyPressed()   // 1P: Q / 2P: U
  && equippedSkill.CanActivate(playerIndex)
→ energy.ConsumeAll() + equippedSkill.Activate(playerIndex, arena)
```

エナジー蓄積は `GameManager.RegisterBlockDestroyed()` → `GetCurrentBand().gaugeRateMul` → `SkillController.AddEnergy()` のルートで行われる。

---

### SkillDefinition（純粋C#クラス群）

```csharp
public abstract class SkillDefinition
{
    public abstract string DisplayName { get; }
    public virtual bool CanActivate(int playerIndex) => true;
    public abstract void Activate(int playerIndex, ArenaController arena);
}
```

| 実装クラス | CanActivate | Activate |
|---|---|---|
| `SkillPaddle_Enlarge` | 常時 | `player.SetWidthTemporary(1.5, 10)` |
| `SkillBall_Attribute_Fire` | 常時 | `ball.SetAttributeTemporary(Fire, 10)` |
| `SkillBall_Multi` | 常時 | `arena.SpawnExtraBall(10)` |
| `SkillForceCatch` | 常時 | `skillController.SetForceCatch(true)` |
| `SkillPanic_BlockClear` | HP ≤ 1/3 のみ | 上半分ブロックを全 Destroy + ヒットストップ |

---

### UIManager

`CenterUI` にアタッチ。`Update()` で毎フレーム `GameManager.Instance` をポーリングして表示更新（プッシュ型ではなくプル型）。

**ShowInterferenceOverlay（妨害通知）:**
```csharp
// C# 7.2 の ref 条件式でブランチを統合
ref Coroutine slot = ref (playerIndex == 1 ? ref p1OverlayRoutine : ref p2OverlayRoutine);
if (slot != null) StopCoroutine(slot);
slot = StartCoroutine(OverlayRoutine(cg, txt, label));
```
前のコルーチンが走っていれば停止してから再開（妨害が連続で飛んできた場合の重複防止）。  
`WaitForSecondsRealtime` を使うため `Time.timeScale=0` でも1.5秒後に消える。

---

## 7. 主要データフロー

### ブロック破壊 → 妨害送付

```
Ball.OnCollisionEnter(Block)
  └→ block.TakeDamage(ball.GetDamage(), ball)
        └→ currentHp <= 0 → block.OnDestroyed(ball)
              ├→ GameManager.AddScore(playerIndex, score)
              └→ GameManager.RegisterBlockDestroyed(playerIndex)
                    └→ p1DestroyedCount++
                          └→ >= comboThreshold
                                └→ SendSabotageTo(2)
                                      └→ SelectInterferenceType() → 重み付き抽選
                                      └→ ApplyInterference(arena2, type)
                                            ├→ AddRow: spawner.ReceiveSabotageRow()
                                            ├→ Harden: arena.HardenBlocks()
                                            ├→ Spike:  spawner.ReceiveSpikeRow()
                                            ├→ Poison: arena.SpawnZonePoison(randomPos)
                                            └→ Slow:   arena.SpawnZoneSlow(randomPos)
                                      └→ arena2.TriggerHitStop(10)
                                      └→ arena2.ShowInterferenceOverlay(label)
                                            └→ UIManager.ShowInterferenceOverlay(2, label)
```

### ボール落下 → ラウンド終了

```
Ball が DeadZone.isTrigger に接触
  └→ DeadZone.OnTriggerEnter
        ├→ GameManager.OnBallDropped(playerIndex)
        │     └→ ApplyDamage(playerIndex, damageBallDrop)
        │           └→ p1HP.TakeDamage(5)
        │                 └→ IsAlive == false → EndRound(winner=2)
        │                       ├→ p2RoundWins++
        │                       ├→ p2RoundWins >= roundsToWin → MatchOver処理
        │                       │     ├→ TriggerHitStop(60, strong, shake) 両アリーナ
        │                       │     └→ MatchOverCoroutine → Time.timeScale=0
        │                       └→ それ以外 → RoundOver処理
        │                             └→ TriggerHitStop(30) + NextRoundCoroutine
        └→ ball.PrepareRespawn(GetRespawnPos())
```

### アイテム取得

```
Block.OnDestroyed
  └→ TryDropItem(ball)
        └→ dropChance = baseDropChance × itemDropMul
        └→ Random.value <= dropChance
              └→ SelectRandomItemType(goodItemBias) → ItemType
              └→ ArenaController.SpawnItem(blockPos, type)
                    └→ GameObject.CreatePrimitive(Sphere)
                    └→ ItemDrop.Setup(type, playerIndex, this)

ItemDrop.Update (毎フレーム)
  └→ 落下（Vector3.down × dropSpeed × dt）
  └→ OverlapSphere でパドル検出
        └→ PlayerController に接触
              └→ BuildEffect().Apply(playerIndex, arena)
                    └→ EffectXxx の処理（属性付与/パドル拡大/速度変化/回復）
              └→ Destroy(gameObject)
```

---

## 8. ヒットストップシステム

### 設計の核心

`Time.timeScale` を**使わない**。2アリーナが独立して動作しており、片方だけを停止する必要があるため。代わりに `IFreezable` インターフェースを使い、各コンポーネントが自分自身を個別に停止する。

### IFreezable インターフェース

```csharp
public interface IFreezable
{
    void Freeze();
    void Unfreeze();
}
```

実装クラスと停止処理:
| クラス | Freeze() | Unfreeze() |
|---|---|---|
| BallScript | `frozenVelocity = rb.linearVelocity; rb.linearVelocity = Vector3.zero` | `rb.linearVelocity = frozenVelocity` |
| BlockSpawner | `frozen = true` | `frozen = false` |
| PlayerController | `frozen = true` | `frozen = false` |

### 速度閾値ゲート

ブロック衝突・壁バウンスのヒットストップは低速時に発動しない。  
`BallScript.GetHitStopMultiplier()`:
```
naturalSpeed / baseSpeed < hitStopSpeedThreshold (1.5) → 0 を返す（発動なし）
超えている場合 → (ratio - threshold) / (timeAccelMax - threshold) を 0-1 にクランプ
```
これにより、ゲーム序盤（低速）はヒットストップが発動せずテンポが良く、加速した終盤に演出が強くなる。

### ヒットストップイベント一覧

| イベント | フレーム数 | shake |
|---|---|---|
| 妨害受信 | interferenceTriggerFrames (10) | なし |
| BlockExplosive 破壊 | explosiveHitFrames (6) × attributeMultiplier | あり |
| ブロック底到達 | blockDeadZoneHitFrames (5) | あり |
| ラウンド決着（敗者） | roundEndFrames (30) | strong, shake |
| ラウンド決着（勝者） | roundEndFrames (30) | shake=false |
| マッチ決着（敗者） | matchEndFrames (60) | strong, shake |
| マッチ決着（勝者） | matchEndFrames (60) | shake=false |

---

## 9. ボール速度の3層管理

```
Layer 1: naturalSpeed
  = baseSpeed + timeAccelRate × arenaDwellTime
  上限: baseSpeed × timeAccelMax
  → メインボールのみ。リスポーンで arenaDwellTime リセット

Layer 2: speedMultiplier
  = 1.0 (デフォルト)
  → SetSpeedTemporary コルーチンで一時変更（SpeedUp/Hyper アイテム）
  → リスポーンで 1.0 にリセット

Layer 3: slowZoneMul
  = 1.0 (デフォルト)
  → ZoneSlow が毎フレーム上書き（ゾーン内: slowFactor / ゾーン外: 1.0）
  → リスポーンで 1.0 にリセット
  → ZoneSlow の OnDestroy でも 1.0 にリセット

実効速度 = naturalSpeed × speedMultiplier × slowZoneMul
```

Layer 2 と Layer 3 を分離した理由: 両方を同じフィールドで管理するとアイテム効果とゾーン効果が競合する。例えばアイテムで 1.4 倍になっている状態で ZoneSlow に入ると、その乗算がアイテム効果を上書きしてしまう。

---

## 10. アイテム・スキルの共通設計

### EffectDefinition パターン

```
アイテム種別ごとの効果 ──┐
                         │ BuildEffect() → EffectDefinition.Apply()
スキル種別ごとの効果 ───┘
```

アイテムと スキルは「何が（ItemType/SkillDefinition）」と「どう動かすか（EffectDefinition）」を分離している。新しいアイテム・スキルを追加する手順:

1. `ItemType` enum に値を追加（アイテムの場合）
2. `ItemDrop.BuildEffect()` に case を追加して EffectDefinition を返す
3. `Block.SelectRandomItemType()` のプールに追加

スキルの場合は `SkillDefinition` を継承したクラスを追加し、`SkillSelectUI` に登録するだけ。

---

## 11. 妨害システム

### 妨害の発動条件

```
Block破壊 → GameManager.RegisterBlockDestroyed(playerIndex)
  → p_DestroyedCount++
  → >= comboThreshold(15) → p_DestroyedCount = 0, SendSabotageTo(相手)
```

コンボカウントは `GameManager.GetCombo(pi)` で取得可能（UIManager が表示に使う）。

### 妨害種別の重み付き抽選

```csharp
int total = AddRow(2) + Harden(2) + Spike(1) + Poison(1) + Slow(1) = 7
r = Random.Range(0, 7)
r < 2 → AddRow
r < 4 → Harden
r < 5 → Spike
r < 6 → Poison
else  → Slow
```

重みは GameManager の SerializeField で変更可能。0 にすると無効化。

### 各妨害の実装

| 妨害 | 実装 | 効果 |
|---|---|---|
| AddRow | `spawner.ReceiveSabotageRow()` | pendingSabotageRows++ → 次の IsTopClear タイミングで Hard/Absorb 行スポーン |
| Harden | `spawner.HardenRandomBlocks()` | Normal ブロックを hardenCount 個 Hard 化（金色・HP3） |
| Spike | `spawner.ReceiveSpikeRow()` | pendingSpikeRows++ → Spike 行スポーン |
| Poison | `arena.SpawnZonePoison(randomPos)` | 紫球が落下 → パドル付近で停止 → 接触で毎秒ダメージ |
| Slow | `arena.SpawnZoneSlow(randomPos)` | シアン球が落下 → アリーナ中央で停止 → 内部ボールを slowFactor 倍に減速 |

---

## 12. 座標系と2アリーナ分離

### ローカル座標系

**すべての位置指定はアリーナの親 GameObject（Arena1/Arena2）のローカル座標で行う。**

- `Arena1.position = (0, 0, 0)` → Arena1の子の localPos(0,0,0) がワールドの原点
- `Arena2.position = (50, 0, 0)` → Arena2の子の localPos(0,0,0) がワールドの (50,0,0)
- BlockSpawner、PlayerController、BallScript は localPosition で動作する
- カメラも Arena の子なので `Camera1.worldPos = Arena1.worldPos + Camera1.localPos`

### ArenaController の位置問題

ArenaController は `Arena1` の**子**として配置されているが、兄弟オブジェクト（Ball、Player など）にアクセスするには**親（ArenaRoot）** から検索する必要がある。

```csharp
private Transform ArenaRoot => transform.parent != null ? transform.parent : transform;
// ArenaController の親 = Arena ルート

// 兄弟の PlayerController を取得
cachedPlayer = ArenaRoot.GetComponentInChildren<PlayerController>();
```

### GetArena() の実装がスクリプトごとに異なる理由

| スクリプト | 階層 | GetArena() のパス |
|---|---|---|
| Ball | Arena の子 | `transform.parent?.GetComponentInChildren<ArenaController>()` |
| PlayerController | Arena の子 | `(transform.parent ?? transform).GetComponentInChildren<ArenaController>()` |
| Block | BlockSpawner の子（Arena の孫） | `transform.parent?.parent?.GetComponentInChildren<ArenaController>()` |
| BlockSpawner | Arena の子 | `transform.parent?.GetComponentInChildren<ArenaController>()` |

---

## 13. パラメータ管理方針

**ScriptableObject は使用しない。** すべてのバランスパラメータは各コンポーネントの `[SerializeField]` に直接持ち、Unity Inspector から調整する。

### パラメータの所在

| パラメータ種別 | 管理コンポーネント |
|---|---|
| HP量・ダメージ量・コンボ閾値 | GameManager |
| ヒットストップフレーム数（試合制御） | GameManager |
| ヒットストップフレーム数（ブロック衝突） | Block |
| ヒットストップフレーム数（底到達） | BlockSpawner |
| ボール速度・時間加速・属性ダメージ | BallScript |
| ブロック降下速度・スポーン間隔・構成比率 | BlockSpawner |
| メトロノーム振れ幅・周期・自動発射時間 | LaunchAimer |
| パドル速度・移動範囲 | PlayerController |
| HP帯別パラメータ（倍率・フラグ） | GameManager.hpStateBands[] |
| 妨害種別の重み | GameManager |

---

## 14. 設計上の重要な判断とその理由

### Time.timeScale を使わない

2アリーナが独立しているため、片方だけを停止する必要がある。`timeScale=0` は全体に影響してしまう。`IFreezable` で各コンポーネントが自分自身を停止するアーキテクチャにより、アリーナ単位での制御が可能になった。

### UIManager はプル型（毎フレームポーリング）

GameManager が UI 更新のトリガーを保持するよりも、UIManager が毎フレーム状態を読み取るほうが単純で、ゲームロジックが UI の存在を知る必要がない。HP など数値が小数点以下で変化するものはイベント方式より毎フレームのほうが精度が高い。

### List\<Block\> で管理（GameObject でなく）

Block スクリプトへの参照を直接持つことで、`HardenRandomBlocks()` の LINQ や毎フレームの null チェックで `GetComponent<Block>()` を呼ぶ必要がなくなる。

### slowZoneMul を BallScript の public フィールドに

`SetSpeedTemporary` コルーチンと干渉しないよう、ゾーン効果は独立したフィールドで管理する。同じ `speedMultiplier` を使うとアイテム効果が ZoneSlow に上書きされる問題が生じる。

### ItemDrop は OnTriggerEnter を使わない

パドルは `isKinematic = true`。Unity では kinematic と kinematic の衝突は `OnTriggerEnter` が発火しないため、毎フレーム `Physics.OverlapSphere` でポーリングする。

### 追加ボール（ExtraBall）の独立性

`isExtraBall = true` のボールは:
- 時間加速なし（`naturalSpeed` が `baseSpeed` を超えない）
- 落下ペナルティなし（DeadZone で Destroy するだけ、GameManager.OnBallDropped を呼ばない）
- 発射: `isExtraBall = true` で `Start()` の通常発射をスキップ → ArenaController がコルーチンで発射

---

## 15. エディタ拡張

`Assets/Editor/` 配下のスクリプトは Unity メニュー `BurokkuKuzushi >` から実行する。すべて冪等（何度実行しても同じ結果）。

| メニュー | スクリプト | 実行タイミング |
|---|---|---|
| Setup HP UI | `SetupHPUI.cs` | UI要素の生成・UIManager へのバインド |
| Setup HitStop | `SetupHitStop.cs` | HitStopController の生成・カメラバインド |
| Setup LaunchAimer | `SetupLaunchAimer.cs` | LaunchAimer の生成・ArenaController バインド |
| Setup MatchResult UI | `SetupMatchResultUI.cs` | MatchResultPanel の生成・バインド |

新規にプロジェクトをクローンした場合や UI を再構築したい場合はこの順序で実行する（順序依存なし）。

---

## スクリプト関係一覧（クイックリファレンス）

```
GameManager ←── 全コンポーネントから通知を受ける（OnXxx系メソッド）
           ────→ ArenaController に指示（SendSabotageTo → ApplyInterference）

ArenaController
  ←── GameManager / Block / BallScript から TriggerHitStop 要求
  ────→ HitStopController (TriggerHitStop)
  ────→ BlockSpawner (ReceiveSabotageRow / ReceiveSpikeRow / HardenRandomBlocks)
  ────→ UIManager (ShowInterferenceOverlay)
  ────→ ZonePoison / ZoneSlow 生成
  ────→ ItemDrop 生成

Ball ←──→ Block (OnCollisionEnter: 相互)
Ball ────→ GameManager (暗黙的: Block.OnDestroyed 経由)
Ball ←──   ZoneSlow (slowZoneMul 書き込み)
Ball ←──   LaunchAimer (PrepareRespawn / LaunchInDirection)

Block ────→ GameManager.RegisterBlockDestroyed / AddScore / OnSpikeHit
Block ────→ ArenaController.TriggerHitStop / SpawnZonePoison / SpawnItem

ZonePoison ────→ GameManager.OnPoisonTick
ZoneSlow   ────→ BallScript.slowZoneMul

ItemDrop   ────→ EffectDefinition.Apply → ball / player / GameManager

UIManager  ←── GameManager (毎フレームポーリング)
```
