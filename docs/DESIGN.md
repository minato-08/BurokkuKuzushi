# BurokkuKuzushi 仕様書

最終更新: 2026-05-19（UI レイアウト Figma 準拠、単カメラ化反映）

---

## 1. 概要

| 項目 | 内容 |
|---|---|
| タイトル | BurokkuKuzushi |
| ジャンル | ローカル2人対戦ブロック崩し |
| プラットフォーム | PC (Unity 6 / URP) |
| プレイ人数 | 2人（同一画面、左右スプリットスクリーン） |
| 想定試合時間 | 1試合 3〜5分 |
| 入力 | キーボード（Unity Input System） |

各プレイヤーは独立したアリーナを持ち、上から降ってくるブロックをボールとパドルで破壊する。ブロックを連続で破壊すると相手のアリーナに妨害が発生する。先に HP が 0 になった方が負け。

---

## 2. 勝利条件

- 各プレイヤーに HP がある（初期値 500）。
- 以下のイベントで HP が減少する:
  - ボールの落下
  - 自陣のブロックが底に到達
  - 妨害効果による被弾（毒エリア、棘ブロック等）
- HP が 0 になったプレイヤーが負け。
- 試合は複数ラウンド制（先取数で勝利、デフォルト 1 本先取）。
- 先取数は試合前の設定画面で変更可能（1 / 3 / 5 本から選択）。

---

## 3. ゲームフロー

```
[メニュー]
   |
   | 先取本数など試合設定
   ▼
[マッチ開始]
   │
   │ スキル装備（1〜2 個セット、Phase D 以降）
   ▼
[ラウンド開始]
   │  プレイ中:
   │   - ボール処理 / ブロック破壊（コア）
   │   - アイテム取得 / スキル発動 / 角度発射（戦術）
   │   - コンボによる妨害自動送付
   ▼
[ラウンド終了]
   │  どちらかの HP が 0
   │  → 勝者アリーナ強調演出 + 簡易リザルト表示（約2秒）
   │    リザルト: 勝者名 / 残HP / 今ラウンドのスコア
   ▼
[次ラウンド or マッチ終了]
   │
   ▼
[マッチ結果画面]
   │  再戦 or メニューへ戻る を選択
   ▼
[メニュー or 次のマッチ開始]
```

---

## 4. プレイヤー操作

| プレイヤー | 移動 | 発射 | スキル |
|---|---|---|---|
| 1P | A / D | S（Phase B 以降） | Q（Phase D 以降） |
| 2P | J / L | K | U |

> ここは実際の操作感に合わせて変更する可能性あり
---

## 5. システム仕様

### 5.1 HP

- 各プレイヤーは HP を持つ（初期値 500）。
- ラウンド開始時に最大値までリセット。
- HP が 0 になった瞬間にそのラウンドの敗者が確定する。

#### ダメージ表

| 発生源 | ダメージ |
|---|---|
| ボール落下 | 20 |
| ブロック1個 底到達 | 10 |
| 棘ブロック接触 | 30 |
| 毒エリア滞在 | 5 / 秒 |
| 上部攻撃被弾（Phase G+） | 40 |

> **TBD**: ブロックが多数同時に底に到達した場合のダメージ計算方式は要検討。
> 現状は線形（10 × 個数）。実プレイ後、5 個以上の同時到達に対して累進的に
> ダメージを増やす（罰則強化）方式を採用するか判断する。

#### HP帯ごとの動的パラメータ

現在 HP の最大値に対する割合に応じて、以下のパラメータが切り替わる。

| HP割合 | スキルゲージ蓄積倍率 | アイテムドロップ倍率 | スコア倍率 | その他 |
|---|---|---|---|---|
| 100% 〜 70% | 1.0 | 1.0 | 1.0 | - |
| 70% 〜 30% | 1.3 | 1.2 | 1.0 | - |
| 30% 〜 10% | 1.6 | 1.5 | 1.5 | 有利アイテム偏重 |
| 10% 以下 | 1.6 | 1.5 | 1.5 | ピンチ専用スキル解禁 |

### 5.2 ボール

- 各アリーナに通常1個。スキル/アイテムで一時的に増えることがある。
- 一定速度で動き続ける（速度はバランス設定で指定）。
- アイテムの効果などにより加速することがある。
- ブロック・壁・パドルに衝突して反射する。
- パドル下のデッドゾーンに到達すると落下扱い（プレイヤー HP -20）。

#### 軌道補正
- 反射後、X/Y 成分どちらかが閾値未満（デフォルト 0.2）の場合、強制的に角度を修正して壁沿いのループを防ぐ。

#### 時間加速
- アリーナ滞在時間に比例してボール速度が徐々に上昇する（メインボールのみ）。リスポーンでリセット。
- 上限は基本速度の `timeAccelMax` 倍（デフォルト 2.0）。加速量・上限は SerializeField で設定。

#### 属性
ボールには属性を持たせることができる。属性は通常は付与されておらず、スキル/アイテム/ゲート通過で一時的に付与される。

| 属性 | 効果 |
|---|---|
| Normal | 通常（属性なし）。ダメージ1 |
| Fire | 着弾点周囲のブロックにダメージを与える |
| Thunder | 着弾点周囲の同種ブロックに連鎖ダメージ |
| Ice | ダメージ2 |
| Heavy | ダメージ3、ブロックを貫通 |

### 5.3 パドル

- 左右のみ移動可能。
- 通常時は当たったボールを自動反射する。

#### メトロノーム式発射（Phase B 以降）
- ボールリスポーン時、パドル上に角度インジケーターが表示される。
- インジケーターは一定周期（デフォルト1秒）で左右に振れる（範囲 ±60°）。
- 発射キーを押した瞬間のインジケーター角度でボールが発射される。

#### ループ対策
- キャッチ機能は不採用。以下の機構で対応する。
  - 角度補正（`ClampAngle`）: X/Y 成分を最低 0.2 以上に強制し、壁沿いのループを防ぐ。
  - 時間加速: アリーナ滞在時間に比例して速度が上昇するため、長時間ループしてもいずれ抜ける。

### 5.4 ブロック

#### 分類

| 起源 | 説明 |
|---|---|
| Neutral (N) | 通常スポーンで降ってくる |
| Self-generated (S) | 自分のスキル/アイテムで自陣に生成 |
| Opponent-sent (O) | 相手の干渉により自陣に発生 |

#### ビジュアル表現
通常の見た目に加えて、オーラにより視覚情報を追加する。

- N: 標準カラー (種類により異なる見た目)
- S: 青系オーラ
- O: 赤系オーラ、ヒビ・棘などの不穏な見た目

妨害が送付された瞬間、受け取り側のプレイヤーに「送られてきた」ことを伝える演出を入れる（ブロックのオーラ or スクリーンオーバーレイ）。Phase E で実装。

#### ブロック一覧

| 名前 | 起源 | 性質 | 効果 | 実装フェーズ |
|---|---|---|---|---|
| BlockNormal | N | Block | HP1。1撃で破壊 | 既存 |
| BlockHard | N | Block | HP2〜3。徐々にヒビが入る演出 | 既存 |
| BlockExplosive | N | Block | 破壊で周囲ブロックに巻き込みダメージ | 既存 |
| BlockAbsorb | N | Block | 当たったボールを数秒間減速させる | 既存 |
| BlockItem | N | Block | 破壊で確定アイテムドロップ | C |
| BlockSpike | O | Block | 破壊時に毒エリアを残す | E |
| BlockHardened | O | Block | 通常ブロックが硬化変換されたもの | E |
| ZonePoison | O | Zone | パドル接触で HP減少 | E |
| ZoneSlow | O | Zone | ボール減速エリア | E |
| ZoneHeal | S | Zone | ボール通過で HP微回復 | G |
| ZoneAutoClear | S | Zone | 一定時間、降下ブロックを自動破壊 | G |
| GatePower | N/S | Gate | 通過したボールに属性付与 | G |
| GateSpeed | N/S | Gate | 通過したボールに速度上昇 | G |
| GateMulti | N/S | Gate | 通過したボールを分裂させる | G |

#### スポーン仕様
- 一定間隔（デフォルト5秒）で 1 行ずつ降ってくる。
- 1行あたりのブロック数（デフォルト7）、ブロック間の隙間（デフォルト0.1）。
- 降下速度はデフォルト 0.3 unit/秒。
- 通常行の構成: 通常 / Hard（20%） / Explosive（10%）。
- 妨害行（妨害送付時）の構成: Hard / Absorb のミックス。

### 5.5 アイテム

- ブロック破壊時に確率でドロップ。
- 落下し、パドルでキャッチすると効果が発動。
- 有利・不利の両方が存在する。

#### アイテム一覧

| 名前 | 種別 | 効果 |
|---|---|---|
| ItemAttribute_Fire | 有利 | ボールを 10 秒間 Fire 属性に |
| ItemAttribute_Thunder | 有利 | ボールを 10 秒間 Thunder 属性に |
| ItemAttribute_Ice | 有利 | ボールを 10 秒間 Ice 属性に |
| ItemAttribute_Heavy | 有利 | ボールを 10 秒間 Heavy 属性に |
| ItemPaddle_Enlarge | 有利 | パドルサイズを 10 秒間 1.5 倍に |
| ItemPaddle_SpeedUp | 有利? | パドル移動速度を 10 秒間 増加 |
| ItemBall_Pierce | 有利 | ボールが数回ブロックを貫通 |
| ItemHeal | 有利 | HP +50 |
| ItemPaddle_Shrink | 不利 | パドルを 10 秒間 0.7 倍に |
| ItemBall_Hyperspeed | 不利 | ボール速度が 10 秒間 大幅上昇 |
| ItemView_Disturb | 不利 | 視界エフェクト 10 秒間 |

### 5.6 スキル

- 試合開始前に 1〜2 個のスキルを装備する。
- ゲージが満タンになるとキー入力 1 つで発動できる。
- 発動中の効果は一定時間で自動解除される。
- 発動による代償（HP 消費・移動制限・他能力低下など）はない。
- ゲージはブロック破壊・コンボで蓄積され、蓄積率は HP帯に応じて変動する（5.1参照）。

#### スキル一覧

| 名前 | 効果 |
|---|---|
| SkillPaddle_Enlarge | パドル 10 秒間 1.5 倍 |
| SkillBall_Multi | ボール +1、10 秒間 |
| SkillBall_Attribute_Fire | ボール 10 秒間 Fire 属性 |
| SkillForceCatch | 次に当たったボールを強制キャッチ |
| SkillPanic_BlockClear | 上半分のブロックを破壊。HP 1/3 以下のみ発動可 |

### 5.7 妨害

#### 発動条件
- 自分のコンボ数が閾値（デフォルト 15 個）に達した時点で自動的に相手に妨害を送付する。
- プレイヤーの操作介入はない。

#### 妨害種別

| 名前 | 効果 |
|---|---|
| InterferenceHarden | 相手アリーナの通常ブロック数個を Hard 化 |
| InterferenceSpike | 相手アリーナのランダムブロックを Spike 化 |
| InterferencePoison | 相手アリーナ下部に毒エリアを数秒間生成 |
| InterferenceSlow | 相手アリーナ中央にスローエリア生成 |
| InterferenceAddRow | 相手アリーナの既存ブロック上に 1 行追加 |
| InterferenceDirectAttack | 上部から予告型の攻撃を発生（Phase G+） |

送付される種別はランダム抽選、または送付側のスキル装備に応じて変動する。

### 5.8 ヒットストップ

特定のイベントで、該当アリーナ内のアクター（ボール / ブロック / パドル / ブロック降下処理）を指定フレーム数だけ停止させる演出。

`Time.timeScale` は使用しない（2人プレイのため、片方のアリーナだけ止める必要がある）。

#### 演出
- フリーズ中はカメラシェイクを同時に発生させる。
- シェイクするカメラはイベントによって異なる（通常は発生アリーナのみ、ラウンド/マッチ決着は両方）。

#### 適用イベントと対象

| イベント | フレーム数 | 停止対象アリーナ | カメラシェイク |
|---|---|---|---|
| BlockNormal / Hard / Absorb 衝突 | 0（SerializeField で設定可） | 発生側 | 発生側のみ |
| BlockExplosive 爆発 | 6（SerializeField） | 発生側 | 発生側のみ |
| BlockSpike 破壊 | 4 | 発生側 | 発生側のみ |
| ブロック底到達 | 5（SerializeField） | 発生側 | 発生側のみ |
| パドル受け止め | 0（SerializeField で設定可） | 発生側 | 発生側のみ |
| 壁反射 | 0（SerializeField で設定可） | 発生側 | 発生側のみ |
| 妨害発動瞬間 | 10（SerializeField） | 発生側 | 発生側のみ |
| ピンチスキル発動 | 15 | 発生側 | 発生側のみ |
| ラウンド決着 | 30 | 両方 | 敗者側のみ |
| マッチ決着 | 60 | 両方 | 敗者側のみ |

**速度閾値ゲート**: ブロック衝突・壁反射のヒットストップは `naturalSpeed / baseSpeed` が `hitStopSpeedThreshold`（デフォルト 1.5）を超えた場合にのみ発動する。フレーム数はその超過量に比例して 0→設定値 にスケールする。BlockExplosive の爆発演出は速度閾値によらず属性倍率のみ適用する。

### 5.9 ラウンド終了・マッチ終了

#### ラウンド終了
- HP が 0 になった瞬間にラウンド終了。
- 勝者アリーナを強調する演出を再生しつつ、簡易リザルトを表示（約2秒）。
  - 表示内容: 勝者名 / 残HP / 今ラウンドのスコア
- 2秒後、次ラウンドへ自動移行。
- ボールのリスポーン演出（落下時エフェクトなど）は Phase F でまとめて実装。

#### マッチ終了
- 先取条件を満たしたラウンドが終わるとマッチ終了。
- マッチ結果画面に遷移し、「再戦」または「メニューへ戻る」を選択できる。
- ラウンド間待機時間のデフォルト: 2 秒。

---

## 6. UI

### 6.1 画面構成（Figma 準拠）

```
┌─────────┬─────────────────┬─────────────────┬─────────┐
│  P1 HUD │   P1 Arena      │   P2 Arena      │  P2 HUD │
│         │   (左)          │   (右)          │         │
│ P1 Tag  │   ┌─────────┐   │   ┌─────────┐   │  P2 Tag │
│ Keys    │   │ ▢▢▢▢▢▢ │   │   │ ▢▢▢▢▢▢ │   │  Keys   │
│         │   │ ▢▢▢▢▢▢ │   │   │ ▢▢▢▢▢▢ │   │         │
│ COMBO   │   │         │   │   │         │   │ COMBO   │
│  10/15  │   │   ●     │   │   │    ●    │   │  10/15  │
│         │   │  ━━━━   │   │   │  ━━━━   │   │         │
│ SCORE   │   │ ─────   │   │   │ ─────   │   │ SCORE   │
│ 12,340  │   │   ▬     │   │   │   ▬     │   │ 12,340  │
│         │   └─────────┘   │   └─────────┘   │         │
│ ITEM    │                 │                 │ ITEM    │
│ SKILL Q │                 │                 │ SKILL U │
└─────────┴─────────────────┴─────────────────┴─────────┘

中央には Incoming インジケータ（受け側プレイヤーが食らう予定の妨害量を可視化）
画面上部に HP バー + ROUND 表示 + ドット式ラウンドカウンタ
```

- 各アリーナを Bloom 装飾枠で囲み（P1=青系 / P2=オレンジ系）プレイヤー識別性を高める
- HUD は左右端に配置、アリーナ表示領域を最大化
- 装飾要素・固定ラベル（操作ヒント等）は Figma で書き出した一枚絵を BG として配置、その上に動的要素を重ねる

### 6.2 各表示要素

| 要素 | 内容 |
|---|---|
| HP バー | 9-slice 角丸 Frame + 内側 Fill（RectTransform Width 制御）。HP 割合に応じてカラー変化（緑/黄/赤） |
| HP テキスト | `{current}` 大きく Bebas Neue、`/ {max}` 小さく |
| Score テキスト | `{score}` カンマ区切り（例: `12,340`）、Bebas Neue |
| Combo テキスト | `{current}` 大、`x /{threshold}` 小、Bebas Neue + JetBrainsMono |
| ラウンド表示 | 中央上部 `ROUND {N}` + 先取数分のドット（点灯/非点灯で勝利数表示） |
| Incoming インジケータ | 中央の縦長 2 枠、受ける予定の妨害が積み重なって可視化される |
| アイテム表示 | 取得中アイテムのアイコン + 名前 + 残り秒数（最後に取った1個のみ表示） |
| スキル表示 | スキル名 + キー（Q / U）+ READY 状態（ゲージ満タンで光る） |
| 試合状態テキスト | ラウンド/マッチ終了時のみ表示。`Round Over!` / `P{N} WINS!` |
| 妨害通知 | 各画面半分を 1.5 秒赤フラッシュ（CanvasGroup alpha） |

### 6.3 視覚演出

- **Bloom 演出**: アリーナ枠・READY 表示・装飾要素等は HDR カラー（Intensity > 1）で着色し、URP Bloom Threshold 越えで発光
- **Breath アニメーション**: 装飾枠は `BreathPulse` コンポーネントで HDR Intensity を Sin 波で脈動 → 生命感ある発光
- **フォント方針**: 数字は Bebas Neue（ディスプレイ系）、固定ラベルは JetBrainsMono（モノスペース）で雰囲気統一

---

## 7. アーキテクチャ

### 7.1 パラメータ管理方針

ScriptableObject / Profile は使用しない。すべてのバランスパラメータは各コンポーネントの `SerializeField` に直接持ち、Unity Inspector から調整する。

- `GameManager` — HP量・ダメージ量・ヒットストップフレーム数・コンボ閾値など
- `BallScript` — 速度・時間加速・属性ダメージ・時間加速閾値など
- `BlockSpawner` — 行生成間隔・降下速度・ブロック構成比率など
- `LaunchAimer` — メトロノーム振れ幅・周期・自動発射時間など
- `HPStateBand[]` — GameManager の Inspector 配列で HP帯ごとのパラメータを設定（空なら全倍率 1.0）

### 7.2 EffectDefinition（抽象基底クラス）

```
EffectDefinition (abstract ScriptableObject)
├── ItemDefinition       （アイテムの効果を定義）
├── SkillDefinition      （スキルの効果を定義）
└── GateEffectDefinition （ゲート通過効果を定義）
```

`Apply(GameContext)` / `Remove(GameContext)` メソッドを持ち、効果の発動と解除を共通インターフェースで扱う。

### 7.3 IFreezable インターフェース

```csharp
public interface IFreezable {
    void Freeze();
    void Unfreeze();
}
```

`BallScript` / `PlayerController` / `BlockSpawner` が実装。各アリーナの `HitStopController` が `RegisterFreezable()` で対象を登録し、ヒットストップ中は各 Update/FixedUpdate を停止する。

### 7.4 InterferencePayload

```csharp
public enum InterferenceType {
    Harden, Spike, Poison, Slow, AddRow, DirectAttack,
}
public class InterferencePayload {
    public InterferenceType type;
    public float intensity;
    public float duration;
}
```

コンボ閾値到達時にこのオブジェクトを生成し、相手アリーナに渡して効果を発動させる。

### 7.5 シーン構造

```
SampleScene
├── EventSystem
├── GameManager        (Singleton, SerializeField で全パラメータ保持)
├── Directional Light
├── Global Volume      (URP Post Processing: Bloom 等)
├── MainCamera         (単 Orthographic、world (0,0,-34.8), ortho size 12.1, HDR ON, TAA High)
├── _UI                (トップレベル UI フォルダ、3 Canvas を集約)
│   └── _CameraSpace / _Components / _P1Components / _P2Components
├── Arena1             (world (-9.2, 0.66, 0))
│   ├── TopWall / LeftWall / RightWall
│   ├── Ball / Player (パドル) / DeadZone / BlockSpawner
│   └── ArenaController
│       ├── HitStopController
│       └── LaunchAimer
└── Arena2             (world (9.2, 0.66, 0)、Arena1 と同構成)
```

- 単 Ortho カメラで両アリーナを横並びに収める（旧 Camera1/Camera2 分割描画を廃止）
- HitStop シェイクはアリーナ Transform 自体を揺らす方式（`HitStopController.SetShakeTarget(ArenaRoot)`）
- ゲーム内の位置指定（Player / Ball / Block）は引き続き各 Arena のローカル座標で管理
- UI 階層の詳細・命名規則は `CLAUDE.md` の「UI Hierarchy 構成」セクション参照

---

## 8. 命名規則

### 8.1 コード・アセット

| 対象 | 形式 | 例 |
|---|---|---|
| クラス / アセット名 | PascalCase | `BlockNormal`, `ItemPaddle_Enlarge` |
| プレフィックス | カテゴリ → 対象 → 詳細 の順 | `SkillBall_Multi`, `InterferenceHarden` |
| 変数 | camelCase | `damageBallDrop`, `spawnInterval` |
| ファイル名 | クラス名と一致 | `BlockSpawner.cs` |

ファイル名でソートしたときに同種が綺麗に並ぶように、カテゴリを先頭に置く。

### 8.2 UI Hierarchy（GameObject 名）

UI 構造を整理し、コードから触る要素を一目で識別できるようプレフィックス規則を導入。

| プレフィックス | 意味 | 例 |
|---|---|---|
| `_PascalCase` | フォルダ親（空 GameObject、組織化のため） | `_Base`, `_P1Components`, `_P1HpIndicator` |
| `$PascalCase` | 動的要素（コードが `.text` / `.fillAmount` / `.color` 等を書き換える） | `$P1HpValue`, `$P1ScoreValue` |
| `PascalCase` | 静的要素（一度配置したら触らない） | `P1HpLabel`, `P1ArenaFrame` |
| `P1` / `P2` | プレイヤー番号プレフィックス | `P1HpFill`, `P2ScoreValue` |
| **禁則** | スペース・スラッシュ・括弧（`transform.Find()` で破綻するため使わない） | — |

この規則により、Hierarchy をパッと見て「コードから触る要素」が即わかる。UIManager 等の SerializeField 再バインド時の作業範囲が明確になり、P1 → P2 ミラー化も機械的に処理できる。

---

## 9. 主要パラメータ一覧

すべてのパラメータは各コンポーネントの SerializeField で管理する。以下はコードデフォルト値。実際の調整値は Inspector で設定する。

### GameManager
| フィールド | デフォルト | 意味 |
|---|---|---|
| maxHP | 500 | HP初期値 |
| damageBallDrop | 5 | ボール落下ダメージ |
| damageBlockReachBottom | 10 | ブロック1個 底到達ダメージ |
| damageBlockSpike | 15 | 棘ブロック接触ダメージ |
| damagePoisonPerSec | 5 | 毒ダメージ/秒 |
| damageForceRespawn | 5 | S/K 強制リスポーンペナルティ |
| comboThreshold | 15 | 妨害発動コンボ数 |
| energyPerBlock | 1 | ブロック破壊あたりのゲージ増加量 |
| interferenceTriggerFrames | 10 | 妨害発動ヒットストップ（フレーム） |
| roundEndFrames | 30 | ラウンド決着ヒットストップ |
| matchEndFrames | 60 | マッチ決着ヒットストップ |
| nextRoundDelay | 2 | 次ラウンドまでの待機秒 |

### BallScript
| フィールド | デフォルト | 意味 |
|---|---|---|
| speed | 7 | 基本速度 |
| minAxisRatio | 0.2 | 軌道補正 最小軸成分比率 |
| timeAccelRate | 0.05 | 時間加速量/秒 |
| timeAccelMax | 2.0 | 時間加速上限倍率 |
| hitStopSpeedThreshold | 1.5 | ヒットストップ発動速度倍率 |
| wallBounceFrames | 0 | 壁バウンス最大ヒットストップフレーム数 |
| normalDamage / iceDamage / heavyDamage | 1 / 2 / 3 | 属性ダメージ |

### BlockSpawner
| フィールド | デフォルト | 意味 |
|---|---|---|
| blocksPerRow | 6 | 1行あたりのブロック数 |
| spawnInterval | 5 | 行生成間隔（秒） |
| descentSpeed | 0.1 | 降下速度（unit/秒） |
| blockDeadZoneY | -4.5 | ブロックが到達してはいけない Y |
| blockDeadZoneHitFrames | 5 | 底到達ヒットストップフレーム数 |

### LaunchAimer
| フィールド | デフォルト | 意味 |
|---|---|---|
| metronomeAngleRange | 60 | インジケーター振れ幅（±度） |
| metronomePeriodSec | 1.0 | 往復周期（秒） |
| autoLaunchSec | 5.0 | 自動発射までの待機秒 |
| minAutoLaunchSec | 1.5 | ブロック最下段時の最短自動発射秒 |

---

## 10. 関連ドキュメント

- 開発フェーズ・進捗管理: [`ROADMAP.md`](./ROADMAP.md)
- 実装の引継ぎ情報: [`../CLAUDE.md`](../CLAUDE.md)
