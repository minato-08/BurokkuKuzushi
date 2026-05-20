# BurokkuKuzushi 開発ロードマップ

最終更新: 2026-05-20（攻撃アイテム経由モデルへ刷新、発表 2026-06-05 までの逆算スケジュール）

このドキュメントは仕様書 [`DESIGN.md`](./DESIGN.md) を実装に落とすためのフェーズ分けと進捗管理。

---

## 進め方

- 拡張性を最初に組み込み、後から要素を追加できる構造を作る。
- フェーズが完了したら動作確認 → コミット → GitHub プッシュ。
- 各フェーズは「動くものができる」状態まで完結させ、中途半端な状態は残さない。
- **仕様変更が必要になった場合は、まず `DESIGN.md` を更新してから実装に着手する。**

---

## 全体カレンダー

```
2026-05-20  本日。仕様刷新（攻撃アイテム経由モデル）完了
2026-05-20 〜 23   Phase F-Combat: 攻撃アイテム実装・コンボ自動妨害撤去
2026-05-24 〜 26   Phase F-Setup 残作業: CenterUI_Old 削除・Energy/Incoming UI
2026-05-27 〜 29   Phase F-Audio: SE/BGM 最低セット
2026-05-30 〜 06-01 Phase F-Title: Title/Settings/Tutorial 最小実装
2026-06-02 〜 03   Phase F-Polish: 演出強化（破壊飛翔・Trail・破片）
2026-06-04        Playtest + バランス調整 + 最終バグ取り
2026-06-05        発表（部活）
2026-06-06 〜      Phase G+: Gate/Zone/DirectAttack/AI 対戦
```

各フェーズの想定工数は 2〜3 日。スリップしたら Phase F-Polish と Phase F-Title から削る（Audio と Combat はマスト）。

---

## Phase A: 基盤刷新（完了）

ハードコードされた数値を SerializeField に集約し、HP制とヒットストップの基盤を構築。

### Phase A-1: HP制移行 + 設定方針確立

- [x] `HPSystem` クラス新設（プレイヤーごと）
- [x] 残機制 (`maxLives`) を廃止し、HP制 (`maxHP = 500`) に移行
- [x] UI を残機表示 → HPバー表示に変更
- [x] HP帯ごとの動的パラメータ参照機構（`HPStateBand[]` を GameManager SerializeField に集約）
- [x] ボール衝突すり抜け対策（Rigidbody CCD 有効化）
- [x] `GameBalanceProfile` ScriptableObject を削除し、全パラメータを各コンポーネント SerializeField に移行

### Phase A-2: ヒットストップ基盤

- [x] `IFreezable` インターフェース定義
- [x] `BallScript` / `PlayerController` / `BlockSpawner` に実装
- [x] `HitStopController` を `ArenaController` 配下に追加
- [x] カメラシェイク基盤（現在は ArenaRoot シェイク。単 Ortho 化で 2026-05 移行）
- [x] `BlockExplosive` ブロック爆発時にヒットストップ
- [x] パドル受け止め・壁バウンス時にヒットストップ（デフォルト 0）
- [x] ラウンド決着・マッチ決着時に長尺ヒットストップ
- [x] ブロック衝突・壁バウンスは速度閾値ゲート付き
- [x] ブロック底到達時にカメラシェイク

### Phase A-3: マッチ結果画面

- [x] マッチ終了状態を検出し、結果画面を表示
- [x] 再戦 / メニューへ戻る の 2 択 UI
- [x] 入力で選択（A/D または J/L + スペース）

---

## Phase B: メトロノーム発射（完了）

- [x] `LaunchAimer` クラス新設
- [x] リスポーン後に角度インジケーター UI 表示
- [x] 発射キー押下時に確定角度で発射（1P: S、2P: K）
- [x] 振れ幅・周期などを SerializeField で設定
- [x] ボール飛行中に発射キー → 強制リスポーン
- [x] 自動発射タイマーをブロック最下段位置に応じて短縮
- [x] スタック検出廃止 → アリーナ滞在時間加速に置換

---

## Phase C: アイテム（完了）

- [x] `EffectDefinition` 抽象クラス定義
- [x] `ItemDefinition` 実装
- [x] `ItemDrop` MonoBehaviour（落下するアイテム）
- [x] ブロック破壊時にドロップ判定
- [x] パドルキャッチで効果適用
- [x] 強化アイテム（Fire / Ice / Thunder / Heavy / Enlarge / SpeedUp / Pierce / Heal）
- [x] 罠アイテム（Shrink / Hyper）

---

## Phase D: スキル（完了）

- [x] `SkillDefinition` 実装
- [x] エナジーゲージ実装（`EnergySystem` クラス）
- [x] HP帯に応じたゲージ蓄積率変動
- [x] 試合前のスキル装備 UI
- [x] キー入力で発動（1P: Q、2P: U）
- [x] 防御/強化スキル一式

---

## Phase E: 妨害多様化（完了）

- [x] `InterferenceType` enum（AddRow / Harden / Spike / Poison / Slow）
- [x] 各妨害効果の実装（HardenRandomBlocks / ReceiveSpikeRow / SpawnZonePoison / SpawnZoneSlow）
- [x] `BlockType.Spike` — 接触で OnSpikeHit、破壊で ZonePoison 生成
- [x] `ZonePoison` / `ZoneSlow`
- [x] `BlockHardened`（金色で通常 Hard と視覚的に区別）
- [x] ブロック種別カラー全実装
- [x] 妨害送付時のスクリーンオーバーレイ
- [ ] 妨害送付時のブロックオーラ演出 → Phase F-Polish へ

---

## Phase F-Setup: UI / カメラ / シェーダー基盤刷新（ほぼ完了）

### カメラ・レンダリング
- [x] 単 Ortho カメラ化
- [x] HitStop シェイクをカメラ → アリーナ Transform に変更
- [x] HDR + Post Processing 有効化、Bloom 演出基盤

### シェーダー
- [x] `UI/HDRTint` シェーダー追加
- [x] `Custom/HDRUnlit` シェーダー追加
- [x] `BreathPulse` コンポーネント追加

### UI 構造
- [x] 旧 `CenterUI`（Screen Space Overlay）を退避
- [x] 新 `_UI/_CameraSpace/_Components/_P1Components` 階層構造に再編
- [x] Canvas Scaler 統一
- [x] 命名規則統一: `_` フォルダ / `$` 動的要素 / `P1`/`P2` プレフィックス
- [x] フォント追加: BebasNeue / JetBrainsMono
- [x] UI 素材追加
- [x] P2 ミラー構築

### スクリプト連携
- [x] `UIManager` を新 UI 構造に合わせて refactor
- [x] `MatchResultUI` / `SkillSelectUI` を新パネルにバインド済み
- [x] `UIManager` 全 [必須] フィールドを自動バインド
- [x] スコアのカンマ表示
- [x] アクティブアイテム表示の動的データ連携
- [x] スキル READY 表示
- [x] `SetupUIManager.cs` Editor スクリプト

### 残作業（Phase F-Setup クローズアウト・〜2026-05-26）
- [ ] HP Fill バー動作確認（Image Sliced + Horizontal fillAmount）
- [ ] Energy ゲージ UI 要素作成（Image, Vertical Fill）+ バインド
- [ ] Incoming インジケータ UI 要素作成 + バインド
- [ ] Score / Combo 表示の最終確認（既存実装の playtest）
- [ ] 旧 `CenterUI_Old` の削除コミット
- [ ] Round ドット / 勝利数表示（先取本数 > 1 で意味を持つ）

---

## Phase F-Combat: 攻撃アイテム経由モデル実装（〜2026-05-23）

DESIGN.md 5.5.2 / 5.7 に従い、コンボ自動妨害を撤廃して攻撃アイテム経路に置換する。

### コア変更
- [ ] `ItemType` enum に `AttackHarden / AttackSpike / AttackAddRow / AttackPoison / AttackSlow` を追加
- [ ] `EffectAttack` 系 EffectDefinition を新設（または `ItemDrop` で系統分岐）
- [ ] `Block.SelectRandomItemType()` を 3 系統（buff/attack/trap）抽選に書き換え
- [ ] HP帯バンドに `dropBiasBuff` フィールドを追加し、抽選で反映
- [ ] `GameManager.SendInterference(targetPi, payload)` 公開メソッドを新設
- [ ] `GameManager.SendSabotageTo` / `RegisterBlockDestroyed` 内の自動送付ロジックを削除
- [ ] `RegisterBlockDestroyed` をコンボ更新のみに簡略化（spawn 通知 + scoreMul/gaugeMul 算出）

### コンボの再配置
- [ ] `comboTimer[]` + `comboTimeout`(=3s) 実装。`TickCombo` を Update に
- [ ] `maxCombo[]` をラウンドごとに記録
- [ ] `scoreComboMul / gaugeComboMul / itemDropComboMul` 算出関数（`comboScoreStep` 等基準）
- [ ] `AddScore` / `AddEnergy` / `TryDropItem` で各倍率を反映
- [ ] UI の `$P1ComboValue` 表示を「現在コンボ」に統一（旧「次の妨害までの残り」を撤去）

### 攻撃アイテム強化（DESIGN.md 5.5.2 / 5.7 の刷新分を含む）

- [ ] 攻撃アイテム用カラー / アイコン（赤系オーラ）
- [ ] 取得時の SE 仮当て（Phase F-Audio で正式音）
- [ ] 攻撃側 HUD への `SENT → P{N}: [種別]` ラベル表示（1.5s, スライドフェードアウト）
- [ ] AttackHarden で対象ブロックを 3s 降下停止させる実装（`RigidbodyConstraints` or Transform 固定）
- [ ] アイテム寿命タイマー（8s、残 2s で高速点滅）を ItemDrop に追加
- [ ] `GameManager.StartRetaliationWindow(playerIndex)` を実装（妨害受信後 5s、次の攻撃効果 2x）
- [ ] RetaliationWindow の攻撃種別ごとの 2x 効果を `SendInterference` ルーティングに反映（DESIGN.md 5.7 参照）
- [ ] `UIManager` に `RETALIATION READY` インジケーター表示を追加
- [ ] `SkillForceCatch` に `ForceCatchBonusDrop` フラグを追加（再発射後の最初のブロック命中で攻撃アイテム確定ドロップ）

### コンボマイルストーン

- [ ] `comboMilestones[]` 配列（デフォルト {10, 20, 30}）を GameManager SerializeField に追加
- [ ] マイルストーン到達時のオーバーレイ表示（達成者 HUD + 相手 HUD の警告）
- [ ] `se_combo_milestone.wav` の仮当て（ピッチ差分付き）

### コミット粒度
1. ItemType 拡張 + 抽選ロジック refactor
2. SendInterference の経路統一 + RegisterBlockDestroyed 簡略化
3. コンボの自己強化系統 (scoreMul/gaugeMul) + マイルストーン
4. アイテム寿命 + RetaliationWindow
5. AttackHarden 降下停止 + UI 反映 + 攻撃アイテムビジュアル

---

## Phase F-Audio: SE/BGM 最低セット（〜2026-05-29）

DESIGN.md 10. の音響設計を最低限実装。発表で「音が無くて寂しい」と思われないライン。

- [ ] `AudioMixer` 作成（Master / BGM / SE / Voice）
- [ ] ボール反射 SE（速度層でピッチ可変）
- [ ] ブロック衝突 SE（Normal/Hard/Absorb/Explosive/Spike で音色差）
- [ ] ブロック破壊 SE
- [ ] アイテム取得 SE（系統別: 強化 / 攻撃 / 罠 で 3 音）
- [ ] スキル発動 SE + チャージ完了 SE
- [ ] 妨害受信 SE（種別ごとに短発ラベル発音）
- [ ] ラウンド開始 / 勝利 / マッチ勝利 ジングル
- [ ] BGM: タイトル 1 曲 + 試合中 1 曲（クロスフェード）
- [ ] PlayerPrefs ベースの音量設定（vol.master/bgm/se）
- [ ] 設定 UI と連動

音源は自作 or フリー素材（CC0/Creative Commons）から調達。生成優先順位はブロック衝突から。

---

## Phase F-Title: タイトル/設定/チュートリアル（〜2026-06-01）

発表で 1 人プレイの取っ掛かりが必要なため最小実装する。

- [ ] `TitleScene` 新設（または `SampleScene` 内パネルで疑似実装）
- [ ] メニュー UI（START / TUTORIAL / SETTINGS / QUIT）
- [ ] `SettingsPanel` UI（音量 × 3、先取本数、HitStop 強度、シェイク強度）
- [ ] チュートリアルフロー（段階解説、HP 減算オフ）
- [ ] `ResultScene` または既存 `MatchResultPanel` の演出強化
- [ ] BGM はタイトル/試合で切り替え

スリップした場合の優先順:
1. タイトル + START（最低限）
2. 設定（音量）
3. チュートリアル

---

## Phase F-Polish: 演出強化（〜2026-06-03）

時間が許す範囲で見映えを上げる。優先度順。

### 必須演出（デモで「地味」と思われないライン）

- [ ] ラウンド開始カウントダウン（3-2-1-GO!）シーケンス実装（PlayerController Freeze + LaunchAimer 起動タイミング）
- [ ] ラウンド決着演出（勝者アリーナ白フラッシュ / 敗者アリーナ暗転 + `ROUND WIN!` / `ROUND OVER` 表示）
- [ ] Last Stand 演出（HP 10%: アリーナ枠BreathPulse高速化 + 赤化 + HP バー点滅 + `PANIC READY` 表示）
- [ ] BlockHard / BlockHardened HP pip 表示（ブロック上部に ● ドット、命中で減少）
- [ ] AttackAddRow 妨害行の着弾アニメーション（上端から滑り込み 0.3s + 2f HitStop + SE）
- [ ] アイテムの寿命点滅（残 2s でアイテムが高速点滅）

### 追加演出

- [ ] 破壊ブロック飛翔演出（Screen Space Overlay アニメーション）
- [ ] Trail Renderer（ボール軌跡、属性ごとに色変化）
- [ ] ブロック破壊の破片（Rigidbody 欠片）
- [ ] 妨害送付時のブロックオーラ演出（Phase E から繰越）
- [ ] 攻撃アイテム発射時のトレイル（パドルから上空へ）
- [ ] スキル発動時の画面演出（縁取り光 + 短時間スロー）
- [ ] ヒットストップ / カメラシェイクの拡張
- [ ] スペシャル行の出現演出と SE（`se_special_row.wav`）
- [ ] パドル反射ゾーン実装（`PlayerController.OnBallHit` 角度補正）
- [ ] LaunchAimer センター通過音（真上 90° 通過時の「ティック」SE）
- [ ] アイテムアイコンに形状識別（○ / ★ / △）を追加（アクセシビリティ対応）

---

## Phase F-Playtest: バランス調整（2026-06-04）

最終日にまとめてプレイテスト → 数値調整。

- [ ] BALANCE.md Section 9 のデモパラメータ（maxHP=250, baseDropChance=0.5）で開始
- [ ] フルマッチを 5 試合プレイ（自分 + 他者 2 人）
- [ ] 1 ラウンドあたりの平均試合時間を計測（目標 60〜90s）
- [ ] 攻撃アイテムドロップ率の調整（RetaliationWindow 発動頻度が適切か）
- [ ] HP 帯バンドのカムバック感の確認（劣勢から逆転できるか）
- [ ] コンボマイルストーン（10/20/30）が実際の試合で到達できるか
- [ ] AttackHarden の視覚インパクト確認（3s 停止が「あっ」と感じさせるか）
- [ ] コンボ持続が短すぎないか（comboTimeout 値の妥当性）
- [ ] 一行底到達ペナルティの調整（線形 / 累進どちらか確定）
- [ ] 致命バグ・クラッシュ対応

---

## Phase G+: 拡張要素（2026-06-06 以降）

発表後の継続開発で実装。

- [ ] Gate 系（GatePower / GateSpeed / GateMulti）
- [ ] 自陣 Zone 系（ZoneHeal / ZoneAutoClear）
- [ ] 攻撃スキル群（SkillAttack_Harden / SpikeRow / Cannon / Surge）
- [ ] `InterferenceDirectAttack`（5s 予告型 40 ダメージ）
- [ ] `TrapView_Disturb`（ポストプロセス歪み）
- [ ] AI 対戦（1P モード、難易度 3 段階）
- [ ] キーバインド変更機能
- [ ] リプレイ機能（任意）
- [ ] 統計画面（破壊力% / 翻弄度など独自スコア）

---

## 進捗管理ルール

- 完了したチェックボックスは `[x]` に変更
- フェーズ完了時にコミット + GitHub プッシュ
- フェーズ完了時にこのファイルの「最終更新」日付を更新
- 仕様変更が必要になったら `DESIGN.md` を先に更新してから実装に着手

---

## 現在のステータス

- **完了**: Phase A〜E（ゲームのコアシステム）、Phase F-Setup の大半
- **直近**: 2026-05-20 仕様刷新（コンボ自動妨害撤廃、攻撃アイテム経由モデル確定）
- **次に着手**: Phase F-Combat（攻撃アイテム実装、コンボ自動送付撤去）
- **発表**: 2026-06-05（部活）— 残り 16 日。Combat / Audio / Title はマスト、Polish はバッファ

仕様変更があった日（2026-05-20）以降、ROADMAP は `docs/spec-refinement-2026-05-20` ブランチで管理されている。実装着手時に main へマージするか、別ブランチを切る判断は実装者が行う。
