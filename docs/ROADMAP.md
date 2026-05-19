# BurokkuKuzushi 開発ロードマップ

最終更新: 2026-05-19（UIManager 全バインド完了・アクティブアイテム表示実装）

このドキュメントは仕様書 [`DESIGN.md`](./DESIGN.md) を実装に落とすためのフェーズ分けと進捗管理。

---

## 進め方

- 拡張性を最初に組み込み、後から要素を追加できる構造を作る。
- フェーズが完了したら動作確認 → コミット → GitHub プッシュ。
- 各フェーズは「動くものができる」状態まで完結させ、中途半端な状態は残さない。

---

## Phase A: 基盤刷新

ハードコードされた数値を ScriptableObject に集約し、HP制とヒットストップの基盤を構築する。

### Phase A-1: HP制移行 + 設定方針確立

- [x] `HPSystem` クラス新設（プレイヤーごと）
- [x] 残機制 (`maxLives`) を廃止し、HP制 (`maxHP = 500`) に移行
- [x] UI を残機表示 → HPバー表示に変更
- [x] HP帯ごとの動的パラメータ参照機構（`HPStateBand[]` を GameManager SerializeField に集約）
- [x] ボール衝突すり抜け対策（Rigidbody CCD 有効化）
- [x] 動作確認（HPバー表示、ボール落下 HP減、色変化、ブロック破壊）
- [x] `GameBalanceProfile` ScriptableObject を削除し、全パラメータを各コンポーネント SerializeField に移行

### Phase A-2: ヒットストップ基盤

- [x] `IFreezable` インターフェース定義
- [x] `BallScript` / `PlayerController` / `BlockSpawner` に実装
- [x] `HitStopController` を `ArenaController` 配下に追加
- [x] カメラシェイク基盤（HitStop と同時発火、通常は片側カメラ・決着時は両方）
- [x] `BlockExplosive` ブロック爆発時にヒットストップ（`GetAttributeMultiplier()` でスケール）
- [x] パドル受け止め・壁バウンス時にヒットストップ（デフォルト 0、SerializeField で設定可）
- [x] ラウンド決着・マッチ決着時に長尺ヒットストップ（勝者 shake:false / 敗者 shake:true）
- [x] ブロック衝突・壁バウンスは速度閾値ゲート付き（`GetHitStopMultiplier()` が 0→1 にスケール）
- [x] ブロック底到達時にカメラシェイク（`blockDeadZoneHitFrames`）

### Phase A-3: マッチ結果画面

- [x] マッチ終了状態を検出し、結果画面を表示（勝者・最終スコア・ラウンド勝利数）
- [x] 再戦 / メニューへ戻る の 2 択 UI
- [x] 入力で選択（A/D または J/L + スペース）→ `StartRematch()` or シーンリロード
- [x] `Time.timeScale = 0` のフリーズ解除（Confirm() で timeScale=1 に戻す）

---

## Phase B: メトロノーム発射

- [x] `LaunchAimer` クラス新設
- [x] リスポーン後に角度インジケーター UI 表示（LineRenderer）
- [x] 発射キー押下時に確定角度で発射（1P: S、2P: K）
- [x] 振れ幅・周期などを SerializeField で設定
- [x] ボール飛行中に発射キー → 強制リスポーン（HP ペナルティ `damageForceRespawn`）
- [x] 自動発射タイマーをブロック最下段位置に応じて短縮（危機時に自動発射が速くなる）
- [x] スタック検出廃止 → アリーナ滞在時間加速に置換（リスポーンでリセット、上限 `timeAccelMax` 倍）

---

## Phase C: アイテム

- [x] `EffectDefinition` 抽象クラス定義
- [x] `ItemDefinition` 実装（`ItemDrop.cs` 内の static クラス）
- [x] `ItemDrop` MonoBehaviour（落下するアイテム）
- [x] ブロック破壊時にドロップ判定
- [x] パドルキャッチで効果適用
- [x] アイテム実装:
  - [x] 属性付与系（Fire / Ice / Thunder / Heavy）
  - [x] パドル強化系（Enlarge）
  - [x] ボール速度系（SpeedUp / Hyper）
  - [x] ボール強化系（Pierce）— BallAttribute.Pierce 貫通（Heavy と同機構、ヒットストップなし）
  - [x] 回復系（Heal）
  - [x] 不利系（Shrink / Hyper）

---

## Phase D: スキル

- [x] `SkillDefinition` 実装
- [x] エナジーゲージ実装（`EnergySystem` クラス）
- [x] HP帯に応じたゲージ蓄積率変動
- [x] 試合前のスキル装備 UI（`SkillSelectUI` + `GameState.SkillSelect`）
- [x] キー入力で発動（1P: Q、2P: U）
- [x] スキル実装:
  - [x] `SkillPaddle_Enlarge`
  - [x] `SkillBall_Multi`
  - [x] `SkillBall_Attribute_Fire`
  - [x] `SkillForceCatch`
  - [x] `SkillPanic_BlockClear`（HP 1/3 以下のみ発動可）

---

## Phase E: 妨害多様化

- [x] `InterferenceType` enum (AddRow / Harden / Spike / Poison / Slow) + 重み付きランダム dispatch
- [x] `InterferenceHarden` — BlockSpawner.HardenRandomBlocks() で既存 Normal を Hard 化（HP 3）
- [x] `InterferenceSpike` — Spike 行を送付（BlockSpawner.ReceiveSpikeRow()）
- [x] `InterferencePoison` — ZonePoison を直接生成（ArenaController.SpawnZonePoison()）
- [x] `InterferenceSlow` — ZoneSlow を直接生成（ArenaController.SpawnZoneSlow()）
- [x] `BlockType.Spike` — 接触で OnSpikeHit、破壊で ZonePoison 生成
- [x] `ZonePoison` — 落下して着地後パドル接触 HP ダメージ（duration 秒で消滅）
- [x] `ZoneSlow` — 落下してアリーナ中央付近に着地。内部ボールを slowFactor 倍に減速（duration 秒）
- [x] `BlockHardened` — 妨害 Harden で変換されたブロックは金色で通常 Hard と視覚的に区別
- [x] ブロック種別カラー全実装（Normal=水色 / Hard=橙 / Absorb=青紫 / Explosive=赤 / Spike=濃紫）
- [x] 妨害送付時の通知演出（スクリーンオーバーレイ 1.5 秒フラッシュ）— Setup HP UI で自動生成
- [ ] 妨害送付時のブロックオーラ演出（Phase F へ）

---

## Phase F-Setup: UI / カメラ / シェーダー基盤刷新（進行中）

Phase E 完了後、視覚品質と保守性を上げるため UI 構造全体と描画パイプラインを刷新。

### カメラ・レンダリング
- [x] 単 Ortho カメラ化（旧 Camera1/Camera2 分割描画 → 単 MainCamera）
- [x] HitStop シェイクをカメラ → アリーナ Transform に変更（単カメラ対応、独立シェイク維持）
- [x] HDR + Post Processing 有効化、Bloom 演出基盤

### シェーダー
- [x] `UI/HDRTint` シェーダー追加（UI Image 用、HDR Tint で Bloom 連動）
- [x] `Custom/HDRUnlit` シェーダー追加（Sprite / Mesh 用、HDR Base Color のみの Unlit）
- [x] `BreathPulse` コンポーネント追加（HDR Intensity Sin 波脈動）

### UI 構造
- [x] 旧 `CenterUI`（Screen Space Overlay フラット配置）を退避
- [x] 新 `_UI/_CameraSpace/_Components/_P1Components` 階層構造に再編
- [x] Canvas Scaler を全 Canvas で Scale With Screen Size / 1920x1080 / Match 0.5 に統一
- [x] 命名規則統一: `_` フォルダ / `$` 動的要素 / `P1`/`P2` プレフィックス
- [x] フォント追加: BebasNeue（数字）/ JetBrainsMono（ラベル）
- [x] UI 素材追加: BG / Bloom Frame / HP Indicator / Item Indicator / Mask 等
- [x] P2 ミラー構築（`_P2Components` 一括リネーム）

### スクリプト連携
- [x] `UIManager` を新 UI 構造に合わせて refactor（[必須]/[任意]/[演出] 3 区分）
- [x] `MatchResultUI` / `SkillSelectUI` を新パネルにバインド済み
- [x] `UIManager` 全 [必須] フィールドを `SetupUIManager.cs` で自動バインド（HP/Combo/Score/ActiveItem × P1・P2）
- [x] スコアのカンマ表示（`ToString("N0")`）
- [x] `P1HpMax` / `P1ComboMax` 等の静的ラベルを `GameManager` 実値で Start() 時に初期化
- [x] アクティブアイテム表示の動的データ連携（`GameManager.RegisterActiveItem` + `ItemDrop` 通知）
- [x] スキル READY 表示（`EnergyRatio >= 1` で suffix 付加）
- [x] `GameManager` に `RegisterActiveItem` / `GetActiveItemName` / `GetActiveItemRemaining` API 追加
- [x] `SetupUIManager.cs` Editor スクリプト追加（`BurokkuKuzushi > Setup UIManager Bindings` で冪等実行）
- [ ] HP Fill バー動作確認（Image Sliced + Horizontal fillAmount）
- [ ] Energy / Incoming インジケータ実装（UI 要素未作成）
- [ ] 旧 `CenterUI_Old` の最終削除

---

## Phase F: 演出強化

- [ ] 破壊ブロック飛翔演出（Screen Space Overlay アニメーション）
- [ ] ヒットストップ / カメラシェイクの拡張（妨害発動時のフラッシュ等）
- [ ] Trail Renderer（ボール軌跡、属性ごとに色変化）
- [ ] ブロック破壊の破片（Rigidbody 欠片）
- [ ] 属性ボールの動的ライティング

---

## Phase G+: 拡張要素

- [ ] Gate 系
  - [ ] `GatePower`
  - [ ] `GateSpeed`
  - [ ] `GateMulti`
- [ ] 自陣 Zone 系
  - [ ] `ZoneHeal`
  - [ ] `ZoneAutoClear`
- [ ] 上部攻撃（`InterferenceDirectAttack`）
- [ ] ブロック送付のバリエーション（速度差・配置差）
- [ ] 独自スコア表示（破壊力%、翻弄度など）
- [ ] BGM / SE
- [ ] タイトル画面 / モード選択

---

## 進捗管理ルール

- 完了したチェックボックスは `[x]` に変更
- フェーズ完了時にコミット + GitHub プッシュ
- フェーズ完了時にこのファイルの「最終更新」日付を更新
- 仕様変更が必要になったら `DESIGN.md` を先に更新してから実装に着手

---

## 現在のステータス

- **完了**: Phase A 全フェーズ（A-1 HP制・SerializeField統合、A-2 ヒットストップ、A-3 マッチ結果画面）
- **完了**: Phase B（メトロノーム発射 + 強制リスポーン + 時間加速）
- **完了**: Phase C（アイテム）
- **完了**: Phase D（スキル）
- **完了**: Phase E（妨害多様化フル実装 — ZoneSlow / BlockHardened 視覚区別 / Pierce アイテム含む）
- **進行中**: Phase F-Setup（UI / カメラ / シェーダー基盤刷新）
  - カメラ単 Ortho 化・HitStop シェイク方式変更・HDR シェーダー・UI 階層刷新は完了
  - UIManager 全バインド・アクティブアイテム表示・スキル READY・スコアカンマ整形が完了
  - **残作業**: Energy / Incoming インジケータ（UI 要素作成）、CenterUI_Old 削除
- **次**: Phase F-Setup 残作業を終えたら Phase F（演出強化）。発表（2026-06-05 頃）までに UI 動作を優先
