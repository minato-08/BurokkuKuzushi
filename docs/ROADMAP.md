# BurokkuKuzushi 開発ロードマップ

最終更新: 2026-05-14

このドキュメントは仕様書 [`DESIGN.md`](./DESIGN.md) を実装に落とすためのフェーズ分けと進捗管理。

---

## 進め方

- 拡張性を最初に組み込み、後から要素を追加できる構造を作る。
- フェーズが完了したら動作確認 → コミット → GitHub プッシュ。
- 各フェーズは「動くものができる」状態まで完結させ、中途半端な状態は残さない。

---

## Phase A: 基盤刷新

ハードコードされた数値を ScriptableObject に集約し、HP制とヒットストップの基盤を構築する。

### Phase A-1: GameBalanceProfile + HP制移行

- [x] `GameBalanceProfile` ScriptableObject 新設
- [x] `HPSystem` クラス新設（プレイヤーごと）
- [x] 残機制 (`maxLives`) を廃止し、HP制 (`maxHP = 500`) に移行
- [x] 既存ハードコードを Profile 参照に置換
- [x] UI を残機表示 → HPバー表示に変更
- [x] HP帯ごとの動的パラメータ参照機構
- [x] ボール衝突すり抜け対策（Rigidbody CCD 有効化）
- [x] Unity Editor で Profile アセット生成 + UI差し替えの手動セットアップ実行
- [x] 動作確認（HPバー表示、ボール落下時 -20HP、色変化、ブロック破壊）

### Phase A-2: ヒットストップ基盤

- [x] `IFreezable` インターフェース定義
- [x] `BallScript` / `PlayerController` / `BlockSpawner` に実装
- [x] `HitStopController` を `ArenaController` 配下に追加
- [x] カメラシェイク基盤（HitStop と同時発火、通常は片側カメラ・決着時は両方）
- [x] `BlockExplosive` ブロック爆発時にヒットストップ
- [x] パドル受け止め時に軽いヒットストップ
- [x] ラウンド決着・マッチ決着時に長尺ヒットストップ + 両カメラシェイク

### Phase A-3: マッチ結果画面

- [ ] マッチ終了状態を検出し、結果画面を表示（勝者・最終スコア・各種統計）
- [ ] 再戦 / メニューへ戻る の 2 択 UI
- [ ] 入力で選択 → `StartNewMatch()` or メニュー遷移
- [ ] `Time.timeScale = 0` のフリーズ解除（演出は HitStop 側で管理）

---

## Phase B: メトロノーム発射

- [ ] `LaunchAimer` クラス新設
- [ ] リスポーン後に角度インジケーター UI 表示
- [ ] 発射キー押下時に確定角度で発射
- [ ] `LaunchSettings`（振れ幅・周期）を Profile から参照
- [ ] スタック検出による速度倍率付与（既存 `ClampAngle` と併用）

---

## Phase C: アイテム

- [ ] `EffectDefinition` 抽象クラス定義
- [ ] `ItemDefinition` 実装
- [ ] `ItemDrop` MonoBehaviour（落下するアイテム）
- [ ] ブロック破壊時にドロップ判定
- [ ] パドルキャッチで効果適用
- [ ] アイテム実装:
  - [ ] 属性付与系（Fire / Ice / Thunder / Heavy）
  - [ ] パドル強化系（Enlarge / SpeedUp）
  - [ ] ボール強化系（Pierce）
  - [ ] 回復系（Heal）
  - [ ] 不利系（Shrink / Hyperspeed）

---

## Phase D: スキル

- [ ] `SkillDefinition` 実装
- [ ] エナジーゲージ実装
- [ ] HP帯に応じたゲージ蓄積率変動
- [ ] 試合前のスキル装備 UI
- [ ] キー入力で発動
- [ ] スキル実装:
  - [ ] `SkillPaddle_Enlarge`
  - [ ] `SkillBall_Multi`
  - [ ] `SkillBall_Attribute_Fire`
  - [ ] `SkillForceCatch`
  - [ ] `SkillPanic_BlockClear`

---

## Phase E: 妨害多様化

- [ ] `BlockDefinition` ScriptableObject 化（既存 enum から移行）
- [ ] `InterferencePayload` 機構実装
- [ ] 妨害種別の実装:
  - [ ] `InterferenceHarden`
  - [ ] `InterferenceSpike`
  - [ ] `InterferencePoison`
  - [ ] `InterferenceSlow`
- [ ] 新ブロック / Zone 実装:
  - [ ] `BlockSpike`
  - [ ] `BlockHardened`
  - [ ] `ZonePoison`
  - [ ] `ZoneSlow`
- [ ] 妨害送付時の通知演出（受け取り側にオーラ or スクリーンオーバーレイ）

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

- **完了**: Phase A-1（HP制・GameBalanceProfile）、Phase A-2（ヒットストップ基盤）
- **進行中**: Phase A-3（マッチ結果画面）
- **次フェーズ**: Phase A-3 完了後 → Phase B（メトロノーム発射）
