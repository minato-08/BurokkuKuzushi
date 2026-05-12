# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

このファイルは新しい開発者やツールが現状の実装を把握するための技術情報をまとめたもの。
ゲーム仕様は別ファイルに切り出してある。

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

新規にプロジェクトを開いた場合や、Profile / UI を初期化したい場合：

1. `BurokkuKuzushi > Setup GameBalanceProfile` を実行
   - `Assets/Settings/GameBalanceProfile.asset` を生成し GameManager にバインド
2. `BurokkuKuzushi > Setup HP UI` を実行
   - CenterUI 配下の UI 要素を検出・生成し、UIManager に参照をバインド
   - 両メニュー操作とも冪等（何度実行しても安全）

---

## シーン構成

アクティブシーン: `Assets/Scenes/SampleScene.unity`

```
SampleScene
├── Main Camera        ← Arena1専用カメラ (Viewport: 0,0,0.5,1)
├── Camera2            ← Arena2専用カメラ (Viewport: 0.5,0,0.5,1)
├── EventSystem
├── GameManager        ← Singleton、GameBalanceProfile 参照を保持
├── CenterUI           ← Canvas (Screen Space Overlay)
│   ├── P1HPText / P1HPFill / P1Score / P1Combo / P1Wins
│   ├── P2HPText / P2HPFill / P2Score / P2Combo / P2Wins
│   └── GameOverText
├── Arena1             ← ワールド座標 (-17, 0, 0)
│   ├── TopWall / LeftWall / RightWall / Plane
│   ├── Ball / Player / DeadZone / BlockSpawner
│   └── ArenaController
└── Arena2             ← ワールド座標 (+17, 0, 0)
    └── （Arena1と同構成）
```

- Main Camera: (-17, 0, -15) / Camera2: (+17, 0, -15)、両方 Perspective FOV 45°
- CenterUI は Screen Space Overlay なので両カメラに重なる
- Camera2 には AudioListener なし

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

### ArenaController の設定配布

`ArenaController.Awake()` が唯一のアリーナサイズ源。子コンポーネントへ `ConfigureFromArena()` で値を配布する。

```
ArenaController.Awake()
  → spawner.ConfigureFromArena(halfWidth, halfHeight)
  → PlayerController.ConfigureFromArena(halfWidth, halfHeight, paddleMargin)
  → DeadZone.ConfigureFromArena(halfHeight, ballSpawnLocalPos)
```

### GameBalanceProfile の読み込みタイミング

各スクリプトは `Start()` で一度だけ Profile を読み込む。**試合中にアセットを編集しても反映されない**。次ラウンド / 試合開始時のみ反映される。

---

## スクリプト一覧

### `GameManager.cs`
- Singleton (`GameManager.Instance`)
- `HPSystem` をプレイヤーごとに保持し、`ApplyDamage()` が全ダメージの最終窓口
- HP帯に応じた動的パラメータ参照: `GetCurrentBand(playerIndex)` → `HPStateBand`
- `WaitForSecondsRealtime` 使用（`Time.timeScale=0` でも動作）

### `GameBalanceProfile.cs`（ScriptableObject）
- 全パラメータを集約するアセット（`Assets/Settings/GameBalanceProfile.asset`）
- サブ設定: `HPSettings`, `HPStateBand[]`, `ComboSettings`, `BallSettings`, `LaunchSettings`, `HitStopSettings`, `BlockSpawnSettings`
- `GetBandForRatio(ratio)`: thresholdPercent 降順配列を線形探索

### `HPSystem.cs`
- 純粋C# クラス（MonoBehaviour ではない）
- API: `TakeDamage / Heal / Reset / SetMaxHP`
- プロパティ: `CurrentHP`, `MaxHP`, `Ratio`, `IsAlive`

### `ArenaController.cs`
- アリーナサイズの唯一の管理者。`arenaHalfWidth / arenaHalfHeight` を変えると全コンポーネントが追従
- `leftWall / rightWall / topWall` を Inspector でバインドすると壁位置も自動調整される（任意）

### `BlockSpawner.cs`
- タイマーで行を生成、毎フレーム降下、底判定
- 妨害行はキューに積んで、スポーン位置が空いてから生成（重なり防止）
- 底到達: `GameManager.OnBlocksReachedBottom(playerIndex, count)` を通知

### `BallScript.cs`
- `BallAttribute` enum: `Normal / Fire / Thunder / Ice / Heavy`
- `OnCollisionEnter` で衝突直後に角度補正（`ClampAngle`）→ 壁沿いループ防止
- `lastVelocity` は `FixedUpdate` でのみ更新（Heavy属性の貫通処理が衝突前速度を復元するために使用）
- `Launch()`: `transform.parent.TransformDirection()` でローカル→ワールド変換

### `PlayerController.cs`
- `rb.isKinematic = true` + `transform.localPosition` 直接操作
- 1P: A/D（または矢印キー）、2P: J/L

### `Block.cs`
- `BlockType` enum: `Normal / Hard / Absorb / Explosive`
- `OnCollisionEnter` で `ball.GetDamage()` + `ball.OnHitBlock(this)` 呼び出し
- スコア値（`normalScore` / `hardScore`）は現時点でハードコード（Profile 未対応）

### `UIManager.cs`
- `CenterUI` にアタッチ、毎フレーム GameManager をポーリングして更新
- HP バー色: 緑（≥70%）→ 黄（≥30%）→ 赤（<30%）

### Editor スクリプト (`Assets/Editor/`)
- `SetupGameBalanceProfile.cs`: `BurokkuKuzushi > Setup GameBalanceProfile`
- `SetupHPUI.cs`: `BurokkuKuzushi > Setup HP UI`（冪等）
- `SetupUIManager.cs` / `SetupSplitScreen.cs`: 旧スクリプト、現在は不要

---

## ローカル座標系の重要事項

**すべての位置指定はアリーナの親オブジェクトのローカル座標で行う。**

- Arena1 / Arena2 の子の `localPosition(0,0,0)` = そのアリーナの中心
- `BlockSpawner` が生成するブロックは BlockSpawner の子 → ローカル座標で管理
- `PlayerController` は `transform.localPosition` で移動
- ワールド座標でデバッグしても両アリーナの値が重なって見えないので注意

---

## 既知の問題

- **MatchOver 後の結果画面未実装**: 試合終了後 `Time.timeScale=0` のまま。マッチ結果画面（再戦 / メニューへ戻る）の実装は Phase A-3 で予定。仕様詳細は `docs/DESIGN.md` §5.9 参照。
- **Block スコアが Profile 未対応**: `Block.cs` の `normalScore` / `hardScore` はハードコード。Phase B 以降で Profile 移行予定。
- **Recovery ファイル**: `Assets/_Recovery/` 以下の Unity 自動生成ファイルは Git にコミットしない。
