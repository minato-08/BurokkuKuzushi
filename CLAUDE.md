# BurokkuKuzushi — プロジェクト引継ぎドキュメント

## ⚠️ ドキュメント構成

- **`docs/DESIGN.md`** ← ゲーム設計仕様書（最新の確定仕様）
- **`docs/ROADMAP.md`** ← 開発フェーズ計画・進捗管理
- **本ファイル (`CLAUDE.md`)** ← 現状の実装状態・技術的な引継ぎ情報

仕様の詳細は `docs/DESIGN.md` を参照。**仕様変更があった場合は必ずそちらを更新してから実装に入る**こと。

---

## プロジェクト概要

大学IVRC（VRゲーム大会）向け、一年生の課題として作成中のローカル2人対戦ブロック崩しゲーム。

- Unity 6 + URP (Universal Render Pipeline)
- ブロックが上から降ってくるテトリス式
- ボールに属性あり（炎/雷/氷/重）
- HP制（HP500）でカムバック構造あり ※Phase A-1 で残機制から移行予定
- コンボで相手フィールドに妨害（変化中心）
- 左右スプリットスクリーン（カメラ2台のPerspective）

---

## 技術スタック

| 項目 | 内容 |
|---|---|
| エンジン | Unity 6 |
| レンダリング | URP |
| UI | TextMeshPro |
| 物理 | Unity 3D Physics (Rigidbody) |
| 入力 | Unity Input System |
| バージョン管理 | Git / GitHub |

---

## シーン構成

**アクティブシーン**: `Assets/SampleScene.unity`

```
SampleScene
├── Main Camera        ← Arena1専用カメラ (Viewport: 0,0,0.5,1)
├── Camera2            ← Arena2専用カメラ (Viewport: 0.5,0,0.5,1)
├── Directional Light
├── Global Volume
├── EventSystem
├── GameManager        ← Singleton、試合/ラウンド全体管理
├── CenterUI           ← Canvas (Screen Space Overlay)
│   ├── P1Score/P1Lives/P1Combo/P1Wins  ← 左上
│   ├── P2Score/P2Lives/P2Combo/P2Wins  ← 右上
│   └── GameOverText   ← 中央ステータス表示
├── Arena1             ← ワールド座標 (-17, 0, 0)
│   ├── TopWall / LeftWall / RightWall
│   ├── Plane          ← 床
│   ├── Ball
│   ├── Player         ← パドル
│   ├── DeadZone       ← ボール落下検知トリガー
│   ├── BlockSpawner
│   └── ArenaController
└── Arena2             ← ワールド座標 (+17, 0, 0)
    └── （Arena1と同構成）
```

---

## スクリプト一覧

### `GameManager.cs`
- **Singleton**パターン。`GameManager.Instance` でどこからでもアクセス可
- ラウンド/試合管理、残機、スコア、コンボカウンター
- `StartNewMatch()` → `EndRound()` → `StartNextRound()` or `MatchOver`
- `WaitForSecondsRealtime` を使用（`Time.timeScale=0` でも動作）
- **注意**: `roundsToWin=1` がデフォルト。複数ラウンドテストは `2` 以上に変更

### `ArenaController.cs`
- **アリーナサイズの一元管理**。ここを変えると全コンポーネントが追従
- `Awake()` で子コンポーネント（BlockSpawner/PlayerController/DeadZone）に値を配信
- `SerializeField`:
  - `arenaHalfWidth = 5f` ← 幅の半分
  - `arenaHalfHeight = 4.5f` ← 高さの半分
  - `paddleMargin = 0.8f` ← パドルを下端から何ユニット上に置くか
  - `leftWall / rightWall / topWall` ← 設定すると壁位置も自動調整（任意）

### `BlockSpawner.cs`
- タイマーで行を生成、毎フレーム降下、底判定
- `ConfigureFromArena()` で spawnY/bottomY/blockWidth を自動計算
- 妨害行はキューに積んで、スポーン位置が空いてから生成（重なり防止）
- `SerializeField`:
  - `blocksPerRow`, `blockGap`, `spawnInterval`, `descentSpeed`
  - `explosiveBlockChance`, `hardBlockChance`, `sabotageHardRatio`

### `BallScript.cs`
- `BallAttribute` enum: `Normal / Fire / Thunder / Ice / Heavy`
- `OnCollisionEnter` で衝突直後に角度補正 → 壁沿いのループ防止
- `minAxisRatio = 0.2f` → X/Y それぞれ最低20%の成分を保証
- `lastVelocity` は `FixedUpdate` でのみ更新（Heavy属性の貫通処理が使用）
- `Launch()` は `transform.parent.TransformDirection()` でローカル→ワールド変換

### `PlayerController.cs`
- `rb.isKinematic = true` + `transform.localPosition` 直接操作
- **ワールド座標に依存しない**設計（Arena がどこにあっても動作）
- 1P: A/D キー or 矢印キー、2P: J/L キー
- `ConfigureFromArena()` で xLimit/paddleLocalY を自動設定

### `Block.cs`
- `BlockType` enum: `Normal / Hard / Absorb / Explosive`
- `OnCollisionEnter` で `ball.GetDamage()` + `ball.OnHitBlock(this)` 呼び出し
- 破壊時に `GameManager.RegisterBlockDestroyed()` でコンボカウント

### `DeadZone.cs`
- ボール落下時にペナルティ通知 + リスポーン処理
- `ConfigureFromArena()` で位置とリスポーン座標を自動設定

### `UIManager.cs`
- `CenterUI` にアタッチ、毎フレーム GameManager から値を読んで更新
- `statusText`（GameOverText）: RoundOver/MatchOver 時のみ表示

---

## ローカル座標系の重要事項

**すべての位置指定はアリーナの親オブジェクトのローカル座標で行う。**

- Arena1/Arena2 の子オブジェクトの `localPosition(0,0,0)` = そのアリーナの中心
- `BlockSpawner` が生成するブロックはすべて BlockSpawner の子 → ローカル座標で管理
- `PlayerController` は `transform.localPosition` で移動
- 壁・DeadZone などのシーン配置もローカル座標で確認すること

---

## スプリットスクリーン設定

| カメラ | 位置 | Viewport |
|---|---|---|
| Main Camera | (-17, 0, -15) | (0, 0, 0.5, 1) 左半分 |
| Camera2 | (+17, 0, -15) | (0.5, 0, 0.5, 1) 右半分 |

- 両方 Perspective / FOV 45°（ユーザーが調整済み）
- CenterUI は Screen Space Overlay なので両カメラに重なる
- Camera2 には AudioListener なし（1つだけ必要なため）

---

## 残タスク

詳細・進捗は **`docs/ROADMAP.md`** を参照。

### 次に着手するフェーズ
**Phase A-1: GameBalanceProfile + HP制移行**
- `GameBalanceProfile` ScriptableObject 新設
- 残機制を廃止し、HP制（HP500）に移行
- 既存ハードコードを Profile 参照に置換
- HP帯ごとの動的パラメータ機構

### 概略フェーズ計画
- **Phase A**: 基盤刷新（GameBalanceProfile、HP制、ヒットストップ、即リスタート）
- **Phase B**: メトロノーム式発射
- **Phase C**: アイテム（パドルキャッチ）
- **Phase D**: スキル（装備制、代償なし）
- **Phase E**: 妨害多様化（変化中心）
- **Phase F**: 演出強化（破壊ブロック飛翔、Trail、カメラシェイク等）
- **Phase G+**: Gate/Zone、上部攻撃、独自スコア表示等

---

## 既知の問題・設計上の注意点

### ArenaController の壁参照が未設定
`leftWall / rightWall / topWall` は任意参照。未設定でも動作するが、アリーナサイズ変更時に壁位置は自動調整されない。Inspector でドラッグして設定すると完全自動になる。

### MatchOver 後に Time.timeScale = 0 のまま
現状は試合終了後にフリーズしたままになる。リスタート処理が未実装。

### ボール属性の選択 UI がない
`BallScript.attribute` は Inspector で手動設定。ゲーム中に変更する UI は未実装。

### Editor スクリプト（Assets/Editor/）
- `SetupUIManager.cs`: UIManager の参照を一括設定するワンショットスクリプト（使用済み）
- `SetupSplitScreen.cs`: スプリットスクリーンを設定するワンショットスクリプト（使用済み）

---

## 調整しやすいパラメータ一覧

### ゲームバランス（GameManager Inspector）
| パラメータ | デフォルト | 意味 |
|---|---|---|
| maxLives | 3 | 残機数 |
| roundsToWin | 1 | 何本先取で勝利 |
| nextRoundDelay | 2f | ラウンド間の待機秒数 |
| comboThreshold | 5 | 妨害行を送るのに必要な破壊数 |

### アリーナサイズ（ArenaController Inspector）
| パラメータ | デフォルト | 意味 |
|---|---|---|
| arenaHalfWidth | 5f | 幅の半分（変えると全体追従） |
| arenaHalfHeight | 4.5f | 高さの半分 |
| paddleMargin | 0.8f | パドルの高さ位置 |

### ブロック（BlockSpawner Inspector）
| パラメータ | デフォルト | 意味 |
|---|---|---|
| blocksPerRow | 7 | 1行のブロック数 |
| blockGap | 0.1f | ブロック間の隙間 |
| spawnInterval | 5f | 行生成間隔（秒） |
| descentSpeed | 0.3f | 降下速度（ユニット/秒） |
| explosiveBlockChance | 0.1f | 爆発ブロック出現率 |
| hardBlockChance | 0.2f | 硬ブロック出現率 |

### ボール（BallScript Inspector）
| パラメータ | デフォルト | 意味 |
|---|---|---|
| speed | 7f | ボール速度 |
| minAxisRatio | 0.2f | 軌道補正の強さ（大きいほど急角度） |
| relaunchAngleSpread | 0.5f | リスポーン時の発射角度のランダム幅 |
