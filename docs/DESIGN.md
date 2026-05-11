# BurokkuKuzushi — ゲーム設計仕様書

最終更新: 2026-05-11

このドキュメントは「BurokkuKuzushi」（ローカル2人対戦ブロック崩しゲーム）のゲーム設計仕様の決定版である。実装の指針はここを参照し、仕様変更があった場合はここを更新する。

---

## 1. コアコンセプト

> **リスクを取ってコンボを伸ばし、相手より速く相手のフィールドを変質させる駆け引きゲーム**

- ジャンル: ローカル2人対戦ブロック崩し
- 想定プレイ時間: 1試合3〜5分
- 設計思想:
  - **「同条件耐久」にならない相互干渉**: コンボ→自動妨害送付＋アイテム＋スキルで状況が動的に変化する
  - **プレイヤー主権 + 操作シンプル**: 「やれば強いが必須ではない」レイヤーを用意し、コアは複雑化させない
  - **ピンチほど攻めれる**: HP帯ごとの動的パラメータでカムバック性を担保
  - **不確実性（≠ランダム性）**: ランダム要素は常に選択肢を生む形で導入する
  - **プランナーフレンドリー**: 全パラメータをScriptableObjectに外出しし、ハードコードを排除

---

## 2. 操作レイヤー構造

ゲームを「やらなければいけない」と「やれば強い」に分ける。

| 層 | 内容 | 自動度 |
|---|---|---|
| **コア層**（必須） | ボールを返す・ブロックを壊す | 手動 |
| **戦術層**（やれば強い） | アイテム取捨・スキル発動・メトロノーム発射 | 任意 |
| **自動層**（気にしなくていい） | 妨害送付・ボール角度補正・リスポーン・ループ脱出 | 自動 |

---

## 3. ゲームフロー

```
[マッチ開始]
   │ スキル装備（試合前 1〜2 個セット、UI 簡易）
   ▼
[ラウンド開始] ← 演出は短く、即操作可
   │
   ├─ コア: ボール処理・ブロック破壊
   ├─ 戦術: アイテム取捨・スキル発動
   ├─ 自動: 妨害送付・カムバック発動
   │
   ▼
[ラウンド決着] ← ボスストップ風演出、1秒以内に次へ
   │
   ├─ HP残ありなら次ラウンド
   └─ HP0 ならマッチ終了
   │
   ▼
[マッチ終了] ← 1ボタンで即リスタート
```

---

## 4. システム仕様

### 4.1 HP / ダメージ

- `maxHP = 500`（粒度を持たせ、様々なダメージを混在可能に）
- ダメージソース:
  | イベント | ダメージ | 備考 |
  |---|---|---|
  | ボール落下 | 20 | 軽く惜しい |
  | ブロック1個底到達 | 10 | チリも積もれば |
  | ブロック多数同時底到達 | スタック | 5個以上で罰則強化 |
  | 棘ブロック接触 | 30 | 設置型妨害 |
  | 毒エリア滞在 | 5/秒 | 残留型 |
  | 上部攻撃被弾（将来） | 40 | 予告ありの避け要素 |

### 4.2 HP帯ごとの動的パラメータ（カムバック機構）

`HPStateBand` を ScriptableObject で複数定義し、現在HPに応じて参照プロファイルを切り替える。

| HP帯 | 効果 |
|---|---|
| 100% 〜 70% | デフォルト |
| 70% 〜 30% | スキルゲージ蓄積 ×1.3、アイテムドロップ ×1.2 |
| 30% 〜 10% | スキルゲージ ×1.6、アイテムドロップ ×1.5、良アイテム偏重、スコア倍率 ×1.5 |
| 10% 以下 | ピンチBGM、ピンチ専用スキル解禁 |

数値はプロファイルアセットでInspector調整可能。

### 4.3 ボール

- 通常は1個。スキル/アイテムで一時的に増えることはある
- 属性 (`BallAttribute`): Normal / Fire / Thunder / Ice / Heavy
  - 属性は **常時付与でなく**、スキル/アイテム/ゲート通過で一時的に発動する設計
- 軌道補正（既存）: 壁沿いのループ防止
- **ループ脱出保険**: 5秒間ブロックに当たらないと速度+10%（爆走化）

### 4.4 パドル

- 横移動のみ、ローカル座標で動作
- 通常時は当たったボールを自動反射
- **メトロノーム式発射**（初回発射時に確定）:
  - リスポーン後、角度インジケーターが左右にオシレーション
  - 専用キー入力でその角度に発射
  - キャッチ機能（試合中も保持可能）の実装は様子見

### 4.5 ブロック種類

#### ブロック分類軸
- **起源軸**: N (Neutral, 通常スポーン) / S (Self-generated, バフ) / O (Opponent-sent, 妨害)
- **性質軸**: Block（破壊可） / Zone（床エリア） / Gate（通過効果）

#### ビジュアル区別ルール
- N: 標準カラー
- S: 青系オーラ
- O: 赤系オーラ、ヒビ/棘などの不穏な見た目

#### 実装カタログ

| 名前 | 起源 | 性質 | 効果 | 実装フェーズ |
|---|---|---|---|---|
| `BlockNormal` | N | Block | HP1 | 既存 |
| `BlockHard` | N | Block | HP2-3 | 既存 |
| `BlockExplosive` | N | Block | 隣接破壊 | 既存 |
| `BlockAbsorb` | N | Block | スコア吸収 | 既存 |
| `BlockItem` | N | Block | 確定アイテムドロップ | C |
| `BlockSpike` | O | Block | 破壊時に毒エリアを残す | E |
| `BlockHardened` | O | Block | 通常→硬化変換 | E |
| `ZonePoison` | O | Zone | パドル接触でダメージ | E |
| `ZoneSlow` | O | Zone | ボール減速 | E |
| `ZoneHeal` | S | Zone | ボール通過で微回復 | G |
| `ZoneAutoClear` | S | Zone | 一定時間ブロック自動破壊 | G |
| `GatePower` | N or S | Gate | 通過で属性付与 | G |
| `GateSpeed` | N or S | Gate | 通過で加速 | G |
| `GateMulti` | N or S | Gate | 通過でボール分裂 | G |

### 4.6 アイテム

- ブロック破壊時に確率ドロップ
- 落下してパドルでキャッチ
- 不利アイテムも混在 → 「取るか避けるか」の判断要素
- 効果はすべて `ItemDefinition`（EffectDefinitionの具象）として定義

#### 有利系
| 名前 | 効果 |
|---|---|
| `ItemAttribute_Fire` | ボール属性=炎、10秒 |
| `ItemAttribute_Ice` | ボール属性=氷、10秒 |
| `ItemAttribute_Thunder` | ボール属性=雷、10秒 |
| `ItemAttribute_Heavy` | ボール属性=重、10秒 |
| `ItemPaddle_Enlarge` | パドル1.5倍、10秒 |
| `ItemPaddle_SpeedUp` | パドル加速、10秒 |
| `ItemBall_Pierce` | ボール貫通、数回 |
| `ItemHeal` | HP +50 |

#### 不利系
| 名前 | 効果 |
|---|---|
| `ItemPaddle_Shrink` | パドル0.7倍、10秒 |
| `ItemBall_Hyperspeed` | ボール超加速、10秒 |
| `ItemView_Disturb` | 視界エフェクト、10秒 |

### 4.7 スキル（装備制・自己強化型）

- 試合前に1〜2個装備
- エナジー満タンで**キー1つで発動**
- **代償なし**（シンプルに発動するだけ。リスクは発動タイミングの選択にある）

#### 仕様
| 名前 | 効果 |
|---|---|
| `SkillPaddle_Enlarge` | パドル1.5倍、10秒 |
| `SkillBall_Multi` | ボール+1、10秒 |
| `SkillBall_Attribute_Fire` | 炎属性、10秒 |
| `SkillForceCatch` | 次のボール強制キャッチ |
| `SkillPanic_BlockClear` | 上半分のブロック破壊（HP1/3以下のみ発動可、ピンチ専用） |

エナジー蓄積はブロック破壊・コンボで増える。HP帯に応じてゲージ蓄積率が変動（カムバック）。

### 4.8 妨害（変化中心・自動送付）

- コンボ閾値到達で **自動送付**（プレイヤー操作なし、コア層の集中を維持）
- 「変化させる」が主軸、ブロック追加は弱保険
- `InterferencePayload` で種類・強度・継続時間を汎用化

#### 種類
| 名前 | 効果 |
|---|---|
| `InterferenceHarden` | 既存Normalブロック数個をHard化 |
| `InterferenceSpike` | ランダムブロックをSpike化 |
| `InterferencePoison` | 盤面下部に毒エリアを数秒生成 |
| `InterferenceSlow` | 盤面中央にスローエリア生成 |
| `InterferenceAddRow` | 1段だけ既存ブロックの上に追加（保険） |
| `InterferenceDirectAttack` | 上部攻撃（将来枠） |

送付の種別はランダム抽選 or 自分のスキル装備に応じて変動。

### 4.9 演出（ヒットストップ）

桜井政博氏が強調する「手応えの基盤」。Phase A-2 で基盤を構築する。

#### 実装ルール
- `IFreezable` インターフェース（Ball / Block / Paddle / BlockSpawner が実装）
- `HitStopController`（各 ArenaController に1つ）が司令塔
- アクター個別停止（`Time.timeScale` は使わない: 2P対戦のため）

#### 適用場面
| イベント | フレーム数 | 備考 |
|---|---|---|
| Normalブロック破壊 | **なし** | 頻発するため除外 |
| Hardブロック破壊 | **なし** | 頻発するため除外 |
| Explosiveブロック爆発 | 6f | 連鎖の頂点で手応え |
| Spikeブロック破壊 | 4f | 妨害破壊時の手応え |
| パドルで受け止め | 1〜2f | ごく軽い手応え |
| 妨害発動瞬間 | 8〜10f | 派手なフラッシュ＋振動 |
| ピンチスキル発動 | 15f | ボスストップ風 |
| ラウンド決着 | 30〜60f | 完全な決着演出 |
| マッチ決着 | 60f以上 | フィニッシュ演出 |
| 壁反射 | **なし** | ボール挙動がガタつくため |

数値はすべて `HitStopSettings`（GameBalanceProfile内）で調整可能。

### 4.10 リスタート機構

- マッチ終了後、Space キー等の1ボタンで `StartNewMatch()` を呼ぶ
- 演出は短く（1秒以内に操作復帰）
- 「冷める前にバトンを渡す」原則

---

## 5. アーキテクチャ

### 5.1 GameBalanceProfile（中核ScriptableObject）

```
GameBalanceProfile
├── HPSettings { maxHP, ダメージ各種 }
├── HPStateBand[] { threshold, gaugeMul, itemDropMul, scoreMul, ... }
├── ComboSettings { threshold, interferenceTriggerValue }
├── ItemDropTable { ItemDefinition[] with weight }
├── BlockSpawnTable { BlockDefinition[] with weight, spawnInterval, descentSpeed, ... }
├── SkillCatalog { SkillDefinition[] }
├── InterferenceSettings { 妨害方式の重み・パラメータ }
├── BallSettings { speed, minAxisRatio, hyperspeedTimeout, ... }
├── LaunchSettings { metronome 振れ幅・周期 }
└── HitStopSettings { 各イベントごとのフレーム数 }
```

シーンに `GameBalanceProfile` の参照を1つ持たせ、`GameManager` 経由でアクセス。差し替えるだけでバランスを総入れ替えできる。

### 5.2 EffectDefinition（効果の共通基底）

```csharp
public abstract class EffectDefinition : ScriptableObject {
    public string displayName;
    public Sprite icon;
    public float duration;
    public abstract void Apply(GameContext ctx);
    public abstract void Remove(GameContext ctx);
}
```

- `ItemDefinition` / `SkillDefinition` / `GateEffectDefinition` が継承
- 「パドル拡大」効果が Item から来ても Skill から来ても Gate から来ても **同じコードで動く**
- 拡張性が高い（後付けで Gate を追加するときも同じ枠で済む）

### 5.3 BlockDefinition

- ブロック種を enum でなく ScriptableObject で管理（既存の `BlockType` enum から段階移行）
- 後から新ブロックを追加してもコード変更不要

### 5.4 InterferencePayload

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

- コンボで `InterferencePayload` を作って相手アリーナに渡す
- 新種類追加は enum拡張 + ハンドラ追加のみ

### 5.5 IFreezable + HitStopController

```csharp
public interface IFreezable {
    void Freeze(int frames);
    void Unfreeze();
}
public class HitStopController : MonoBehaviour {
    public void RequestFreeze(int frames);
    // 子要素のIFreezable全てに伝搬
}
```

各 Arena に 1 つ `HitStopController` を置く。

---

## 6. 命名規則

桜井政博氏の方針を参考にしたソートしやすい命名。

### ファイル / クラス / アセット名
- カテゴリ → 対象 → 詳細 の順
- 例:
  - `BlockNormal`, `BlockHard`, `BlockExplosive`, `BlockSpike`
  - `ItemAttribute_Fire`, `ItemPaddle_Enlarge`
  - `SkillPaddle_Enlarge`, `SkillBall_Multi`
  - `InterferenceHarden`, `InterferenceSpike`
- PascalCase

### 変数名
- camelCase
- 数値パラメータは ScriptableObject に出す（`[SerializeField]` での個別公開は最小限）

### 言語
- 英語ベース（チームに海外の人が来ても通じる）
- 日本語コメントはOK

---

## 7. パラメータ一覧（プランナー触り場）

実装が進むにつれて埋めていく。すべて `GameBalanceProfile` 配下に存在する想定。

### HPSettings
| 名前 | デフォルト | 意味 |
|---|---|---|
| maxHP | 500 | 最大HP |
| damageBallDrop | 20 | ボール落下時のダメージ |
| damageBlockReachBottom | 10 | ブロック底到達時のダメージ |
| damageSpike | 30 | 棘ブロック接触ダメージ |
| damagePoisonPerSec | 5 | 毒エリア滞在ダメージ/秒 |

### ComboSettings
| 名前 | デフォルト | 意味 |
|---|---|---|
| comboTimeoutSec | 2 | コンボ判定の連続時間 |
| interferenceTriggerCombo | 5 | 妨害送付に必要なコンボ数 |

### BallSettings
| 名前 | デフォルト | 意味 |
|---|---|---|
| speed | 7 | ボール速度 |
| minAxisRatio | 0.2 | 軌道補正の強さ |
| relaunchAngleSpread | 0.5 | リスポーン時の発射角度ランダム幅 |
| stuckTimeoutSec | 5 | この時間ブロック未ヒットで加速 |
| stuckSpeedMul | 1.1 | 加速倍率 |

### LaunchSettings（メトロノーム）
| 名前 | デフォルト | 意味 |
|---|---|---|
| metronomeAngleRange | 60 | 振れ幅（左右±度数） |
| metronomePeriodSec | 1.0 | 1周期の時間 |

### HitStopSettings
| 名前 | デフォルト | 意味 |
|---|---|---|
| explosiveBlockFrames | 6 | Explosive破壊時 |
| spikeBlockFrames | 4 | Spike破壊時 |
| paddleBounceFrames | 1 | パドル受け止め |
| interferenceTriggerFrames | 10 | 妨害発動 |
| panicSkillFrames | 15 | ピンチスキル発動 |
| roundEndFrames | 30 | ラウンド決着 |
| matchEndFrames | 60 | マッチ決着 |

---

## 8. 既知の方針決定（議論履歴の結論）

過去の議論で確定した方針を記録する。後で「なぜそうしたか」を忘れないため。

- **HP制を選択**: 残機制はリアルタイム対戦と相性が悪い
- **HP500**: 粒度を持たせて様々なダメージを混在可能に
- **スキルは代償なし**: ゲーム性が上がれば一般性が下がる、プレイヤーがパンクする
- **妨害は変化中心**: いきなりブロックを降らせるのは危険、既存盤面の変質を主軸
- **メトロノーム発射採用**: 角度入力問題の解決策として優れる
- **キャッチ機能は様子見**: 初回発射のみで十分な可能性、要検討
- **直接攻撃は将来枠に温存**: 諦めないが、ストックを優先
- **ブロック破壊のヒットストップ**: Normal/Hardは除外（頻発のため）、Explosiveは入れる
- **GameBalanceProfile 一元集約**: ハードコード排除、プランナー触れる構造に

---

## 9. このドキュメントの保守

- 仕様変更があったら必ずここを更新する
- 実装に入った後、Inspector で調整した最新デフォルト値もここに反映
- 議論の結論は §8 に追記していく
