# CLAUDE.md

実装の現状を把握するための技術情報。

| ドキュメント | 内容 |
|---|---|
| [`docs/DESIGN.md`](./docs/DESIGN.md) | ゲーム設計仕様書（**最新仕様の真実**） |
| [`docs/IMPLEMENTATION.md`](./docs/IMPLEMENTATION.md) | **As-Built 対応表**（DESIGN ↔ 実装の差異・未実装） |
| [`docs/ROADMAP.md`](./docs/ROADMAP.md) | 開発フェーズ計画・進捗・発表逆算スケジュール |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | 実装アーキテクチャ詳細・依存関係 |
| [`docs/BALANCE.md`](./docs/BALANCE.md) | バランス哲学・パラメータ調整ガイド |
| [`docs/ASSETS.md`](./docs/ASSETS.md) | SE/BGM/ビジュアルアセット一覧 |
| [`docs/PRESENTATION.md`](./docs/PRESENTATION.md) | 発表（2026-06-12）のデモ進行・準備 |
| [`docs/LEARNING.md`](./docs/LEARNING.md) | C# / Unity 学習ロードマップ |
| 本ファイル | コード実装の現状・シーン構成・座標系・既知の問題 |

**仕様変更が必要なら、まず `docs/DESIGN.md` を更新してから実装する。**
**乖離の明示**: DESIGN.md とコードが食い違う箇所は本ファイルで `⚠️ 仕様とコードの乖離` と記す。CLAUDE.md=「現在のコード」、DESIGN.md=「目標仕様」の二層運用。

---

## プロジェクト概要

ローカル2人対戦ブロック崩しゲーム。

- **Unity 6** + URP（Universal Render Pipeline）— Unity Hub で 6.x で開く
- TextMeshPro / Unity Input System / Unity 3D Physics / Git・GitHub

ルール・システム詳細は `docs/DESIGN.md`、進捗は `docs/ROADMAP.md` を参照。

---

## Unity Editor セットアップ

新規に開いた場合：

1. `BurokkuKuzushi > Setup HitStop` — Arena1/Arena2 の子に `HitStopController` を生成（シェイク対象は ArenaController.Awake が自動バインド）
2. `BurokkuKuzushi > Setup LaunchAimer` — Arena1/Arena2 の子に `LaunchAimer` を生成し ArenaController にバインド

> ⚠️ `Setup HP UI` / `Setup MatchResult UI` / `Setup Skill Select UI` は**旧 `CenterUI` レイアウト前提で現 UI に合わない**。実行すると新 `_UI/_CameraSpace/` 構造を壊す可能性があるので使わない。新 UI は Figma レイアウトに沿って手動構築。

全メニュー操作は冪等。

---

## シーン構成

アクティブシーン: `Assets/SampleScene.unity`

```
SampleScene
├── EventSystem
├── GameManager        ← Singleton
├── Directional Light
├── Global Volume      ← URP Post Processing（Bloom 等）
├── MainCamera         ← 単 Ortho カメラ。world (0,0,-34.8), ortho size 12.1
│                        HDR ON / Post Processing ON / TAA High
├── _UI                ← トップレベル UI フォルダ（後述）
├── Arena1             ← world (-9.2, 0.66, 0)
│   ├── Ball                         ← ★ShakeRoot の外（シェイクに引きずられない）
│   ├── ArenaController
│   │   ├── HitStopController
│   │   └── LaunchAimer
│   └── ShakeRoot                    ← シェイク対象（local 0,0,0）。揺らしてよい要素だけ収める
│       ├── TopWall / LeftWall / RightWall
│       └── Player / DeadZone / BlockSpawner
└── Arena2             ← world (9.2, 0.66, 0)、Arena1 と鏡像同構成
```

> `CenterUI_Old` は削除済み（新 UI へ完全移行）。重複 UIManager/MatchResultUI/SkillSelectUI も一掃。

### カメラ構成（単カメラ Ortho 化）

- 旧: Arena 毎に Camera1/Camera2 で画面分割。新: **単一 `MainCamera`（Orthographic）**で両アリーナを横並び収納
- メリット: ポスプロ単純・UI Canvas 1 つ・Scene 編集楽
- 影響: HitStop は **`Arena{N}/ShakeRoot` を揺らす方式**（`SetShakeTarget`）。**Ball は ShakeRoot の外（Arena 直下）**。非キネマティック Rigidbody は親 Transform を揺らすと毎フレーム teleport され飛行が止まる/トレイルが裂けるため、ボールだけシェイク対象から除外。壁/パドル/DeadZone/BlockSpawner は ShakeRoot 配下で揺れる

### 主要 Inspector 値

> ⚠️ **正確な現在値は Unity Inspector で確認**。シーンのインスタンス値はコードの SerializeField デフォルトと異なる場合がある。目安: MainCamera ortho size 12.1 / far 100、PlayerController speed 16・xLimit 4.7・paddleLocalY -8、BlockSpawner blocksPerRow 6・blockWidth 1.5667・spawnY 4.5・blockDeadZoneY -4.5・descentSpeed 0.1、ArenaController/DeadZone ballSpawnOffsetY 1.3、Ball localScale 0.36、DeadZone localPos (0,-11,0)。

---

## UI Hierarchy 構成

Figma 準拠。3 つの Canvas を `_UI` 配下に階層化。

```
_UI                                    ← トップレベルフォルダ（Transform のみ）
├── _CameraSpace                       ← Screen Space - Camera Canvas（MainCamera 参照）
│   ├── _Base                          ← 背景・装飾・動かない要素
│   │   ├── Background / P1ArenaFrame / P2ArenaFrame
│   │   ├── P1BlockDeadLine / P2BlockDeadLine
│   │   └── _BloomyFrames/ (Bloom Left / Bloom Right)
│   └── _Components                    ← 機能 UI
│       ├── _TitlePanel / _SettingsPanel / _SkillSelectPanel / _MatchResultPanel  (各モーダル)
│       ├── _P1Components/             ← P1 HUD（左）
│       │   ├── P1PlayerTag / P1KeyBind / P1Separator
│       │   ├── _P1HpIndicator/ (P1HpFrame/P1HpLabel/P1HpMax 静的, $P1HpFill/$P1HpValue 動的)
│       │   ├── _P1Combo/   (P1ComboLabel/P1ComboMax 静的, $P1ComboValue 動的)
│       │   ├── _P1Score/   (P1ScoreLabel 静的, $P1ScoreValue 動的)
│       │   └── _P1ItemInfo/(P1ItemFrame/P1ItemFrameFill/P1ItemIconBg 静的, $P1ItemName/$P1ItemDuration 動的)
│       └── _P2Components/             ← P2 HUD（右、P1 のミラー）
```

各 Canvas は Scale With Screen Size / 1920x1080 / Match 0.5。

### UI 命名規則

| プレフィックス | 意味 | 例 |
|---|---|---|
| `_PascalCase` | フォルダ親（空 GameObject） | `_Base`, `_P1Components` |
| `$PascalCase` | 動的要素（コードが `.text`/`.fillAmount`/`.color` を書換） | `$P1HpValue` |
| `PascalCase` | 静的要素（配置後触らない） | `P1HpLabel` |
| `P1`/`P2` | プレイヤー番号 | `P1HpFill` |
| スペース・スラッシュ・括弧 | **禁則**（`transform.Find()` が破綻） | — |

Hierarchy を見て「コードから触る要素」が即わかり、再バインド範囲が明確になる。

### UI 連携の現状

- `_UI/_CameraSpace/_Base` が rootCanvas。`UIManager`/`MatchResultUI`/`SkillSelectUI`/`TitleUI`/`SettingsUI` はここにアタッチ
- `MatchResultUI`/`TitleUI`/`SettingsUI` は**バインド済み・実機確認済み**
- `SkillSelectUI` は `panel`/`p1StatusText`/`p2StatusText` バインド済みで機能するが、**`cardP1/P2Cursors[]`・`cardP1/P2Ready[]`（選択カーソル/Ready の GameObject 配列, SetActive 方式）は未バインド**。カードは手動配置後にバインド
- `UIManager` は新 UI に refactor 済み。SerializeField を 3 区分:
  - **[必須]** HP/Combo/Score/ActiveItem → 要バインド
  - **[任意]** Energy/Skill/Round/Status/妨害オーバーレイ → 配置後バインド。**スキル HUD（`pXEnergyFill`=`PXSkillGauge`、`pXSkillName`=`$PXSkillName`、`pXSkillIcon`=`$PXSkillIcon`＋アイコン配列）はバインド済み**
  - **[演出]** 色閾値等
- `GameManager` はアクティブ効果を **`ActiveEffect` のリスト**（スロット/名前/期限）で追跡（同 `ItemEffectSlot` は上書き、期限切れ自動除去）。`RegisterActiveItem(playerIndex, slot, name, duration)` を `ItemDrop` が呼ぶ。HUD は当面 `GetActiveItemName`/`GetActiveItemRemaining` が**末尾（最新）1 個**を返し既存 1 スロットに表示。複数同時表示は残作業（`GetActiveEffects()` で全件取得可）。`IsEffectSlotActive()` はドロップ過多抑制に使用

### 残作業（UI 連携）

`_UI/_CameraSpace/_Base` の **UIManager** で次をバインド:

| フィールド | バインド先 |
|---|---|
| `p1HpFill` | `$P1HpFill`（Image **Sliced**。HP 比率は `RectTransform.sizeDelta.x`=フル幅×ratio で削る。pivot.x=0 で右から減る。Sliced は fillAmount が効かないため width 制御） |
| `p1HpValue`/`p1ComboValue`/`p1ScoreValue` | `$P1HpValue`/`$P1ComboValue`/`$P1ScoreValue` |
| `p1ItemInfoRoot` | `_P1ItemInfo`（表示/非表示の親） |
| `p1ItemName`/`p1ItemDuration` | `$P1ItemName`/`$P1ItemDuration` |
| P2 側 | 上記の P2 ミラー |

[任意] は UI 要素を作ってからバインド（未バインドでも null セーフ）。**バインド済み**: Energy ゲージ（`PXSkillGauge` Filled Horizontal）/ Skill 名（`$PXSkillName`、名前のみ・READY suffix なし）/ スキルアイコン（`$PXSkillIcon`＋`skillIconsReady[]`/`skillIconsUnavailable[]`、`SkillId` index で可/不可スプライト差替）。**未バインド**: Round ドット/勝利数 / 試合状態テキスト（Round Over バナー）/ 妨害通知オーバーレイ / 攻撃送付ラベル（`pXSentLabel`、`SENT → P{N}: 種別`）/ コンボマイルストーン（`pXComboMilestoneOverlay`+`pXComboMilestoneLabel`、10/20/30 で `{N} COMBO!!`）/ Victory Bar（`victoryBar` Horizontal Fill、fillAmount=P1HP/(P1HP+P2HP)）/ Incoming インジケータ（`pXIncomingSlots[]` 各最大 3）/ アイテムアイコン Image。

### Bloom 演出

- URP Bloom Threshold = 1.0 想定。`UI/HDRTint`（Image 用）/ `Custom/HDRUnlit`（Sprite/Mesh 用）が [HDR] Tint Color を持ち、Intensity > 1 で発光
- `BreathPulse.cs` で HDR Intensity を Sin 波脈動

---

## アーキテクチャ・データフロー

### 中央イベントハブ GameManager

全イベントは `GameManager.Instance` 経由。各コンポーネントは直接 HP を操作せず GameManager のメソッドを呼ぶ。

```
Block.OnCollisionEnter
  → ball.GetDamage() + ball.OnHitBlock(this)
  → GameManager.RegisterBlockDestroyed(playerIndex)   ← コンボ・妨害トリガー
DeadZone.OnTriggerEnter
  → GameManager.OnBallDropped(playerIndex) → HPSystem.TakeDamage()
  → HP=0 で EndRound() → timeScale=0 or NextRoundCoroutine()
UIManager.Update()（毎フレーム）
  → GameManager.GetHP/GetScore/GetCombo/GetCurrentState をポーリング
```

### 設定方針（SerializeField 直接管理 ＋ 共通値は ArenaSharedConfig）

ScriptableObject / Profile は使わない。各コンポーネントは自分の SerializeField を持つが、**Arena1/Arena2 で同値であるべき共通値は `ArenaSharedConfig`（シーン内 MonoBehaviour 1 個）に集約**し、各コンポーネントが初期化時に読んで適用。

- **`ArenaSharedConfig`**（`Assets/ArenaSharedConfig.cs`）: シーンに 1 個。`Instance`（`FindFirstObjectByType` で解決）。`PlayerController`/`BlockSpawner`/`BallScript`/`LaunchAimer`/`SkillController`/`ArenaController`/`DeadZone` が初期化冒頭で `ApplySharedConfig()` を呼ぶ
- **null セーフ・段階移行可**: 無ければ `Instance`=null で各自の SerializeField 値で動作
- **per-arena 固有（共有しない）**: `playerIndex`、子オブジェクト参照（`ball`/`spawner`/`launchAimer`/`blockPrefab`）
- `GameManager` はシングルトンなので対象外（HP量/ダメージ/ヒットストップ等は GameManager の SerializeField）。`Block` はプレハブ共有で対象外
- `DeadZone.ballSpawnOffsetY` と `ArenaController.ballSpawnOffsetY` は共有設定で同値化

`ArenaController.arenaHalfWidth/arenaHalfHeight` は `SpawnItem()` のアイテム底面計算のみに使用。

---

## スクリプト一覧

### `ArenaSharedConfig.cs`
- Arena1/Arena2 共通チューニング値を集約するシーン内 MonoBehaviour（1 個前提）。`Instance`
- 保持: パドル（speed/xLimit/paddleLocalY/フラッシュ色）/ ブロックスポーン（行数・幅・spawnY・Escalation・各確率・HP・妨害・スライド演出）/ ボール（速度・軌道・属性ダメージ・半径・ヒットストップ倍率・属性色・Ball Heat・トレイル）/ エイマー / `maxEnergy` / `arenaHalfWidth`/`arenaHalfHeight`/`ballSpawnOffsetY` / **アイテムアイコン**（`itemIcons[]`=ItemType→Sprite＋`GetItemIcon(type)`、`itemIconWorldSize`、`itemIconGlow`=Bloom 発光量。`Setup Item Icons` で自動結線）
- **ヒットストップ/カメラシェイクの「手応え」を一元集約**: `impactBaseFrames`/`impactSpeedWeight`/`impactThreshold`/`impactMaxFrames`（ブロック衝突）/ `explosiveHitFrames`（Explosive 破壊の下限）/ `freezeSkipSpeedFactor`（この倍率超の高速時はフリーズせずシェイクのみ）/ `shakeIntensityNormal`/`shakeIntensityStrong` / `skillPanicHitStopFrames`。`HitStopController`/`Block`/`BallScript` が読む。**試合フロー系（`interferenceTriggerFrames`/`roundEndFrames`/`matchEndFrames`）は単一の `GameManager` SerializeField のまま**
- 共有しないのは `playerIndex` と各アリーナ子オブジェクト参照のみ

### `GameManager.cs`
- Singleton。`HPSystem` をプレイヤー毎に保持、`ApplyDamage()` が全ダメージの最終窓口
- `HPStateBand` も同ファイル定義。Inspector で `hpStateBands[]` を設定（空なら全倍率 1.0）。`GetCurrentBand(playerIndex)` で参照
- `WaitForSecondsRealtime` 使用（`timeScale=0` でも動作）
- `GetCombo(playerIndex)` は現在コンボ。コンボは **ブロック破壊ごと**に `RegisterBlockDestroyed` で +1（DESIGN 5.8）。Thunder/Fire で複数破壊すると一気に伸びる。同メソッドがコンボ++/タイマーリセット/マイルストーン/エナジー蓄積を担う
- ラウンド/マッチ決着のシェイクは勝者 `shake:false`、敗者 `shake:true` で区別

> **Phase F 実装状況サマリー**（詳細は各節）
> - **F-Polish**: Ball Heat / Danger Proximity / Last Stand / HP pip / AttackAddRow 着弾 / スペシャル行 / §12.12 入力制御 / コンボマイルストーン 実装済み
> - **F-Combat**: 攻撃アイテム経由モデル（`EffectAttack`→`SendInterference`、コンボ自動妨害は撤廃）/ コンボ再定義（comboTimeout 6s・落下リセット・各 Mul、**ブロック破壊ごと +1**）/ 罠アイテム（`trapDisguiseChance` で強化枠偽装・入力反転）/ Dynamic Escalation / コンボマイルストーン・SENT ラベル（UI 未バインド）/ Incoming インジケータ（FIFO3・`incomingDisplaySec`3s、UI 未バインド）/ Victory Bar（UI 未バインド）/ アイテム取得パドルフラッシュ（系統色 Buff青/Attack赤/Trap紫、`material.color`）
> - **F-Audio**: `AudioManager.cs`（シングルトン）。dB 変換（`20×log10(v/100)`）＋PlayerPrefs 音量、全 SE 配線、**種別別 break/hit SE**（未割当は Normal フォールバック・Hard のみ -2 半音・とどめは破壊音のみ）、底到達 SE、衝突 SE 50ms クールダウン、BGM クロスフェード（HP 30% 帯・5% ヒステリシス）実装済み。**残**: 音源クリップ割り当てと `Assets/Audio/MasterMixer.mixer` 作成＋Expose Param（未割当でも無音で安全）
> - **F-Title**: `GameState` 7 状態（`Title` 起動時 timeScale=0 / `Settings` / `SkillSelect` / `Countdown` 3,2,1,GO! / `Playing` / `RoundOver` / `MatchOver`）。フロー: StartFromTitle→Settings→ConfirmSettings→SkillSelect→BeginCountdown→Countdown→Playing、ReturnToTitle。RoundIntermission は作らず `RoundOver`＋`RoundIntermissionRemaining`（unscaled）で代替。TitleUI/SettingsUI は `_Base` にバインド済み
>
> **未実装の DESIGN 演出**: ボール属性 VFX(5.2) / エイマー振れ角幅・予想軌道・センター通過音(5.3) / ブロック起源オーラ N/S/O(5.4) / Explosive・Fire 範囲 VFX / ラウンド決着テキスト overlay
> **廃止仕様**（削除）: パドル反射ゾーン / 反撃ウィンドウ / AttackSpike・BlockSpike / AttackHarden 降下停止 / CATCH & SHOOT / ポーズ / チュートリアル / AI対戦 / アイテム寿命(`itemLifetime`)

### `HPSystem.cs`
- 純粋 C# クラス。API: `TakeDamage`/`Heal`/`Reset`/`SetMaxHP`。プロパティ: `CurrentHP`/`MaxHP`/`Ratio`/`IsAlive`

### `EnergySystem.cs`
- 純粋 C# クラス。`SkillController` が保持

### `IFreezable.cs`（インターフェース）
- `Freeze()`/`Unfreeze()` のみ。`BallScript`/`BlockSpawner`/`PlayerController` が実装。ヒットストップ中は各 Update/FixedUpdate を停止

### `HitStopController.cs`
- `ArenaController` の子（Setup HitStop で生成）。`RegisterFreezable(IFreezable)` で管理対象登録（ArenaController.Awake が呼ぶ）
- `TriggerHitStop(frames, strong, shake, freeze)`: 対象を freeze → **アリーナ Transform をシェイク** → unfreeze を `unscaledDeltaTime` ベースのコルーチンで制御
- **フリーズ/シェイク分離**（`freeze` 引数）: `freeze:false` で**フリーズせずシェイクのみ**。**ボール衝突以外は全て `freeze:false`**（飛行中ボールを止めない）。該当: 底到達/スライド着地/妨害受信/スキル発動/**高速ボールのブロック衝突**。フリーズするのは通常速度のボール衝突（ブロック/壁/パドル/Explosive 破壊）。実効速度が `freezeSkipSpeedFactor`(=2.5) 倍超なら高速とみなしブロック衝突も `freeze:false`（止めると爽快さ低下＋トレイル退色。`BallScript.ShouldFreezeOnImpact()` が判定）。ラウンド/マッチ決着は意図的にフリーズ（飛行中ボールが無い例外）。割り込みガードは `activeFroze` フラグで前ルーチンが shake-only なら `UnfreezeAll` を呼ばない
- **多重発火ガード**: シェイク中に再度来たら旧コルーチン停止時に `RestoreShakeTarget()` で位置を基準へ戻して再開（オフセット残り防止）。正常終了時も同メソッドを呼ぶ
- シェイク対象は **`Arena{N}/ShakeRoot`**（壁/パドル/DeadZone/BlockSpawner を収める空オブジェクト, local 0,0,0）。アリーナ毎に独立。**Ball は ShakeRoot の外**なので引きずられない。`SetShakeTarget(Transform)`（ArenaController.Awake で `ShakeRoot` を渡す。未解決なら `ArenaRoot.Find("ShakeRoot")`→ArenaRoot フォールバック）
- **アリーナ枠も同期シェイク**（`SetFrameShakeTarget(Transform)`）: `P{N}ArenaFrame`（UI 上の SpriteRenderer）を ShakeRoot と同一ワールド変位で揺らす。キャンバススケール非依存にするため world `position` をオフセット。ArenaController.Awake が `UIManager.GetArenaFrameTransform(playerIndex)` で取得（未バインドなら null セーフ）。枠色は Last Stand が別途制御し競合しない
- `strong=true` で強シェイク（ラウンド/マッチ決着）。`shakeIntensityNormal`/`shakeIntensityStrong` は `Awake` の `ApplySharedConfig` で読む
- Freeze 中はボール `linearVelocity=0`、Player は kinematic、Block は Rigidbody なし

### `ArenaController.cs`
- `arenaHalfWidth/arenaHalfHeight` は `SpawnItem()` の底面 Y 計算のみ
- `ballSpawnOffsetY` → `GetBallSpawnLocalPos()` が実行時に `cachedPlayer.localPosition.y` を読んで動的算出
- `cachedPlayer`/`cachedUIManager` を Awake でキャッシュ。`ArenaRoot` プロパティ（`parent ?? transform`）
- Awake で `hitStop.SetShakeTarget(shakeRoot)`（`shakeRoot` 未設定なら名前解決でフォールバック。カメラ参照は持たない）。`[SerializeField] shakeRoot` は未バインドでも動く
- `TriggerHitStop(frames, strong, shake, freeze)` を公開 — Block/BallScript/GameManager が呼ぶ
- `launchAimer` を Inspector でバインド → Awake で Initialize
- `GetBall()`/`GetSpawner()`/`GetSkillController()` で子コンポーネント公開
- `SpawnZonePoison(worldPos)`/`SpawnZoneSlow(worldPos)` — ゾーン生成（親 `ArenaRoot`）
- `SpawnItem(worldPos, type)` — 落下アイテム生成。**`GetItemIcon(type)` で Sprite が取れれば SpriteRenderer 製アイコン**（`CreateIconItem`、`itemIconWorldSize` でスケール、`Custom/HDRSprite` 共有マテリアル＋`_Color`(HDR)=`itemIconGlow` で発光、コライダー無し＝ピックアップは ItemDrop 側の OverlapSphere）、**取れなければ色付き球**（`CreateSphereItem`、フォールバック）。アイコンは `Assets/UI/item-icons/`＋`Setup Item Icons`
- **スキル用**: `SpawnHyperFloor(duration)`（HYPER の床。`[SerializeField] hyperFloor` バインド時は発動中だけ `SetActive(true)`＝Unity 上で調整可。未バインドなら `HyperFloor_Runtime` キューブを ShakeRoot 配下に実行時生成し duration 後 Destroy）/ `BeginBurst(shots, interval, angle, ballLifetime)`（BURST → 自前コルーチン `BurstFireRoutine` で `shots` 発を `interval` 秒間隔で自動連射。`LaunchAimer` は使わず発射操作に干渉しない。角度は鉛直上 0°基準で +angle/-angle 交互）/ `SpawnBurstBall(localDir, lifetime)`（BURST の 1 発。`ball.BaseScale` で素サイズ化＋`PrepareRespawn` で効果全リセット＝プレーンな追加ボール `isExtraBall`）
- `BurstFireRoutine`: `Playing` 中のみ進行。各発 `deg=(i%2==0)?angle:-angle`。`ResetForNewRound` で `StopCoroutine`
- 追加ボール生成時に `hitStop?.RegisterFreezable(bs)`（ヒットストップ中も止める）
- `ResetForNewRound()` がクリア/解除: メインボール再配置＋スポーナー再生成 / 追加ボール（`isExtraBall`）破棄 / HYPER 床（バインド済み `SetActive(false)`、実行時は破棄）/ 未取得アイテム破棄 / パドル一時効果解除（`PlayerController.ResetState()`）/ ZonePoison / ZoneSlow。加えて `GameManager` が `ClearActiveItems()` を BeginMatch/StartNextRound で呼ぶ

### `LaunchAimer.cs`
- `ArenaController` の子（Setup LaunchAimer で生成）。`Initialize(ball, playerIndex, arena)`
- `ball.IsWaitingToLaunch` を監視し true でメトロノーム発動。sin 波で ±`metronomeAngleRange`° を `metronomePeriodSec` 周期往復
- 1P: S / 2P: K で確定発射 → `ball.LaunchInDirection(localDir)`。**発射は `GameState.Playing` 限定**（カウントダウン中無効, 12.12）
- LineRenderer でリアルタイム発射角インジケーター（ワールド座標）
- `ResetAim()`: ラウンド遷移でメトロノーム位相を中央へリセット
- **BURST 連射モードは撤去済み**: BURST は `ArenaController` 自前の自動連射に移行。LaunchAimer は通常のメインボール発射照準のみ

### `BlockSpawner.cs`
- タイマーで行生成、毎フレーム降下、底判定
- 妨害行（`pendingSabotageRows`）をキュー管理。`IsTopClear()` 次第スポーン
- `blockDeadZoneY` を超えたブロックを削除し `GameManager.OnBlocksReachedBottom(playerIndex, count)` を通知。同時に `TriggerHitStop(..., freeze:false)` で**シェイクのみ**（飛行中ボールを止めない）。妨害行スライド着地も `freeze:false`
- `ReceiveSabotageRow()` — GameManager から呼ばれる
- `HardenRandomBlocks()` — LINQ で Normal を `hardenCount` 個選び `HardenToHp(hardenTargetHp)`
- `ConvertRandomToExplosive(float fraction)` — EXPLOSION。盤面の現在ブロック数の `fraction`（既定 0.3）をランダム選択し `ConvertToExplosive()`。盤面密度に比例
- `GetLowestBlockY()`/`GetSpawnY()`/`GetBlockDeadZoneY()` 公開 — LaunchAimer の自動発射タイマー短縮に使用
- 通常行は `explosiveBlockChance`/`hardBlockChance`/`itemBlockChance`(0.08) で種別割当
- **スペシャル行**（5.4, `specialRowChance`=0.125・妨害予約が無いとき抽選）: 全Item / 全Explosive / 歯抜け(2列スキップ) を `PickSpecialKind`→`SpawnRow(special)`。`AudioManager.PlaySpecialRow`
- **行スライドイン演出**（6.3）: `SpawnRowWithSlide(type, distance, duration, impact, special)` → `SlideInRow` コルーチン。上空（distance）から duration で滑り込む。スライド中（`slidingBlocks`）は降下対象外
  - 通常行: 控えめ（`normalSlideDistance`=1.5 / `normalSlideDuration`=0.2、impact なし）
  - 妨害行: 派手（`addRowSlideDistance`=6 / `addRowSlideDuration`=0.3、impact あり）。着地で `Block.FlashImpact`＋`addRowImpactFrames`(2) ヒットストップ＋`se_addrow_land`
  - `ClearAndRespawn`: `StopAllCoroutines`＋`slidingBlocks.Clear()`＋`pendingSabotageRows=0`＋再スポーン

### `BallScript.cs`
- `BallAttribute` enum: `Normal`/`Fire`（範囲）/`Thunder`（同種連鎖）/`Ice`（高ダメ）/`Heavy`（高ダメ・速度0.7倍・**非貫通**=通常反射, 5.2）/`Pierce`（貫通+通常ダメ+ヒットストップなし）。`OnHitBlock` の貫通（`lastVelocity` 復元）case は **Pierce のみ**
- **Pierce 素通り（軌道カクつき対策）**: Pierce 中 `FixedUpdate` で `OverlapSphereNonAlloc` でブロック検出し `Physics.IgnoreCollision` で物理反発を無効化して直進、ダメージは overlap で 1 回だけ（`pierceIgnored` HashSet で重複防止）。高速で検出より先に衝突したら従来の `OnHitBlock` 復元がフォールバックし当該ブロックを `pierceIgnored` 登録。終了/`PrepareRespawn` で `RestorePierceCollisions()`
- **速度 4 層**: `naturalSpeed`（基本+時間加速）× `speedMultiplier`（アイテム）× `slowZoneMul`（ZoneSlow）× **属性速度係数**（Heavy=`heavySpeedFactor`(0.7)・他 1.0）= 実効速度。計算は `EffectiveSpeed()`/`AttributeSpeedFactor()` に集約
- `slowZoneMul`: ZoneSlow が毎フレーム書込む public フィールド。離脱/破棄時 1 に戻す。PrepareRespawn でリセット
- `FixedUpdate` で実効速度に正規化。時間加速はメインボールのみ（`isExtraBall=false`）
- `OnCollisionEnter` で角度補正（`ClampAngle`）→ 壁沿いループ防止。壁バウンス検出（Block/PlayerController が見つからない衝突=壁）で `GetHitStopMultiplier()` が 0 超なら `TriggerHitStop(wallBounceFrames*mul, shake:true)`
- `lastVelocity` は `FixedUpdate` でのみ更新（Pierce が衝突前速度復元に使用）
- `Launch()`: `transform.parent.TransformDirection()` でローカル→ワールド変換
- ボール GameObject に `"BallTag"` Unity タグ必須（Block/DeadZone が `CompareTag` で判定）
- `PrepareRespawn(localPos)`: コライダー無効化＋`IsWaitingToLaunch=true`。コルーチン停止・速度状態リセット＋角速度/回転(localRotation)もリセット
- **Ball Heat**（`Update()`, 5.3）: 属性 Normal のときコンボ段階で 白→クリーム→橙→赤 に Lerp（`GetHeatColor`）。属性付与中は属性カラー優先。`unscaledDeltaTime` 駆動。**トレイルも追従**（`SetTrailColor`＋Gradient キャッシュ再利用で GC 回避）。Renderer は `cachedRenderer`
- **トレイル可視制御**: `SetTrailVisible(visible, clear)`（PrepareRespawn/LaunchInDirection で履歴 Clear＝テレポート場面の旧位置との線消去）。`Start()` は `AddComponent` 前に `GetComponent<TrailRenderer>()` を試行（二重生成防止）
  - **Freeze/Unfreeze は履歴 Clear しない**: ボールは ShakeRoot の外で動かされず履歴は裂けない。フリーズ中は `trail.emitting=false`、Unfreeze で `emitting=!IsWaitingToLaunch` に戻すだけ（Clear すると HYPER 等の頻繁ヒットストップでトレイル消失）
- `LaunchInDirection(localDir)`: コライダー再有効化＋発射
- **`GetImpactFrames()`（手応え, 5.2）**: ブロック衝突の停止フレーム数を **速度×攻撃力** で算出。`impact = speedTerm × GetAttributeMultiplier()`、`speedTerm = 1 + impactSpeedWeight×(実効速度/baseSpeed − 1)`。`impact < impactThreshold` は 0（軽い当たりは止めない）、以上は `clamp(round(impactBaseFrames×impact), 1, impactMaxFrames)`。Pierce は 0。Block の通常衝突・Explosive 破壊が使う
- `GetHitStopMultiplier()`: `naturalSpeed/baseSpeed` が `hitStopSpeedThreshold` 未満なら 0、以上で 0→1 スケール。壁バウンス・パドル反射のフレーム数に乗算
- `GetAttributeMultiplier()`: 属性倍率＝手応えの攻撃力重み（Normal1.0/Ice・Fire1.2/Thunder1.1/Heavy3.0/Pierce0）
- **`ShouldFreezeOnImpact()`（5.2/5.6）**: 実効速度/baseSpeed が `freezeSkipSpeedFactor`(=2.5) 倍以上なら false＝シェイクのみ。0 以下で機能無効（常にフリーズ）。Block の両ヒットストップ呼び出しが `freeze:` に渡す
- `SetAttributeTemporary(attr, duration)` / `SetSpeedTemporary(multiplier, duration)`（Hyper 用）: アイテム/スキルで属性・速度を一時変更（コルーチン、重ね掛け上書き）
- `SetScaleTemporary(multiplier, duration)`: GIANT でボール一時巨大化（`baseScale × multiplier`）。`baseScale` は Start でキャプチャ、PrepareRespawn で復元。Pierce 検出半径は `bounds.extents` 由来なので薙ぎ払い幅も自動拡大
- 境界チェック: `FixedUpdate` でアリーナ外なら、メインボールはペナルティなしリスポーン、追加ボールは Destroy

### `PlayerController.cs`
- `rb.isKinematic=true`＋`transform.localPosition` 直接操作。1P: A/D（矢印）、2P: J/L
- **移動可能なのは `Playing` と `Countdown` のみ**（12.12）。Countdown は `timeScale=0` なので `unscaledDeltaTime`。他状態は移動不可
- `SetWidthTemporary` / `SetSpeedTemporary`（BuffPaddle_SpeedUp、`baseMoveSpeed` は Start で ApplySharedConfig 後キャプチャ）/ `SetInputReversedTemporary`（TrapBall_Reversed）
- `ResetState()`: 幅・入力反転・フラッシュ全停止し初期値へ復元＋パドル位置を中央(x=0)へ復帰（`ResetForNewRound` から）

### `DeadZone.cs`
- `ballSpawnOffsetY` と PlayerController.localPosition.y から動的にリスポーン位置算出。ArenaController.ballSpawnOffsetY と同値にする

### `ZonePoison.cs`
- InterferencePoison（AttackPoison 取得）で生成される毒エリア。`Setup(playerIndex, targetWorldY)` で落下→着地後 `duration` 秒持続
- 着地後 `OverlapSphereNonAlloc`（事前確保バッファ）でパドル接触を毎フレーム検出し `GameManager.OnPoisonTick(playerIndex, deltaTime)`
- **毒ダメージは端数累積方式**: 毎 tick `RoundToInt` だと端数消失/過剰になるため `pXPoisonDamageRemainder` に小数累積し `FloorToInt` で適用、余りを次 tick へ繰越。ラウンド/マッチ開始時 `ResetPoisonDamageRemainders()`
- `Destroy(gameObject, duration)` で自動消滅。`ResetForNewRound()` でも即時削除

### `ZoneSlow.cs`
- InterferenceSlow で生成されるボール減速エリア。`Setup(targetWorldY)` で落下→着地後 `duration` 秒持続
- `OverlapSphereNonAlloc` でボール検出、内部ボールに `ball.slowZoneMul = slowFactor` を毎フレーム設定。前フレーム減速分をフレーム先頭でリセット→離脱を自動検出
- `OnDestroy()` で `slowZoneMul` を 1 に戻す（即時破棄対応）

### `Block.cs`
- `BlockType` enum: `Normal`（1撃）/`Hard`（複数撃）/`Absorb`（当たると `absorbSpeedMultiplier` 倍に減速）/`Explosive`（破壊で `explosionRadius`(=2) 内に `explosionDamage`(=1) 巻き込み。同 Explosive を巻き込むと**連鎖爆発**, 5.4）/`Item`（HP1・破壊で**確定** 1 個ドロップ, 12.17）。Spike はコードに無い
- **Explosive 連鎖（遅延カスケード）**: `OnDestroyed` は巻き込みを即時適用せず `BlockSpawner.ScheduleExplosion(pos, radius, damage, ball, explosionChainDelay)` に委譲＝`explosionChainDelay`(=0.07s) 後に `OverlapSphere` 内の各 Block へ `TakeDamage`。巻き込まれた Explosive が再び遅延スケジュール → **波が一拍ずつ外へ広がる**。`destroyed`/`IsDestroyed` で各ブロック一度だけ。コルーチンは破壊される Block でなく永続する `BlockSpawner` で走る（`ClearAndRespawn` の `StopAllCoroutines` で自動キャンセル）。スコア/コンボは各 `OnDestroyed` が個別加算。スポーナー未取得時は即時フォールバック
  - ⚠️ **範囲 VFX は未実装**（5.4 / Fire の攻撃範囲表示も）。挙動のみ DESIGN 準拠
- ブロック種別カラーを Awake キャッシュの Renderer に Start で適用（BlockSpawner が blockType を設定した後に実行）
- **HP pip（残耐久ドット, 5.4）**: HP>1（Hard/Hardened）は `BuildHpPips()` で子キューブのドットを hp 個生成、`TakeDamage` で currentHp 本に減らす。親の非一様スケールをワールド換算で打ち消す。Item/Normal は非表示
- **多重破壊ガード**: `destroyed` フラグで `OnDestroyed` を一度だけ
- `FlashImpact(color, dur)`: 妨害行着弾フラッシュ
- `HardenToHp(int targetHp)`: InterferenceHarden から。blockType を Hard に変換し hp/currentHp 設定、Renderer を金色（`hardenedColor`）、HP pip 再生成
- `ConvertToExplosive()`: EXPLOSION から。blockType を Explosive・hp/currentHp=1・色を `explosiveColor`。爆発・連鎖は既存経路
- `OnCollisionEnter` で `ball.GetDamage()`＋`ball.OnHitBlock(this)`（`"BallTag"` 必須）
- Normal/Hard/Absorb 衝突: `ball.GetImpactFrames()` で停止（0 なら無し）。`freeze:` に `ball.ShouldFreezeOnImpact()` を渡す
- Explosive 破壊: `Mathf.Max(ball.GetImpactFrames(), explosiveHitFrames)` で停止（下限保証）。高速時は `freeze:false`
- `blockType`/`hp` は public フィールド。BlockSpawner が Instantiate 後に直接代入
- `GetArena()`: `transform.parent?.parent?.GetComponentInChildren<ArenaController>()`（Block→BlockSpawner→Arena root）
- 破壊時に `TryDropItem()`（通常は確率、`BlockType.Item` は確定ドロップ）

### `EffectDefinition.cs`
- アイテム・スキル効果の抽象基底（`Apply(playerIndex, arena)`）。実装: `EffectBallAttribute`/`EffectPaddleScale`/`EffectBallSpeed`（Hyper）/`EffectPaddleSpeed`（SpeedUp, 5.5）/`EffectHeal`/`EffectAttack`（妨害送付）/`EffectInputReverse`（TrapBall_Reversed）

### `ItemDrop.cs`
- `ItemType` enum（全15種）: **Buff(属性)** `Fire/Ice/Thunder/Heavy/Pierce` / **Buff(パドル・回復)** `Enlarge/SpeedUp/Heal` / **Attack(妨害送付)** `AttackHarden/AttackAddRow/AttackPoison/AttackSlow` / **Trap(取得回避が戦略)** `Shrink/Hyper/Reversed`
- `ItemDefinition` static: `GetColor(type)`/`GetName(type)`
- `ItemDrop` MonoBehaviour: `Setup()` 初期化、`Update()` で落下＋`Physics.OverlapSphereNonAlloc`（事前確保 `_overlapBuffer`）でパドル接触判定（kinematic 間は OnTriggerEnter が発火しないため毎フレーム Overlap、GC 回避で NonAlloc）
- AddComponent で生成（Prefab なし）。`SpawnItem(worldPos, type)` から。底 Y を超えたら Destroy
- パドル接触で `BuildEffect().Apply()`＋`GameManager.RegisterActiveItem(...)`。`slot`=`GetEffectSlot()`、duration=`GetActiveDuration()`（Heal/Attack は slot=None・duration=0 で登録されない）

### `SkillController.cs`
- ArenaController.Awake で自動生成・Initialize。エナジーゲージ管理。スキルキー（1P: Q / 2P: U）で発動
- `maxEnergy` を SerializeField（蓄積上限）。**発動判定はスキルごとの必要量基準**: `IsReady`＝`energy.Energy >= equippedSkill.EnergyCost`。発動で `energy.Consume(EnergyCost)`。`EnergyRatio`＝`Clamp01(energy / EnergyCost)`（安いスキルほど早く満タン表示）。旧 `PanicReady` は撤去
- `EquippedSkillId`（`SkillId?`、未装備 null）公開 — UIManager がアイコン選択に使う（`GameManager.GetEquippedSkillId` 経由）
- **発動後ゲージ回復ロックアウト**: 発動時に `chargeLockUntil = Time.time + chargeLockSeconds`(既定 10 秒)。`AddEnergy` はロック中無視＝ブロックを壊しても溜まらない。`ResetEnergy`（ラウンド跨ぎ）で解除。`Playing` 中 timeScale=1 なので scaled `Time.time` で判定

### `SkillDefinition.cs`
- スキル効果の抽象基底。`EnergyCost`（必要ゲージ量）と `Id`（`SkillId` enum: Hyper=0/Explosion=1/Burst=2/Giant=3、UI アイコン配列引きに使う・`AllSkills` と同順）を持つ
- **実装（5.6）**: すべて自己強化/盤面有利系（攻撃送付スキルは無い）
  - `SkillHyper`（cost6）: ボール高速化＋`SpawnHyperFloor`（Dead Zone 付近に BoxCollider 床、duration 後 Destroy）
  - `SkillExplosion`（cost8）: `ConvertRandomToExplosive(0.3)`＝盤面 3 割を Explosive 化＋発動シェイク（`skillPanicHitStopFrames`, freeze:false）
  - `SkillBurst`（cost10）: `BeginBurst(shots=10, interval=0.2, angle=45, ballLifetime)` → 自前コルーチンで自動連射（操作に干渉しない）。各弾はプレーンボール（`isExtraBall`、リセットで破棄）
  - `SkillGiant`（cost5）: `SetAttributeTemporary(Pierce)`＋`SetScaleTemporary`。巨大化で薙ぎ払い幅も自動拡大
- 全 public フィールドでパラメータ保持（`energyCost`/`duration` 等を直接編集）。旧 4 種＋`SkillForceCatch` は削除済み

### `MatchResultUI.cs`
- `_Base` にアタッチ。`GameState.MatchOver` を検出してパネル表示。`Start()` で `HidePanel()`
- フィールド（全 GameObject SetActive/TMP, null セーフ）: `matchResultPanel`、勝者バナー `pXWinsBanner`、スコア `pXScoreText`、勝数 `pXRoundsWonText`、スタッツ `pXBestComboText`（マッチ最大）/`pXBlocksText`（総破壊）/`pXInterferenceText`（被妨害）、WIN/LOSE タグ `pXTagWin`/`pXTagLose`、選択状態 `rematchSelected`/`rematchUnselect`/`menuSelected`/`menuUnselect`（GameObject 切替）
- A/D（J/L）で再戦/メニュー選択、Space 確定。再戦→`StartRematch()`、メニュー→`ReturnToTitle()`（シーンはリロードしない）

### `RoundResultUI.cs`
- ラウンド間結果（`GameState.RoundOver`・マッチ未決着）。`_Base` にアタッチ、全 null 安全。数秒後に自動で次ラウンド
- フィールド: `panel` / ラウンド勝者バナー `pXRoundBanner`（GameObject 切替）/ 勝数 `pXWinsText`・`tallyText`("P1 a - b P2") / 今ラウンド最大コンボ `pXBestComboText`（`GetMaxComboRound`）/ `nextRoundTimeText`（`RoundIntermissionRemaining` を毎フレーム更新）

### `CountdownUI.cs`
- ラウンド開始前カウントダウン（3,2,1,GO!）。`GameState.Countdown` 中＋GO! 表示中だけ表示。`_Base`、null 安全
- フィールド: `countdownTexts[]`（各アリーナに同値を出せる TMP 配列）。`GameManager.CountdownLabel` が空でない間だけ表示

### `SkillSelectUI.cs`
- 試合開始前のスキル選択。`GameState.SkillSelect` 中に panel 表示
- **4 枚カード方式（GameObject 切替で選択表現）**（5.6）。1P: A/D で移動・S 確定 / 2P: J/L で移動・K 確定
  - 選択中 = `cardP{N}Cursors[i]` 表示（他は非表示）。確定後 = Cursors を消し `cardP{N}Ready[i]` 表示
- フィールド: `panel`/`cardP1Cursors[]`/`cardP2Cursors[]`/`cardP1Ready[]`/`cardP2Ready[]`（index=`AllSkills` 並び 0..3）/`p1StatusText`/`p2StatusText`。配列未バインドでも安全に動作（入力・確定・BeginMatch は機能）
- `AllSkills` = `SkillHyper`(0)/`SkillExplosion`(1)/`SkillBurst`(2)/`SkillGiant`(3)
- ⚠️ **要バインド**: `panel`/`cardP1/P2Cursors[]`/`cardP1/P2Ready[]`/`p1/p2StatusText`

### `TitleUI.cs`
- 起動時タイトル。`GameState.Title` 中 panel 表示。`_Base` 済み。**メニューは持たない**。Space/Enter で `StartFromTitle()`（→Settings→SkillSelect）
- `pressToStartText` を `blinkPeriod`(1.0s)/`blinkMinAlpha`(0.15) で alpha 点滅（timeScale=0 なので `unscaledTime`）。入場初フレームは入力スキップ（前画面 Space の二重消費防止）
- フィールド: `panel`/`pressToStartText`/`blinkPeriod`/`blinkMinAlpha`。panel 未バインドでも Space 開始は機能

### `SettingsUI.cs`
- 設定（最小・**先取数のみ**, 11.3）。`_Base` 済み。`Open()`/`Close()`/`IsOpen`
- 先取数 1〜5 を A/D・←/→ で増減、`roundsValueText` に反映。Esc/Space/Enter で閉じる
- `PlayerPrefs "match.roundsToWin"` に保存、`Start()` で `SetRoundsToWin()` に適用
- ⚠️ **要バインド**: `panel`/`roundsValueText`

### `UIManager.cs`
- `_Base` Canvas にアタッチ。毎フレーム GameManager をポーリング更新。SerializeField は **[必須]/[任意]/[演出]** の 3 区分
- HP バー色: 白（≥70%）→黄（≥30%）→赤（<30%）
- アクティブアイテム: `GetActiveItemName`/`GetActiveItemRemaining` を毎フレーム参照、残り>0 のとき `pXItemInfoRoot` を SetActive(true)、`$PXItemName`/`$PXItemDuration` 更新
- HP バー本体: **Sliced のまま `RectTransform.sizeDelta.x` を HP 比率で縮める**（pivot.x=0 で右から削れる。フル幅は Start でキャッシュ）。スコア表示は内部値の **×10**
- スキル名: `pXSkillName` は装備スキル名のみ（READY 可否はアイコン側で表現。旧 suffix は撤去）
- **スキルアイコン可/不可切替**（`UpdateSkillIcon`, 5.6）: `pXSkillIcon`(Image) に `GetEquippedSkillId` の `SkillId` を index に `skillIconsReady[]`/`skillIconsUnavailable[]` を引いて `IsSkillReady` に応じ差替。未装備（id=null）は `icon.enabled=false`。範囲外/null は `SpriteAt`/`SpriteForId` ヘルパで吸収
- 任意セクションは未バインドでも null セーフ
- `ShowInterferenceOverlay(int playerIndex, string label)`: P1/P2 各画面半分を 1.5 秒赤フラッシュ（CanvasGroup alpha コルーチン）
- **Danger Proximity**（`UpdateDangerLine`, 5.4）: 最下段ブロックが `blockDeadZoneY + dangerRange(1.5)` 以内で `PX BlockDeadLine`(SpriteRenderer) を alpha 点滅（接近で周期 `dangerBlinkSlow→Fast` を**位相累積**で速める＝位相飛び対策）。底到達ペナルティで `FlashDangerLine` が白フラッシュ。死線スプライトは白で色相=`SpriteRenderer.color`/発光=material `UI/HDRTint` の `_TintColor`
- **Last Stand**（`UpdateLastStand`, 5.10）: HP ≤ `lastStandThreshold(0.10)` で `PX ArenaFrame` を**元色のまま明るさだけ周期低下**（消えかけ電球風）、HP バーを赤明滅。`Playing` 以外では非アクティブ（脱出フレームで枠を元色へ復元）
- **Combo Timer Arc**（`UpdateComboArc`, 6.2）: `pXComboArc`(Filled Image) の fillAmount に `GetComboTimerRatio`(1→0) を反映。combo0 で非表示、消滅間際は橙。**要素未配置＝未バインド**

### `BreathPulse.cs`
- Material の HDR Intensity を Sin 波脈動で Bloom Threshold をまたぐ「呼吸」演出。`SpriteRenderer`/`UI.Image` 両対応。Inspector で `minIntensity`/`maxIntensity`/`cycleSeconds`/`colorPropertyName`。Material はインスタンス化される

### シェーダー (`Assets/Shaders/`)
- `UI/HDRTint`: UI Image 用。`UI/Default` に `[HDR]` Tint Color 追加。Stencil/Clip Rect/AlphaClip 完備
- `Custom/HDRUnlit`: 3D Mesh/**不透明** SpriteRenderer 用。HDR Base Color のみのシンプル Unlit。**不透明固定（Blend One Zero）なので透過 PNG 不可**（黒四角になる）
- `Custom/HDRSprite`: **透過スプライト用** HDR Unlit。`_MainTex`(PerRendererData)＋頂点カラー＋`SrcAlpha OneMinusSrcAlpha`。**落下アイテムアイコンの Bloom 発光に使用**（`CreateIconItem` が共有マテリアル割当、`_Color`(HDR) に `itemIconGlow`）。⚠️ **発光は `SpriteRenderer.color` 不可**（Color32 0〜1 にクランプ）→必ずマテリアル `_Color` 経由。⚠️ ランタイム `Shader.Find` 生成のため**ビルドで使うなら Always Included Shaders に追加**
- `Custom/KawaseBlur`: メニュー背景の磨りガラス（止め画ブラー）用。`BackdropBlur.cs` が使用

### `BackdropBlur.cs`
- メニュー系状態（Title/Settings/SkillSelect/RoundOver/MatchOver）の背景を止め画＋Kawase ブラー＋状態別 darken で磨りガラス化。常駐、全 null 安全
- フィールド: `backdropImage`(全画面 RawImage)/`blurMaterial`/`downsample`(2)/`iterations`(5)/ 状態別 darken（Title0.30/Settings0.40/SkillSelect0.40/RoundOver0.25/MatchOver0.55）/`fadeInSeconds`(0.22)/`fadeOutSeconds`(0.18)
- メニュー状態で `ScreenCapture` で撮影しブラー、それ以外はクリア

### フォント (`Assets/`)
- `BebasNeue-Regular` + SDF — 数字（HUD の HP/Score/Combo）
- `JetBrainsMono-{Regular,Bold,ExtraBold}` + 各 SDF — ラベル・固定文言
- `NotoSansJP-VariableFont_wght` + SDF（`_fullJP SDF` 含む）— **日本語主フォント**。アトラスは 4096・約 37MB（アトラス解像度＋Custom Characters 数で肥大化注意）。旧 RocknRollOne は削除しタイトルは「DUAL BREAK」に統一
- ⚠️ TMP 既定フォールバック `LiberationSans SDF` は Latin 描画に使用（NotoSansJP は Latin グリフ未生成構成があるためパネル英字は LiberationSans 指定）

### Editor スクリプト (`Assets/Editor/`)
- `SetupHitStop.cs` / `SetupLaunchAimer.cs`: 各 ArenaController の子に生成（冪等）
- `SetupItemIcons.cs`: `Assets/UI/item-icons/*.png` を Sprite に揃え再インポートし、ファイル名→ItemType で `itemIcons[]` 自動結線（冪等）。`item-attack-direct` は対応 ItemType 無しでスキップ
- `SetupCameraViewports.cs`: 単カメラ化前の名残（未使用、将来削除）
- `SetupUIManager.cs`: 新 UI 構造へ UIManager の SerializeField を結線する補助
- `SetupHPUI.cs`/`SetupMatchResultUI.cs`/`SetupSkillSelectUI.cs`: **旧 CenterUI 前提で現 UI に不適合**。実行しない。リライト or 削除予定

---

## ローカル座標系の重要事項

**位置指定はアリーナ親オブジェクトのローカル座標で行う。**

- Arena1/Arena2 の子の `localPosition(0,0,0)` = そのアリーナ中心
- `BlockSpawner` が生成するブロックは BlockSpawner の子 → ローカル座標管理
- `PlayerController` は `transform.localPosition` で移動
- 単カメラ化後はカメラがシーン root にあるため、Arena をオフセットしてもカメラと独立
- HitStop シェイクは `ArenaRoot.localPosition` を直接揺らす（Arena1/2 はシーン直下なので localPosition = world position）

---

## 既知の問題

- **Block スコアが SerializeField 未対応**: `Block.cs` の `normalScore`(10)/`hardScore`(20)/`absorbScore`(25)/`explosiveScore`(30) は Prefab 依存のため Instantiate 後に BlockSpawner から設定されず実質ハードコード。種別ごとに `OnDestroyed` の switch で選択（DESIGN スコア表準拠）。Explosive 巻き込みで倒した各ブロックは個別に自分のスコアを加算
- **Recovery ファイル / 旧重複シーン**: `Assets/_Recovery/` の Unity 自動生成ファイル、および旧重複シーン `Assets/Scenes/SampleScene.unity`（`Scenes/` サブフォルダ側）は **`.gitignore` 済み**（実ファイルも削除済み）。**本物のアクティブシーン `Assets/SampleScene.unity`（Assets 直下）は Git 追跡対象**。`.gitignore` パターンは `Scenes/` 配下だけにマッチするので本物は除外されない
