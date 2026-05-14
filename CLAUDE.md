# CLAUDE.md

このファイルは実装の現状を把握するための技術情報をまとめたもの。

| ドキュメント | 内容 |
|---|---|
| [`docs/DESIGN.md`](./docs/DESIGN.md) | ゲーム設計仕様書 |
| [`docs/ROADMAP.md`](./docs/ROADMAP.md) | 開発フェーズ計画・進捗 |
| 本ファイル | 実装の現状、シーン構成、座標系、既知の問題 |

**仕様変更が必要になった場合は、まず `docs/DESIGN.md` を更新してから実装に着手すること。**

---

## プロジェクト概要

ローカル2人対戦ブロック崩しゲーム。

- **Unity 6** + URP (Universal Render Pipeline) — Unity Hub で 6.x を使って開く
- TextMeshPro / Unity Input System / Unity 3D Physics
- バージョン管理: Git / GitHub

ゲームのルール・システム詳細は `docs/DESIGN.md` を参照。
開発フェーズと進捗は `docs/ROADMAP.md` を参照。

---

## Unity Editor セットアップ

新規にプロジェクトを開いた場合や UI を初期化したい場合：

1. `BurokkuKuzushi > Setup HP UI` を実行
   - CenterUI 配下の UI 要素を検出・生成し、UIManager に参照をバインド
2. `BurokkuKuzushi > Setup HitStop` を実行
   - Arena1 / Arena2 の子に `HitStopController` GameObject を生成し、カメラ参照をバインド
3. `BurokkuKuzushi > Setup LaunchAimer` を実行
   - Arena1 / Arena2 の子に `LaunchAimer` GameObject を生成し、ArenaController にバインド
4. `BurokkuKuzushi > Setup MatchResult UI` を実行
   - MatchResultPanel を生成・MatchResultUI にバインド

すべてのメニュー操作は冪等（何度実行しても安全）。

---

## シーン構成

アクティブシーン: `Assets/Scenes/SampleScene.unity`

```
SampleScene
├── EventSystem
├── GameManager        ← Singleton
├── CenterUI           ← Canvas (Screen Space Overlay)
│   ├── P1HPText / P1HPFill / P1Score / P1Combo / P1Wins
│   ├── P1EnergyFill / P1SkillText
│   ├── P2HPText / P2HPFill / P2Score / P2Combo / P2Wins
│   ├── P2EnergyFill / P2SkillText
│   ├── GameOverText
│   ├── MatchResultPanel
│   └── SkillSelectPanel
├── Arena1             ← ワールド座標 (0, 0, 0)
│   ├── Camera1        ← Arena1専用カメラ。localPos: (-0.3, 0, -25), FOV 45°
│   ├── TopWall / LeftWall / RightWall
│   ├── Ball / Player / DeadZone / BlockSpawner
│   └── ArenaController
│       ├── HitStopController
│       └── LaunchAimer
└── Arena2             ← ワールド座標 (50, 0, 0)
    ├── Camera2        ← Arena2専用カメラ。localPos: (0.2, 0, -25), FOV 45°, AudioListener なし
    └── （Arena1と同構成）
```

- カメラは各 Arena の子として配置されている（ワールド座標ではなくローカル座標で管理）
- CenterUI は Screen Space Overlay なので両カメラに重なる

### 現在の主要な Inspector 値

| コンポーネント | パラメータ | 値 |
|---|---|---|
| PlayerController | speed | 16 |
| PlayerController | xLimit | 4.7 |
| PlayerController | paddleLocalY | -8 |
| BlockSpawner | blocksPerRow | 6 |
| BlockSpawner | blockWidth | 1.5667 |
| BlockSpawner | spawnY / blockDeadZoneY | 4.5 / -4.5 |
| BlockSpawner | descentSpeed | 0.1 |
| ArenaController | ballSpawnOffsetY | 1.3 |
| DeadZone | ballSpawnOffsetY | 1.3 |
| Ball | localScale | (0.36, 0.36, 0.36) |
| DeadZone | localPos | (0, -11, 0) |

---

## アーキテクチャ・データフロー

### 中央イベントハブとしての GameManager

すべてのゲームイベントは `GameManager.Instance` 経由で通知される。各コンポーネントは直接 HP を操作せず、GameManager のメソッドを呼ぶ。

```
Block.OnCollisionEnter
  → ball.GetDamage() + ball.OnHitBlock(this)
  → GameManager.RegisterBlockDestroyed(playerIndex)   ← コンボカウント・妨害トリガー

DeadZone.OnTriggerEnter
  → GameManager.OnBallDropped(playerIndex)
  → HPSystem.TakeDamage()
  → HP=0 で EndRound() → Time.timeScale=0 or NextRoundCoroutine()

UIManager.Update()（毎フレーム）
  → GameManager.GetHP / GetScore / GetCombo / GetCurrentState をポーリング
```

### 設定方針（すべて Inspector SerializeField で直接管理）

ScriptableObject / Profile は使用しない。各コンポーネントのパラメータはそれぞれの SerializeField に持つ。

- `PlayerController.paddleLocalY / xLimit` → Inspector で直接設定
- `BlockSpawner.blockWidth / spawnY / blockDeadZoneY` → Inspector で直接設定
- `ArenaController.ballSpawnOffsetY` → パドルLocalY + このオフセットでボール初期位置を算出
- `DeadZone.ballSpawnOffsetY` → ArenaController と同じ値にすること（現在両方 1.3）
- `GameManager` → HP量、ダメージ量、ヒットストップフレーム等をすべて直接 SerializeField で保持

`ArenaController.arenaHalfWidth / arenaHalfHeight` は `SpawnItem()` のアイテム底面計算にのみ使用。

---

## スクリプト一覧

### `GameManager.cs`
- Singleton (`GameManager.Instance`)
- `HPSystem` をプレイヤーごとに保持し、`ApplyDamage()` が全ダメージの最終窓口
- `HPStateBand` クラスも同ファイルで定義。Inspector で hpStateBands[] 配列を設定する（空なら全倍率1.0で動作）
- HP帯に応じた動的パラメータ参照: `GetCurrentBand(playerIndex)` → `HPStateBand`
- `WaitForSecondsRealtime` 使用（`Time.timeScale=0` でも動作）
- `GetCombo(playerIndex)` は「次の妨害送付までのブロック破壊カウント」を返す（`p1DestroyedCount`）
- `OnForceRespawn(playerIndex)`: S/K 強制リスポーン時のHP減算窓口（`damageForceRespawn`）
- ラウンド/マッチ決着のカメラシェイクは勝者アリーナ `shake:false`、敗者アリーナ `shake:true` で区別

### `HPSystem.cs`
- 純粋C# クラス（MonoBehaviour ではない）
- API: `TakeDamage / Heal / Reset / SetMaxHP`
- プロパティ: `CurrentHP`, `MaxHP`, `Ratio`, `IsAlive`

### `EnergySystem.cs`
- 純粋C# クラス。`SkillController` が保持する

### `IFreezable.cs`（インターフェース）
- `Freeze()` / `Unfreeze()` の2メソッドのみ
- `BallScript` / `BlockSpawner` / `PlayerController` が実装。ヒットストップ中は各 Update/FixedUpdate を停止する

### `HitStopController.cs`
- `ArenaController` の子 GameObject にアタッチ（Setup HitStop で自動生成）
- `RegisterFreezable(IFreezable)` で管理対象を登録（ArenaController.Awake で呼ばれる）
- `TriggerHitStop(frames, strong)`: 対象を freeze → カメラシェイク → unfreeze の一連を `Time.unscaledDeltaTime` ベースのコルーチンで制御
- `strong=true` で強シェイク（ラウンド/マッチ決着時）

### `ArenaController.cs`
- `arenaHalfWidth / arenaHalfHeight` は `SpawnItem()` の底面 Y 計算にのみ使用（子コンポーネントへの配布なし）
- `ballSpawnOffsetY` → `GetBallSpawnLocalPos()` が実行時に PlayerController の localPosition.y を読んで動的に算出
- `arenaCamera` を Inspector でバインド（Setup HitStop で自動設定）→ `HitStopController` に渡す
- `TriggerHitStop(frames, strong, shake)` を公開 — Block / BallScript / GameManager はこれを呼ぶ
- `launchAimer` を Inspector でバインド（Setup LaunchAimer で自動設定）→ Awake で Initialize
- `GetBall()` / `GetSpawner()` / `GetSkillController()` で子コンポーネントを公開

### `LaunchAimer.cs`
- `ArenaController` の子 GameObject にアタッチ（Setup LaunchAimer で自動生成）
- `Initialize(ball, playerIndex, arena)` で対象ボール・プレイヤー番号・ArenaController を受け取る
- `ball.IsWaitingToLaunch` を監視し、true になるとメトロノーム発動
- sin 波で ±`metronomeAngleRange`° を `metronomePeriodSec` 周期で往復
- 1P: S キー / 2P: K キーで確定発射 → `ball.LaunchInDirection(localDir)` を呼ぶ
- ボール飛行中に発射キーを押すと強制リスポーン（`GameManager.OnForceRespawn` でHP減算 → `ball.PrepareRespawn`）
- 自動発射タイマーはブロック最下段位置に応じて短縮（`autoLaunchSec` → `minAutoLaunchSec` に線形補間）
- LineRenderer でリアルタイムに発射角インジケーターを描画（ワールド座標）

### `BlockSpawner.cs`
- タイマーで行を生成、毎フレーム降下、底判定
- 妨害行はキューに積んで、スポーン位置が空いてから生成（重なり防止）
- `blockDeadZoneY`（旧 `bottomY`）を超えたブロックを削除し `GameManager.OnBlocksReachedBottom(playerIndex, count)` を通知。同時に `TriggerHitStop` でカメラシェイク
- `GetLowestBlockY()` / `GetSpawnY()` / `GetBlockDeadZoneY()` を公開 — LaunchAimer が自動発射タイマー短縮に使用

### `BallScript.cs`
- `BallAttribute` enum: `Normal / Fire`（範囲ダメージ）`/ Thunder`（同種ブロック連鎖）`/ Ice`（高ダメ）`/ Heavy`（貫通）
- 速度の2層管理: `naturalSpeed`（基本速度 + 時間加速）× `speedMultiplier`（アイテム効果） = 実効速度
- `FixedUpdate` で毎フレーム実効速度に正規化。時間加速はメインボールのみ（`isExtraBall=false`）。`arenaDwellTime` はリスポーンでリセット
- `OnCollisionEnter` で衝突直後に角度補正（`ClampAngle`）→ 壁沿いループ防止
- `OnCollisionEnter` で壁バウンス検出（Block / PlayerController が GetComponent で見つからない衝突 = 壁）。`GetHitStopMultiplier()` が 0 より大なら `TriggerHitStop(wallBounceFrames * mul, shake:true)`
- `lastVelocity` は `FixedUpdate` でのみ更新（Heavy属性の貫通処理が衝突前速度を復元するために使用）
- `Launch()`: `transform.parent.TransformDirection()` でローカル→ワールド変換
- ボール GameObject に `"BallTag"` Unity タグが必須（`Block` / `DeadZone` どちらも `CompareTag("BallTag")` で判定）
- `PrepareRespawn(localPos)`: コライダー無効化 + `IsWaitingToLaunch=true`。コルーチン停止・速度状態リセットも行う
- `LaunchInDirection(localDir)`: コライダー再有効化 + 発射。LaunchAimer から呼ばれる
- `GetHitStopMultiplier()`: `naturalSpeed/baseSpeed` が `hitStopSpeedThreshold` 未満なら 0、以上なら 0→1 にスケール。ブロック衝突・壁バウンスのフレーム数に乗算する
- `GetAttributeMultiplier()`: 属性倍率のみ（>= 1.0）。Explosive 破壊など速度閾値によらず掛けたい場合に使用
- `SetAttributeTemporary(attr, duration)`: アイテム効果で属性を一時変更（コルーチン、重ね掛け上書き）
- `SetSpeedTemporary(multiplier, duration)`: アイテム効果でボール速度を一時変更（`speedMultiplier` コルーチン、重ね掛け上書き）
- 境界チェック: `FixedUpdate` でアリーナ外に出た場合、メインボールはペナルティなしリスポーン、追加ボールは Destroy

### `PlayerController.cs`
- `rb.isKinematic = true` + `transform.localPosition` 直接操作
- 1P: A/D（または矢印キー）、2P: J/L
- `SetWidthTemporary(multiplier, duration)`: アイテム効果でパドル幅を一時変更（`localScale.x` 変更、コルーチン）

### `DeadZone.cs`
- `ballSpawnOffsetY` と PlayerController.localPosition.y から動的にリスポーン位置を算出
- ArenaController.ballSpawnOffsetY と同じ値にすること

### `Block.cs`
- `BlockType` enum: `Normal`（1撃）/ `Hard`（複数撃）/ `Absorb`（当たると`absorbSpeedMultiplier`倍に減速）/ `Explosive`（破壊で周囲ブロックのHPを増加）
- `OnCollisionEnter` で `ball.GetDamage()` + `ball.OnHitBlock(this)` 呼び出し — ボールに `"BallTag"` Unity タグが必須
- Normal/Hard/Absorb 衝突時: `normalHitFrames / hardHitFrames / absorbHitFrames`（デフォルト 0）に `ball.GetHitStopMultiplier()` を乗算してヒットストップ
- Explosive 破壊時: `explosiveHitFrames`（デフォルト 6）に `ball.GetAttributeMultiplier()` を乗算してヒットストップ（速度閾値によらず発動）
- `blockType` / `hp` はパブリックフィールド。`BlockSpawner` が `Instantiate` 後に直接代入して種類・HP を設定する
- `GetArena()`: `transform.parent?.parent?.GetComponentInChildren<ArenaController>()` — Block → BlockSpawner → Arena root の順で辿る
- 破壊時に `TryDropItem()` を呼んで確率でアイテムをドロップ

### `EffectDefinition.cs`
- アイテム・スキル効果の抽象基底クラス（`Apply(playerIndex, arena)` メソッド）
- 実装クラス: `EffectBallAttribute` / `EffectPaddleScale` / `EffectBallSpeed` / `EffectHeal`

### `ItemDrop.cs`
- `ItemType` enum: `Fire / Ice / Thunder / Heavy / Enlarge / SpeedUp / Shrink / Hyper / Heal`
- `ItemDefinition` static クラス: `GetColor(type)` / `GetName(type)` を提供
- `ItemDrop` MonoBehaviour: `Setup()` で初期化、`Update()` で落下 + `Physics.OverlapSphere` によるパドル接触判定
- kinematic-kinematic 間の OnTriggerEnter は発火しないため、毎フレーム OverlapSphere でパドルを検出
- アイテムは AddComponent で生成（Prefab なし）。public フィールドの値がそのまま使われる
- `ArenaController.SpawnItem(worldPos, type)` から生成。底 Y を超えたら自動 Destroy

### `SkillController.cs`
- ArenaController.Awake() で自動生成・Initialize される
- エナジーゲージを管理。スキルキー（1P: Q / 2P: U）でスキル発動
- `maxEnergy` を SerializeField で保持

### `SkillDefinition.cs`
- スキル効果の抽象基底クラス（`SkillDefinition`）
- 実装: `SkillPaddle_Enlarge` / `SkillBall_Attribute_Fire` / `SkillBall_Multi` / `SkillForceCatch` / `SkillPanic_BlockClear`
- すべて public フィールドでパラメータを保持（Profile 参照なし）

### `MatchResultUI.cs`
- `CenterUI` にアタッチ。`GameState.MatchOver` を検出してパネルを表示
- A/D または J/L で「再戦」/「メニューへ戻る」を選択、スペースで確定
- 再戦: `GameManager.StartRematch()` — スキル選択画面に戻る

### `SkillSelectUI.cs`
- 試合開始前のスキル選択画面。GameState.SkillSelect 中に panel を表示
- 1P: A/D でサイクル・S で確定 / 2P: J/L でサイクル・K で確定

### `UIManager.cs`
- `CenterUI` にアタッチ、毎フレーム GameManager をポーリングして更新
- HP バー色: 緑（≥70%）→ 黄（≥30%）→ 赤（<30%）
- `RoundOver` のみ `statusText` を表示（MatchOver は MatchResultUI が担当）

### Editor スクリプト (`Assets/Editor/`)
- `SetupHPUI.cs`: `BurokkuKuzushi > Setup HP UI`（冪等）
- `SetupHitStop.cs`: `BurokkuKuzushi > Setup HitStop`（冪等）— Camera1/Camera2 を ArenaController にバインド
- `SetupMatchResultUI.cs`: `BurokkuKuzushi > Setup MatchResult UI`（冪等）
- `SetupLaunchAimer.cs`: `BurokkuKuzushi > Setup LaunchAimer`（冪等）
- `SetupSkillSelectUI.cs`: `BurokkuKuzushi > Setup Skill Select UI`（冪等）

---

## ローカル座標系の重要事項

**すべての位置指定はアリーナの親オブジェクトのローカル座標で行う。**

- Arena1 / Arena2 の子の `localPosition(0,0,0)` = そのアリーナの中心
- `BlockSpawner` が生成するブロックは BlockSpawner の子 → ローカル座標で管理
- `PlayerController` は `transform.localPosition` で移動
- カメラも Arena の子なので、ワールド座標は Arena のワールド座標 + localPos になる

---

## 既知の問題

- **Block スコアが SerializeField 未対応**: `Block.cs` の `normalScore` / `hardScore` は Inspector から変更可能だが、Prefab に依存しているため Instantiate 後は BlockSpawner から設定されない。ハードコードと同義。
- **Recovery ファイル**: `Assets/_Recovery/` 以下の Unity 自動生成ファイルは Git にコミットしない。
