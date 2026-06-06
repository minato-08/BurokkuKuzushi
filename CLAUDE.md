# CLAUDE.md

このファイルは実装の現状を把握するための技術情報をまとめたもの。

| ドキュメント | 内容 |
|---|---|
| [`docs/DESIGN.md`](./docs/DESIGN.md) | ゲーム設計仕様書（**最新仕様の真実**） |
| [`docs/IMPLEMENTATION.md`](./docs/IMPLEMENTATION.md) | **As-Built 対応表**（DESIGN ↔ 実装の差異・未実装を節番号順に一覧） |
| [`docs/ROADMAP.md`](./docs/ROADMAP.md) | 開発フェーズ計画・進捗・発表逆算スケジュール |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | 実装アーキテクチャ詳細・依存関係 |
| [`docs/BALANCE.md`](./docs/BALANCE.md) | バランス哲学・パラメータ調整ガイド・デモ設定 |
| [`docs/ASSETS.md`](./docs/ASSETS.md) | SE/BGM/ビジュアルアセット一覧・調達ガイド |
| [`docs/PRESENTATION.md`](./docs/PRESENTATION.md) | 発表（2026-06-12）のデモ進行・準備チェックリスト |
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
│   ├── Ball                         ← ★ShakeRoot の外（シェイクに引きずられないため）
│   ├── ArenaController
│   │   ├── HitStopController
│   │   └── LaunchAimer
│   └── ShakeRoot                    ← シェイク対象（local 0,0,0）。揺らしてよい要素だけを収める
│       ├── TopWall / LeftWall / RightWall
│       └── Player / DeadZone / BlockSpawner
└── Arena2             ← world (9.2, 0.66, 0)、Arena1 と同構成（鏡像）
```

> `CenterUI_Old` は 2026-05-31 に削除済み（新 UI へ完全移行）。重複していた UIManager/MatchResultUI/SkillSelectUI も一掃。

### カメラ構成（単カメラ Ortho 化）

- 旧構成: Arena1/Arena2 にそれぞれ Camera1/Camera2 を子配置、画面分割レンダリング
- 新構成: **単一 `MainCamera`（Orthographic）**で両アリーナを横並びに収める
- メリット: ポスプロが単純、UI Canvas が 1 つで済む、Scene 編集楽
- 影響: HitStop は **`Arena{N}/ShakeRoot` を揺らす方式**（`HitStopController.SetShakeTarget`）。**Ball は ShakeRoot の外（Arena 直下）に置く**。非キネマティック Rigidbody は親 Transform を揺らすと毎フレーム teleport されて飛行が止まる/トレイルが裂けるため、ボールだけシェイク対象から除外（2026-06-03）。壁/パドル/DeadZone/BlockSpawner は ShakeRoot 配下で揺れる

### 現在の主要な Inspector 値

> ⚠️ **正確な現在値は Unity Inspector で確認すること**。シーン（`Assets/SampleScene.unity`）のインスタンス値はコードの SerializeField デフォルトと異なる場合がある（例: コード側 PlayerController `xLimit=5.5`/`paddleLocalY=-5`、DeadZone `ballSpawnOffsetY=1`、BlockSpawner `blockDeadZoneY=-4.5`）。主なシーン値（未検証）の目安: MainCamera ortho size 12.1 / far 100、PlayerController speed 16・xLimit 4.7・paddleLocalY -8、BlockSpawner blocksPerRow 6・blockWidth 1.5667・spawnY 4.5・blockDeadZoneY -4.5・descentSpeed 0.1、ArenaController/DeadZone ballSpawnOffsetY 1.3、Ball localScale 0.36、DeadZone localPos (0,-11,0)。

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
- `SkillSelectUI` は `panel` / `p1StatusText` / `p2StatusText` バインド済みで機能するが、**`cardP1Cursors[]` / `cardP2Cursors[]` / `cardP1Ready[]` / `cardP2Ready[]`（カード選択中カーソル/確定後 Ready の GameObject 配列, SetActive 方式）は未バインド**。カードは手動配置後にバインドする（※色ハイライト Image 方式ではない。`SkillSelectUI.cs` 節参照）
- `UIManager` は新 UI 構造に合わせて refactor 済み。SerializeField を 3 区分に整理:
  - **[必須]** HP / Combo / Score / ActiveItem（新 UI に既存）→ Inspector でバインドが必要
  - **[任意]** Energy / Skill / Round / Status / 妨害オーバーレイ → 配置後にバインド。**スキル HUD（`p1/p2EnergyFill`=`PXSkillGauge`、`p1/p2SkillName`=`$PXSkillName`、`p1/p2SkillIcon`=`$PXSkillIcon`＋アイコン配列）はバインド済み（2026-06-06）**
  - **[演出]** 色閾値 等
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
- ~~Energy ゲージ~~ → **バインド済み**（`PXSkillGauge`, Image Filled Horizontal）
- ~~Skill 名表示 TMP~~ → **バインド済み**（`$PXSkillName`。名前のみ表示・READY suffix なし）
- ~~スキルアイコン~~ → **バインド済み**（`$PXSkillIcon` Image＋`skillIconsReady[]`/`skillIconsUnavailable[]`。`SkillId` index で可/不可スプライト差替）
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

### 設定方針（SerializeField 直接管理 ＋ 左右共通値は ArenaSharedConfig）

ScriptableObject / Profile（アセット）は使用しない。各コンポーネントは従来どおり自分の SerializeField を持つが、**Arena1/Arena2 で同値であるべき共通チューニング値は `ArenaSharedConfig`（シーン内 MonoBehaviour 1 個）に集約**し、各コンポーネントが初期化時に読んで自分へ適用する（2026-06-02 導入）。

- **`ArenaSharedConfig`**（`Assets/ArenaSharedConfig.cs`）: シーンに 1 個だけ置く共有設定。`Instance`（`FindFirstObjectByType` で解決）。`PlayerController`/`BlockSpawner`/`BallScript`/`LaunchAimer`/`SkillController`/`ArenaController`/`DeadZone` が Awake/Start/Initialize 冒頭で `ApplySharedConfig()` を呼び、共通値を上書き適用する。
- **null セーフ・段階移行可**: 共有設定 GameObject が無ければ `Instance` は null を返し、各コンポーネントは自前の SerializeField 値で動作する。共有を効かせるにはシーンに `ArenaSharedConfig` を付けた GameObject を 1 個作り、正となる値を設定する。
- **per-arena 固有（共有しない）**: `playerIndex`、各アリーナ子オブジェクトへの参照（`ball`/`spawner`/`launchAimer`/`blockPrefab`）。
- `GameManager` は元々シングルトン（共有）なので対象外。HP量/ダメージ/ヒットストップ等は引き続き GameManager の SerializeField。
- `Block` はプレハブ共有のため左右で重複しておらず対象外。
- `DeadZone.ballSpawnOffsetY` と `ArenaController.ballSpawnOffsetY` は共有設定で同値化される（旧: 手動で両方 1.3 に揃える運用）。

`ArenaController.arenaHalfWidth / arenaHalfHeight` は `SpawnItem()` のアイテム底面計算にのみ使用。

---

## スクリプト一覧

### `ArenaSharedConfig.cs`
- Arena1/Arena2 で同値であるべき**共通チューニング値を集約**するシーン内 MonoBehaviour（1 個前提）。`Instance`（未解決なら `FindFirstObjectByType` で都度解決）
- 保持: パドル（speed/xLimit/paddleLocalY 等・フラッシュ色）/ ブロックスポーン（行数・幅・spawnY・Escalation・各種確率・HP・妨害・スライド演出）/ ボール（速度・軌道・属性ダメージ・半径・ヒットストップ倍率・属性色・Ball Heat・トレイル）/ エイマー / `maxEnergy` / `arenaHalfWidth`/`arenaHalfHeight`/`ballSpawnOffsetY` / **アイテムアイコン**（`itemIcons[]`＝ItemType→Sprite マップ＋`GetItemIcon(type)`、`itemIconWorldSize`、`itemIconGlow`＝Bloom 発光量。落下アイテム本体の見た目に使用、`BurokkuKuzushi > Setup Item Icons` で自動結線）
- **ヒットストップ/カメラシェイクの「手応え」を一元集約**: `impactBaseFrames`/`impactSpeedWeight`/`impactThreshold`/`impactMaxFrames`（ブロック衝突）/ `explosiveHitFrames`（Explosive 破壊の下限）/ `freezeSkipSpeedFactor`（この倍率超の高速時はフリーズせずシェイクのみ）/ `shakeIntensityNormal`/`shakeIntensityStrong`（振幅）/ `skillPanicHitStopFrames`。`HitStopController`/`Block`/`BallScript` が `ApplySharedConfig` で読む。**試合フロー系（`interferenceTriggerFrames`/`roundEndFrames`/`matchEndFrames`）は単一インスタンスの `GameManager` SerializeField のまま**（集約対象外）
- 各コンポーネントが Awake/Start/Initialize 冒頭で `ApplySharedConfig()`（自分の private フィールドへ上書き適用）。**未配置なら各自の SerializeField 値で動作（null セーフ）**
- 共有しないのは `playerIndex` と各アリーナ子オブジェクト参照のみ。詳細は「設定方針」セクション

### `GameManager.cs`
- Singleton (`GameManager.Instance`)
- `HPSystem` をプレイヤーごとに保持し、`ApplyDamage()` が全ダメージの最終窓口
- `HPStateBand` クラスも同ファイルで定義。Inspector で hpStateBands[] 配列を設定する（空なら全倍率1.0で動作）
- HP帯に応じた動的パラメータ参照: `GetCurrentBand(playerIndex)` → `HPStateBand`
- `WaitForSecondsRealtime` 使用（`Time.timeScale=0` でも動作）
- `GetCombo(playerIndex)` は現在のコンボ値（`p1Combo` / `p2Combo`）を返す。コンボは **ブロック破壊ごと**に `RegisterBlockDestroyed` で +1（2026-06-01 接触ベースから戻した、DESIGN.md 5.8）。Thunder/Fire 等で複数破壊すると破壊数ぶん一気に伸びる。同メソッドがコンボ++/タイマーリセット/マイルストーン/エナジー蓄積を担う（旧 `RegisterBallHitBlock` は撤去）
- ラウンド/マッチ決着のカメラシェイクは勝者アリーナ `shake:false`、敗者アリーナ `shake:true` で区別

> **Phase F 実装状況サマリー**（詳細は各スクリプト節）
>
> - **F-Polish**: Ball Heat / Danger Proximity / Last Stand / HP pip / AttackAddRow 着弾 / スペシャル行 / §12.12 入力制御 / コンボマイルストーン 実装済み。
> - **F-Combat**: 攻撃アイテム経由モデル（`EffectAttack`→`SendInterference`、コンボ自動妨害は撤廃）/ コンボ再定義（comboTimeout 6s・落下リセット・各 Mul、**ブロック破壊ごとに +1** `RegisterBlockDestroyed`）/ 罠アイテム（`trapDisguiseChance` で強化枠偽装・入力反転）/ Dynamic Escalation / コンボマイルストーン・SENT ラベル（UI 未バインド）/ Incoming インジケータ（FIFO3・`incomingDisplaySec`3s、UI 未バインド、シンボルは DESIGN 12.6 準拠）/ Victory Bar P1HP/(P1+P2)HP（UI 未バインド）/ アイテム取得パドルフラッシュ（系統色 Buff青/Attack赤/Trap紫、`material.color`、即動作）。
> - **F-Audio**: `AudioManager.cs`（シングルトン）。dB 変換（`20×log10(v/100)`）＋PlayerPrefs 音量、全 SE トリガー配線、**種別別 break/hit SE**（未割当は Normal フォールバック・Hard 衝突のみ -2 半音・とどめは破壊音のみ）、底到達 SE、衝突 SE 50ms クールダウン、BGM クロスフェード（HP 30% 帯・5% ヒステリシス）実装済み。**残**: 音源クリップ割り当てと `Assets/Audio/MasterMixer.mixer` 作成＋Expose Param（未割当でも無音で安全動作）。
> - **F-Title**: `GameState` 7 状態（`Title` 起動時 timeScale=0 / `Settings` / `SkillSelect` / `Countdown` 3,2,1,GO! / `Playing` / `RoundOver` / `MatchOver`）。フロー: StartFromTitle→Settings→ConfirmSettings→SkillSelect→BeginCountdown→Countdown→Playing、ReturnToTitle。RoundIntermission は作らず `RoundOver`＋`RoundIntermissionRemaining`（unscaled）で代替。TitleUI/SettingsUI は `_Base` にアタッチ・バインド済み。
>
> **未実装の DESIGN 演出**: ボール属性 VFX(5.2) / エイマー振れ角幅・予想軌道・センター通過音(5.3) / ブロック起源オーラ N/S/O(5.4) / Explosive・Fire 範囲 VFX / ラウンド決着テキスト overlay。
> **廃止仕様**（未実装ではなく削除）: パドル反射ゾーン / 反撃ウィンドウ / AttackSpike・BlockSpike / AttackHarden 降下停止 / CATCH & SHOOT / ポーズ / チュートリアル / AI対戦。アイテム寿命(`itemLifetime`)は実装しない方針。

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
- `TriggerHitStop(frames, strong, shake, freeze)`: 対象を freeze → **アリーナ Transform 自体をシェイク** → unfreeze の一連を `Time.unscaledDeltaTime` ベースのコルーチンで制御
- **フリーズ/シェイク分離**（`freeze` 引数, DESIGN.md 5.x）: `freeze:false` で**フリーズせずシェイクのみ**。**ボール衝突以外のイベントは全て `freeze:false`**（飛行中ボールを空中で止めない）。該当: 底到達 / スライド着地 / 妨害受信(`SendInterference`) / スキル発動 / **高速ボールのブロック衝突**。フリーズするのは**通常速度のボール衝突**（ブロック衝突・壁バウンス・パドル反射・Explosive 破壊）。ただし実効速度が `freezeSkipSpeedFactor`(=2.5) 倍超の高速時（HYPER 等）はブロック衝突も `freeze:false`（止めると爽快さ低下＋トレイル退色。`BallScript.ShouldFreezeOnImpact()` が判定し両 `TriggerHitStop` に渡す）。※ラウンド/マッチ決着は意図的にフリーズ（飛行中ボールが無いため例外）。割り込みガードは `activeFroze` フラグで前ルーチンが shake-only なら `UnfreezeAll` を呼ばない（未フリーズ対象を壊さない）
- **多重発火ガード**: シェイク中に再度 `TriggerHitStop` が来たら、旧コルーチン停止時に `RestoreShakeTarget()` でアリーナ位置を基準へ戻してから再開（オフセット残り防止）。`RestoreShakeTarget()` は正常終了時も呼ぶ共通メソッド
- 単カメラ運用に合わせ、カメラではなく **`Arena{N}/ShakeRoot`**（壁/パドル/DeadZone/BlockSpawner を収める空オブジェクト, local 0,0,0）を揺らす方式。アリーナごとに独立してシェイク可能。**Ball は ShakeRoot の外（Arena 直下）**なのでシェイクに引きずられない
- `SetShakeTarget(Transform)` でシェイク対象を受け取る（ArenaController.Awake で `ShakeRoot` を渡す。未解決なら `ArenaRoot.Find("ShakeRoot")`→ArenaRoot にフォールバック）
- **アリーナ枠も同期シェイク**（`SetFrameShakeTarget(Transform)`）: `P{N}ArenaFrame`（UI キャンバス上の SpriteRenderer）を ShakeRoot と同一のワールド変位で揺らす。キャンバススケールに依存しないよう `localPosition` でなく world `position` をオフセット。ArenaController.Awake が `UIManager.GetArenaFrameTransform(playerIndex)` で取得して渡す（未バインドなら null セーフ）。枠の色は Last Stand が別途制御し競合しない
- `strong=true` で強シェイク（ラウンド/マッチ決着時）。`shakeIntensityNormal`/`shakeIntensityStrong` は `Awake` の `ApplySharedConfig` で `ArenaSharedConfig` から読む（P1/P2 二重管理を解消, 2026-06-03）
- Freeze 中はボール `linearVelocity=0`、Player は kinematic、Block は Rigidbody なし → 親 Transform 駆動の移動でも物理的攪乱なし

### `ArenaController.cs`
- `arenaHalfWidth / arenaHalfHeight` は `SpawnItem()` の底面 Y 計算にのみ使用（子コンポーネントへの配布なし）
- `ballSpawnOffsetY` → `GetBallSpawnLocalPos()` が実行時に `cachedPlayer.localPosition.y` を読んで動的に算出
- `cachedPlayer` / `cachedUIManager` を `Awake` でキャッシュ（`GetComponentInChildren` / `FindFirstObjectByType` の都度呼び出しを回避）
- `ArenaRoot` プロパティ (`transform.parent != null ? transform.parent : transform`) に統一
- Awake で `hitStop.SetShakeTarget(shakeRoot)` を呼んでシェイク対象をバインド（`shakeRoot` 未設定なら `ArenaRoot.Find("ShakeRoot")`→ArenaRoot にフォールバック。カメラ参照は持たない）。`[SerializeField] shakeRoot` は Inspector 未バインドでも名前解決で動く
- `TriggerHitStop(frames, strong, shake, freeze)` を公開 — Block / BallScript / GameManager はこれを呼ぶ。`freeze:false` でシェイクのみ（BlockSpawner の底到達・スライド着地が使用）
- `launchAimer` を Inspector でバインド（Setup LaunchAimer で自動設定）→ Awake で Initialize
- `GetBall()` / `GetSpawner()` / `GetSkillController()` で子コンポーネントを公開
- `SpawnZonePoison(worldPos)` / `SpawnZoneSlow(worldPos)` — ゾーン生成。親は `ArenaRoot`
- `SpawnItem(worldPos, type)` — 落下アイテム本体を生成。**`ArenaSharedConfig.GetItemIcon(type)` でアイコン Sprite が取れれば SpriteRenderer 製のアイコン本体**（`CreateIconItem`、`itemIconWorldSize` でワールド高さにスケール、`Custom/HDRSprite` 共有マテリアル＋`_Color`(HDR)=`itemIconGlow` で Bloom 発光、コライダー無し＝ピックアップは ItemDrop 側のパドル OverlapSphere）、**取れなければ従来の色付き球**（`CreateSphereItem`、null セーフフォールバック）。アイコンは `Assets/UI/item-icons/` 配下＋`BurokkuKuzushi > Setup Item Icons` で取り込み・結線
- **スキル用メソッド**: `SpawnHyperFloor(duration)`（HYPER の床。`[SerializeField] hyperFloor` をバインドしていれば発動中だけ `SetActive(true)`＝Unity 上で位置/サイズ/見た目/コライダー調整可・非アクティブ非トリガーコライダー付きで配置。未バインドなら `HyperFloor_Runtime` キューブを ShakeRoot 配下に実行時生成し duration 後 Destroy）/ `BeginBurst(shots, interval, angle, ballLifetime)`（BURST → 自前コルーチン `BurstFireRoutine` で `shots` 発を `interval` 秒間隔で自動連射。`LaunchAimer` は使わない＝プレイヤーの発射操作に干渉しない。角度は鉛直上0°基準で +angle/-angle 交互, 2026-06-06）/ `SpawnBurstBall(localDir, lifetime)`（BURST の 1 発。複製直後に `ball.BaseScale` で素サイズ化＋発射前 `PrepareRespawn` で効果全リセット＝**プレーンな追加ボール**, `isExtraBall`）
- `BurstFireRoutine`: `Playing` 中のみ進行（他状態で break）。各発 `deg = (i%2==0)?angle:-angle`。`burstRoutine` を保持し `ResetForNewRound` で `StopCoroutine`
- 追加ボール生成（BURST）時に **`hitStop?.RegisterFreezable(bs)` を呼ぶ**（追加ボールを HitStopController に登録してヒットストップ中も止める）
- `ResetForNewRound()` は次をクリア/解除: メインボール再配置 + スポーナー再生成 / **追加ボール（BURST 等 `isExtraBall`）破棄** / **HYPER の床（バインド済みは `SetActive(false)`、実行時 `HyperFloor_Runtime` は破棄）** / **未取得の落下アイテム破棄** / **パドル一時効果解除 (`PlayerController.ResetState()`)** / ZonePoison / ZoneSlow。加えて `GameManager` 側で `ClearActiveItems()` を BeginMatch/StartNextRound で呼ぶ

### `LaunchAimer.cs`
- `ArenaController` の子 GameObject にアタッチ（Setup LaunchAimer で自動生成）
- `Initialize(ball, playerIndex, arena)` で対象ボール・プレイヤー番号・ArenaController を受け取る
- `ball.IsWaitingToLaunch` を監視し、true になるとメトロノーム発動
- sin 波で ±`metronomeAngleRange`° を `metronomePeriodSec` 周期で往復
- 1P: S キー / 2P: K キーで確定発射 → `ball.LaunchInDirection(localDir)` を呼ぶ。**発射は `GameState.Playing` 限定**（カウントダウン中は無効, DESIGN.md 12.12）
- LineRenderer でリアルタイムに発射角インジケーターを描画（ワールド座標）
- `ResetAim()`: ラウンド遷移でメトロノーム位相を中央へリセット（待機中にラウンドが終わると角度が引き継がれるのを防止。`ArenaController.ResetForNewRound` から呼ぶ）
- **BURST 連射モードは撤去済み（2026-06-06）**: BURST は `ArenaController` 自前の自動連射（`BurstFireRoutine`）に移行。`LaunchAimer` は通常のメインボール発射照準のみを担い、BURST 関連メソッド/状態は持たない（発射キーがスキル弾で暴発しない）

### `BlockSpawner.cs`
- タイマーで行を生成、毎フレーム降下、底判定
- 妨害行（`pendingSabotageRows`）をキューで管理。`IsTopClear()` になり次第スポーン（旧 `pendingSpikeRows` は AttackSpike 廃止に伴い不要）
- `blockDeadZoneY`（旧 `bottomY`）を超えたブロックを削除し `GameManager.OnBlocksReachedBottom(playerIndex, count)` を通知。同時に `TriggerHitStop(..., freeze:false)` で**シェイクのみ**（ボール衝突でないので飛行中ボールを止めない, 2026-06-03）。妨害行スライド着地も同様に `freeze:false`
- `ReceiveSabotageRow()` — GameManager から呼ばれる（`ReceiveSpikeRow()` は AttackSpike 廃止で不要、コードに残っている場合は削除対象）
- `HardenRandomBlocks()` — LINQ で Normal ブロックをランダムに `hardenCount` 個選び `HardenToHp(hardenTargetHp)` で Hard 化
- `ConvertRandomToExplosive(float fraction)` — EXPLOSION スキル。盤面の現在ブロック数（非null全体）の `fraction` 割合（既定0.3）をランダムに選び `Block.ConvertToExplosive()` で Explosive 化。母集団に既 Explosive も含む（再設定で無害）。盤面密度に比例して個数がスケール（2026-06-06 固定数10〜20個から割合方式へ変更）
- `GetLowestBlockY()` / `GetSpawnY()` / `GetBlockDeadZoneY()` を公開 — LaunchAimer が自動発射タイマー短縮に使用
- 通常行は `explosiveBlockChance` / `hardBlockChance` / **`itemBlockChance`**(0.08, BlockItem) の確率で種別を割り当てる
- **スペシャル行**（DESIGN.md 5.4, `specialRowChance`=0.125・妨害予約が無いとき抽選）: 全Item / 全Explosive / 歯抜け(2列スキップ) の 3 種を `PickSpecialKind`→`SpawnRow(special)` で構築。スポーン時 `AudioManager.PlaySpecialRow`（`se_special_row` クリップ未配置）
- **行スライドイン演出**（DESIGN.md 6.3）: 行スポーンは `SpawnRowWithSlide(type, distance, duration, impact, special)` 経由で `SlideInRow` コルーチンを起動。上空（distance）へずらし duration かけて滑り込む。スライド中は `slidingBlocks` により降下対象外。
  - **通常行**: 控えめに（`normalSlideDistance`=1.5 / `normalSlideDuration`=0.2、impact なし）＝湧き感の軽減。
  - **妨害行**: 派手に（`addRowSlideDistance`=6 / `addRowSlideDuration`=0.3、impact あり）。着地で `Block.FlashImpact` + `addRowImpactFrames`(2) ヒットストップ + `se_addrow_land`。
  - `ClearAndRespawn` で `StopAllCoroutines` + `slidingBlocks.Clear()` + `pendingSabotageRows=0` + 再スポーン。

### `BallScript.cs`
- `BallAttribute` enum: `Normal / Fire`（範囲ダメージ）`/ Thunder`（同種ブロック連鎖）`/ Ice`（高ダメ）`/ Heavy`（高ダメ・速度0.7倍・**非貫通**=通常反射, DESIGN.md 5.2）`/ Pierce`（貫通+通常ダメ+ヒットストップなし）。`OnHitBlock` の貫通（`lastVelocity` 復元）case は **Pierce のみ**（2026-06-03 Heavy を非貫通に修正）
- **Pierce 素通り（軌道カクつき対策）**: Pierce 中 `FixedUpdate` で `OverlapSphereNonAlloc` によりブロックを検出し、`Physics.IgnoreCollision(ball, block, true)` で物理反発を無効化して直進させ、ダメージは衝突経由でなく overlap で1回だけ与える（`pierceIgnored` HashSet で重複防止）。高速で検出より先に衝突したら従来の `OnHitBlock` 復元がフォールバックし、当該ブロックを `pierceIgnored` 登録して二重ダメージを防ぐ。Pierce 終了/`PrepareRespawn` で `RestorePierceCollisions()` が解除
- 速度の4層管理: `naturalSpeed`（基本速度 + 時間加速）× `speedMultiplier`（アイテム効果）× `slowZoneMul`（ZoneSlow）× **属性速度係数**（Heavy=`heavySpeedFactor`(0.7)・他1.0, DESIGN 5.2「速度0.7倍」, 2026-06-03） = 実効速度。共通計算は `EffectiveSpeed()` / `AttributeSpeedFactor()` に集約（FixedUpdate 正規化・衝突角度補正・`GetImpactFrames` で共用）
- `slowZoneMul`: ZoneSlow が毎フレーム書き込む public フィールド。ZoneSlow が OnDestroy / 検出失敗時に 1 に戻す。PrepareRespawn でもリセット
- `FixedUpdate` で毎フレーム実効速度に正規化。時間加速はメインボールのみ（`isExtraBall=false`）。`arenaDwellTime` はリスポーンでリセット
- `OnCollisionEnter` で衝突直後に角度補正（`ClampAngle`）→ 壁沿いループ防止
- `OnCollisionEnter` で壁バウンス検出（Block / PlayerController が GetComponent で見つからない衝突 = 壁）。`GetHitStopMultiplier()` が 0 より大なら `TriggerHitStop(wallBounceFrames * mul, shake:true)`
- `lastVelocity` は `FixedUpdate` でのみ更新（Pierce 属性の貫通処理が衝突前速度を復元するために使用）
- `Launch()`: `transform.parent.TransformDirection()` でローカル→ワールド変換
- ボール GameObject に `"BallTag"` Unity タグが必須（`Block` / `DeadZone` どちらも `CompareTag("BallTag")` で判定）
- `PrepareRespawn(localPos)`: コライダー無効化 + `IsWaitingToLaunch=true`。コルーチン停止・速度状態リセット + **角速度/回転(localRotation)もリセット**（ラウンド遷移で残らない）
- **Ball Heat**（`Update()`, DESIGN.md 5.3）: 属性 Normal のときコンボ段階でボール色を 白→クリーム→橙→赤 に Lerp（`GetHeatColor`）。属性付与中は属性カラー優先。`unscaledDeltaTime` 駆動で HitStop 中も継続。**トレイルも追従**（`SetTrailColor` 共通化＋Gradient キャッシュ再利用で GC 回避）。Renderer は `cachedRenderer` にキャッシュ
- **トレイル可視制御**: `SetTrailVisible(visible, clear)` ヘルパ（PrepareRespawn/LaunchInDirection で使用＝ボールがテレポートする場面では履歴 Clear して旧位置との線を消す）。`Start()` は `AddComponent` 前に `GetComponent<TrailRenderer>()` を試行（二重生成防止）
  - **Freeze/Unfreeze は履歴を Clear しない**: ボールは ShakeRoot の外（Arena 直下）でシェイクに動かされないため履歴は裂けない。フリーズ中は `trail.emitting=false`（描画は継続）、Unfreeze で `emitting=!IsWaitingToLaunch` に戻すだけ（Clear すると HYPER 等の頻繁なヒットストップでトレイルが消える）。各フリーズ（実時間 frames/60 秒、timeScale は 0 にしない）ぶんは `trailTime`(0.18s) で経年退色するので頻繁だと短くはなる（消えはしない）
- `LaunchInDirection(localDir)`: コライダー再有効化 + 発射。LaunchAimer から呼ばれる
- **`GetImpactFrames()`（手応え, DESIGN.md 5.2）**: ブロック衝突の停止フレーム数を **速度×攻撃力** で算出。`impact = speedTerm × GetAttributeMultiplier()`、`speedTerm = 1 + impactSpeedWeight×(実効速度/baseSpeed − 1)`（実効速度 = naturalSpeed×speedMultiplier×slowZoneMul）。`impact < impactThreshold` は 0（軽い当たりは止めずテンポ維持）、以上は `clamp(round(impactBaseFrames×impact), 1, impactMaxFrames)`。Pierce は 0。**Block の通常衝突・Explosive 破壊はこれを使う**（旧 per-type frames は撤去）。「速い/強属性ほど手応え」を一本化
- `GetHitStopMultiplier()`: `naturalSpeed/baseSpeed` が `hitStopSpeedThreshold` 未満なら 0、以上なら 0→1 にスケール。**壁バウンス・パドル反射**（攻撃力概念が無い面）のフレーム数に乗算する
- `GetAttributeMultiplier()`: 属性倍率＝**手応えの攻撃力重み**（Normal1.0 / Ice・Fire1.2 / Thunder1.1 / Heavy3.0 / Pierce0）。`GetImpactFrames` から使われる
- **`ShouldFreezeOnImpact()`（2026-06-05, DESIGN.md 5.2/5.6）**: ブロック衝突でフリーズすべきか。実効速度/baseSpeed が `freezeSkipSpeedFactor`(=2.5) 倍以上なら false＝シェイクのみ（HYPER 等の高速時に止まらず爽快＋トレイル維持）。0 以下で機能無効（常にフリーズ）。Block の両ヒットストップ呼び出しが `freeze:` に渡す
- `SetAttributeTemporary(attr, duration)`: アイテム効果で属性を一時変更（コルーチン、重ね掛け上書き）
- `SetSpeedTemporary(multiplier, duration)`: アイテム効果でボール速度を一時変更（`speedMultiplier` コルーチン、重ね掛け上書き）
- `SetScaleTemporary(multiplier, duration)`: GIANT スキルでボールを一時巨大化（`baseScale × multiplier`、コルーチン）。`baseScale` は Start でキャプチャ、`PrepareRespawn` で復元。Pierce 検出半径は `bounds.extents` 由来なので巨大化で薙ぎ払い幅も自動拡大（2026-06-05）
- 境界チェック: `FixedUpdate` でアリーナ外に出た場合、メインボールはペナルティなしリスポーン、追加ボールは Destroy

### `PlayerController.cs`
- `rb.isKinematic = true` + `transform.localPosition` 直接操作
- 1P: A/D（または矢印キー）、2P: J/L
- **移動可能なのは `Playing` と `Countdown` のみ**（DESIGN.md 12.12）。Countdown は `timeScale=0` なので `unscaledDeltaTime` で移動（パドルのポジショニング許可）。それ以外の状態（Title/SkillSelect/結果等）は移動不可
- `SetWidthTemporary(multiplier, duration)`: アイテム効果でパドル幅を一時変更（`localScale.x` 変更、コルーチン）
- `SetSpeedTemporary(multiplier, duration)`: BuffPaddle_SpeedUp でパドル移動速度を一時変更（`speed = baseMoveSpeed × multiplier`、コルーチン。`baseMoveSpeed` は Start で ApplySharedConfig 後にキャプチャ。ResetState で復元）
- `SetInputReversedTemporary(duration)`: 左右入力反転（TrapBall_Reversed）
- `ResetState()`: ラウンド遷移時に幅・入力反転・フラッシュコルーチンを全停止し、スケール/色を初期値へ復元 + **パドル位置を中央(x=0)へ復帰**（`ArenaController.ResetForNewRound` から呼ばれる）

### `DeadZone.cs`
- `ballSpawnOffsetY` と PlayerController.localPosition.y から動的にリスポーン位置を算出
- ArenaController.ballSpawnOffsetY と同じ値にすること

### `ZonePoison.cs`
- Phase E で新設。InterferencePoison（AttackPoison 取得）で生成される毒エリア
- `Setup(playerIndex, targetWorldY)` でパドル Y とオーナー設定 → 落下して着地後 `duration` 秒間持続
- 着地後は `OverlapSphereNonAlloc`（事前確保バッファ）でパドル接触を毎フレーム検出し `GameManager.OnPoisonTick(playerIndex, deltaTime)` を呼ぶ
- **毒ダメージは端数累積方式**（`GameManager.OnPoisonTick`, codex レビュー fix 2026-06-02）: 毎tick `RoundToInt` だと端数が消失/過剰になるため、`p1/p2PoisonDamageRemainder` に小数を累積し `FloorToInt` で整数ダメージを適用、余りを次tickへ繰り越す。ラウンド/マッチ開始時に `ResetPoisonDamageRemainders()` でリセット
- `Destroy(gameObject, duration)` で自動消滅。`ArenaController.ResetForNewRound()` でも即時削除

### `ZoneSlow.cs`
- Phase E で新設。InterferenceSlow で生成されるボール減速エリア
- `Setup(targetWorldY)` でアリーナ中央付近の着地 Y を設定 → 落下して着地後 `duration` 秒間持続
- 着地後は `OverlapSphereNonAlloc` でボール検出。内部ボールに `ball.slowZoneMul = slowFactor` を毎フレーム設定
- 前フレームで減速したボールをフレーム先頭でリセット → ゾーン離脱を自動検出
- `OnDestroy()` で `slowZoneMul` を確実に 1 に戻す（ResetForNewRound による即時破棄対応）

### `Block.cs`
- `BlockType` enum: `Normal`（1撃）/ `Hard`（複数撃）/ `Absorb`（当たると`absorbSpeedMultiplier`倍に減速）/ `Explosive`（破壊で `explosionRadius`(=2) 内の周囲ブロックに `explosionDamage`(=1) の**巻き込みダメージ**。同 Explosive を巻き込むと**連鎖爆発**, DESIGN.md 5.4）/ `Item`（HP1・破壊で**確定**1個ドロップ, DESIGN.md 12.17）。※ Spike は現状コードに無い（旧記述削除）
- **Explosive 連鎖（遅延カスケード）**: `OnDestroyed` は巻き込みを**即時適用せず `BlockSpawner.ScheduleExplosion(pos, radius, damage, ball, explosionChainDelay)` に委譲**＝`explosionChainDelay`(=0.07s) 後に `OverlapSphere(explosionRadius)` 内の各 Block へ `TakeDamage`。巻き込まれた Explosive はその `OnDestroyed` が再び遅延スケジュール → **波が一拍ずつ外へ広がる**。`destroyed`/`IsDestroyed` で各ブロック一度だけ・爆発済みをスキップ（無限再帰なし）。コルーチンは**破壊される Block ではなく永続する `BlockSpawner`** で走る（`ClearAndRespawn` の `StopAllCoroutines` で自動キャンセル）。各 Explosive のヒットストップ/シェイクは自身が pop する瞬間に出る。スコア/コンボは各 `OnDestroyed` が個別加算。スポーナー未取得時は即時フォールバック。
  - ⚠️ **範囲 VFX は未実装**（DESIGN 5.4 243「爆発のエフェクト」/ Fire の攻撃範囲表示も同様）。挙動のみ DESIGN 準拠。実機未確認
- ブロック種別カラーを `Awake` でキャッシュした `Renderer` に `Start()` で適用（BlockSpawner が blockType を設定した後に実行される）
- **HP pip（残耐久ドット, DESIGN.md 5.4）**: HP>1（Hard/Hardened）は `BuildHpPips()` で子キューブのドットを hp 個生成、`TakeDamage` で currentHp 本に減らす。親の非一様スケール(1.3,0.5,1)をワールド換算で打ち消す。Item/Normal(HP1) は非表示。位置/サイズ/色は SerializeField
- **多重破壊ガード**: `destroyed` フラグで `OnDestroyed` を一度だけに（Destroy 遅延中の同フレーム追撃での二重カウント防止）
- `FlashImpact(color, dur)`: 妨害行着弾演出のフラッシュ（BlockSpawner から呼ばれる）
- `HardenToHp(int targetHp)`: InterferenceHarden から呼ばれる。blockType を Hard に変換し hp/currentHp を直接設定。Renderer を金色（`hardenedColor`）に更新。HP pip も再生成
- `ConvertToExplosive()`: EXPLOSION スキルから呼ばれる。blockType を Explosive に変換し hp/currentHp=1・色を `explosiveColor` に更新（HP1 なので pip 非表示）。爆発・連鎖は既存の Explosive 経路をそのまま使う（2026-06-05）
- `OnCollisionEnter` で `ball.GetDamage()` + `ball.OnHitBlock(this)` 呼び出し — ボールに `"BallTag"` Unity タグが必須
- Normal/Hard/Absorb 衝突時: `ball.GetImpactFrames()`（速度×攻撃力, BallScript 節参照）で停止。0 ならヒットストップ無し（軽い当たり）。**`freeze:` に `ball.ShouldFreezeOnImpact()` を渡す**＝高速時はシェイクのみ
- Explosive 破壊時: `Mathf.Max(ball.GetImpactFrames(), explosiveHitFrames)` で停止（手応えが下限未満でも `explosiveHitFrames`(=6) を保証）。`explosiveHitFrames` は `ArenaSharedConfig` で一元調整。こちらも高速時は `freeze:false`
- `blockType` / `hp` はパブリックフィールド。`BlockSpawner` が `Instantiate` 後に直接代入して種類・HP を設定する
- `GetArena()`: `transform.parent?.parent?.GetComponentInChildren<ArenaController>()` — Block → BlockSpawner → Arena root の順で辿る
- 破壊時に `TryDropItem()` を呼ぶ。通常は確率ドロップ、**`BlockType.Item` は確定ドロップ**（確率/スロット抑制をスキップ）

### `EffectDefinition.cs`
- アイテム・スキル効果の抽象基底クラス（`Apply(playerIndex, arena)` メソッド）
- 実装クラス: `EffectBallAttribute` / `EffectPaddleScale` / `EffectBallSpeed`（Hyper 用・ボール速度）/ `EffectPaddleSpeed`（SpeedUp 用・パドル移動速度, DESIGN.md 5.5 BuffPaddle_SpeedUp）/ `EffectHeal` / `EffectAttack`（妨害送付）/ `EffectInputReverse`（TrapBall_Reversed）

### `ItemDrop.cs`
- `ItemType` enum（全15種）: **Buff(属性)** `Fire / Ice / Thunder / Heavy / Pierce` / **Buff(パドル・回復)** `Enlarge / SpeedUp / Heal` / **Attack(相手へ妨害送付)** `AttackHarden / AttackAddRow / AttackPoison / AttackSlow` / **Trap(取得回避が戦略)** `Shrink / Hyper / Reversed`
- `ItemDefinition` static クラス: `GetColor(type)` / `GetName(type)` を提供
- `ItemDrop` MonoBehaviour: `Setup()` で初期化、`Update()` で落下 + `Physics.OverlapSphereNonAlloc`（事前確保 `_overlapBuffer`）によるパドル接触判定
- kinematic-kinematic 間の OnTriggerEnter は発火しないため、毎フレーム OverlapSphere でパドルを検出（毎フレーム実行のため GC を避け NonAlloc 方式＝ZonePoison/ZoneSlow と統一）
- アイテムは AddComponent で生成（Prefab なし）。public フィールドの値がそのまま使われる
- `ArenaController.SpawnItem(worldPos, type)` から生成。底 Y を超えたら自動 Destroy
- パドル接触で `BuildEffect().Apply()` と同時に `GameManager.RegisterActiveItem(playerIndex, slot, name, duration)` を呼ぶ。`slot` は `ItemDefinition.GetEffectSlot()`、duration は `GetActiveDuration()`（Heal/Attack は slot=None・duration=0 で登録されない）

### `SkillController.cs`
- ArenaController.Awake() で自動生成・Initialize される
- エナジーゲージを管理。スキルキー（1P: Q / 2P: U）でスキル発動
- `maxEnergy` を SerializeField で保持（ゲージの蓄積上限。各スキルの必要量はこれ以下に配分）
- **発動判定はスキルごとの必要ゲージ量基準**（2026-06-05 刷新）: `IsReady`＝`energy.Energy >= equippedSkill.EnergyCost`。発動で `energy.Consume(EnergyCost)`（旧 `ConsumeAll` は廃止）。`EnergyRatio`＝`Clamp01(energy / EnergyCost)`（＝装備スキルの必要量に対する充填率。安いスキルほど早く満タン表示）。旧 `PanicReady`（HP 帯限定スキル）は撤去
- `EquippedSkillId`（`SkillId?`、未装備で null）を公開 — UIManager がアイコン選択に使う（`GameManager.GetEquippedSkillId` 経由）
- **発動後ゲージ回復ロックアウト（2026-06-06）**: 発動時に `chargeLockUntil = Time.time + chargeLockSeconds`(既定10秒) をセット。`AddEnergy` はロック中（`Time.time < chargeLockUntil`）は加算を無視＝ブロックを壊してもゲージが溜まらない。`ResetEnergy`（ラウンド跨ぎ）でロック解除。`Playing` 中 timeScale=1 のため scaled `Time.time` で判定

### `SkillDefinition.cs`
- スキル効果の抽象基底クラス（`SkillDefinition`）。`EnergyCost`（発動に必要なゲージ量）と `Id`（`SkillId` enum: Hyper=0/Explosion=1/Burst=2/Giant=3。UI のアイコン配列引きに使う・`AllSkills` と同順）を持つ
- **実装（DESIGN.md 5.6）**: `SkillHyper` / `SkillExplosion` / `SkillBurst` / `SkillGiant`。すべて自己強化/盤面有利系（攻撃送付スキルは無い）
  - `SkillHyper`（HYPER, cost6）: ボール高速化（`SetSpeedTemporary`）＋ `ArenaController.SpawnHyperFloor`（Dead Zone 付近に BoxCollider の床を生成し duration 後 Destroy）
  - `SkillExplosion`（EXPLOSION, cost8）: `BlockSpawner.ConvertRandomToExplosive(fraction=0.3)`＝盤面ブロックの3割を Explosive 化＋発動シェイク（`skillPanicHitStopFrames` を流用, freeze:false）
  - `SkillBurst`（BURST, cost10）: `ArenaController.BeginBurst(shots, interval, angle, ballLifetime)` → 自前コルーチンで `shots`(=10) 発を `interval`(=0.2s) 間隔で**自動連射**（プレイヤー操作に干渉しない）。角度は鉛直上0°基準で `±angle`(=45°) 交互。各弾はアイテム/スキル効果を持たない**プレーンボール**（`isExtraBall`、ラウンドリセットで破棄）
  - `SkillGiant`（GIANT, cost5）: `SetAttributeTemporary(Pierce)`＋新規 `BallScript.SetScaleTemporary`。Pierce 検出半径は `bounds.extents` 由来なので巨大化で薙ぎ払い幅も自動拡大
- すべて public フィールドでパラメータを保持（Profile 参照なし。`energyCost`/`duration` 等を直接編集して調整）
- 旧 4 種（`SkillPaddle_Enlarge`/`SkillBall_Attribute_Fire`/`SkillBall_Multi`/`SkillPanic_BlockClear`）と `SkillForceCatch`（CATCH & SHOOT, 2026-05-28 廃止）はコードから削除済み

### `MatchResultUI.cs`
- `_UI/_CameraSpace/_Base` にアタッチ。`GameState.MatchOver` を検出してパネル表示。`Start()` で `HidePanel()`（起動時に隠す、`panelShown` 初期 false 対策）
- フィールド（全て GameObject SetActive / TMP, null セーフ）:
  - `matchResultPanel`、勝者バナー `p1WinsBanner`/`p2WinsBanner`
  - スコア `p1ScoreText`/`p2ScoreText`、勝数 `p1RoundsWonText`/`p2RoundsWonText`
  - スタッツ `p1/p2BestComboText`（マッチ最大コンボ）/ `p1/p2BlocksText`（総破壊）/ `p1/p2InterferenceText`（被妨害）
  - WIN/LOSE タグ `p1TagWin`/`p1TagLose`/`p2TagWin`/`p2TagLose`
  - 選択状態 `rematchSelected`/`rematchUnselect`/`menuSelected`/`menuUnselect`（**色トグルでなく GameObject 切替**）
- A/D（J/L）で再戦/メニュー選択、Space 確定。**再戦→`GameManager.StartRematch()`、メニュー→`GameManager.ReturnToTitle()`（シーンはリロードしない）**

### `RoundResultUI.cs`
- ラウンド間結果（`GameState.RoundOver`・マッチ未決着）。`_Base` にアタッチ、全 null 安全。数秒後に GameManager が自動で次ラウンドへ（入力不要）
- フィールド: `panel` / ラウンド勝者バナー `p1RoundBanner`/`p2RoundBanner`（GameObject 切替）/ 勝数 `p1WinsText`($P1RoundTally)・`p2WinsText`・`tallyText`("P1 a - b P2") / 今ラウンド最大コンボ `p1/p2BestComboText`（`GetMaxComboRound`）/ `nextRoundTimeText`($NextRoundTime, `RoundIntermissionRemaining` を毎フレーム更新）

### `CountdownUI.cs`
- ラウンド開始前カウントダウン（3,2,1,GO!）。`GameState.Countdown` の間＋GO! 表示中だけ表示。`_Base` にアタッチ、null 安全
- フィールド: `countdownTexts[]`（複数の表示先＝各アリーナに同値を出せる TMP 配列）。`GameManager.CountdownLabel` が空でない間だけ各要素を表示し文字を流し込む

### `SkillSelectUI.cs`
- 試合開始前のスキル選択画面。`GameState.SkillSelect` 中に panel を表示
- **4 枚カード方式（GameObject の表示切替で選択表現）**（DESIGN.md 5.6）。1P: A/D でカード移動・S 確定 / 2P: J/L でカード移動・K 確定。
  - 選択中カード = `cardP{N}Cursors[i]` を表示（他は非表示）
  - 確定後 = `cardP{N}Cursors` を消し `cardP{N}Ready[i]` を表示
- フィールド: `panel` / `cardP1Cursors[]` / `cardP2Cursors[]` / `cardP1Ready[]` / `cardP2Ready[]`（GameObject 配列, index=`AllSkills` 並び順 0..3）/ `p1StatusText` / `p2StatusText`。**配列は未バインドでも安全に動作**（入力・確定・BeginMatch は機能）。
- `AllSkills` = `SkillHyper`(0) / `SkillExplosion`(1) / `SkillBurst`(2) / `SkillGiant`(3)（2026-06-05 刷新）。
- ⚠️ **要バインド**: `panel` / `cardP1Cursors[]` / `cardP2Cursors[]` / `cardP1Ready[]` / `cardP2Ready[]` / `p1StatusText` / `p2StatusText`

### `TitleUI.cs`
- 起動時のタイトル画面。`GameState.Title` の間 panel を表示。`_Base` にアタッチ済み
- **メニューは持たない**。Space / Enter で `GameManager.StartFromTitle()`（→ Settings → SkillSelect の流れ）。SETTINGS/QUIT 項目は無い。
- `pressToStartText` を `blinkPeriod`(1.0s) / `blinkMinAlpha`(0.15) で alpha 点滅（タイトルは timeScale=0 なので `unscaledTime` 駆動）。
- タイトル入場初フレームは入力スキップ（前画面の Space 二重消費を防止）。
- フィールド: `panel` / `pressToStartText` / `blinkPeriod` / `blinkMinAlpha`。**panel 未バインドでも Space 開始は機能**。

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
- スキル名表示: `p1SkillName`/`p2SkillName` は装備スキル名のみを表示（READY 可否は**アイコン側**で表現するため suffix は付けない。旧 ` · READY`/`PANIC READY` は撤去, 2026-06-06）
- **スキルアイコン可/不可切替**（`UpdateSkillIcon`, 2026-06-06, DESIGN.md 5.6）: `p1SkillIcon`/`p2SkillIcon`(Image) に、`GameManager.GetEquippedSkillId` で得た `SkillId` を index に `skillIconsReady[]`/`skillIconsUnavailable[]` を引いて `IsSkillReady` に応じスプライト差替。未装備（id=null）は `icon.enabled=false` で隠す。配列範囲外/null は `SpriteAt` ヘルパで吸収（null なら据え置き）
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
- `Custom/HDRUnlit` (`HDRUnlit.shader`): 3D Mesh / **不透明** SpriteRenderer 用。HDR Base Color のみのシンプル Unlit。Lit 計算なしで Bloom 発光のみ。**不透明固定（Blend One Zero）なので透過 PNG には不可**（黒四角になる）
- `Custom/HDRSprite` (`HDRSprite.shader`): **透過スプライト用** HDR Unlit。`_MainTex`(PerRendererData)＋頂点カラー＋`SrcAlpha OneMinusSrcAlpha`。テクスチャ×(頂点カラー×HDR ティント)。**落下アイテムアイコンの Bloom 発光に使用**（`ArenaController.CreateIconItem` が遅延生成・共有マテリアルを割当、マテリアル `_Color`(HDR) に `itemIconGlow` を載せて閾値超え）。⚠️ **発光は `SpriteRenderer.color` では不可**（Color32 0〜1 にクランプされ 1 超が消える）→ 必ずマテリアル `_Color` 経由。⚠️ ランタイム `Shader.Find` 生成のため、**ビルドで使うなら Always Included Shaders に追加**（Editor/Play は不要）
- `Custom/KawaseBlur` (`KawaseBlur.shader`): メニュー背景の磨りガラス（止め画ブラー）用。`BackdropBlur.cs` が使用

### `BackdropBlur.cs`
- メニュー系状態（Title/Settings/SkillSelect/RoundOver/MatchOver）の背景を**止め画＋Kawase ブラー＋状態別 darken** で磨りガラス化。常駐 GameObject にアタッチ、全 null 安全
- フィールド: `backdropImage`(全画面 RawImage) / `blurMaterial`(`Custom/KawaseBlur`) / `downsample`(2) / `iterations`(5) / 状態別 darken（`darkenTitle`0.30 / `darkenSettings`0.40 / `darkenSkillSelect`0.40 / `darkenRoundOver`0.25 / `darkenMatchOver`0.55）/ `fadeInSeconds`(0.22) / `fadeOutSeconds`(0.18)
- メニュー状態に入ると `ScreenCapture` でその瞬間を撮影しブラー、それ以外ではクリア

### フォント (`Assets/`)
- `BebasNeue-Regular.ttf` + `BebasNeue-Regular SDF.asset` — 数字表示用（HUD の HP/Score/Combo 等）
- `JetBrainsMono-{Regular,Bold,ExtraBold}.ttf` + 各 SDF Asset — ラベル・固定文言用
- `NotoSansJP-VariableFont_wght.ttf` + `NotoSansJP-VariableFont_wght SDF.asset` / `NotoSansJP-VariableFont_wght_fullJP SDF.asset` — **日本語主フォント**（パネル文言等）。アトラスは 4096 で再生成し約 37MB（アトラス解像度＋Custom Characters 数で肥大化するので注意）。旧 RocknRollOne は削除しタイトルは「DUAL BREAK」に統一
- ⚠️ TMP 既定フォール bック `LiberationSans SDF` は Latin 描画に使用（NotoSansJP は Latin グリフ未生成の構成があるため、パネルの英字は LiberationSans を指定）
- TMP Font Asset Creator で Custom Characters 指定で生成

### Editor スクリプト (`Assets/Editor/`)
- `SetupHitStop.cs`: `BurokkuKuzushi > Setup HitStop`（冪等）— 各 ArenaController の子に HitStopController GameObject を生成
- `SetupLaunchAimer.cs`: `BurokkuKuzushi > Setup LaunchAimer`（冪等）
- `SetupItemIcons.cs`: `BurokkuKuzushi > Setup Item Icons`（冪等）— `Assets/UI/item-icons/*.png` を Texture Type=Sprite に揃えて再インポートし、ファイル名→ItemType の対応で `ArenaSharedConfig.itemIcons[]` を自動結線。`item-attack-direct` は対応 ItemType 無しでスキップ（余り）
- `SetupCameraViewports.cs`: 単カメラ化前の名残（現状未使用、将来削除予定）
- `SetupUIManager.cs`: 新 UI 構造へ UIManager の SerializeField を結線する補助
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

- **Block スコアが SerializeField 未対応**: `Block.cs` の `normalScore`(10) / `hardScore`(20) / `absorbScore`(25) / `explosiveScore`(30) は Inspector から変更可能だが、Prefab に依存しているため Instantiate 後は BlockSpawner から設定されない。ハードコードと同義。種別ごとに `OnDestroyed` の switch で選択（DESIGN.md 5.x スコア表 Normal10/Hard20/Absorb25/Explosive30 準拠）。Explosive 破壊時の巻き込みで倒した各ブロックは個別に自分のスコアを加算する。
- **Recovery ファイル / 旧重複シーン**: `Assets/_Recovery/` の Unity 自動生成ファイル、および**旧重複シーン** `Assets/Scenes/SampleScene.unity`（`Scenes/` サブフォルダ側）は **`.gitignore` 済み**（codex レビューで実ファイルも削除済み, 2026-06-02）。**本物のアクティブシーン `Assets/SampleScene.unity`（Assets 直下）は通常どおり Git 追跡対象**。`.gitignore` のパターンは `Scenes/` 配下だけにマッチするので本物は除外されない。
