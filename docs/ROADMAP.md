# BurokkuKuzushi 開発ロードマップ

最終更新: 2026-06-01（Phase F-Combat / F-Title 完了をコードと突き合わせて反映、発表 2026-06-12 へ変更）

このドキュメントは仕様書 [`DESIGN.md`](./DESIGN.md) を実装に落とすためのフェーズ分けと進捗管理。

> **2026-06-01 実コード照合メモ**: Phase F-Combat は **完了し main にマージ済み**（commit c005760）。Phase F-Title（タイトル/設定/カウントダウン/ラウンド・マッチ結果）も実装済み。本ファイルのチェックボックスは 2026-05-20 以降更新されておらず未着手のように見えていたため、コードと突き合わせて `[x]` を反映した。**残作業の実体は Phase F-Audio（音は一行も未実装）/ Phase F-Polish（演出群）/ MatchStats 集計（最大コンボ等）**。発表は 2026-06-12 に変更。

---

## 進め方

- 拡張性を最初に組み込み、後から要素を追加できる構造を作る。
- フェーズが完了したら動作確認 → コミット → GitHub プッシュ。
- 各フェーズは「動くものができる」状態まで完結させ、中途半端な状態は残さない。
- **仕様変更が必要になった場合は、まず `DESIGN.md` を更新してから実装に着手する。**

---

## 全体カレンダー

```
（〜2026-05-31  完了）Phase F-Combat / F-Setup / F-Title 実装・UI を Unity 上で構築
2026-06-01        本日。実コード照合で ROADMAP を実態に更新
2026-06-01 〜 04   Phase F-Audio: SE/BGM 最低セット（最優先・発表 Go 基準）
2026-06-05 〜 07   HUD [任意] バインド + MatchStats 集計（最大コンボ等）
2026-06-08 〜 10   Phase F-Polish: 演出強化（決着フラッシュ・Ball Heat・Danger Proximity 等）
2026-06-11        Playtest + バランス調整 + 最終バグ取り
2026-06-12        発表（部活）
2026-06-13 〜      Phase G+: Gate/Zone/DirectAttack/AI 対戦
```

各フェーズはバッファ込み。スリップしたら Phase F-Polish から削る（Audio はマスト）。

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

### 残作業（Phase F-Setup クローズアウト）
- [x] HP Fill バー動作確認（Image Sliced + Horizontal fillAmount）
- [ ] Energy ゲージ UI 要素作成（Image, Vertical Fill）+ バインド ← UIManager 側コードは実装済み・UI 要素未作成
- [ ] Incoming インジケータ UI 要素作成 + バインド ← `UIManager.PushIncoming` 実装済み・UI 要素未作成
- [x] Score / Combo 表示の最終確認
- [x] 旧 `CenterUI_Old` の削除コミット（2026-05-31、GameObject ごと削除）
- [ ] Round ドット / 勝利数表示 ← `UIManager.p1RoundWins/p2RoundWins` フィールドあり・未バインド

---

## Phase F-Combat: 攻撃アイテム経由モデル実装（〜2026-05-23）

DESIGN.md 5.5.2 / 5.7 に従い、コンボ自動妨害を撤廃して攻撃アイテム経路に置換する。

### コア変更
- [x] `ItemType` enum に `AttackHarden / AttackAddRow / AttackPoison / AttackSlow` を追加
- [x] `EffectAttack` 系 EffectDefinition を新設（`EffectDefinition.cs` に `EffectAttack` / `EffectInputReverse` を実装）
- [x] `Block.SelectRandomItemType()` を 3 系統（buff/attack/trap）抽選に書き換え
- [x] HP帯バンドに `dropBiasBuff` フィールドを追加し、抽選で反映（`HPStateBand.goodItemBias`）
- [x] `GameManager.SendInterference(targetPi, payload)` 公開メソッドを新設
- [x] `GameManager.SendSabotageTo` / `RegisterBlockDestroyed` 内の自動送付ロジックを削除
- [x] `RegisterBlockDestroyed` をエナジー蓄積のみに簡略化（コンボ加算は接触側 `RegisterBallHitBlock` へ移譲）
- [x] `ItemType` に `TrapBall_Reversed`（`Reversed`）を追加（`PlayerController.inputReversed` フラグ・`EffectInputReverse` 経由）

### コンボの再配置
- [x] `comboTimer[]` + `comboTimeout`(=6.0s) 実装。`TickComboTimer` を Update に（DESIGN.md 5.8 で 6s に確定）
- [x] `maxCombo[]` をラウンド/マッチごとに記録（2026-06-01。`GetMaxComboRound/Match` + 総破壊数 `GetBlocksDestroyed` + 被妨害数 `GetInterferenceReceived`。UI 側コードも配線済み・要バインド）
- [x] `scoreComboMul / gaugeComboMul / itemDropComboMul` 算出関数（`comboScoreStep` 等基準）
- [x] `AddScore` / `AddEnergy` / `TryDropItem` で各倍率を反映
- [x] UI の `$P1ComboValue` 表示を「現在コンボ」に統一（旧「次の妨害までの残り」を撤去）
- [x] コンボ加算を「破壊数」→「ブロック接触数」に再定義（`RegisterBallHitBlock`、DESIGN.md 5.8 / 2026-05-30）

### 攻撃アイテム強化（DESIGN.md 5.5.2 / 5.7 の刷新分を含む）

- [x] 攻撃アイテム用カラー（`ItemDefinition.GetColor` で赤系）
- [ ] 取得時の SE 仮当て（Phase F-Audio で正式音）← **音は全体未実装**
- [x] 攻撃側 HUD への `SENT → P{N}: [種別]` ラベル表示（`UIManager.ShowSentLabel`、※ UI 要素は未バインド）
- [ ] ~~アイテム寿命タイマー（8s）~~ ← **不採用確定（2026-05-29）**。実装しない方針
- [ ] 妨害送受信のオーブ演出（相手→自分へ飛ぶエフェクト）← **未実装**（`ShowInterferenceOverlay` の赤フラッシュのみ実装、オーブ飛翔は無し）

> 2026-05-28 仕様変更で以下は廃止: AttackSpike / AttackHarden 降下停止 / 反撃ウィンドウ (RetaliationWindow) / CATCH & SHOOT (`SkillForceCatch`)。

### コンボマイルストーン

- [x] `comboMilestones[]` 配列（デフォルト {10, 20, 30}）を GameManager SerializeField に追加
- [x] マイルストーン到達時のオーバーレイ表示ロジック（`UIManager.ShowComboMilestone`、※ UI 要素は未バインド）
- [ ] `se_combo_milestone.wav` の仮当て（ピッチ差分付き）← **音は全体未実装**

### ラウンド内エスカレーション（DESIGN.md 5.4.1）

- [x] `BlockSpawner` に `spawnIntervalBase / spawnIntervalDecayPerMin / spawnIntervalMin / descentSpeedBase / descentSpeedGainPerMin / descentSpeedMax` を SerializeField 追加
- [x] `roundElapsedTime` を `BlockSpawner.Update()` で毎フレーム加算し、スポーン間隔・降下速度をリアルタイム算出（`ResetForNewRound()` でリセット）
- [x] `comboTimer[]` タイマー起点を「最後のブロック接触後」に（DESIGN.md 5.8 注記。`RegisterBallHitBlock` でリセット）

### コミット粒度
1. ItemType 拡張 + 抽選ロジック refactor
2. SendInterference の経路統一 + RegisterBlockDestroyed 簡略化
3. コンボの自己強化系統 (scoreMul/gaugeMul) + マイルストーン
4. アイテム寿命 + 攻撃アイテムビジュアル
5. 妨害送受信のオーブ演出 + Incoming オーバーレイ
6. Dynamic Escalation（BlockSpawner 時間スケール）

---

## Phase F-Audio: SE/BGM 最低セット（〜2026-05-29）

DESIGN.md 10. の音響設計を最低限実装。発表で「音が無くて寂しい」と思われないライン。

> **2026-06-01 土台実装済み**: `AudioManager.cs`（シングルトン、SE プール、dB 音量、50ms クールダウン、BGM クロスフェード）を新設し、`AudioManager` GameObject をシーンに配置。下記の発火点は**全てコード配線済み**。**残るは (1) 音源クリップを Inspector に割り当て (2) `Assets/Audio/MasterMixer.mixer` を作成して Expose Param をバインド** の 2 点（どちらもユーザー依存。未割り当てでも null セーフに無音動作する）。

- [~] `AudioMixer`（Master/BGM/SE/Voice）+ dB 変換 `20×log10(v/100)` ← **dB 変換・`ApplyVolumes` 実装済み。Mixer asset 作成と Expose Param バインドが残**
- [x] ボール反射 SE 配線（`BallScript`/`PlayerController`、壁はピッチ可変 `1+(ratio-1)×0.2`）
- [x] ブロック衝突 SE 配線（Normal/Hard/Absorb 音色差、Hard -2 半音、アリーナごと 50ms クールダウン）
- [x] ブロック破壊 SE 配線（Explosive 専用音, `Block.OnDestroyed`）
- [x] アイテム取得 SE 配線（系統別 3 音, `PlayerController.OnItemPickup`）+ アイテム出現 SE（`ArenaController.SpawnItem`）
- [x] スキル発動 SE + チャージ完了 SE 配線（`SkillController` で `IsFull` 立ち上がり検出）
- [x] 妨害受信 SE 配線（`GameManager.ApplyInterference`）
- [x] コンボマイルストーン SE 配線（ピッチ +N 半音, `UIManager.ShowComboMilestone`）
- [x] ラウンド開始 / 勝利 / マッチ勝利 SE 配線（`CountdownCoroutine`/`EndRound`）
- [x] BGM: タイトル / 試合 クロスフェード scaffold（`PlayTitleBGM`/`PlayMatchBGM`/`PlayResultJingle`）
- [x] HP30% 帯クロスフェード（5% ヒステリシス, `GameManager.Update` → `SetTenseLayer`）
- [x] PlayerPrefs 音量（vol.master/bgm/se）→ `ApplyVolumes` で dB 反映
- [ ] 設定 UI と連動（現状 Settings は先取数のみ。音量スライダー追加は要判断）
- [ ] **音源クリップ未割り当て**（ユーザー調達, ASSETS.md）/ **Mixer asset 未作成**
- [ ] 未配線: `se_addrow_land`（妨害行着弾）/ UI 確定音の一部画面 ← 必要なら追補

音源は自作 or フリー素材（CC0/Creative Commons）から調達。生成優先順位はブロック衝突から。SE コードトリガーマッピングは DESIGN.md 10.4 を参照。

---

## Phase F-Title: タイトル/設定/チュートリアル（〜2026-06-01）

発表で 1 人プレイの取っ掛かりが必要なため最小実装する。

- [x] `SampleScene` 内パネルで疑似実装（`_TitlePanel` / `_SettingsPanel`、`TitleUI` / `SettingsUI`）
- [x] メニュー UI（START / SETTINGS / QUIT、テキスト色で選択表現）
- [x] `GameState` enum に状態追加（`Title` / `Settings` / `Countdown` を実装。`RoundIntermission` は `RoundOver` + `RoundIntermissionRemaining` で代替）
- [ ] ラウンド開始カウントダウン中の入力制御（DESIGN.md 12.12: 移動可・発射不可・降下停止）← **未充足**: 現状 Countdown は `Time.timeScale=0` で全停止のため「移動可」になっていない
- [~] 既存パネルで結果画面（DESIGN.md 5.10）
  - [x] 必須: 大見出し `P{N} WINS!` / 最終スコア（`MatchResultUI` サマリー版・実機確認済み）
  - [x] 必須: **最大コンボ表示**（集計実装済み 2026-06-01。`MatchResultUI.p1/p2BestComboText` に配線済み・**UI 要素の配置とバインド待ち**）
  - [x] 任意: ブロック破壊数・受信妨害数（集計実装済み。`p1/p2BlocksText` / `p1/p2InterferenceText` に配線済み・**要バインド**）
- [x] ラウンド間リザルト分離（`RoundResultUI` + カウントダウン）と起動時カウントダウン（3,2,1,GO!）
- [ ] BGM はタイトル/試合で切り替え ← **音は全体未実装**

> **2026-05-28 廃止 → 一部復活**: ポーズ機能 / チュートリアル / AI 対戦 は廃止のまま。**設定 UI は「先取数のみ」で最小復活（2026-05-30）** — `SettingsUI` が `PlayerPrefs "match.roundsToWin"` を扱い、`GameState.Settings` を新設。音量設定は音声未実装のためスコープ外。

実装済みのフロー: Title(START) → Settings(先取数) → SkillSelect(4枚カード) → Countdown(3,2,1,GO!) → 対戦 → RoundResult → … → MatchResult(Rematch/Menu) → ReturnToTitle。

---

## Phase F-Polish: 演出強化（〜2026-06-03）

時間が許す範囲で見映えを上げる。優先度順。

### 必須演出（デモで「地味」と思われないライン）

- [x] ラウンド開始カウントダウン（3-2-1-GO!）シーケンス実装（GameManager `CountdownCoroutine`、GO! の瞬間に Playing 開始）
- [x] ラウンド決着演出（勝者アリーナ白フラッシュ / 敗者アリーナ暗転 + `ROUND WIN!` / `ROUND OVER` 表示）← フラッシュ/暗転は `UIManager.RoundResultRoutine` で実装済み。大型見出しは `GameManager.P1/P2DecisionLabel`（2.5s 自動消去）＋ `RoundDecisionUI` で実装。**TMP 要素（p1/p2DecisionText）の配置・バインドが Unity 側に残**
- [ ] Last Stand 演出（HP 10%: アリーナ枠BreathPulse高速化 + 赤化 + HP バー点滅 + `PANIC READY` 表示）
- [ ] BlockHard / BlockHardened HP pip 表示（ブロック上部に ● ドット、命中で減少）
- [ ] AttackAddRow 妨害行の着弾アニメーション（上端から滑り込み 0.3s + 2f HitStop + SE）
- [ ] アイテムの寿命点滅（残 2s でアイテムが高速点滅）
- [ ] Victory Bar（画面上部中央：P1/P2 HP 比の横長バー、観客向け一目確認）
- [ ] Combo タイマーアーク（コンボ数字下の弧形残時間インジケーター、弧消滅 = コンボリセット）
- [ ] Ball Heat（コンボ段階でボール色変化: 0-9 白 / 10-19 黄 / 20-29 橙 / 30+ 赤）
- [x] アイテム取得時パドルフラッシュ（系統ごとに 0.1s 色フラッシュ: buff=青 / attack=赤 / trap=紫。`PlayerController.OnItemPickup`）
- [ ] Danger Proximity 演出（最下段ブロックが死線 +1.5u 以内で P1/P2BlockDeadLine 赤点滅、+0.5u で高速点滅）
- [ ] LaunchAimer センター通過ビジュアル（真上 ±10° で LineRenderer をシアン HDR に切替）

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
- [x] LaunchAimer センター通過音（`CheckCenterPass` で 0°=真上 を符号反転検出し `PlayCenterTick`。`se_center_tick` クリップ割当待ち）
- [ ] アイテムアイコンに形状識別（○ / ★ / △）を追加（アクセシビリティ対応）

---

## Phase F-Playtest: バランス調整（2026-06-04）

最終日にまとめてプレイテスト → 数値調整。

- [ ] BALANCE.md Section 11.3 の改訂デモパラメータ（maxHP=200, baseDropChance=0.25, dropChanceAttack=0.40）で開始（Section 9 から改訂・理由は BALANCE.md 11.3 参照）
- [ ] フルマッチを 5 試合プレイ（自分 + 他者 2 人）
- [ ] 1 ラウンドあたりの平均試合時間を計測（目標 60〜90s）
- [ ] 攻撃アイテムドロップ率の調整（攻撃 / 強化の偏りが体感バランスに合っているか）
- [ ] HP 帯バンドのカムバック感の確認（劣勢から逆転できるか）
- [ ] コンボマイルストーン（10/20/30）が実際の試合で到達できるか
- [ ] AttackHarden の視覚インパクト確認（金色オーラが「あっ」と感じさせるか）
- [ ] コンボ持続が短すぎないか（comboTimeout 値の妥当性）
- [ ] 一行底到達ペナルティの調整（線形 / 累進どちらか確定）
- [ ] 致命バグ・クラッシュ対応

### 発表 Go/No-Go 基準

**Go（発表実施）に必要な最低ライン**（すべてクリアしていれば発表できる）:
- [ ] **クラッシュなし**: 5 試合連続でクラッシュ・フリーズが発生しない
- [ ] **試合が成立**: ラウンド → リザルト → 再戦 → ラウンドの一連の流れが破綻なく進む
- [ ] **両プレイヤーが操作可能**: 1P / 2P 双方でパドル操作・発射・スキル発動ができる
- [ ] **HP・スコア・コンボが表示**: HUD の基本数値（HP、スコア、コンボ）が更新される
- [ ] **音が出る**: 最低限「ブロック衝突」「アイテム取得」「ラウンド勝利」の 3 種類の SE が再生される（BGM は任意）
- [ ] **DESIGN.md と矛盾しない**: 主要な仕様（HP=200、攻撃アイテム経由モデル、Dynamic Escalation）が動作する

**No-Go（発表延期 or デモ縮小）の判断基準**:
- 1 試合に 1 回以上の頻度でクラッシュ → 当日は事前録画ビデオに切り替え
- 攻撃アイテムが取れない / 妨害が発生しない → コア体験が伝わらないので「ブロック崩しのみ」のデモに縮退
- 主要操作キーが効かない → 発表者が代行操作（1P 自操作のみで進める）

**妥協ライン（達成できなくてもよい）**:
- BGM が無音でも構わない（SE のみで成立）
- マッチ結果画面の詳細スタッツは「勝者表示のみ」でも可
- Last Stand 演出が動かなくても基本ゲームは進む
- AI 対戦は実装不要（2 名揃わない場合は片方を発表者が操作）
- Phase F-Polish の追加演出（Trail、破片、攻撃アイテム発射トレイル）はゼロでも可

### 当日デモ品質チェック（発表 30 分前に実施）

- [ ] Editor から Play で動く（停止しない）
- [ ] スキル選択 → 試合 → ラウンド終了 → 試合 → マッチ終了 → 再戦 の完走を 1 回成功
- [ ] 音量バランス確認（会場スピーカーで聞こえる、SE が BGM に埋もれない）
- [ ] デモ params が適用されている（maxHP=200, demoMode=true 確認）
- [ ] 2P 役の人にキーバインドを再確認させる

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

## 現在のステータス（2026-06-01 実コード照合）

- **完了**: Phase A〜E（コアシステム）、Phase F-Setup（UI/カメラ/シェーダー基盤）、**Phase F-Combat（攻撃アイテム経由モデル・コンボ再定義・Dynamic Escalation、main マージ済み）**、**Phase F-Title（タイトル/設定/カウントダウン/ラウンド・マッチ結果のフロー）**
- **未着手の主要残作業**:
  - **Phase F-Audio**: 音は一行も実装されていない（`AudioSource`/`AudioMixer` 皆無）。発表 Go 基準に SE 3 種が含まれるため最優先。
  - **MatchStats 集計**: `maxCombo[]` / 総破壊数 / 被妨害数が未集計 → リザルトの「最大コンボ」等が出せない。
  - **Phase F-Polish の演出群**: ラウンド決着フラッシュ/暗転・Ball Heat・Danger Proximity・HP pip・センター通過シアン・スペシャル行・Last Stand 等（コードに痕跡なし）。
  - **HUD [任意] バインド**: Energy/Skill/Incoming/Victory Bar/Combo マイルストーン/SENT ラベルは **コードは実装済みだが UI 要素が未配置・未バインド**。
- **発表**: 2026-06-12（部活）。Audio はマスト、Polish はバッファ。

> **2026-06-01 注意**: 本ファイルは長らく 2026-05-20 で止まっており、実装済みのコアが未着手のように見えていた。逆に [[project-postmerge-plan]] memory は演出系を「コード済み」と過大申告していた。**実コードが真実**。今後は完了時に必ず `[x]` を反映すること。
