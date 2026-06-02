# CLAUDE.md

このファイルは実装の現状を把握するための技術情報をまとめたもの。

| ドキュメント | 内容 |
|---|---|
| [`docs/DESIGN.md`](./docs/DESIGN.md) | ゲーム設計仕様書（**最新仕様の真実**） |
| [`docs/ROADMAP.md`](./docs/ROADMAP.md) | 開発フェーズ計画・進捗・発表逆算スケジュール |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | 実装アーキテクチャ詳細・依存関係 |
| [`docs/BALANCE.md`](./docs/BALANCE.md) | バランス哲学・パラメータ調整ガイド・デモ設定 |
| [`docs/ASSETS.md`](./docs/ASSETS.md) | SE/BGM/ビジュアルアセット一覧・調達ガイド |
| [`docs/PRESENTATION.md`](./docs/PRESENTATION.md) | 発表（2026-06-05）のデモ進行・準備チェックリスト |
| [`docs/LEARNING.md`](./docs/LEARNING.md) | C# / Unity 学習ロードマップ |
| 本ファイル | コード実装の現状、シーン構成、座標系、既知の問題 |

**仕様変更が必要になった場合は、まず `docs/DESIGN.md` を更新してから実装に着手すること。**

**重要**: DESIGN.md とコード実装が乖離している箇所は、本ファイルで `⚠️ 仕様とコードの乖離` と明示する。CLAUDE.md は「現在のコードがどうなっているか」、DESIGN.md は「目標仕様」の二層で運用する。

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

新規にプロジェクトを開いた場合：

1. `BurokkuKuzushi > Setup HitStop` を実行
   - Arena1 / Arena2 の子に `HitStopController` GameObject を生成（シェイク対象は ArenaController.Awake が自動バインド）
2. `BurokkuKuzushi > Setup LaunchAimer` を実行
   - Arena1 / Arena2 の子に `LaunchAimer` GameObject を生成し、ArenaController にバインド

> ⚠️ `Setup HP UI` / `Setup MatchResult UI` / `Setup Skill Select UI` は**旧 `CenterUI` レイアウト前提のため現状の UI には合わない**。実行すると新しい `_UI/_CameraSpace/` 構造を壊す可能性があるので使わない。新 UI は Figma レイアウトに沿って手動で構築している。

すべてのメニュー操作は冪等（何度実行しても安全）。

---

## シーン構成

アクティブシーン: `Assets/SampleScene.unity`

```
SampleScene
├── EventSystem
├── GameManager        ← Singleton
├── Directional Light
├── Global Volume      ← URP Post Processing（Bloom 等）
├── MainCamera         ← 単 Ortho カメラ。world (0, 0, -34.8), ortho size 12.1
│                        HDR ON / Post Processing ON / TAA High
├── _UI                ← トップレベル UI フォルダ（後述）
├── Arena1             ← world (-9.2, 0.66, 0)
│   ├── TopWall / LeftWall / RightWall
│   ├── Ball / Player / DeadZone / BlockSpawner
│   └── ArenaController
│       ├── HitStopController
│       └── LaunchAimer
└── Arena2             ← world (9.2, 0.66, 0)、Arena1 と同構成（鏡像）
```

> `CenterUI_Old` は 2026-05-31 に削除済み（新 UI へ完全移行）。重複していた UIManager/MatchResultUI/SkillSelectUI も一掃。

### カメラ構成（単カメラ Ortho 化）

- 旧構成: Arena1/Arena2 にそれぞれ Camera1/Camera2 を子配置、画面分割レンダリング
- 新構成: **単一 `MainCamera`（Orthographic）**で両アリーナを横並びに収める
- メリット: ポスプロが単純、UI Canvas が 1 つで済む、Scene 編集楽
- 影響: HitStop はアリーナ Transform 自体を揺らす方式に変更（`HitStopController.SetShakeTarget`）

### 現在の主要な Inspector 値

| コンポーネント | パラメータ | 値 |
|---|---|---|
| MainCamera | orthographic / size | true / 12.1 |
| MainCamera | far clip | 100 |
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

## UI Hierarchy 構成

Figma レイアウトに準拠した新構造。3 つの Canvas を `_UI` 配下に階層化。

```
_UI                                    ← トップレベルフォルダ（Transform のみ）
├── _CameraSpace                       ← Screen Space - Camera Canvas（MainCamera 参照）
│   ├── _Base                          ← 背景・装飾・動かない要素
│   │   ├── Background                 (SpriteRenderer、Figma 出力 BG)
│   │   ├── P1ArenaFrame               (Bloom 装飾枠 左)
│   │   ├── P2ArenaFrame               (Bloom 装飾枠 右)
│   │   ├── P1BlockDeadLine / P2BlockDeadLine
│   │   └── _BloomyFrames/
│   │       ├── Bloom Left / Bloom Right
│   └── _Components                    ← 機能 UI
│       ├── _TitlePanel                (モーダル、TitleUI が制御。START/SETTINGS/QUIT)
│       ├── _SettingsPanel             (モーダル、SettingsUI が制御。先取数のみ)
│       ├── _SkillSelectPanel          (モーダル、SkillSelectUI が制御)
│       ├── _MatchResultPanel          (モーダル、MatchResultUI が制御)
│       ├── _P1Components/             ← P1 HUD（左側）
│       │   ├── P1PlayerTag / P1KeyBind / P1Separator
│       │   ├── _P1HpIndicator/
│       │   │   ├── P1HpFrame / P1HpLabel / P1HpMax (静的)
│       │   │   └── $P1HpFill / $P1HpValue       (動的)
│       │   ├── _P1Combo/
│       │   │   ├── P1ComboLabel / P1ComboMax    (静的)
│       │   │   └── $P1ComboValue                (動的)
│       │   ├── _P1Score/
│       │   │   ├── P1ScoreLabel                 (静的)
│       │   │   └── $P1ScoreValue                (動的)
│       │   └── _P1ItemInfo/
│       │       ├── P1ItemFrame / P1ItemFrameFill / P1ItemIconBg (静的)
│       │       └── $P1ItemName / $P1ItemDuration (動的)
│       └── _P2Components/             ← P2 HUD（右側、P1 のミラー）
└── （その他、Bloom テクスチャ等）
```

各 Canvas は Scale With Screen Size / 1920x1080 / Match 0.5 で統一。

### UI 命名規則

| プレフィックス | 意味 | 例 |
|---|---|---|
| `_PascalCase` | フォルダ親（空 GameObject、組織化のため） | `_Base`, `_P1HpIndicator`, `_P1Components` |
| `$PascalCase` | 動的要素（コードが `.text` / `.fillAmount` / `.color` 等を書き換える） | `$P1HpValue`, `$P1ScoreValue` |
| `PascalCase` | 静的要素（一度配置したら触らない） | `P1HpLabel`, `P1ArenaFrame` |
| `P1` / `P2` | プレイヤー番号プレフィックス（全要素に付与） | `P1HpFill`, `P2ScoreValue` |
| スペース・スラッシュ・括弧 | **禁則**（`transform.Find()` で破綻するため使わない） | — |

このルールにより、Hierarchy をパッと見て「コードから触る要素」が即わかり、UIManager 再バインド作業の範囲が明確になる。

### UI 連携の現状

- `_UI/_CameraSpace/_Base` が rootCanvas（Screen Space - Camera / MainCamera 参照）。`UIManager` / `MatchResultUI` / `SkillSelectUI` / `TitleUI` / `SettingsUI` はここにアタッチ
- `MatchResultUI`（→`_MatchResultPanel`）/ `TitleUI`（→`_TitlePanel`）/ `SettingsUI`（→`_SettingsPanel`）は **バインド済み・実機表示確認済み**（2026-05-31）
- `SkillSelectUI` は `panel` / `p1StatusText` / `p2StatusText` バインド済みで機能するが、**`cardP1Highlights[4]` / `cardP2Highlights[4]`（4枚カードのハイライト Image）は未バインド**。カードは手動配置後にバインドする
- `UIManager` は新 UI 構造に合わせて refactor 済み。SerializeField を 3 区分に整理:
  - **[必須]** HP / Combo / Score / ActiveItem（新 UI に既存）→ Inspector でバインドが必要
  - **[任意]** Energy / Skill / Round / Status / 妨害オーバーレイ（まだ UI 要素が無い）→ 配置後にバインド
  - **[演出]** 色閾値・スキル READY suffix 等
- `GameManager` はアクティブ効果を **`ActiveEffect` のリスト**（スロット/名前/期限）で追跡（同 `ItemEffectSlot` は上書き、期限切れ自動除去）。`RegisterActiveItem(playerIndex, slot, name, duration)` を `ItemDrop` が効果適用時に呼ぶ。HUD は当面 `GetActiveItemName` / `GetActiveItemRemaining` が**末尾（最新）1 個**を返して既存 1 スロットに表示。複数同時表示 UI は残作業（`GetActiveEffects()` で全件取得可）。`IsEffectSlotActive()` はドロップ過多抑制（`Block` の同スロット再抽選・スキップ）に使用

### 残作業（UI 連携）

`_UI/_CameraSpace/_Base` の **UIManager** Inspector で次をバインド:

| フィールド | バインド先 |
|---|---|
| `p1HpFill` | `$P1HpFill`（Image **Sliced**。HP 比率は `RectTransform.sizeDelta.x`=フル幅×ratio で削る。pivot.x=0 で右から減る。Sliced は fillAmount が効かないため width 制御, 2026-06-01） |
| `p1HpValue` | `$P1HpValue` |
| `p1ComboValue` | `$P1ComboValue` |
| `p1ScoreValue` | `$P1ScoreValue` |
| `p1ItemInfoRoot` | `_P1ItemInfo`（GameObject、表示/非表示の親） |
| `p1ItemName` / `p1ItemDuration` | `$P1ItemName` / `$P1ItemDuration` |
| P2 側 | 上記の P2 ミラー |

[任意] セクションは UI 要素を作ってからバインドする（未バインドでも null セーフで動く）:
- Energy ゲージ（Image, Vertical Fill）
- Skill 名表示 TMP（READY 状態で suffix が付く）
- Round ドット/勝利数 TMP
- 試合状態テキスト（Round Over バナー）
- 妨害通知オーバーレイ（CanvasGroup + Label の P1/P2 ペア）
- 攻撃送付ラベル（`p1SentLabel` / `p2SentLabel` TMP。攻撃者 HUD に `SENT → P{N}: 種別` 表示）
- コンボマイルストーン演出（`pXComboMilestoneOverlay` CanvasGroup + `pXComboMilestoneLabel` TMP。10/20/30 到達で `{N} COMBO!!`）
- Victory Bar（`victoryBar` Image, Horizontal Fill。fillAmount = P1HP/(P1HP+P2HP)）
- Incoming インジケータ（`p1IncomingSlots[]` / `p2IncomingSlots[]` TMP 配列、各最大 3。受信予約のシンボル表示）
- アイテムアイコン Image（現状は名前テキストのみ）

### Bloom 演出

- URP Bloom Threshold = 1.0 想定。`UI/HDRTint`（Image 用）/ `Custom/HDRUnlit`（Sprite/Mesh 用）シェーダーが [HDR] Tint Color を持ち、Intensity > 1 で Bloom Threshold 越えで発光
- `BreathPulse.cs` コンポーネントで HDR Intensity を Sin 波で脈動させる演出が可能

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
- `GetCombo(playerIndex)` は現在のコンボ値（`p1Combo` / `p2Combo`）を返す。コンボは **ブロック破壊ごと**に `RegisterBlockDestroyed` で +1（2026-06-01 接触ベースから戻した、DESIGN.md 5.8）。Thunder/Fire 等で複数破壊すると破壊数ぶん一気に伸びる。同メソッドがコンボ++/タイマーリセット/マイルストーン/エナジー蓄積を担う（旧 `RegisterBallHitBlock` は撤去）
- ラウンド/マッチ決着のカメラシェイクは勝者アリーナ `shake:false`、敗者アリーナ `shake:true` で区別

> ⚠️ **仕様とコードの乖離 — Phase F-Polish 追加実装**: 以下は DESIGN.md に定義済みだがコードに未実装。Phase F-Polish のチェックリストに含まれる:
> - （パドル反射ゾーンは 2026-05-28 仕様変更で廃止済み — 単純な物理反射に統一）

> **Phase F-Combat 実装状況（feature/phase-f-combat ブランチ）**
>
> 実装済み:
> - **攻撃アイテム経由モデル**: ItemType 拡張 + `EffectAttack` → `GameManager.SendInterference` 経路。コンボ自動妨害は撤廃済み。
> - **コンボ再定義** (DESIGN.md 5.8): comboTimeout(6s)/落下リセット + scoreComboMul/gaugeComboMul/itemDropComboMul。**ブロック破壊ごとに +1**（2026-06-01 接触ベースから戻した、`RegisterBlockDestroyed`）。タイマー起点は「最後のブロック破壊後」。
> - **罠アイテム** (Shrink/Hyper/Reversed): `Block.trapDisguiseChance` で強化枠に偽装。`PlayerController.inputReversed` 実装済み。
> - **Dynamic Escalation**: `BlockSpawner` の base/decay/min・base/gain/max + `roundElapsedTime` 実装済み。
> - **コンボマイルストーン / 攻撃側 SENT ラベル**: `UIManager.ShowComboMilestone` / `ShowSentLabel` とトリガー実装済み。**UI 要素は未バインド**（後述の任意セクションでバインド）。
> - **アイテム取得パドルフラッシュ**: `PlayerController.OnItemPickup(ItemCategory)` 実装済み。`ItemDrop` が取得時に系統色（Buff=青/Attack=赤/Trap=紫）で 0.1s フラッシュ。パドルは MeshRenderer なので `Renderer.material.color` を使用（DESIGN.md は SpriteRenderer 記述だが実体に合わせた）。**バインド不要で即動作**。
> - **Incoming インジケータ UI キュー** (`UIManager.PushIncoming`): FIFO 3 件 + `incomingDisplaySec`(3s) 自動失効 + Playing 以外で全消去。`GameManager.SendInterference` → `ArenaController.PushIncoming` → UIManager 経路。**UI 要素（`p1/p2IncomingSlots[]` TMP 配列）は未バインド**。シンボルは DESIGN.md 12.6 準拠（`⬛HARD`/`↓ROW`/`☣PSION`/`🐌SLOW`）だが絵文字グリフはバインドフォント依存。
> - **Victory Bar** (`$VictoryBar` Image.fillAmount): `UpdateVictoryBar()` が P1HP/(P1HP+P2HP) を毎フレーム反映（両 0 のみ 0.5）。**UI 要素は未バインド**。
> - **2026-05-28 廃止分の削除**: 反撃ウィンドウ / AttackSpike・BlockSpike / AttackHarden 降下停止 / CATCH & SHOOT (`SkillForceCatch`) — コード側も削除済み。
>
> 未実装（別フェーズ）:
> - **アイテム寿命** (`itemLifetime=8s`): 実装しない方針（2026-05-29 判断）。

> ⚠️ **仕様とコードの乖離 — Phase F-Audio 追加実装（2026-05-20）**: DESIGN.md 10.4 / 10.5 で定義済みだがコードに未実装:
> - **AudioMixer + dB 変換**: `dB = 20 × log10(value/100)` で PlayerPrefs 0-100 整数を dB に変換。
> - **SE コードトリガーマッピング**: DESIGN.md 10.4 の SE 発火位置に AudioSource.PlayOneShot を仕込む。
> - **ブロック衝突 SE 50ms クールダウン**: `lastBlockSeTime` を保持し `unscaledTime` 差分で抑制。
> - **BGM クロスフェード（HP 30% 帯・5% ヒステリシス）**: `bgm_match_base` と `bgm_match_tense` の同時再生 + Volume Lerp。

> **Phase F-Title 実装状況**:
> - **`GameState.Title` 実装済み**（旧 `WaitingToStart` を流用）。起動時 `Title`（`Time.timeScale=0`）→ `StartFromTitle()` で `SkillSelect` へ。`TitleUI` / `SettingsUI`（先取数のみ）を `_Base` に追加済み（パネル等は Figma 後にバインド）。`SetRoundsToWin/GetRoundsToWin` 追加。
> - 未実装: **GameState 拡張** `Countdown` / `RoundIntermission` の 2 状態（DESIGN.md 定義済み）。
> - **2026-05-28 廃止**: ポーズ機能 / チュートリアル / AI対戦 (`AIPlayerController`)。設定 UI は「先取数のみ」で最小復活（2026-05-30、音量/アクセシビリティは含めない）。

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
- `TriggerHitStop(frames, strong)`: 対象を freeze → **アリーナ Transform 自体をシェイク** → unfreeze の一連を `Time.unscaledDeltaTime` ベースのコルーチンで制御
- 単カメラ運用に合わせ、カメラではなくアリーナ Transform 自体（`ArenaRoot`）を揺らす方式。アリーナごとに独立してシェイク可能
- `SetShakeTarget(Transform)` でシェイク対象を受け取る（ArenaController.Awake で `ArenaRoot` を渡す）
- `strong=true` で強シェイク（ラウンド/マッチ決着時）
- Freeze 中はボール `linearVelocity=0`、Player は kinematic、Block は Rigidbody なし → 親 Transform 駆動の移動でも物理的攪乱なし

### `ArenaController.cs`
- `arenaHalfWidth / arenaHalfHeight` は `SpawnItem()` の底面 Y 計算にのみ使用（子コンポーネントへの配布なし）
- `ballSpawnOffsetY` → `GetBallSpawnLocalPos()` が実行時に `cachedPlayer.localPosition.y` を読んで動的に算出
- `cachedPlayer` / `cachedUIManager` を `Awake` でキャッシュ（`GetComponentInChildren` / `FindFirstObjectByType` の都度呼び出しを回避）
- `ArenaRoot` プロパティ (`transform.parent != null ? transform.parent : transform`) に統一
- Awake で `hitStop.SetShakeTarget(ArenaRoot)` を呼んでシェイク対象をバインド（カメラ参照は持たない）
- `TriggerHitStop(frames, strong, shake)` を公開 — Block / BallScript / GameManager はこれを呼ぶ
- `launchAimer` を Inspector でバインド（Setup LaunchAimer で自動設定）→ Awake で Initialize
- `GetBall()` / `GetSpawner()` / `GetSkillController()` で子コンポーネントを公開
- `SpawnZonePoison(worldPos)` / `SpawnZoneSlow(worldPos)` — ゾーン生成。親は `ArenaRoot`
- `ResetForNewRound()` は次をクリア/解除: メインボール再配置 + スポーナー再生成 / **SkillBall_Multi の追加ボール破棄** / **未取得の落下アイテム破棄** / **パドル一時効果解除 (`PlayerController.ResetState()`)** / ZonePoison / ZoneSlow。加えて `GameManager` 側で `ClearActiveItems()` を BeginMatch/StartNextRound で呼ぶ

### `LaunchAimer.cs`
- `ArenaController` の子 GameObject にアタッチ（Setup LaunchAimer で自動生成）
- `Initialize(ball, playerIndex, arena)` で対象ボール・プレイヤー番号・ArenaController を受け取る
- `ball.IsWaitingToLaunch` を監視し、true になるとメトロノーム発動
- sin 波で ±`metronomeAngleRange`° を `metronomePeriodSec` 周期で往復
- 1P: S キー / 2P: K キーで確定発射 → `ball.LaunchInDirection(localDir)` を呼ぶ。**発射は `GameState.Playing` 限定**（カウントダウン中は無効, DESIGN.md 12.12）
- LineRenderer でリアルタイムに発射角インジケーターを描画（ワールド座標）
- `ResetAim()`: ラウンド遷移でメトロノーム位相を中央へリセット（待機中にラウンドが終わると角度が引き継がれるのを防止。`ArenaController.ResetForNewRound` から呼ぶ）

### `BlockSpawner.cs`
- タイマーで行を生成、毎フレーム降下、底判定
- 妨害行（`pendingSabotageRows`）をキューで管理。`IsTopClear()` になり次第スポーン（旧 `pendingSpikeRows` は AttackSpike 廃止に伴い不要）
- `blockDeadZoneY`（旧 `bottomY`）を超えたブロックを削除し `GameManager.OnBlocksReachedBottom(playerIndex, count)` を通知。同時に `TriggerHitStop` でカメラシェイク
- `ReceiveSabotageRow()` — GameManager から呼ばれる（`ReceiveSpikeRow()` は AttackSpike 廃止で不要、コードに残っている場合は削除対象）
- `HardenRandomBlocks()` — LINQ で Normal ブロックをランダムに `hardenCount` 個選び `HardenToHp(hardenTargetHp)` で Hard 化
- `GetLowestBlockY()` / `GetSpawnY()` / `GetBlockDeadZoneY()` を公開 — LaunchAimer が自動発射タイマー短縮に使用
- 通常行は `explosiveBlockChance` / `hardBlockChance` / **`itemBlockChance`**(0.08, BlockItem) の確率で種別を割り当てる
- **スペシャル行**（DESIGN.md 5.4, `specialRowChance`=0.125・妨害予約が無いとき抽選）: 全Item / 全Explosive / 歯抜け(2列スキップ) の 3 種を `PickSpecialKind`→`SpawnRow(special)` で構築。スポーン時 `AudioManager.PlaySpecialRow`（`se_special_row` クリップ未配置）
- **妨害行 着弾演出**（DESIGN.md 6.3）: 妨害行スポーン時に上空（`addRowSlideDistance`）へずらし `SlideInSabotageRow` コルーチンで `addRowSlideDuration`(0.3s) かけて滑り込み。スライド中は `slidingBlocks` により降下対象外。着地で `Block.FlashImpact` + `addRowImpactFrames`(2) ヒットストップ + `se_addrow_land`。`ClearAndRespawn` で `StopAllCoroutines`+`slidingBlocks` クリア

### `BallScript.cs`
- `BallAttribute` enum: `Normal / Fire`（範囲ダメージ）`/ Thunder`（同種ブロック連鎖）`/ Ice`（高ダメ）`/ Heavy`（貫通+高ダメ）`/ Pierce`（貫通+通常ダメ+ヒットストップなし）
- 速度の3層管理: `naturalSpeed`（基本速度 + 時間加速）× `speedMultiplier`（アイテム効果）× `slowZoneMul`（ZoneSlow） = 実効速度
- `slowZoneMul`: ZoneSlow が毎フレーム書き込む public フィールド。ZoneSlow が OnDestroy / 検出失敗時に 1 に戻す。PrepareRespawn でもリセット
- `FixedUpdate` で毎フレーム実効速度に正規化。時間加速はメインボールのみ（`isExtraBall=false`）。`arenaDwellTime` はリスポーンでリセット
- `OnCollisionEnter` で衝突直後に角度補正（`ClampAngle`）→ 壁沿いループ防止
- `OnCollisionEnter` で壁バウンス検出（Block / PlayerController が GetComponent で見つからない衝突 = 壁）。`GetHitStopMultiplier()` が 0 より大なら `TriggerHitStop(wallBounceFrames * mul, shake:true)`
- `lastVelocity` は `FixedUpdate` でのみ更新（Heavy/Pierce 属性の貫通処理が衝突前速度を復元するために使用）
- `Launch()`: `transform.parent.TransformDirection()` でローカル→ワールド変換
- ボール GameObject に `"BallTag"` Unity タグが必須（`Block` / `DeadZone` どちらも `CompareTag("BallTag")` で判定）
- `PrepareRespawn(localPos)`: コライダー無効化 + `IsWaitingToLaunch=true`。コルーチン停止・速度状態リセット + **角速度/回転(localRotation)もリセット**（ラウンド遷移で残らない）
- **Ball Heat**（`Update()`, DESIGN.md 5.3）: 属性 Normal のときコンボ段階でボール色を 白→クリーム→橙→赤 に Lerp（`GetHeatColor`）。属性付与中は属性カラー優先。`unscaledDeltaTime` 駆動で HitStop 中も継続。**トレイルも追従**（`SetTrailColor` 共通化＋Gradient キャッシュ再利用で GC 回避）。Renderer は `cachedRenderer` にキャッシュ
- `LaunchInDirection(localDir)`: コライダー再有効化 + 発射。LaunchAimer から呼ばれる
- `GetHitStopMultiplier()`: `naturalSpeed/baseSpeed` が `hitStopSpeedThreshold` 未満なら 0、以上なら 0→1 にスケール。ブロック衝突・壁バウンスのフレーム数に乗算する
- `GetAttributeMultiplier()`: 属性倍率のみ（>= 1.0）。Explosive 破壊など速度閾値によらず掛けたい場合に使用。Pierce は 0f（ヒットストップなし）
- `SetAttributeTemporary(attr, duration)`: アイテム効果で属性を一時変更（コルーチン、重ね掛け上書き）
- `SetSpeedTemporary(multiplier, duration)`: アイテム効果でボール速度を一時変更（`speedMultiplier` コルーチン、重ね掛け上書き）
- 境界チェック: `FixedUpdate` でアリーナ外に出た場合、メインボールはペナルティなしリスポーン、追加ボールは Destroy

### `PlayerController.cs`
- `rb.isKinematic = true` + `transform.localPosition` 直接操作
- 1P: A/D（または矢印キー）、2P: J/L
- **移動可能なのは `Playing` と `Countdown` のみ**（DESIGN.md 12.12）。Countdown は `timeScale=0` なので `unscaledDeltaTime` で移動（パドルのポジショニング許可）。それ以外の状態（Title/SkillSelect/結果等）は移動不可
- `SetWidthTemporary(multiplier, duration)`: アイテム効果でパドル幅を一時変更（`localScale.x` 変更、コルーチン）
- `SetInputReversedTemporary(duration)`: 左右入力反転（TrapBall_Reversed）
- `ResetState()`: ラウンド遷移時に幅・入力反転・フラッシュコルーチンを全停止し、スケール/色を初期値へ復元 + **パドル位置を中央(x=0)へ復帰**（`ArenaController.ResetForNewRound` から呼ばれる）

### `DeadZone.cs`
- `ballSpawnOffsetY` と PlayerController.localPosition.y から動的にリスポーン位置を算出
- ArenaController.ballSpawnOffsetY と同じ値にすること

### `ZonePoison.cs`
- Phase E で新設。InterferencePoison（AttackPoison 取得）で生成される毒エリア
- `Setup(playerIndex, targetWorldY)` でパドル Y とオーナー設定 → 落下して着地後 `duration` 秒間持続
- 着地後は `OverlapSphereNonAlloc`（事前確保バッファ）でパドル接触を毎フレーム検出し `GameManager.OnPoisonTick()` を呼ぶ
- `Destroy(gameObject, duration)` で自動消滅。`ArenaController.ResetForNewRound()` でも即時削除

### `ZoneSlow.cs`
- Phase E で新設。InterferenceSlow で生成されるボール減速エリア
- `Setup(targetWorldY)` でアリーナ中央付近の着地 Y を設定 → 落下して着地後 `duration` 秒間持続
- 着地後は `OverlapSphereNonAlloc` でボール検出。内部ボールに `ball.slowZoneMul = slowFactor` を毎フレーム設定
- 前フレームで減速したボールをフレーム先頭でリセット → ゾーン離脱を自動検出
- `OnDestroy()` で `slowZoneMul` を確実に 1 に戻す（ResetForNewRound による即時破棄対応）

### `Block.cs`
- `BlockType` enum: `Normal`（1撃）/ `Hard`（複数撃）/ `Absorb`（当たると`absorbSpeedMultiplier`倍に減速）/ `Explosive`（破壊で周囲ブロックのHPを増加）/ `Item`（HP1・破壊で**確定**1個ドロップ, DESIGN.md 12.17）。※ Spike は現状コードに無い（旧記述削除）
- ブロック種別カラーを `Awake` でキャッシュした `Renderer` に `Start()` で適用（BlockSpawner が blockType を設定した後に実行される）
- **HP pip（残耐久ドット, DESIGN.md 5.4）**: HP>1（Hard/Hardened）は `BuildHpPips()` で子キューブのドットを hp 個生成、`TakeDamage` で currentHp 本に減らす。親の非一様スケール(1.3,0.5,1)をワールド換算で打ち消す。Item/Normal(HP1) は非表示。位置/サイズ/色は SerializeField
- **多重破壊ガード**: `destroyed` フラグで `OnDestroyed` を一度だけに（Destroy 遅延中の同フレーム追撃での二重カウント防止）
- `FlashImpact(color, dur)`: 妨害行着弾演出のフラッシュ（BlockSpawner から呼ばれる）
- `HardenToHp(int targetHp)`: InterferenceHarden から呼ばれる。blockType を Hard に変換し hp/currentHp を直接設定。Renderer を金色（`hardenedColor`）に更新。HP pip も再生成
- `OnCollisionEnter` で `ball.GetDamage()` + `ball.OnHitBlock(this)` 呼び出し — ボールに `"BallTag"` Unity タグが必須
- Normal/Hard/Absorb 衝突時: `normalHitFrames / hardHitFrames / absorbHitFrames`（デフォルト 0）に `ball.GetHitStopMultiplier()` を乗算してヒットストップ
- Explosive 破壊時: `explosiveHitFrames`（デフォルト 6）に `ball.GetAttributeMultiplier()` を乗算してヒットストップ（速度閾値によらず発動）
- `blockType` / `hp` はパブリックフィールド。`BlockSpawner` が `Instantiate` 後に直接代入して種類・HP を設定する
- `GetArena()`: `transform.parent?.parent?.GetComponentInChildren<ArenaController>()` — Block → BlockSpawner → Arena root の順で辿る
- 破壊時に `TryDropItem()` を呼ぶ。通常は確率ドロップ、**`BlockType.Item` は確定ドロップ**（確率/スロット抑制をスキップ）

### `EffectDefinition.cs`
- アイテム・スキル効果の抽象基底クラス（`Apply(playerIndex, arena)` メソッド）
- 実装クラス: `EffectBallAttribute` / `EffectPaddleScale` / `EffectBallSpeed` / `EffectHeal`

### `ItemDrop.cs`
- `ItemType` enum: `Fire / Ice / Thunder / Heavy / Pierce / Enlarge / SpeedUp / Shrink / Hyper / Heal`
- `ItemDefinition` static クラス: `GetColor(type)` / `GetName(type)` を提供
- `ItemDrop` MonoBehaviour: `Setup()` で初期化、`Update()` で落下 + `Physics.OverlapSphere` によるパドル接触判定
- kinematic-kinematic 間の OnTriggerEnter は発火しないため、毎フレーム OverlapSphere でパドルを検出
- アイテムは AddComponent で生成（Prefab なし）。public フィールドの値がそのまま使われる
- `ArenaController.SpawnItem(worldPos, type)` から生成。底 Y を超えたら自動 Destroy
- パドル接触で `BuildEffect().Apply()` と同時に `GameManager.RegisterActiveItem(playerIndex, slot, name, duration)` を呼ぶ。`slot` は `ItemDefinition.GetEffectSlot()`、duration は `GetActiveDuration()`（Heal/Attack は slot=None・duration=0 で登録されない）

### `SkillController.cs`
- ArenaController.Awake() で自動生成・Initialize される
- エナジーゲージを管理。スキルキー（1P: Q / 2P: U）でスキル発動
- `maxEnergy` を SerializeField で保持

### `SkillDefinition.cs`
- スキル効果の抽象基底クラス（`SkillDefinition`）
- 実装: `SkillPaddle_Enlarge` / `SkillBall_Attribute_Fire` / `SkillBall_Multi` / `SkillPanic_BlockClear`（`SkillForceCatch` は 2026-05-28 仕様から廃止）
- すべて public フィールドでパラメータを保持（Profile 参照なし）

> **`SkillForceCatch` (CATCH & SHOOT)** は 2026-05-28 仕様改訂で廃止。コード側も削除済み（`feature/phase-f-combat`）。

### `MatchResultUI.cs`
- `_UI/_CameraSpace/_Base` Canvas にアタッチ。`GameState.MatchOver` を検出してパネルを表示
- `Start()` で `HidePanel()`（シーン既定で active 保存されていても起動時に隠す。`panelShown` 初期 false 対策）
- **サマリー版**（2026-05-31 簡素化、Result A の勝数ピップ/スコア分割は廃止）: `matchWinnerText`("P{N} WINS!") / `scoreSummaryText`("P1: x pts  P2: y pts") / `winsSummaryText`("P1: a wins  P2: b wins") / `rematchText`・`menuText`(選択色トグル) / `hintText`
- A/D（J/L）で再戦/メニュー選択、Space 確定。再戦→`GameManager.StartRematch()`、メニュー→シーンリロード
- 動的要素は全て null セーフ。シーンの `_MatchResultPanel/...` 配下にバインド（`scoreSummaryText`/`winsSummaryText` は再バインドが必要）

### `SkillSelectUI.cs`
- 試合開始前のスキル選択画面。GameState.SkillSelect 中に panel を表示
- **4 枚カード方式（カード色で選択表現）**（DESIGN.md 5.6, 2026-05-31 簡素化）。1P: A/D でカード移動・S 確定 / 2P: J/L でカード移動・K 確定。別カーソル GameObject は置かず、各カードに重ねた P1/P2 ハイライト Image の **色**を切り替える（選択=点灯 P1水色/P2赤、未選択=透明、確定=不透明）
- `cardP1Highlights[4]` / `cardP2Highlights[4]`（Image 配列、index=AllSkills 並び順）。**未バインドでも安全に動作**（入力・確定・BeginMatch は機能）
- カード名/説明は静的（シーン側に固定配置）。旧 `p1SkillText`/`p2SkillText`（単一サイクル表示）・旧 `cardP1Cursors`/`cardP1Confirmed`（SetActive 方式）は廃止
- ⚠️ **要バインド**: `panel` / `cardP1Highlights[4]` / `cardP2Highlights[4]` / `p1StatusText` / `p2StatusText`

### `TitleUI.cs`
- 起動時のタイトル画面。`GameState.Title` の間 panel を表示。`_Base` にアタッチ済み
- メニュー 0=START / 1=SETTINGS / 2=QUIT。W/S・↑/↓ で移動、Space/Enter 確定。START→`GameManager.StartFromTitle()`、SETTINGS→`settingsUI.Open()`、QUIT→`Application.Quit()`
- **選択中項目はテキスト色で表現**（2026-05-31 簡素化、別カーソル不要）: `startText`/`settingsText`/`quitText` の色を `selectedColor`/`normalColor` で切替。設定を開いている間は panel を隠す
- ⚠️ **要バインド**: `panel` / `startText` / `settingsText` / `quitText`（`settingsUI` は同 `_Base` の SettingsUI に配線）

### `SettingsUI.cs`
- 設定画面（最小・**先取数のみ**, DESIGN.md 11.3）。`_Base` にアタッチ済み。`Open()`/`Close()`/`IsOpen`
- 先取数 1〜5 を A/D・←/→ で増減、`roundsValueText` に反映。Esc/Space/Enter で閉じる
- `PlayerPrefs "match.roundsToWin"` に保存、`Start()` で `GameManager.SetRoundsToWin()` に適用
- ⚠️ **要バインド**: `panel` / `roundsValueText`

### `UIManager.cs`
- `_UI/_CameraSpace/_Base` Canvas にアタッチ。毎フレーム GameManager をポーリングして更新
- SerializeField は **[必須] / [任意] / [演出]** の 3 区分に整理（詳細は「UI 連携の現状」セクション）
- HP バー色: 白（≥70%）→ 黄（≥30%）→ 赤（<30%）
- アクティブアイテム表示: `GetActiveItemName / GetActiveItemRemaining` を毎フレーム参照し、残り時間 > 0 のとき `p1ItemInfoRoot` を SetActive(true)、`$P1ItemName` `$P1ItemDuration` を更新
- HP バー本体: **Sliced のまま `RectTransform.sizeDelta.x` を HP 比率で縮める**（fillAmount ではない。pivot.x=0 で右から削れる。フル幅は Start でキャッシュ）。スコア表示は内部値の **×10**
- スキル READY 表示: `EnergyRatio >= 1` のとき `p1SkillName` に suffix（既定 ` · READY`）。緊急スキル発動可能時（`GameManager.IsPanicReady`）は `PANIC READY` で上書き
- 任意セクションは未バインドでも null セーフで動作（コンパイル・実行ともに影響なし）
- `ShowInterferenceOverlay(int playerIndex, string label)`: P1/P2 各画面半分を 1.5 秒赤フラッシュ（CanvasGroup alpha コルーチン、未バインドなら何もしない）
- **Danger Proximity**（`UpdateDangerLine`, DESIGN.md 5.4）: 最下段ブロックが `blockDeadZoneY + dangerRange(1.5)` 以内で `P1/P2BlockDeadLine`(SpriteRenderer) を **alpha 点滅**（色相=赤の `SpriteRenderer.color`、接近で周期 `dangerBlinkSlow→Fast` を**位相累積**で速める＝接近時の位相飛び対策）。底到達ペナルティで `FlashDangerLine` が白フラッシュ（`_TintColor` を HDR 白×Intensity・太さ×3）。死線スプライトは**白**で、色相=`SpriteRenderer.color`/発光=material `UI/HDRTint` の `_TintColor`
- **Last Stand**（`UpdateLastStand`, DESIGN.md 5.10）: HP ≤ `lastStandThreshold(0.10)` で `P1/P2ArenaFrame` を**元色のまま明るさだけ周期低下**（消えかけ電球風）、HP バーを赤明滅。`Playing` 以外では非アクティブ（脱出フレームで枠を元色へ復元）
- **Combo Timer Arc**（`UpdateComboArc`, DESIGN.md 6.2）: `p1/p2ComboArc`(Filled Image) の fillAmount に `GameManager.GetComboTimerRatio`(1→0) を反映。combo0 で非表示、消滅間際は橙。**要素未配置＝未バインド**

### `BreathPulse.cs`
- Material の HDR カラー Intensity を Sin 波で脈動させて Bloom Threshold をまたぐ「呼吸」演出
- `SpriteRenderer` / `UI.Image` 両対応
- Inspector で `minIntensity / maxIntensity / cycleSeconds` / `colorPropertyName` を設定
- Material はインスタンス化される（複数オブジェクトで Material を共有しない）

### シェーダー (`Assets/Shaders/`)
- `UI/HDRTint` (`UI_HDRTint.shader`): UI Image 用。標準 `UI/Default` に `[HDR]` Tint Color を追加。Stencil / Clip Rect / AlphaClip 完備
- `Custom/HDRUnlit` (`HDRUnlit.shader`): 3D Mesh / SpriteRenderer 用。HDR Base Color のみのシンプル Unlit。Lit 計算なしで Bloom 発光のみ

### フォント (`Assets/`)
- `BebasNeue-Regular.ttf` + `BebasNeue-Regular SDF.asset` — 数字表示用（HUD の HP/Score/Combo 等）
- `JetBrainsMono-{Regular,Bold,ExtraBold}.ttf` + 各 SDF Asset — ラベル・固定文言用
- TMP Font Asset Creator で Custom Characters 指定で生成

### Editor スクリプト (`Assets/Editor/`)
- `SetupHitStop.cs`: `BurokkuKuzushi > Setup HitStop`（冪等）— 各 ArenaController の子に HitStopController GameObject を生成
- `SetupLaunchAimer.cs`: `BurokkuKuzushi > Setup LaunchAimer`（冪等）
- `SetupCameraViewports.cs`: 単カメラ化前の名残（現状未使用、将来削除予定）
- `SetupHPUI.cs` / `SetupMatchResultUI.cs` / `SetupSkillSelectUI.cs`: **旧 CenterUI 構造前提のため現 UI には不適合**。実行しないこと。新 UI 確定後にリライト or 削除予定

---

## ローカル座標系の重要事項

**ゲーム内の位置指定はアリーナの親オブジェクトのローカル座標で行う。**

- Arena1 / Arena2 の子の `localPosition(0,0,0)` = そのアリーナの中心
- `BlockSpawner` が生成するブロックは BlockSpawner の子 → ローカル座標で管理
- `PlayerController` は `transform.localPosition` で移動
- 単カメラ化後はカメラがシーン root にあるため、Arena をオフセットしてもカメラとは独立（旧構成と異なる）
- HitStop シェイクは `ArenaRoot.localPosition` を直接揺らす（Arena1/2 はシーン直下なので localPosition = world position）

---

## 既知の問題

- **Block スコアが SerializeField 未対応**: `Block.cs` の `normalScore` / `hardScore` は Inspector から変更可能だが、Prefab に依存しているため Instantiate 後は BlockSpawner から設定されない。ハードコードと同義。
- **Recovery ファイル**: `Assets/_Recovery/` 以下の Unity 自動生成ファイルは Git にコミットしない。
