# BurokkuKuzushi 実装引継ぎ情報

このファイルは新しい開発者やツールが現状の実装を把握するための技術情報をまとめたもの。
ゲーム仕様は別ファイルに切り出してある。

| ドキュメント | 内容 |
|---|---|
| [`docs/DESIGN.md`](./docs/DESIGN.md) | ゲーム設計仕様書 |
| [`docs/ROADMAP.md`](./docs/ROADMAP.md) | 開発フェーズ計画・進捗 |
| 本ファイル | 実装の現状、シーン構成、座標系、既知の問題 |

仕様変更が必要になった場合は、まず `docs/DESIGN.md` を更新してから実装に着手すること。

---

## プロジェクト概要

ローカル2人対戦ブロック崩しゲーム。

- Unity 6 + URP (Universal Render Pipeline)
- TextMeshPro / Unity Input System / Unity 3D Physics
- バージョン管理: Git / GitHub

ゲームのルール・システム詳細は `docs/DESIGN.md` を参照。

---

## シーン構成

アクティブシーン: `Assets/Scenes/SampleScene.unity`

```
SampleScene
├── Main Camera        ← Arena1専用カメラ (Viewport: 0,0,0.5,1)
├── Camera2            ← Arena2専用カメラ (Viewport: 0.5,0,0.5,1)
├── Directional Light
├── Global Volume
├── EventSystem
├── GameManager        ← Singleton、profile 参照を保持
├── CenterUI           ← Canvas (Screen Space Overlay)
│   ├── P1HPText / P1HPFill / P1Score / P1Combo / P1Wins
│   ├── P2HPText / P2HPFill / P2Score / P2Combo / P2Wins
│   └── GameOverText   ← 中央ステータス表示
├── Arena1             ← ワールド座標 (-17, 0, 0)
│   ├── TopWall / LeftWall / RightWall
│   ├── Plane          ← 床
│   ├── Ball
│   ├── Player         ← パドル
│   ├── DeadZone
│   ├── BlockSpawner
│   └── ArenaController
└── Arena2             ← ワールド座標 (+17, 0, 0)
    └── （Arena1と同構成）
```

| カメラ | 位置 | Viewport |
|---|---|---|
| Main Camera | (-17, 0, -15) | (0, 0, 0.5, 1) 左半分 |
| Camera2 | (+17, 0, -15) | (0.5, 0, 0.5, 1) 右半分 |

- 両方 Perspective / FOV 45°
- CenterUI は Screen Space Overlay なので両カメラに重なる
- Camera2 には AudioListener なし

---

## スクリプト一覧

### `GameManager.cs`
- Singleton (`GameManager.Instance`)
- `GameBalanceProfile profile` を Inspector でバインド
- `HPSystem` を プレイヤーごとに 1 つ保持
- ラウンド/試合管理、スコア、コンボカウンター
- HP帯に応じた動的パラメータ参照 (`GetCurrentBand`)
- `WaitForSecondsRealtime` を使用（`Time.timeScale=0` でも動作）

### `GameBalanceProfile.cs`（ScriptableObject）
- 全パラメータを集約するアセット
- 配置: `Assets/Settings/GameBalanceProfile.asset`
- 中身: `HPSettings`, `HPStateBand[]`, `ComboSettings`, `BallSettings`, `LaunchSettings`, `HitStopSettings`, `BlockSpawnSettings`

### `HPSystem.cs`
- 純粋C# クラス（MonoBehaviour ではない）
- `TakeDamage / Heal / Reset / SetMaxHP` メソッド
- `CurrentHP`, `MaxHP`, `Ratio`, `IsAlive` プロパティ

### `ArenaController.cs`
- アリーナサイズの一元管理。ここを変えると全コンポーネントが追従
- `Awake()` で子コンポーネント（BlockSpawner / PlayerController / DeadZone）に値を配信
- 主な SerializeField: `arenaHalfWidth`, `arenaHalfHeight`, `paddleMargin`, `leftWall`, `rightWall`, `topWall`

### `BlockSpawner.cs`
- タイマーで行を生成、毎フレーム降下、底判定
- `GameBalanceProfile.blockSpawn` から設定を読み込む
- `ConfigureFromArena` 内と `Start` の両方で Profile 適用 → blockWidth 再計算
- 妨害行はキューに積んで、スポーン位置が空いてから生成（重なり防止）
- 底到達: 1 個ごとに `GameManager.OnBlocksReachedBottom(playerIndex, count)` を通知

### `BallScript.cs`
- `BallAttribute` enum: `Normal / Fire / Thunder / Ice / Heavy`
- `Start()` で `Rigidbody.collisionDetectionMode = ContinuousDynamic` を設定（すり抜け対策）
- `GameBalanceProfile.ball` から speed/ダメージ/属性範囲を読込
- `OnCollisionEnter` で衝突直後に角度補正 → 壁沿いのループ防止
- `lastVelocity` は `FixedUpdate` でのみ更新（Heavy属性の貫通処理が使用）
- `Launch()` は `transform.parent.TransformDirection()` でローカル→ワールド変換

### `PlayerController.cs`
- `rb.isKinematic = true` + `transform.localPosition` 直接操作
- ワールド座標に依存しない設計（Arena がどこにあっても動作）
- 1P: A/D キー or 矢印キー、2P: J/L キー
- `ConfigureFromArena()` で xLimit/paddleLocalY を自動設定

### `Block.cs`
- `BlockType` enum: `Normal / Hard / Absorb / Explosive`
- `OnCollisionEnter` で `ball.GetDamage()` + `ball.OnHitBlock(this)` 呼び出し
- 破壊時に `GameManager.RegisterBlockDestroyed()` でコンボカウント

### `DeadZone.cs`
- ボール落下時に `GameManager.OnBallDropped` 通知 + リスポーン処理
- `ConfigureFromArena()` で位置とリスポーン座標を自動設定

### `UIManager.cs`
- `CenterUI` にアタッチ、毎フレーム GameManager から値を読んで更新
- HP バー (Image, FillType=Filled) を HP割合に応じてカラー変化（緑/黄/赤）
- `statusText`: RoundOver / MatchOver 時のみ表示

### Editor スクリプト (`Assets/Editor/`)
- `SetupGameBalanceProfile.cs`: メニュー `BurokkuKuzushi > Setup GameBalanceProfile` で Profile アセット生成 + GameManager にバインド
- `SetupHPUI.cs`: メニュー `BurokkuKuzushi > Setup HP UI` で既存 P1Lives/P2Lives を HP表示UIに転用
- `SetupUIManager.cs`: 旧UI参照設定用（残機制時代のスクリプト、現在は不要）
- `SetupSplitScreen.cs`: スプリットスクリーン初期設定（使用済み）

---

## ローカル座標系の重要事項

**すべての位置指定はアリーナの親オブジェクトのローカル座標で行う。**

- Arena1 / Arena2 の子オブジェクトの `localPosition(0,0,0)` = そのアリーナの中心
- `BlockSpawner` が生成するブロックは BlockSpawner の子 → ローカル座標で管理
- `PlayerController` は `transform.localPosition` で移動
- 壁・DeadZone などのシーン配置もローカル座標で確認すること

---

## Unity Editor で行う手動セットアップ

新規にプロジェクトを開いた場合や、Profile / UI を初期化したい場合：

1. `BurokkuKuzushi > Setup GameBalanceProfile` を実行
   - `Assets/Settings/GameBalanceProfile.asset` を生成
   - GameManager の `profile` フィールドにバインド
2. `BurokkuKuzushi > Setup HP UI` を実行
   - 既存の P1Lives / P2Lives テキストを HP 表示に転用
   - HP バー Image を自動生成
   - UIManager の参照を再バインド

---

## 既知の問題

### 試合中の Profile 変更は反映されない
`GameBalanceProfile` の値は各スクリプトの `Start` で一度だけ読み込まれる。試合中にアセットを編集しても反映されない。次のラウンド / 試合開始時のみ反映される。

### Block.cs の normalScore / hardScore は Profile 未対応
ブロックのスコア値はまだ Block.cs 内のハードコード。次フェーズ以降で Profile に移行予定。

### ArenaController の壁参照は任意
`leftWall / rightWall / topWall` は Inspector で参照を設定すると、アリーナサイズ変更時に壁位置も自動調整される。未設定でも動作するが、サイズ変更時は手動で壁を動かす必要がある。

### MatchOver 後のリスタートが未実装
試合終了後に `Time.timeScale = 0` でフリーズしたままになる。Space キーでの即リスタートは Phase A-3 で実装予定。

### Recovery ファイル
`Assets/_Recovery/` 以下に Unity が自動生成した復旧ファイルがある場合、これは Git にコミットしない。
