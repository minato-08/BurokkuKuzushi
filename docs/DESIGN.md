# BurokkuKuzushi 仕様書

最終更新: 2026-05-11

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

---

## 3. ゲームフロー

```
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
   ▼
[次ラウンド or マッチ終了]
   │
   ▼
[マッチ終了画面 → 即リスタート可]
```

---

## 4. プレイヤー操作

| プレイヤー | 移動 | 発射 | スキル |
|---|---|---|---|
| 1P | A / D または ← / → | Space（Phase B 以降） | Q（Phase D 以降） |
| 2P | J / L | （未定） | （未定） |

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
- ブロック・壁・パドルに衝突して反射する。
- パドル下のデッドゾーンに到達すると落下扱い（プレイヤー HP -20）。

#### 軌道補正
- 反射後、X/Y 成分どちらかが閾値未満（デフォルト 0.2）の場合、強制的に角度を修正して壁沿いのループを防ぐ。

#### ループ脱出保険
- 一定時間（デフォルト5秒）ブロックに当たらない場合、ボールの速度に倍率（デフォルト1.1）をかける。

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

### 5.4 ブロック

#### 分類

| 起源 | 説明 |
|---|---|
| Neutral (N) | 通常スポーンで降ってくる |
| Self-generated (S) | 自分のスキル/アイテムで自陣に生成 |
| Opponent-sent (O) | 相手の干渉により自陣に発生 |

#### ビジュアル表現
- N: 標準カラー
- S: 青系オーラ
- O: 赤系オーラ、ヒビ・棘などの不穏な見た目

#### ブロック一覧

| 名前 | 起源 | 性質 | 効果 | 実装フェーズ |
|---|---|---|---|---|
| BlockNormal | N | Block | HP1。1撃で破壊 | 既存 |
| BlockHard | N | Block | HP2〜3 | 既存 |
| BlockExplosive | N | Block | 破壊で周囲ブロックに巻き込みダメージ | 既存 |
| BlockAbsorb | N | Block | 当たったボールを減速させる | 既存 |
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
| ItemPaddle_Enlarge | 有利 | パドルを 10 秒間 1.5 倍に |
| ItemPaddle_SpeedUp | 有利 | パドル移動速度を 10 秒間 増加 |
| ItemBall_Pierce | 有利 | ボールが数回ブロックを貫通 |
| ItemHeal | 有利 | HP +50 |
| ItemPaddle_Shrink | 不利 | パドルを 10 秒間 0.7 倍に |
| ItemBall_Hyperspeed | 不利 | ボール速度が 10 秒間 大幅上昇 |
| ItemView_Disturb | 不利 | 視界エフェクト 10 秒間 |

### 5.6 スキル

- 試合開始前に 1〜2 個のスキルを装備する。
- ゲージが満タンになるとキー入力 1 つで発動できる。
- 発動中の効果は一定時間で自動解除される。
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
- 自分のコンボ数が閾値（デフォルト 5 個）に達した時点で自動的に相手に妨害を送付する。
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

#### 適用イベント

| イベント | フレーム数 |
|---|---|
| BlockNormal 破壊 | なし（頻発するため除外） |
| BlockHard 破壊 | なし（頻発するため除外） |
| BlockExplosive 爆発 | 6 |
| BlockSpike 破壊 | 4 |
| パドル受け止め | 1〜2 |
| 妨害発動瞬間 | 8〜10 |
| ピンチスキル発動 | 15 |
| ラウンド決着 | 30 |
| マッチ決着 | 60 |
| 壁反射 | なし |

### 5.9 リスタート

- マッチ終了状態で Space キー押下により次のマッチを即開始する（Phase A-3 で実装予定）。
- ラウンド間の待機時間はデフォルト 2 秒。

---

## 6. UI

### 6.1 画面構成

```
+-------------------+-------------------+
|                   |                   |
|     Arena1        |     Arena2        |
|     (1P)          |     (2P)          |
|                   |                   |
+-------------------+-------------------+
            (Screen Space Overlay)
[P1: HP/Score/Combo/Wins][P2: HP/Score/Combo/Wins]
              [試合状態テキスト]
```

### 6.2 各表示要素

| 要素 | 内容 |
|---|---|
| HP テキスト | `HP {current} / {max}` |
| HP バー | 横長 Image、Fill Type = Filled。HP割合に応じてカラー変化（緑/黄/赤） |
| Score テキスト | `{score}` |
| Combo テキスト | `Combo {current} / {threshold}` |
| Wins テキスト | `Wins: {roundWins}` |
| 試合状態テキスト | ラウンド/マッチ終了時のみ表示。`Round Over!` / `P{N} WINS!` |

---

## 7. アーキテクチャ

### 7.1 GameBalanceProfile（ScriptableObject）

全パラメータを集約するアセット。`Assets/Settings/GameBalanceProfile.asset` に配置。

```
GameBalanceProfile
├── HPSettings { maxHP, ダメージ各種 }
├── HPStateBand[] { thresholdPercent, gaugeRateMul, itemDropMul, scoreMul, ... }
├── ComboSettings { interferenceTriggerCombo, ... }
├── BallSettings { speed, minAxisRatio, ダメージ・属性範囲 }
├── LaunchSettings { metronomeAngleRange, metronomePeriodSec }
├── HitStopSettings { 各イベントのフレーム数 }
└── BlockSpawnSettings { blocksPerRow, spawnInterval, descentSpeed, 各種出現率 }
```

シーンの GameManager が Profile への参照を持ち、各スクリプトは `GameManager.Instance.Profile` 経由でアクセスする。

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
    void Freeze(int frames);
    void Unfreeze();
}
```

ボール / ブロック / パドル / BlockSpawner が実装。各アリーナに 1 つ配置される `HitStopController` がこれらを統括する。

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
├── Main Camera        (Arena1専用カメラ, Viewport 0,0,0.5,1)
├── Camera2            (Arena2専用カメラ, Viewport 0.5,0,0.5,1)
├── EventSystem
├── GameManager        (Singleton, profile 参照, HPSystem 保持)
├── CenterUI           (Canvas, Screen Space Overlay)
│   ├── P1HPText / P1HPFill / P1Score / P1Combo / P1Wins
│   ├── P2HPText / P2HPFill / P2Score / P2Combo / P2Wins
│   └── 試合状態テキスト
├── Arena1
│   ├── TopWall / LeftWall / RightWall / Plane
│   ├── Ball
│   ├── Player (パドル)
│   ├── DeadZone
│   ├── BlockSpawner
│   └── ArenaController
└── Arena2  （Arena1と同構成）
```

座標は各 Arena の親 GameObject に対するローカル座標で扱う。

---

## 8. 命名規則

| 対象 | 形式 | 例 |
|---|---|---|
| クラス / アセット名 | PascalCase | `BlockNormal`, `ItemPaddle_Enlarge` |
| プレフィックス | カテゴリ → 対象 → 詳細 の順 | `SkillBall_Multi`, `InterferenceHarden` |
| 変数 | camelCase | `damageBallDrop`, `spawnInterval` |
| ファイル名 | クラス名と一致 | `BlockSpawner.cs` |

ファイル名でソートしたときに同種が綺麗に並ぶように、カテゴリを先頭に置く。

---

## 9. パラメータ一覧

実装される全パラメータは `GameBalanceProfile` アセット内に集約される。プランナーはこのアセットを編集することでバランス調整を行う。

### HPSettings
| 名前 | デフォルト | 単位 |
|---|---|---|
| maxHP | 500 | HP |
| damageBallDrop | 20 | HP |
| damageBlockReachBottom | 10 | HP |
| damageBlockSpike | 30 | HP |
| damagePoisonPerSec | 5 | HP/秒 |

### ComboSettings
| 名前 | デフォルト | 単位 |
|---|---|---|
| comboTimeoutSec | 2 | 秒 |
| interferenceTriggerCombo | 5 | 個 |

### BallSettings
| 名前 | デフォルト | 単位 |
|---|---|---|
| speed | 7 | unit/秒 |
| minAxisRatio | 0.2 | - |
| relaunchAngleSpread | 0.5 | - |
| stuckTimeoutSec | 5 | 秒 |
| stuckSpeedMul | 1.1 | - |
| normalDamage | 1 | HP |
| iceDamage | 2 | HP |
| heavyDamage | 3 | HP |
| fireRadius | 1.5 | unit |
| thunderRadius | 2.5 | unit |

### LaunchSettings
| 名前 | デフォルト | 単位 |
|---|---|---|
| metronomeAngleRange | 60 | 度 |
| metronomePeriodSec | 1.0 | 秒 |

### HitStopSettings
| 名前 | デフォルト | 単位 |
|---|---|---|
| explosiveBlockFrames | 6 | フレーム |
| spikeBlockFrames | 4 | フレーム |
| paddleBounceFrames | 1 | フレーム |
| interferenceTriggerFrames | 10 | フレーム |
| panicSkillFrames | 15 | フレーム |
| roundEndFrames | 30 | フレーム |
| matchEndFrames | 60 | フレーム |

### BlockSpawnSettings
| 名前 | デフォルト | 単位 |
|---|---|---|
| blocksPerRow | 7 | 個 |
| blockGap | 0.1 | unit |
| blockHeight | 0.7 | unit |
| spawnInterval | 5 | 秒 |
| descentSpeed | 0.3 | unit/秒 |
| explosiveBlockChance | 0.1 | - |
| hardBlockChance | 0.2 | - |
| hardBlockHp | 2 | HP |
| sabotageHardRatio | 0.5 | - |
| sabotageBlockHp | 2 | HP |

### ArenaController（シーン上のコンポーネント、Inspector）
| 名前 | デフォルト | 単位 | 意味 |
|---|---|---|---|
| arenaHalfWidth | 5 | unit | アリーナ幅の半分 |
| arenaHalfHeight | 4.5 | unit | アリーナ高さの半分 |
| paddleMargin | 0.8 | unit | パドルを下端から何 unit 上に置くか |

---

## 10. 関連ドキュメント

- 開発フェーズ・進捗管理: [`ROADMAP.md`](./ROADMAP.md)
- 実装の引継ぎ情報: [`../CLAUDE.md`](../CLAUDE.md)
