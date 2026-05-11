# BurokkuKuzushi — 開発ロードマップ

最終更新: 2026-05-11

このドキュメントは仕様書 [`DESIGN.md`](./DESIGN.md) を実装に落とすためのフェーズ分けと進捗管理。チケット的に使用する。

---

## 全体方針

- 拡張性を最初に仕込む（後から Gate/Zone/上部攻撃を足せる構造に）
- フェーズ完了ごとに動作確認・コミット・GitHub プッシュ
- 各フェーズは「動くものができる」まで完結させる（中途半端な状態を残さない）

---

## Phase A: 基盤刷新

ハードコードされた数値を ScriptableObject に追い出し、HP制とヒットストップ基盤を入れる。**最も大規模なリファクタリング**。

### Phase A-1: GameBalanceProfile + HP制移行

- [ ] `GameBalanceProfile` ScriptableObject 新設
  - [ ] `HPSettings`, `HPStateBand[]`, `ComboSettings`, `BallSettings`, `LaunchSettings`, `HitStopSettings` の構造定義
- [ ] `HPSystem` クラス新設（プレイヤーごとに1つ）
- [ ] 既存の残機制 (`GameManager.lives`) を廃止し、HP制に移行
- [ ] 既存ハードコードを Profile 参照に置換:
  - [ ] `BlockSpawner` の spawnInterval, descentSpeed, blocksPerRow, etc.
  - [ ] `BallScript` の speed, minAxisRatio, etc.
  - [ ] `GameManager` の comboThreshold
- [ ] UI を残機表示 → HPバー表示に変更
- [ ] HP帯ごとの動的パラメータ参照機構

**完了基準**: 残機制が完全に廃止され、HPバーが表示され、HP帯に応じてゲージ蓄積率などが変動する

### Phase A-2: ヒットストップ基盤

- [ ] `IFreezable` インターフェース定義
- [ ] `BallScript` / `Block` / `PlayerController` / `BlockSpawner` が `IFreezable` 実装
- [ ] `HitStopController` を `ArenaController` 配下に追加
- [ ] `Explosive` ブロック破壊時にヒットストップ呼び出し
- [ ] パドル受け止め時に軽いヒットストップ呼び出し
- [ ] ラウンド決着・マッチ決着時にボスストップ風長尺ヒットストップ

**完了基準**: Explosiveブロック爆発時に明確な手応えがあり、決着時に演出としてのストップが入る

### Phase A-3: 即リスタート機構

- [ ] マッチ終了状態を検出し、Spaceキーで `StartNewMatch()` を呼ぶ
- [ ] 演出を短くし、1秒以内に操作復帰
- [ ] `Time.timeScale = 0` のフリーズ解除も処理

**完了基準**: マッチ終了から Space 1 押しで次の試合が即始まる

---

## Phase B: メトロノーム発射

- [ ] `LaunchAimer` クラス新設
- [ ] リスポーン後にメトロノーム角度インジケーター UI 表示
- [ ] 専用キー押下時に確定角度で発射
- [ ] `LaunchSettings`（振れ幅・周期）を Profile から参照
- [ ] キャッチ機能の検討（初回のみで十分か、試合中もか）

**完了基準**: ボールリスポーン時にプレイヤーが角度を選択して発射できる

---

## Phase C: アイテム（パドルキャッチ式）

- [ ] `EffectDefinition` 抽象クラス定義
- [ ] `ItemDefinition`（EffectDefinitionの具象）定義
- [ ] `ItemDrop` MonoBehaviour（落下するアイテム本体）
- [ ] ブロック破壊時に `ItemDropTable` 参照してドロップ判定
- [ ] パドルキャッチで効果適用
- [ ] 初期アイテム実装:
  - [ ] `ItemAttribute_Fire` / `ItemAttribute_Ice` / `ItemAttribute_Thunder` / `ItemAttribute_Heavy`
  - [ ] `ItemPaddle_Enlarge` / `ItemPaddle_SpeedUp`
  - [ ] `ItemHeal`
  - [ ] `ItemPaddle_Shrink`（不利）
  - [ ] `ItemBall_Hyperspeed`（不利）

**完了基準**: アイテムが落下→キャッチ→効果適用→時間経過で解除のフローが動く

---

## Phase D: スキル（装備制・代償なし）

- [ ] `SkillDefinition`（EffectDefinitionの具象）定義
- [ ] エナジーゲージ実装
- [ ] HP帯に応じたゲージ蓄積率変動
- [ ] 試合前のスキル装備UI（1〜2個セット）
- [ ] キーで即発動
- [ ] 初期スキル実装:
  - [ ] `SkillPaddle_Enlarge`
  - [ ] `SkillBall_Multi`
  - [ ] `SkillBall_Attribute_Fire`
  - [ ] `SkillForceCatch`
  - [ ] `SkillPanic_BlockClear`（ピンチ専用）

**完了基準**: 試合前にスキルを選び、試合中にキーで発動できる

---

## Phase E: 妨害多様化（変化中心）

- [ ] `BlockDefinition` ScriptableObject化（既存enumから移行）
- [ ] `InterferencePayload` 機構実装
- [ ] 変化系妨害の実装:
  - [ ] `InterferenceHarden`（ブロック硬化）
  - [ ] `InterferenceSpike`（棘化）
  - [ ] `InterferencePoison`（毒エリア生成）
  - [ ] `InterferenceSlow`（スローエリア生成）
- [ ] 既存「妨害行追加」を `InterferenceAddRow` として残す（保険）
- [ ] 新ブロック種実装: `BlockSpike`, `BlockHardened`
- [ ] 新Zone実装: `ZonePoison`, `ZoneSlow`

**完了基準**: コンボ閾値到達で相手側ブロックが変化する複数の妨害方式が動作する

---

## Phase F: 演出強化

- [ ] 破壊ブロック飛翔演出（Screen Space Overlay）
  - [ ] 自分が破壊 → 画面端 → 相手側へ飛ぶアニメーション
  - [ ] 相手の妨害発生タイミングと同期
- [ ] ヒットストップの拡張（妨害発動時のフラッシュ・振動）
- [ ] カメラシェイク
- [ ] Trail Renderer（ボール軌跡、属性ごとに色変更）
- [ ] ブロック破壊の破片（Rigidbody欠片）
- [ ] 属性ボールの動的ライティング

**完了基準**: プレイ中に「3Dらしさ」と「やった/やられた感」がはっきり感じられる

---

## Phase G+: 拡張要素

優先度順に順次取り組む。Phase A〜F のスキームに乗せて追加していく形。

- [ ] Gate 系（`GateEffectDefinition` を EffectDefinition の具象として）
  - [ ] `GatePower`, `GateSpeed`, `GateMulti`
- [ ] Zone 系（自陣バフ）
  - [ ] `ZoneHeal`, `ZoneAutoClear`
- [ ] 上部攻撃（`InterferenceDirectAttack`）
  - [ ] Undertale風予告型ダメージエリア
- [ ] ブロック送付の強化
  - [ ] `InterferenceAddRow` のバリエーション（速度差・配置差など）
- [ ] スコア独自表示（「破壊力%」「翻弄度」など）
- [ ] BGM/SE 系
- [ ] タイトル画面・モード選択

---

## 進捗管理ルール

- 完了したチェックボックスは `[x]` に変更
- フェーズ完了時にコミット + GitHub プッシュ
- フェーズ完了時にこのファイルの「最終更新」日付を更新
- 仕様変更が必要になったら DESIGN.md を先に更新してから実装に着手

---

## 現在のステータス

- **完了**: Phase 1〜8（初期実装〜スプリットスクリーンUI）
- **次に着手**: Phase A-1（GameBalanceProfile + HP制移行）
