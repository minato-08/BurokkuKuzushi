# BurokkuKuzushi 仕様書

最終更新: 2026-05-20（コンボ自動妨害を撤廃、攻撃アイテム経由モデルへ刷新）

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

各プレイヤーは独立したアリーナを持ち、上から降ってくるブロックをボールとパドルで破壊する。ブロック破壊で確率的にドロップする **攻撃アイテム** をパドルで取得すると、相手アリーナへ妨害（毒・スロー・ブロック変質等）を送り込める。先に HP が 0 になった方が負け。

### デザインの三本柱
1. **コア**: ブロック崩し本来の手触り（反射・破壊・落下回避）を最上位に置く。妨害は「副菜」であって主菜ではない。
2. **意思のある攻撃**: 相手への干渉は必ずプレイヤーの能動的選択（攻撃アイテム取得 / スキル発動）を経由する。コンボ等の数値達成だけで自動的に相手にダメージが及ぶ機構は持たない。
3. **カムバック性**: HP 帯バンドで劣勢側が獲得しやすくなる（スキルゲージ蓄積・アイテムドロップ・スコア倍率）。試合終盤までもつれる設計。但し、倍率は上げすぎない。あくまでもエッセンス。

---

## 2. 勝利条件

- 各プレイヤーに HP がある（初期値 500）。
- 以下のイベントで HP が減少する:
  - 自陣のブロックが底に到達
  - 妨害効果による被弾（毒エリア等）
- HP が 0 になったプレイヤーが負け。
- 試合は複数ラウンド制（先取数で勝利、デフォルト 1 本先取）。
- 先取数は試合前の設定画面で変更可能（1~5本で選択）。

---

## 3. ゲームフロー

```
[タイトル / メニュー]
   |
   | 先取本数・モード選択
   ▼
[マッチ開始]
   │
   │ スキルを一つ選ぶ ← **ラウンド間で再選択可能**
   ▼
[ラウンド開始]
   │  プレイ中:
   │   - ボール処理 / ブロック破壊（コア）
   │   - 強化アイテム / 攻撃アイテム取得 / スキル発動 / 角度発射（戦術）
   │   - コンボ（連続破壊）はスコア倍率・エナジー蓄積倍率の自己強化
   ▼
[ラウンド終了]
   │  どちらかの HP が 0
   │  → 勝者アリーナ強調演出 + 簡易リザルト表示（キーで次へ進む）
   │    リザルト: 勝者名 / 残HP / 今ラウンドのスコア / 最大コンボ
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
| 1P | A / D | S | Q |
| 2P | J / L | K | U |

キーボードの仕様によっては6キー以上の同時押しが認識されない可能性アリ

> **確定(2026-5-28)**: ポーズは実装に手間がかかる上、そこまでの必然性を感じないためカット。

## 5. システム仕様

### 5.1 HP

- 各プレイヤーは HP を持つ（初期値 500）。
- **ラウンド開始時にリセットされるもの**: HP（最大値まで）/ スキルゲージ（0に）/ アクティブ効果（Buff/Attack/Trap アイテム効果、スキル効果）/ コンボカウント / ZonePoison・ZoneSlow（ArenaController.ResetForNewRound で Destroy）。
- ラウンドをまたいで引き継ぐものは**なし**（すべてラウンド単位でリセット）。
- HP が 0 になった瞬間にそのラウンドの敗者が確定する。

#### ダメージ表（仕様確定値・コード SerializeField デフォルトと一致）

| 発生源 | ダメージ | 備考 |
|---|---|---|
| ボール落下 | 0 | DeadZone 通過時 |
| ブロック1個 底到達 | 15 | 同フレーム複数到達は線形加算 |
| 毒エリア滞在 | 3 / 秒 | パドルが ZonePoison 内にいる間 |
| 強制リスポーン（飛行中の発射キー） | 5 | LaunchAimer.ForceRespawn |
| 上部攻撃被弾（Phase G+） | 40 | InterferenceDirectAttack 着弾 |

> **確定（2026-05-20）**: ブロック底到達ダメージは**線形加算**（`damageBlockReachBottom × 個数`）を採用する。
> 累進方式（5 個以上で罰則増加）は実装が複雑なうえ、Dynamic Escalation 導入により
> 「同フレームで多数到達」する状況は終盤に自然発生する。その局面はすでに「ピンチ状態」
> であり追加罰則は過剰。線形で十分な緊張感が生まれる。playtest でダメージ感が
> 弱いと判断した場合は `damageBlockReachBottom` の値（現行 15）を上げることで対応する。
> **確定(2026-05-28)**: ボールのダメージ、棘ブロックは削除。

#### HP帯ごとの動的パラメータ

現在 HP の最大値に対する割合に応じて、以下のパラメータが切り替わる（カムバック性の主要装置）。

| HP割合 | gaugeRateMul | itemDropMul | scoreMul | dropBiasBuff |
|---|---|---|---|---|
| 100% 〜 70% | 1.0 | 1.0 | 1.0 | 0% |
| 70% 〜 30% | 1.3 | 1.2 | 1.0 | +20%（強化寄り） |
| 30% 〜 10% | 1.6 | 1.5 | 1.5 | +40%（強化寄り） |
| 10% 以下 | 1.8 | 1.7 | 1.5 | +50%（強化寄り） |

- **dropBiasBuff** はアイテム抽選時に強化アイテムが選ばれる確率に加算される（攻撃/罠を相対的に減らす）。劣勢側の戦線維持を後押し。
- **HP 帯限定スキル（パニックスキル）は 2026-06-05 のスキル刷新で廃止**（旧 `SkillPanic_BlockClear`）。現在のスキルはすべて HP に依存せず、ゲージが必要量に達すれば発動できる（5.6）。劣勢側のカムバックは `gaugeRateMul`（ゲージ蓄積加速）が担う。
- バンドは `GameManager.hpStateBands[]` の SerializeField 配列で定義。`thresholdPercent` 降順に並べ、空配列なら全倍率 1.0 のデフォルトを使う。

### 5.2 ボール

- 各アリーナに通常1個。スキル/アイテムで一時的に増えることがある。
- 一定速度で動き続ける（速度はバランス設定で指定）。
- アイテムの効果などにより加減速することがある。
- ブロック・壁・パドルに衝突して反射する。
- パドル下のデッドゾーンに到達すると落下扱い。

#### 軌道補正
- 反射後、X/Y 成分どちらかが閾値未満（デフォルト 0.2）の場合、強制的に角度を修正して壁沿いのループを防ぐ。

#### 時間加速
- アリーナ滞在時間に比例してボール速度が徐々に上昇する（メインボールのみ）。リスポーンでリセット。
- 上限は基本速度の `timeAccelMax` 倍（デフォルト 1.5）。加速量・上限は SerializeField で設定。

#### 属性
ボールには属性を持たせることができる。属性は通常は付与されておらず、スキル/アイテム/ゲート通過で一時的に付与される。

| 属性 | 効果 | 補足 | エフェクト |
|---|---|---|---|
| Normal | 通常（属性なし）。ダメージ1 | デフォルト状態 | エフェクトなし |
| Fire | 着弾点周囲のブロックにダメージ（範囲攻撃） | 属性倍率 ×1.2 | 炎を纏わせる。攻撃範囲を着弾時に表示。 |
| Thunder | 着弾点周囲の同種ブロックに連鎖ダメージ | 属性倍率 ×1.1 | バチバチとした微弱な電気エフェクトを纏わせる。巻き込まれた同種ブロックに稲妻が走るような演出。 |
| Ice | ダメージ2 | 属性倍率 ×1.2 | 冷気を纏わせる。着弾時に氷属性のエフェクト。 |
| Heavy | ダメージ3、速度を0.7倍 | 属性倍率 ×1.5 | 重さを感じさせる金属質な見た目に。 |
| Pierce | ダメージ1、ブロックを貫通、ヒットストップ抑制 | テンポ重視貫通。属性倍率 0（ヒットストップ無効化） | トレイルを長くする。 |

>**2026-5-28**属性効果とは、カメラシェイクなどに与えられる係数という認識でいい？
>
>**2026-6-03 確定**: 属性倍率＝**手応え（ヒットストップ／シェイク）の「攻撃力」重み**。ブロック衝突の停止フレーム数を `impact = speedTerm × 属性倍率` で算出する（速い・攻撃力が高いほど手応えが増す）。閾値未満は 0（軽い当たりはテンポ維持）。Pierce は 0（ヒットストップ抑制）。実装は `BallScript.GetImpactFrames()`、係数は `ArenaSharedConfig` で一元調整。詳細は `IMPLEMENTATION.md` 5.2/5.9。
>※ **Heavy の「速度0.7倍」実装済み**（2026-6-03, `heavySpeedFactor`。実効速度 = naturalSpeed × アイテム加減速 × ZoneSlow × 属性速度係数）。

### 5.3 パドル

- 左右のみ移動可能。
- 当たったボールを自動反射する。

#### メトロノーム式発射（Phase B 以降）
- ボールリスポーン時、パドル上に角度インジケーターが表示される。
- インジケーターは一定周期（デフォルト1秒）で左右に振れる（範囲 ±60°）。
- 振れる角度の幅も表示する。
- 発射キーを押した瞬間のインジケーター角度でボールが発射される。
- インジケーターの先に、ボールの予想される軌道を表示する。
- **センター通過音**: インジケーターが真上（90°）を通過する瞬間に短い「ティック」音を鳴らす。発射タイミングの耳コピが可能になり、画面から目を離していても直上発射が狙える。
- **センター通過ビジュアル**: インジケーター（LineRenderer）が真上 ±10° の範囲に入ると、線の色を通常の白から明るいシアン（HDR Intensity 2.0）に切り替える。中央付近であることを一目で確認できる。真上から外れると元の色に戻る。

#### ループ対策
- キャッチ機能は不採用。以下の機構で対応する。
  - 角度補正（`ClampAngle`）: X/Y 成分を最低 0.2 以上に強制し、壁沿いのループを防ぐ。
  - 時間加速: アリーナ滞在時間に比例して速度が上昇するため、長時間ループしてもいずれ抜ける。

#### コンボ熱表示（Ball Heat）

高コンボ中はボールの色が段階的に変化し、「今試合が熱い」状態を視覚的に表現する純粋な演出レイヤー。

| コンボ段階 | ボールのトーン |
|---|---|
| 0〜9 | 白（通常） |
| 10〜19 | 薄いクリーム / 淡い黄色 |
| 20〜29 | 温かみのあるオレンジ |
| 30+ | 深いオレンジ〜赤 |

- 属性（Fire / Ice / Thunder 等）が付与中の場合は属性カラーが Ball Heat に優先する。
- 実装: `BallScript.Update()` 内で `GetHeatColor(combo)` を毎フレーム呼び、`SpriteRenderer.color` を Lerp 更新。ヒットストップ中も更新継続。

### 5.4 ブロック

#### 分類

| 起源 | 説明 |
|---|---|
| Neutral (N) | 通常スポーンで降ってくる |
| Self-generated (S) | 自分のスキル/アイテムで自陣に生成 |
| Opponent-sent (O) | 相手の干渉により自陣に発生 |

#### ビジュアル表現
通常の見た目に加えて、オーラにより視覚情報を追加する。

- N: オーラ無し
- S: 青系オーラ
- O: 赤系オーラ

#### BlockHard / BlockHardened の残耐久表示

HP が 1 より大きいブロックには、ブロック内上部に小さな **HP pip ドット**（●の並び）を表示する。

- BlockHard HP2: ●●
- BlockHard HP3: ●●●
- BlockHardened HP3: ●●●
- 命中で HP が減ると該当数のドットが消える。

これにより「あと何回で壊れるか」の情報が即座にわかる。パドルをそのブロックに向ける価値判断ができ、戦略的なボール誘導が生まれる。Phase F-Combat 以降で追加（コード変更は最小: Block の `Renderer` に pip 用 GameObject を子追加するのみ）。

#### BlockHard のクラック段階（ビジュアル仕様）

HP の残量に応じて、ブロック本体に重なるクラックオーバーレイを段階的に表示する。HP pip と冗長に見えるが、遠目に「壊れかけ」をシルエットで伝える補助的役割を担う。

| ブロック種 | HP 最大 | HP=Max（満タン） | HP=Max-1 | HP=1 |
|---|---|---|---|---|
| BlockHard | 2 | ひびなし（クリーン） | ひびあり（中程度） | ─ |
| BlockHard | 3 | ひびなし | ひび浅め | ひびひどい |
| BlockHardened | 3 | 金色クリーン | 金色+ひび浅め | 金色+ひびひどい |

実装: `Block.currentHp` に応じて `crackMaterial.SetFloat("_CrackAmount", (maxHp - currentHp) / (float)(maxHp - 1))` を呼ぶ。クラックシェーダーは Unity の標準 Sprite / SpriteRenderer にオーバーレイするテクスチャブレンドで実現する。HP pip が主情報源、クラックは補助情報として扱う。

#### ブロックへの着弾フィードバック

#### ブロック一覧

| 名前 | 起源 | 性質 | 効果 | 実装フェーズ |
|---|---|---|---|---|
| BlockNormal | N | Block | HP1。1撃で破壊 | 既存 |
| BlockHard | N | Block | HP2〜3。徐々にヒビが入る演出 | 既存 |
| BlockExplosive | N | Block | 破壊で周囲ブロックに巻き込みダメージ。連鎖は `explosionChainDelay`(=0.07s) ずつ遅延して同心円状に広がる（一拍ずつカスケード）。通常ブロックの巻き込み破壊も同じ遅延で遅れて消える | 既存 |
| BlockAbsorb | N | Block | 当たったボールを数秒間減速させる | 既存 |
| BlockItem | N | Block | 破壊で確定アイテムドロップ | C |
| BlockHardened | O | Block | 通常ブロックが硬化変換されたもの | E |
| ZonePoison | O | Zone | パドル接触で HP減少 | E |
| ZoneSlow | O | Zone | ボール減速エリア | E |
| GateSpeed | N/S | Gate | 通過したボールに速度上昇 | G |
| GateMulti | N/S | Gate | 通過したボールを分裂させる | G |

> **2026-5-28**: Fireと同様に、Explosiveなどは影響範囲を明示する。Explosiveであれば爆発のエフェクト。

#### 初期ブロック配置

ラウンド開始直後のアリーナは**空**（ブロックなし）。`GO!` の瞬間に `spawnTimer` が動き始め、`spawnIntervalBase` 秒後に初行が出現する。初行出現まで 5s 程度の「素振り期間」があっても、LaunchAimer の発射判断とメトロノームタイミングの習得として機能する（ボール-壁の反射音が基本テンポを刻む）。

#### スポーン仕様
- 一定間隔（デフォルト5秒）で 1 行ずつ降ってくる。
- 1行あたりのブロック数（デフォルト6）、ブロック間の隙間（デフォルト0.1）。6 × blockWidth(1.5667) = 9.4 でアリーナ幅（xLimit 4.7 × 2）にほぼぴったり収まる。
- 降下速度はデフォルト 0.3 unit/秒。
- 通常行の構成: 通常 / Item (10%) / Hard（10%） / Explosive（5%）。
- 妨害行（妨害送付時）の構成: Hard / Absorb のミックス。

#### 底到達危険ライン演出（Danger Proximity）

最下段のブロックが `blockDeadZoneY` に近づくにつれて「危険接近」を視覚的に警告する。

- 最下段ブロックが `blockDeadZoneY + 1.5` 以内に入ると、`PXBlockDeadLine`（UIアリーナ下端のライン）が赤く点滅開始（DeadZoneYとの距離に比例して点滅が早くなる。max 0.4, min 0.15）。
- ブロックが底到達でペナルティになった瞬間、ラインが 1s 間白くフラッシュ。
- 「知らなかったのに突然ペナルティを受けた」感を排除し、危機を回避する判断の機会を与える。
- `UIManager.UpdateDangerLine(float closestBlockY)` が毎フレーム最下段 Y を受け取り点滅状態を制御する。

#### スペシャル行（Phase F-Polish 以降）

単調なランダム行の流れを時々変える「スペシャル行」を実装する。スポーン 8 回に 1 回（`specialRowChance = 0.125`）の確率でランダムに選択される。

| スペシャル行タイプ | 構成 | プレイ上の意味 |
|---|---|---|
| 全Item行 | Item × 6 | ラッキー要素。アイテムを盤面に増やすことで展開を動かす |
| 全 Explosive 行 | Explosive × 6 | 1 発で連鎖爆発 |
| 歯抜け行 | 4 ブロック + 隙間 2 個（ランダム配置） | 隙間を通せる角度を狙う技術的な挑戦 |

- 妨害行が予約されている場合は、スペシャル行よりも妨害行を優先する。
- スペシャル行スポーン時に軽い SE（`se_special_row.wav`、落雷系）を鳴らし「普通ではない行が来た」を伝える。

### 5.4.1 ラウンド内エスカレーション（Dynamic Escalation）

ラウンド開始から時間が経つほど、ブロックの圧力が上がる仕組みを持たせる。固定パラメータのまま全ラウンドが均質な圧力で進むと「いつでも同じ難しさ」になり、緊張のピークが生まれない。時間経過でスポーン頻度と降下速度を段階的に上げることで、すべてのラウンドに「序盤は助走、終盤は嵐」の自然な弧を与える。

#### パラメータ（BlockSpawner SerializeField に追加）

| フィールド | デフォルト | 意味 |
|---|---|---|
| `spawnIntervalBase` | 5.0s | ラウンド開始時のスポーン間隔 |
| `spawnIntervalDecayPerMin` | 0.2s | 1 分ごとにスポーン間隔が縮まる量 |
| `spawnIntervalMin` | 3.0s | スポーン間隔の下限 |
| `descentSpeedBase` | 0.3 u/s | ラウンド開始時の降下速度 |
| `descentSpeedGainPerMin` | 0.03 u/s | 1 分ごとに降下速度が増える量 |
| `descentSpeedMax` | 0.45 u/s | 降下速度の上限 |

#### 時間ごとの圧力感

| ラウンド経過時間 | スポーン間隔 | 降下速度 |
|---|---|---|---|
| 0:00 | 5.0 s | 0.30 u/s |
| 1:00 | 4.8 s | 0.33 u/s | 
| 2:00 | 4.6 s | 0.36 u/s |
| 3:00 | 4.4 s | 0.39 u/s |
| 4:00 | 4.2 s | 0.42 u/s |
| 5:00+ | 4.0 s（下限） | 0.45 u/s（下限） |

- ラウンドごとにリセットされる（ラウンドをまたいで蓄積しない）。
- 妨害行（敵から送られた行）は通常行と同じ降下速度で降る（エスカレーション速度に連動）。
- `spawnInterval / descentSpeed` は毎フレーム `roundElapsedTime` から再計算する（SerializeField の基準値を上書きしない、演算結果を使う）。
- エスカレーションは両アリーナに独立して適用される。ラウンド時間はマッチ全体で共有するため、どちらのアリーナも同じ増圧を受ける。

**設計意図**: 発表（5分程度の 1 ラウンド）が「ぬるすぎる序盤 → 終盤でパニック」ではなく「全体通じて面白い」体験を保証する。3 分でほぼ満圧になる上、「無理ゲー」だとは感じさせないギリギリを狙う。

---

### 5.5 アイテム

ブロック破壊時に確率でドロップし、落下する。パドルで取得すると効果が発動する。アイテムは **強化** / **攻撃** / **罠** の 3 系統に分類される。
アイテムを取得するとパドル下部にアイテム名が表示される。

**ドロップ位置**: アイテムは破壊されたブロックのワールド座標（X, Y）にスポーンし、そこから重力なしで `itemFallSpeed`（SerializeField）の一定速度でローカル Y 軸方向に落下する。ブロックがスポーン Y（上部）付近にある場合でも、ブロック座標からそのまま落下する（上限クランプなし）。パドル付近のブロック（底に近い）が壊れた場合は落下時間がほぼゼロ = 即キャッチか即通過になる。

```
ブロック破壊
   │  baseDropChance × HP帯バンドの itemDropMul で抽選
   │
   ├─ 強化アイテム（Buff）  : 自陣に作用する有利効果
   ├─ 攻撃アイテム（Attack） : パドルで取得 → 相手アリーナに妨害送付
   └─ 罠アイテム（Trap）    : 自陣に作用する不利効果（取得回避が戦略）
```

#### 5.5.1 強化アイテム

| 名前 | 効果 | 持続 |
|---|---|---|
| BuffAttribute_Fire | ボール属性 Fire（着弾点周囲ダメージ） | 5s |
| BuffAttribute_Thunder | ボール属性 Thunder（同種ブロック連鎖） | 3s |
| BuffAttribute_Ice | ボール属性 Ice（ダメージ2） | 8s |
| BuffAttribute_Heavy | ボール属性 Heavy（速度0.7倍 / ダメージ3） | 8s |
| BuffBall_Pierce | ボール属性 Pierce（貫通・ヒットストップなし） | 3s |
| BuffPaddle_Enlarge | パドル幅 ×1.5 | 10s |
| BuffPaddle_SpeedUp | パドル移動速度 +30% | 10s |
| BuffHeal | HP +50（即時） | — |

#### 5.5.2 攻撃アイテム（妨害トリガー）

「コンボで自動送付」の旧仕様を撤廃し、攻撃アイテム取得を唯一の妨害トリガーとする。視覚的には赤系のオーラ + 棘付きアイコンで強化アイテムと識別する。

| 名前 | 取得時の効果（相手アリーナへ送付） |
|---|---|
| AttackHarden | 相手の Normal ブロックを `hardenCount` 個 Hard 化（金色オーラ・HP3）|
| AttackAddRow | 相手スポーナーに Hard/Absorb 混合行を 1 予約 |
| AttackPoison | 相手アリーナ下部に ZonePoison（毒）を 1 個落下生成 |
| AttackSlow | 相手アリーナ中央に ZoneSlow（減速）を 1 個落下生成 |
| AttackDirectShot（Phase G+） | 当たると30ダメージの攻撃を0.3秒間隔で、その時点でのパドル座標に5発落とす。 |

**取得回避の判断:** 攻撃アイテムは取得しなくてもプレイヤーに直接の損はない（地面に落ちて消える）。ただし**取得しないと送付できない**ため、攻撃したい場合は能動的にキャッチする必要がある。逆に強化アイテムとの優先順位を考えながらパドルを動かすのが戦略の核となる。

**ドロップ抽選の比率（仮）:** 強化 6 : 攻撃 4 。HP 帯バンドで偏重を変える（劣勢時は強化偏重・優勢時は攻撃偏重）。罠アイテムはこの比率には独立して現れず、**強化枠の一部を「強化に偽装した罠」に置き換える**形で出現する（5.5.3 参照）。

#### 5.5.3 罠アイテム

「不利アイテム」をリネーム。取得しないこと自体が選択肢になる、取得回避ゲームのスパイス。

| 名前 | 効果 | 持続 |
|---|---|---|
| TrapPaddle_Shrink | パドル幅 ×0.7 | 10s |
| TrapBall_Hyperspeed | ボール速度大幅上昇, 4倍（制御困難化） | 3s |
| TrapBall_Reversed | A/D（J/L）の左右入力が反転する。パドルが逆に動く。慣れた操作感覚が裏目に出る「心理トラップ」。 | 5s |

罠アイテムは強化と似たアイコン形状にして「色だけが違う」紛らわしさを意図する（識別力もスキル要素にする）。`TrapBall_Reversed` を取得した瞬間に画面端に小さく `REVERSED!` のラベルを表示し、持続中は HUD のキー表示（A←→D）を反転アイコンに変える。実装は `PlayerController.inputReversed` フラグを 5s 間 `true` にする（LaunchAimer の発射確定キーは反転しない）。

#### 罠アイテムのドロップ経路（偽装枠）

罠アイテムは独立したドロップ枠を持たず、**強化枠が選ばれた抽選において一定確率（`Block.trapDisguiseChance`、デフォルト 0.1）で罠に置き換わる**。これにより：

- 「強化が出た」と思って取りに行くと罠だった、という色違いの紛らわしさ（取得前の識別がスキルになる）が成立する。
- 攻撃アイテム比率（強化 6 : 攻撃 4 の "4"）は罠の影響を受けず安定する。罠は強化枠の内訳から出るため、実効分布は「強化 ≒ 54% / 罠 ≒ 6% / 攻撃 40%」（デフォルト値の場合）になる。
- `trapDisguiseChance = 0` にすれば罠を完全に無効化できる（発表時の難易度調整用）。

実装: `Block.SelectRandomItemType()` が強化枠選択時にこの確率で `TrapPool`（Shrink / Hyper / Reversed）から抽選する。

#### 5.5.4 アクティブ効果の同時成立とドロップ過多抑制

持続効果は **複数同時に成立する**。重ね掛けは「効果スロット」単位で上書きされ、スロットは以下の独立コルーチンに対応する（`ItemEffectSlot`）：

| スロット | 対象アイテム | 上書き挙動 |
|---|---|---|
| `BallAttribute` | Fire / Ice / Thunder / Heavy / Pierce | 同スロットの新規取得で属性・残り時間を上書き |
| `BallSpeed` | SpeedUp / Hyper | 速度倍率を上書き |
| `PaddleScale` | Enlarge / Shrink | パドル幅倍率を上書き |
| `InputReverse` | Reversed | 反転持続を上書き |
| `None` | Heal / Attack 系 | 持続効果なし（追跡しない） |

`GameManager` は取得者ごとに `ActiveEffect`（スロット / 名前 / 期限）のリストで追跡する（同スロットは上書き、期限切れは自動除去）。

> **HUD 表示**: 当面は最新 1 個のみ表示（`GetActiveItemName` が末尾エントリを返す。既存 `$P{N}ItemName` スロット 1 つを流用）。複数スロットの同時表示 UI は残作業（`GetActiveEffects()` で全件取得可能）。

**ドロップ過多抑制**: 同じ持続効果を連続取得して間延びするのを防ぐため、ドロップ抽選で選ばれた持続効果のスロットが **取得者に既に有効** な場合は再抽選する。`Block.maxSlotRerolls`（デフォルト 2）回試しても同スロットのままなら、そのドロップは **スキップ**（生成しない）。`Heal` / `Attack` 系はスロット `None` のため抑制対象外。`baseDropChance` 自体は 0.15 据え置きで、抑制はスロット衝突時のみ働く。

### 5.6 スキル

- 試合開始前にスキルを **1 個** 装備する。
- エナジーゲージが**そのスキルの必要量に達する**とキー入力 1 つで発動できる（1P: Q / 2P: U）。
- 発動中の効果は一定時間で自動解除される。
- 発動による代償（HP 消費・移動制限・他能力低下など）はない。
- ゲージはブロック破壊で蓄積し、蓄積量は `energyPerBlock × HP帯バンドの gaugeRateMul × コンボ倍率（5.10）` で算出する。

#### スキルの方針（2026-06-05 刷新）

旧スキル（BIG PADDLE / DOUBLE BALL / FIRE BALL / EMERGENCY CLEAR）は「アイテムに比べて地味」という課題があったため、**派手で爽快な 4 種に全面刷新**した。新スキルはすべて **自己強化 / 盤面有利** の方向で、相手アリーナへ直接干渉する攻撃系は持たない（攻撃は妨害アイテム 5.7 が担う）。

- **性能差は必要ゲージ量で差別化する**（`SkillDefinition.EnergyCost`）。強いスキルほど多くのゲージを要求する。
- ゲージ表示（エナジーバー）は **そのスキルの必要量に対する充填率**（`energy / EnergyCost` を 0..1 にクランプ）で見せる。安いスキルほどバーが早く満タンになり、READY 表示が早い。
- HP 帯限定スキル（旧パニック）は廃止。全スキルが HP に依存せず発動できる。

#### スキル一覧

スキル選択 UI では「表示名」と「短い説明」を使う（コードネームは非表示）。**必要ゲージ量は調整値**（下表はデフォルト。`maxEnergy`=10 を上限に各スキルへ配分）。

| index | コードネーム | 表示名 | 効果詳細 | 必要ゲージ（既定） |
|---|---|---|---|---|
| 0 | SkillHyper | **HYPER** | ボールを高速化（既定 ×2.2）し、Dead Zone 付近に一時的な床を出現させて発動中（既定 6s）暴れさせる。床があるので発動中はボールを落としにくい | 6 |
| 1 | SkillExplosion | **EXPLOSION** | 自陣のブロックをランダムに 10〜20 個 Explosive へ変換する。以後その列をボールが叩けば連鎖爆発が起きる | 8 |
| 2 | SkillBurst | **BURST** | 発動中（既定 5s）、メトロノームで狙って最大 10 発までボールを連射できる。撃ち切る or 時間切れで終了。撃ったボールは一定時間（既定 8s）で消える | 10 |
| 3 | SkillGiant | **GIANT** | ボールを巨大化（既定 ×3）し Pierce 化する＝巨大貫通弾。発動中（既定 6s）ブロックを反射せず薙ぎ払う。検出半径はボール実寸に追従するので薙ぎ払い幅も広がる | 5 |

> 攻撃系スキル（相手アリーナへの妨害送付）は Phase G+ で再検討。Phase F は上記 4 種（自己強化/盤面有利）で均衡を取る。

#### 実装メモ（各スキルの再利用元）

- **HYPER**: `BallScript.SetSpeedTemporary` ＋ `ArenaController.SpawnHyperFloor`（Dead Zone 付近に床を出す。`hyperFloor` に手動配置オブジェクトをバインドすれば発動中だけ `SetActive`＝位置/サイズ/見た目を調整可。未バインドなら BoxCollider のキューブを実行時生成）。
- **EXPLOSION**: `BlockSpawner.ConvertRandomToExplosive(count)` → `Block.ConvertToExplosive()`（`HardenToHp` と同様の実行時種別変換）。爆発挙動は既存の Explosive 連鎖（5.4）をそのまま使う。
- **BURST**: `LaunchAimer` に burst モードを追加。発動中はメインボール飛行中でもメトロノームで照準でき、発射キーで `ArenaController.SpawnBurstBall(localDir, lifetime)` を呼んで追加ボールを連射する。追加ボールは `isExtraBall=true`（ラウンドリセットで破棄される）。
- **GIANT**: `BallScript.SetAttributeTemporary(Pierce, duration)` ＋ 新規 `SetScaleTemporary(multiplier, duration)`。Pierce の検出半径は `cachedCollider.bounds.extents.x` 由来なので scale 拡大で薙ぎ払い幅も自動拡大（専用検出コード不要）。

**選択 UI のレイアウト方針**: 選択可能なスキルを **4 枚のカードとして横一列に共有表示** し、P1 / P2 が **それぞれ独立したカーソル** で選ぶ。両者のカーソルが同じカード群の上に乗るため、互いの選択がリアルタイムに見える。ローカル 2P の宿命だが、「相手が GIANT を選んだ」情報はそれ自体がメタゲーム（対抗策を選ぶ動機になる）として機能する。同じカードを両者が選んでもよい。

- カードの並び順は実装の `SkillSelectUI.AllSkills` の index と一致させる（左→右で 0 HYPER / 1 EXPLOSION / 2 BURST / 3 GIANT）。
- 選択表現は `SkillSelectUI` のカーソル/Ready GameObject の SetActive 切替（`cardP{N}Cursors[]` / `cardP{N}Ready[]`）。
- 1P: A/D で左右移動・S 確定 / 2P: J/L で左右移動・K 確定。両者確定で `BeginMatch()`。

#### スキルバランスの設計意図

| スキル | 強み | 弱み / コスト |
|---|---|---|
| HYPER | 床で落球リスクをほぼ消しつつ高速ボールで一気に削る。安め（6）で回転が早い | 高速ゆえコントロールが難しい。Absorb で失速。床は発動中のみ |
| EXPLOSION | 盤面を連鎖爆発の地雷原に変える。1 ヒットで大量破壊＝コンボ爆伸び | 変換しても自分で叩かないと爆発しない。やや高コスト（8） |
| BURST | 10 連射でブロックを一掃。最大火力枠 | 最高コスト（10）。撃ち切ると終了、照準が雑だと無駄撃ちになる |
| GIANT | 巨大貫通弾で列をまとめて薙ぎ払う。最安（5）で手軽 | でかいぶん隙間に入れず取りこぼす。貫通中はヒットストップの手応えが無い |

### 5.7 妨害（攻撃アイテム経由）

#### 発動条件
- **コンボ達成による自動送付は行わない**（2026-05-20 以前の旧仕様を撤廃）。
- 妨害は次の 2 経路でのみ相手アリーナに送付される:
  1. 自分が **攻撃アイテム** をパドルで取得した瞬間（5.5.2 参照）
  2. 自分が **攻撃系スキル**（あれば）を発動した瞬間（5.6 参照）
- どちらも能動操作を必須とする。「強くなる行動」と「相手を妨害する行動」を分離せず、同じ「アイテム取得 / スキル発動」の枠で扱う。

#### 妨害種別（送付効果の本体）

攻撃アイテム / 攻撃スキルが内部的に発火する効果。表は「攻撃アイテム名 ↔ 妨害効果名」の対応関係を示す。

| 妨害効果名 | トリガー | 効果 |
|---|---|---|
| InterferenceHarden | AttackHarden / SkillAttack_Harden | 相手 Normal ブロック `hardenCount`(=3) 個を Hard(HP3, 金色) に変換 |
| InterferenceAddRow | AttackAddRow | 相手スポーナーに Hard/Absorb 行を 1 予約 |
| InterferencePoison | AttackPoison | 相手アリーナ下部に ZonePoison を生成（duration 秒）。即座にスポーン。 |
| InterferenceSlow | AttackSlow | 相手アリーナ中央（X=0, Y=0）に ZoneSlow を生成（duration 秒）。ZoneSlow は落下せず Y=0 固定で出現し即座に有効化する。X=0 はアリーナの横中央 = ボールの通り道を塞ぐ位置として機能する。 |
| InterferenceDirectAttack | AttackDirectShot / SkillAttack_Cannon | 相手上空に予告マーカー → 当たると30ダメージの攻撃を0.3秒間隔で、その時点でのパドル座標に5発落とす。（Phase G+） |

#### 妨害送受信の演出 （オーバーレイ + アリーナ間を飛び交うエフェクト）
- 相手のフィールドから妨害が飛んできて、自分のフィールドに適用される、というような効果。Hardenedにされる、などであれば、相手のフィールドから相手のカラーのオーブが飛んできて、それが自分のフィールドのブロックに当たりHardに変化する。妨害行であればオーブが妨害行に変化して降りてくる。
- 特に影響の大きい妨害（AddRowとDirectShot）についてはアリーナに*Incoming*の文字と共に点滅する赤いオーバーレイを表示する。
- 同時に「攻撃送付」「攻撃受信」SEを再生。

### 5.8 コンボ・スコア

コンボは「相手を削る引き金」から外し、**自陣の自己強化を駆動する指標** に再定義する。

#### コンボの定義（2026-06-01 改訂: 接触ベース → 破壊ベースに戻す）
- **ブロックを破壊するたび**にカウントが破壊数ぶん増える（接触ではなく破壊で加算）。
  - 1 ブロック破壊 = +1。Hard ブロックを削り切れずに弾いただけではコンボは伸びない（破壊して初めて加算）。
  - Fire/Thunder 等が 1 接触で複数ブロックをまとめて破壊した場合は、破壊数ぶん一気に伸びる（例: Thunder で 3 個破壊 = +3）。「面で巻き込む属性」がコンボ延長に直結する。
- 次の条件のいずれかで 0 にリセット:
  - **最後のブロック破壊**から次の破壊まで **`comboTimeout`(=6.0s) 経過**
  - ボールが落下（DeadZone 通過）
  - ラウンド開始 / ラウンド終了

> **設計注意点**: タイマーは「最後のブロック破壊から」で計測する。実装: `comboTimer[pi]` はブロック破壊イベント（`RegisterBlockDestroyed`）で 0 にリセット（カウントアップ開始）し、`comboTimeout` を超えた瞬間にコンボを 0 にする。破壊と同時にスコア（`scoreComboMul`）を計算するため、コンボ加算をスコア加算より先に行う。
- カウントには上限を設けず、UI 上は 99 で表示頭打ち（内部値は維持）。

#### コンボがもたらす自己強化（積み上げ式）

| パラメータ | 効果 | キャップ |
|---|---|---|
| スコア倍率 (`scoreComboMul`) | コンボ 5 ごとに +10%（例: 10コンボで +20%） | +100%（@ 50コンボ） |
| エナジー蓄積倍率 (`gaugeComboMul`) | コンボ 5 ごとに +5% | +50% |
| アイテムドロップ倍率 (`itemDropComboMul`) | コンボ 10 ごとに +10% | +50% |

- これらは **HP 帯バンドの倍率と乗算** される（劣勢かつ高コンボのとき最も恩恵）。
- 倍率変動は数値だけで決まり、相手アリーナには直接影響しない。

#### スコア計算

```
1 ブロック破壊スコア = blockBaseScore[blockType]
                       × HP帯バンド scoreMul          ← 乗算（加算ではない）
                       × コンボ scoreComboMul          ← 乗算（加算ではない）
                       × アイテム属性ボーナス（Fire 着弾点周囲ブロックは ×1.0 ずつ加算）
```

> **乗算 vs 加算の確認**: `scoreMul × scoreComboMul` は両方とも乗算。劣勢（scoreMul=1.5）かつ高コンボ（scoreComboMul=1.4 @ 20 combo）の場合、合計倍率は 2.1x。加算なら 1.9x。実プレイ差は小さいが、劣勢 × 高コンボのシナジーを強調するため乗算を採用する。

| blockType | blockBaseScore |
|---|---|
| Normal | 10 |
| Hard | 20 |
| Absorb | 25 |
| Explosive | 30（周囲巻き込みは別途加算） |

#### コンボマイルストーン演出

コンボが 10 / 20 / 30 に達した瞬間、単なる数字増加を超えた「達成の瞬間」を演出する。

- コンボ達成プレイヤーの画面半分に `{N} COMBO!!` のオーバーレイを 1.2s 表示（Bloom 強め、Bebas Neue 大文字）。
- 相手の HUD に `OPPONENT: {N} COMBO` の小さな警告表示（相手が伸びていることを可視化）。
- それぞれ異なる SE（`se_combo_milestone.wav`）を再生。ピッチを 10→20→30 で半音ずつ上げる。

**設計意図**: 観客も「コンボが上がってる」と認識できる。コンボを稼ぐことに達成感のピークポイントが生まれ、「10 コンボ乗った！」という瞬間が語れるゲームになる。

#### 表示
- 現在コンボは P1/P2 HUD の `$P1ComboValue` / `$P2ComboValue` に毎フレーム反映。
- 最大コンボはラウンド終了時のリザルトに表示。

#### スキル表現の段階（プレイヤー熟練度）

このゲームで「うまくなる」にはどういう段階があるかを示す。熟練度差が拮抗した対戦を生むための設計意図の整理。

| 段階 | 行動パターン |
|---|---|
| 入門 | ボールを落とさないことだけを考える。アイテムは偶然当たれば取る |
| 初級 | 強化アイテムを意識してキャッチする。LaunchAimer を少し使う |
| 中級 | 攻撃アイテムと強化アイテムの優先度を判断して取る。パドル角度を意識して発射 |
| 上級 | コンボを維持しながら攻撃アイテムを使う。攻撃と強化の優先順位を瞬時に判断する |
| 超上級 | LaunchAimer で特定ブロック列を狙い撃ち。アイテム優先度と妨害タイミングを読んで盤面を制御する |

熟練度が上がるほど「攻撃アイテム経由の妨害」が有効になる設計。入門〜初級は純粋なブロック崩しとして楽しめ、中級以上で対戦読み合いの奥行きが生まれる。

---

### 5.9 ヒットストップ

特定のイベントで、該当アリーナ内のアクター（ボール / ブロック / ブロック降下処理）を指定フレーム数だけ停止させる演出。

`Time.timeScale` は使用しない（2人プレイのため、片方のアリーナだけ止める必要がある）。

#### 演出
- フリーズ中はカメラシェイクを同時に発生させる。
- シェイクするカメラはイベントによって異なる（通常は発生アリーナのみ、ラウンド/マッチ決着は両方）。
- **フリーズとシェイクの分離（2026-06-03 改訂）**: フリーズ（＝ボール/パドル/ブロックの一時停止）は**ボールが何かに衝突した瞬間**の手応えに限定する。ボール衝突でないイベント（ブロック底到達・妨害行スライド着地）は**シェイクのみ・フリーズしない**。理由: 飛行中のボールが衝突と無関係に空中で一瞬止まると不自然なため。実装は `HitStopController.TriggerHitStop(..., freeze:false)`。

#### 適用イベントと対象

| イベント | フレーム数 | 停止対象アリーナ | カメラシェイク |
|---|---|---|---|
| BlockNormal / Hard / Absorb 衝突 | 0（SerializeField で設定可） | 発生側 | 発生側のみ |
| BlockExplosive 爆発 | 6（SerializeField） | 発生側 | 発生側のみ |
| ブロック底到達 | 5（SerializeField） | **フリーズなし（シェイクのみ）** | 発生側のみ |
| 妨害行スライド着地 | 2（SerializeField） | **フリーズなし（シェイクのみ）** | 発生側のみ |
| 壁反射 | 0（SerializeField で設定可） | 発生側 | 発生側のみ |
| 妨害発動瞬間 | 10（SerializeField） | 発生側 | 発生側のみ |
| ピンチスキル発動 | 15 | 発生側 | 発生側のみ |
| ラウンド決着 | 30 | 両方 | 敗者側のみ |
| マッチ決着 | 60 | 両方 | 敗者側のみ |

**速度閾値ゲート**: ブロック衝突・壁反射のヒットストップは `naturalSpeed / baseSpeed` が `hitStopSpeedThreshold`（デフォルト 1.4）を超えた場合にのみ発動する。フレーム数はその超過量に比例して 0→設定値 にスケールする。BlockExplosive の爆発演出は速度閾値によらず属性倍率のみ適用する。

### 5.10 ラウンド終了・マッチ終了（旧 5.9）

#### ラウンド終了

勝敗の瞬間を「語れる瞬間」にするため、30 フレームのヒットストップに合わせた演出を仕様化する。

| 演出 | 勝者側 | 敗者側 |
|---|---|---|
| アリーナ演出 | アリーナ枠が明るく白くフラッシュ（0.5s） | アリーナが暗転（輝度 50% まで落ちる、0.5s） |
| HUD オーバーレイ | 大きく `ROUND WIN!`（勝者カラー、Bloom） | 大きく `ROUND OVER`（灰色） |
| HP 表示 | 残 HP を強調表示 | HP バーが赤でフラッシュ |
| SE | `se_round_win.wav` | `se_hitstop_strong.wav` |
| カメラシェイク | なし | 敗者アリーナのみシェイク |

- 演出が終わったあと、簡易リザルトを表示する。いずれかのプレイヤーがキーを押すと次のラウンドへ進む。
- 表示内容: 勝者名 / 残HP / 今ラウンドのスコア / 最大コンボ。
- 「勝者が輝いて、敗者が沈む」コントラストを明確にすることで、観客・プレイヤー双方に何が起きたか瞬時に伝わる。

#### ラウンド開始シーケンス

ラウンド（またはマッチ）が始まる際、すぐに操作開始ではなくカウントダウンを挟む。

1. **リセット完了** — ブロック全消去、HP 満タン、ゲージ 0、アリーナがデフォルト状態に戻る。
2. **カウントダウン表示** — 画面中央に `3 → 2 → 1 → GO!`（各 1s）。SE: `se_round_start.wav`（3-2-1-GO ビープ）。
3. **GO!** と同時にボールがパドル上にリスポーン → LaunchAimer が起動してメトロノームスタート。
4. プレイヤーは GO! が出るまで操作できない（PlayerController を Freeze 状態にしておく）。

これにより「準備ができた状態でラウンドが始まる」という明確な区切りが生まれ、不意打ちスタートを防ぐ。

#### マッチ終了
- 先取条件を満たしたラウンドが終わるとマッチ終了。
- マッチ結果画面に遷移し、「再戦」または「メニューへ戻る」を選択できる。
- ラウンド間: 簡易リザルト確認（キーで次へ進む）+ 3 秒（カウントダウン）。マッチ終了はリザルト画面が表示されるまで待つ（自動移行しない）。

#### マッチ結果画面の詳細

ただの「誰が勝った」ではなく「この試合で何が起きたか」を伝える。観客・プレイヤー双方が「あのシーンすごかったね」と話せる材料を提供する。

| 表示要素 | 内容 |
|---|---|
| 大見出し | `P1 WINS!` または `P2 WINS!`（勝者カラー、Bloom 強め、Bebas Neue 大文字） |
| 最終スコア | P1 / P2 双方の合計スコア（カンマ区切り） |
| 最大コンボ | P1 / P2 双方のラウンド全体の最大コンボ（`BEST COMBO`）。高い方を強調 |
| ブロック破壊数 | P1 / P2 双方のマッチ全体の総破壊ブロック数 |
| 受けた妨害数 | P1 / P2 双方が受信した攻撃アイテムの総回数 |
| MVP ラベル（任意） | コンボ / スコア / 攻撃数のうち、最高値を出した側に小さな `⭐` 表示 |
| 操作 | スペース / Enter で「再戦」。Backspace / Esc で「メニューへ戻る」 |

- **実装優先度**: 大見出し・最終スコア・最大コンボ は F-Title 必須。ブロック破壊数・受けた妨害数は数値収集が必要（GameManager で MatchStats を集計）、Phase F-Polish 以降でよい。
- **「再戦」はデフォルト選択**（発表で連続プレイしやすくするため）。

### 5.11 Phase G+ 拡張要素

ここまでで未実装の要素を Phase G+ で順次入れる。各要素は他の系統と独立して実装可能なよう設計する。

#### 5.11.1 Gate（ゲート）

ボールが通過すると一時効果を付与する装置。通常スポーン行とは別の `GateSpawner` から生成する。

| パラメータ | デフォルト | 意味 |
|---|---|---|
| gateSpawnInterval | 15s | ゲート生成間隔 |
| gateMaxPasses | 3 | ボール通過の最大回数（超過で消滅） |
| gateLifetime | 20s | 寿命（通過回数より早く来た方で消滅） |
| gateWidth × gateHeight | 2.0 × 0.3 | スリットサイズ（ボール直径より十分大きく） |

##### 種別

| 名前 | 通過効果 | 持続 | 解禁条件 |
|---|---|---|---|
| GatePower | 通過したボールに属性付与（配置時に Fire / Ice / Thunder のいずれかで固定） | 8s | 試合開始〜 |
| GateSpeed | 通過したボールの速度を ×1.3（`naturalSpeed` 上限を一時超過可能） | 5s | 試合開始〜 |
| GateMulti | 通過時に追加ボール +1（通過上限 1） | — | 試合開始 60s 後 |

##### 通過判定
- ゲートのスリット線分をボールが跨いだフレームで `OnGatePassed` 発火。
- 通過時に `frames=4` のヒットストップ（演出）+ パーティクル + 効果音。
- ゲートは方向性を持たない（裏表どちらから通っても効果同じ）。

##### 出現パターン
- 試合序盤（0〜60s）: GateSpeed 偏重（70%）
- 中盤（60〜120s）: GatePower 偏重（50%）
- 終盤（120s〜）: GateMulti 解禁（20%、他の比率と合算で 100%）

#### 5.11.2 自陣 Zone（S = Self-generated）

スキル / アイテムから自陣に設置するゾーン。Zone Poison / Slow と同じ落下→着地のライフサイクルだが、自分に有利な効果を持つ。

##### ZoneHeal

| パラメータ | デフォルト |
|---|---|
| healPerPass | 2 |
| duration | 10s |
| maxPasses | 5 |

- 持続中、ボール通過のたびに HP +2（`maxPasses` 通過で早期消滅）。
- 取得経路: `BuffZone_Heal` アイテム（強化系・新規追加）または `SkillZone_Heal`。
- 色: 緑系（HDR Green、Bloom 強め）。

##### ZoneAutoClear

| パラメータ | デフォルト |
|---|---|
| clearInterval | 1.0s |
| duration | 8s |

- 持続中、自陣 spawnY 付近に存在するブロック 1 個を `clearInterval` ごとに自動破壊。
- 取得経路: `SkillZone_AutoClear` スキル（防御系・新規追加）。
- 色: 白系（Bloom 強）+ 上下スキャンライン。

#### 5.11.3 DirectAttack（上部攻撃）

`InterferenceDirectAttack` — 強力な単発攻撃。受信側に予告フェーズを与えることで「リアクションゲーすぎず、無視できるほどでもない」を狙う。

##### 発動シーケンス
1. 攻撃側が `AttackDirectShot` アイテム取得 / `SkillAttack_Cannon` 発動。
2. 受信側アリーナ上空（spawnY + 2 ユニット）にターゲットマーカー出現。
3. 3秒間の予告（マーカーが点滅、X 軸位置はパドル位置に連動）の後、0.3秒間隔で5発。受信側 HUD に `INCOMING: DIRECT ATTACK` 表示。
4. パドルに着弾すれば 30 ダメージ。ヒットストップとカメラシェイク15frames。

#### 5.11.4 罠アイテム拡張（ViewDisturb）

`TrapView_Disturb` を Phase F 終盤〜G 序盤で実装。ポストプロセスで自陣アリーナに歪み（Lens Distortion + Chromatic Aberration を 10s 適用）。受信側ゾーンに限定するためにアリーナ単位の Volume を導入する必要があり、単 Ortho カメラとの整合性は要検証（Camera Stacking ではなく Render Feature 化検討）。

#### 5.11.5 まとめ表

| 要素 | 経路 | 効果対象 | 実装担当 Phase |
|---|---|---|---|
| Gate (Power/Speed/Multi) | GateSpawner 自然生成 | 自陣ボール | G |
| ZoneHeal | BuffZone_Heal アイテム / SkillZone_Heal | 自陣ボール | G |
| ZoneAutoClear | SkillZone_AutoClear | 自陣ブロック | G |
| DirectAttack | AttackDirectShot アイテム / SkillAttack_Cannon | 相手パドル + 相手ブロック | G+ |
| TrapView_Disturb | TrapView_Disturb アイテム | 自陣表示 | F+ |

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
画面上部に HP バー + ROUND 表示 + ドット式ラウンドカウンタ + Victory Bar（中央）
```

- 各アリーナを Bloom 装飾枠で囲み（P1=青系 / P2=オレンジ系）プレイヤー識別性を高める
- HUD は左右端に配置、アリーナ表示領域を最大化
- 装飾要素・固定ラベル（操作ヒント等）は Figma で書き出した一枚絵を BG として配置、その上に動的要素を重ねる

### 6.2 各表示要素

| 要素 | 内容 |
|---|---|
| HP バー | 9-slice 角丸 Frame + 内側 Fill（RectTransform Width 制御）。HP 割合に応じてカラー変化（緑→黄→赤） |
| HP テキスト | `{current}` 大きく Bebas Neue、`/ {max}` 小さく |
| Score テキスト | `{score}` カンマ区切り（例: `12,340`）、Bebas Neue |
| Combo テキスト | `{current}` 大、Bebas Neue |
| Combo タイマーアーク | コンボ数字の真下に半円弧（Filled Image, 180° Radial 360, Bottom → Clockwise）を配置。**コンボが発生した瞬間**に弧が満杯（白、Intensity 1.5 程度 Bloom）からスタートし、経過時間に応じて fillAmount が 1→0 に縮小（左端から時計回りに消える）。弧 = 0（消滅）= コンボリセット。コンボ 0 のとき `SetActive(false)` で非表示。消滅の 0.5s 前から橙色にフェードチェンジ（緊急警告）。実装: `UIManager.Update()` で `(comboTimeout - timeSinceLastBlockHit[pi]) / comboTimeout` を `fillAmount` に毎フレーム書き込む。 |
| ラウンド表示 | 中央上部 `ROUND {N}` + 先取数分のドット（点灯/非点灯で勝利数表示） |
| Victory Bar | 画面上部中央の小さな横長バー。`ratio = P1HP / (P1HP + P2HP)` の比で左（P1=青系）/ 右（P2=橙系）に分割。HP が等しければ中央（ratio=0.5）。P1 が優勢なら左に傾く。観客が一瞬で「どちらが優勢か」を読める設計。数字を読まなくてよい。ゼロHP時: P2HP=0 → ratio=1.0（P1 全幅青）/ P1HP=0 → ratio=0.0（P2 全幅橙）。両方 0 は 12.1 のルールにより発生しない（一方が先に 0 になる）。UIManager の $VictoryBar Image.fillAmount を毎フレーム更新。 |
| Incoming インジケータ | 中央領域の縦長 2 列（左列=P1への予約・右列=P2への予約）。妨害送付確定と同時にアイコンが下から追加される（最大 3 個表示、FIFO — 4個目到着時に最古が押し出される）。**表示時間**: `incomingDisplaySec`（デフォルト 3.0s）が経過すると自動削除。アイコンはテキストシンボルで種別を表現（Phase F ではテキスト版で実装、Phase G+ でアイコン画像に差し替え）。種別ごとのシンボル: `⬛HARD`（Harden）/ `☠SPIKE`（Spike）/ `↓ROW`（AddRow）/ `☣PSION`（Poison）/ `🐌SLOW`（Slow）。各アイコンは赤系 HDR カラー。ラウンド終了 / リセット時に全クリア。 |
| アイテム表示 | 取得中アイテムのアイコン + 名前 + 残り秒数（最後に取った1個のみ表示） |
| スキル表示 | スキル名 + キー（Q / U）+ READY 状態（ゲージ満タンで光る） |
| 試合状態テキスト | ラウンド/マッチ終了時のみ表示。`ROUND WIN!` / `ROUND OVER` / `P{N} WINS!` |
| 妨害通知 | 各画面半分を 1.5 秒赤フラッシュ（CanvasGroup alpha） |
| 攻撃送付ラベル | `SENT → P{N}: [種別]` — 攻撃アイテム取得時に攻撃者 HUD に 1.5s 表示、中央方向スライドアウト |
| コンボマイルストーン | コンボ 10/20/30 到達時に `{N} COMBO!!` を 1.2s オーバーレイ（Bloom 強め、Bebas Neue 大文字） |
| ラウンド決着演出 | 勝者側: アリーナフラッシュ白 + `ROUND WIN!` 大文字。敗者側: アリーナ暗転 + `ROUND OVER` |

### 6.3 視覚演出

- **Bloom 演出**: アリーナ枠・READY 表示・装飾要素等は HDR カラー（Intensity > 1）で着色し、URP Bloom Threshold 越えで発光
- **Breath アニメーション**: 装飾枠は `BreathPulse` コンポーネントで HDR Intensity を Sin 波で脈動させることができる → 生命感ある発光
- **フォント方針**: 数字は Bebas Neue（ディスプレイ系）、固定ラベルは JetBrainsMono（モノスペース）で雰囲気統一

#### Last Stand 演出（HP 10% 以下）

HP 10% 以下のアリーナは視覚的に「アラーム状態」になる。プレイヤー本人だけでなく観客も「あのプレイヤーがピンチ」と即認識できる。

- アリーナ枠の BreathPulse を高速化（`cycleSeconds` を通常の 1/3 に変更）かつ HDR 赤に色変更
- HP バーの Fill が赤で点滅（0.3s 周期の点滅、`UnityEngine.UI.Image.color` を Lerp）
- `SkillPanic_BlockClear` が使用可能になった瞬間に `PANIC READY` のラベルを表示（スキル名を上書き）
- **相手 HUD に小さく `OPPONENT 危険!` の通知**を 1s 表示し、「今攻めるチャンス」を知らせる

これにより、HP 10% を切った瞬間が試合の「クライマックスのはじまり」として明確に機能する。

#### アイテム取得フラッシュ

アイテムをパドルで取得した瞬間、パドルを取得アイテムの色系統で **0.1s フラッシュ** する。

| 系統 | フラッシュ色 |
|---|---|
| 強化（Buff） | 青系（Cyan） |
| 攻撃（Attack） | 赤系（Red/Orange） |
| 罠（Trap） | 紫系（Purple） |

即時の感覚的フィードバックとして機能する。「取れた」「何を取ったか」が一瞬で分かり、誤って罠を取ったときの「あっ」感も生む。実装: `PlayerController.OnItemPickup(ItemCategory)` で `SpriteRenderer.color` を 0.1s フラッシュ後に元の色に戻す。

#### AttackAddRow の着弾演出

妨害行（Hard/Absorb ミックス）が相手スポーナーに予約されてスポーンする瞬間に専用演出を入れる。

- 行のブロックが上から「落下投下」するアニメーション: 画面上端から急速に滑り込む（0.3s）
- 着弾時に小さいヒットストップ（2 フレーム）+ 着弾点フラッシュ
- SE: `se_interference_recv.wav` とは別に `se_addrow_land.wav`（ドスッとした着地音）を仮当て

これにより「妨害行が降ってきた！」という視覚的インパクトが生まれ、AttackAddRow 取得が「良い攻撃をした」感に繋がる。

---

## 7. アーキテクチャ

### 7.1 パラメータ管理方針

ScriptableObject / Profile（アセット）は使用しない。各コンポーネントは自分の `SerializeField` を持ち Unity Inspector から調整する。ただし **Arena1/Arena2 で同値であるべき共通チューニング値は、シーン内 MonoBehaviour `ArenaSharedConfig`（1 個）に集約**し、各コンポーネントが初期化時に読んで自分へ適用する（2026-06-02 改訂。左右設定の二重管理を解消）。アセット(SO)ではなくシーン内コンポーネントなので「Profile/SO 不使用」の精神は維持。

- `ArenaSharedConfig` — パドル/ブロックスポーン/ボール/エイマー/スキル/アリーナの**左右共通値**を一元保持。`Instance` を各コンポーネントが参照（無ければ各自の SerializeField 値で動作＝null セーフ）。
- per-arena 固有（`playerIndex`・各アリーナ子オブジェクト参照）は各コンポーネントが保持。
- `GameManager` — HP量・ダメージ量・ヒットストップフレーム数・コンボ閾値など（シングルトンなので元々共有）
- `BallScript` / `BlockSpawner` / `LaunchAimer` / `PlayerController` / `SkillController` / `ArenaController` / `DeadZone` — 共通値は `ArenaSharedConfig` 経由
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

### 7.4 InterferencePayload（攻撃トリガー共通フォーマット）

```csharp
public enum InterferenceType {
    Harden, AddRow, Poison, Slow, DirectAttack,
}
public class InterferencePayload {
    public InterferenceType type;
    public float intensity;   // 例: Poison の濃度倍率
    public float duration;    // 例: ZoneSlow の持続秒
    public int   sourcePlayerIndex;
}
```

**発火元**:
- 攻撃アイテム取得時に `ItemDrop.BuildAttackPayload()` が生成する
- 攻撃スキル発動時に `SkillAttack_*.Activate()` が生成する
- どちらも `GameManager.SendInterference(targetPlayerIndex, payload)` に渡される。`GameManager` は受信側 `ArenaController` の各 Spawn/Receive メソッドにルーティングする。

**重要**: コンボ閾値到達など自動発火経路は持たない。Payload は必ず能動的なプレイヤー操作（アイテム取得 / スキル発動）由来。

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
| damageBallDrop | 0 | ボール落下ダメージ（現在0） |
| damageBlockReachBottom | 15 | ブロック1個 底到達ダメージ |
| damagePoisonPerSec | 3 | 毒ダメージ/秒 |
| damageForceRespawn | 5 | 飛行中の発射キー押下ペナルティ |
| damageDirectAttack | 30 | 上部攻撃着弾ダメージ（Phase G+） |
| comboTimeout | 6.0 | 連続破壊間隔の上限（秒、超過で 0 リセット） |
| comboScoreStep | 5 | コンボ何個ごとにスコア倍率 +10% するか |
| comboGaugeStep | 5 | コンボ何個ごとにゲージ倍率 +5% するか |
| comboItemStep | 10 | コンボ何個ごとにドロップ倍率 +10% するか |
| energyPerBlock | 1 | ブロック破壊あたりのゲージ増加量（バンド・コンボ前） |
| interferenceTriggerFrames | 10 | 妨害受信側のヒットストップフレーム |
| roundEndFrames | 30 | ラウンド決着ヒットストップ |
| matchEndFrames | 60 | マッチ決着ヒットストップ |
| nextRoundDelay | 2 | 次ラウンドまでの待機秒 |
| roundsToWin | 1 | 先取本数（1~5本で選択） |
| dropChanceBuff / Attack | 0.6 / 0.4 | アイテム系統別の基本ドロップ比率（HP帯で偏重。実装は `Block.BASE_BUFF_WEIGHT`） |
| trapDisguiseChance | 0.1 | 強化枠を罠に偽装置換する確率（`Block` SerializeField。0 で罠無効） |
| itemLifetime | 8.0 | アイテムが消滅するまでの秒数（底到達より先に来た場合） |
| itemWarningTime | 2.0 | 消滅 N 秒前から点滅開始（itemLifetime - itemWarningTime = 6s） |
| comboMilestones[] | {10, 20, 30} | コンボマイルストーン演出の閾値一覧 |
| matchStats.blocksDestroyed[] | — | マッチ全体の総破壊ブロック数（P1/P2）。リザルト画面に表示 |
| matchStats.interferenceReceived[] | — | マッチ全体の受信妨害回数（P1/P2）。リザルト画面に表示 |
| roundScore[] | — | 現ラウンドのスコア差分（ラウンド開始時に 0 クリア）。ラウンド終了簡易リザルトに表示 |
| matchScore[] | — | マッチ全体の累積スコア（ラウンドをまたいで加算）。HUD と マッチ結果画面に表示 |

> **削除済み**: 旧 `comboThreshold`（コンボ自動妨害の閾値）。攻撃アイテム経由モデル移行（2026-05-20）で不要になった。

### BallScript
| フィールド | デフォルト | 意味 |
|---|---|---|
| baseSpeed | 7 | 基本速度 |
| minAxisRatio | 0.2 | 軌道補正 最小軸成分比率 |
| timeAccelRate | 0.05 | 時間加速量/秒 |
| timeAccelMax | 1.5 | 時間加速上限倍率 |
| hitStopSpeedThreshold | 1.4 | ヒットストップ発動速度倍率 |
| wallBounceFrames | 0 | 壁バウンス最大ヒットストップフレーム数 |
| normalDamage / iceDamage / heavyDamage / pierceDamage | 1 / 2 / 3 / 1 | 属性ダメージ |

### BlockSpawner
| フィールド | デフォルト | 意味 |
|---|---|---|
| blocksPerRow | 6 | 1行あたりのブロック数（6 × 1.5667 = 9.4 でアリーナ幅に収まる） |
| blockDeadZoneY | -4.5 | ブロックが到達してはいけない Y |
| blockDeadZoneHitFrames | 5 | 底到達ヒットストップフレーム数 |
| sabotageHardRatio | 0.5 | 妨害行の Hard vs Absorb 比率 |
| hardenCount | 3 | AttackHarden で Hard 化する個数 |
| hardenTargetHp | 3 | Hardened ブロックの HP |
| spawnIntervalBase | 5.0 | ラウンド開始時のスポーン間隔（Dynamic Escalation の起点） |
| spawnIntervalDecayPerMin | 0.2 | 1 分ごとにスポーン間隔が縮まる量 |
| spawnIntervalMin | 3.0 | スポーン間隔の下限 |
| descentSpeedBase | 0.3 | ラウンド開始時の降下速度（Dynamic Escalation の起点） |
| descentSpeedGainPerMin | 0.03 | 1 分ごとに降下速度が増える量 |
| descentSpeedMax | 0.45 | 降下速度の上限 |

> **削除済み**: 旧 `spawnInterval` / `descentSpeed` 固定値フィールド。Dynamic Escalation 導入（2026-05-20）により `spawnIntervalBase` / `descentSpeedBase` に置換。

### ZonePoison
| フィールド | デフォルト | 意味 |
|---|---|---|
| poisonRadius | 1.5 | パドルとの接触判定半径（OverlapSphere, unit） |
| duration | 6.0 | ゾーン持続秒数 |
| damagePoisonPerSec | 3 | HP 減少量/秒（GameManager.damagePoisonPerSec と同値） |

### ZoneSlow
| フィールド | デフォルト | 意味 |
|---|---|---|
| slowRadius | 2.0 | ボール検出半径（OverlapSphere, unit） |
| slowFactor | 0.5 | ボール速度倍率（0.5 = 半速） |
| duration | 6.0 | ゾーン持続秒数 |

### LaunchAimer
| フィールド | デフォルト | 意味 |
|---|---|---|
| metronomeAngleRange | 60 | インジケーター振れ幅（±度） |
| metronomePeriodSec | 1.0 | 往復周期（秒） |
| autoLaunchSec | 5.0 | 自動発射までの待機秒 |
| minAutoLaunchSec | 1.5 | ブロック最下段時の最短自動発射秒 |

### UIManager
| フィールド | デフォルト | 意味 |
|---|---|---|
| incomingDisplaySec | 3.0 | Incoming インジケータのアイコン1個あたりの表示時間（秒） |
| overlayFlashDuration | 1.5 | 妨害受信時の赤フラッシュ持続時間（秒） |
| sentLabelDuration | 1.5 | 攻撃送付ラベル（`SENT → P{N}`）の表示時間（秒） |
| comboMilestoneDuration | 1.2 | `{N} COMBO!!` オーバーレイの表示時間（秒） |
| hpColorBands | (white, yellow, red) | HP バーの色閾値（70%/30% で切替）|
| skillReadySuffix | ` · READY` | スキルゲージ満タン時のラベル末尾 |

---

## 10. 音響設計（SE / BGM）

ブロック崩しは音の手触りが体験を支配する。テンポを壊さない範囲で「打鍵感」を最大化する。

### 10.1 SE 設計指針

| カテゴリ | 必須度 | 音像イメージ |
|---|---|---|
| ボール反射（壁/パドル） | 必須 | 軽い「コッ」、ピッチが速度層 (`naturalSpeed/baseSpeed`) で +0〜+5 半音 |
| ブロック衝突（Normal） | 必須 | 短い「カチッ」、ピッチを `blockType` で固定差分 |
| ブロック破壊 | 必須 | 弾けるパッ + 弱い残響 |
| Explosive 爆発 | 必須 | 低音ボン + ヒットストップ同期 |
| Spike 接触 | 必須 | ザリッとした金属きしみ + 高音「ピリッ」 |
| 毒エリアダメージ | 中 | 1 秒ループのジリジリ音（ループ） |
| アイテムドロップ出現 | 中 | ポロン |
| 強化アイテム取得 | 必須 | 上昇アルペジオ（系統で音色変更） |
| 攻撃アイテム取得 | 必須 | 不穏な下降アルペジオ + 低音ヒット |
| 罠アイテム取得 | 中 | 「アッ」を想起させる滑り音 |
| スキル発動 | 必須 | チャージ完了の「キーン」+ 発動「シャッ」 |
| 妨害受信 | 必須 | 低音ドン + ノイズ短発（種別ごとにラベル発音） |
| ラウンド開始 | 必須 | カウントダウン声/ビープ（3 / 2 / 1 / GO） |
| ラウンド勝利 | 必須 | 短い勝利フレーズ |
| マッチ勝利 | 必須 | より長い勝利フレーズ |
| UI 移動・確定 | 必須 | カチ / ピッ |

### 10.2 BGM 構成

- **タイトル**: ループ可能なミッドテンポ。`MainTheme.ogg`（1 曲）。
- **試合中**: 2 段階構成。
  - 通常レイヤー: クール / ミッド BPM
  - HP 1/3 以下のいずれかが入った瞬間、層を 1 段足す（クロスフェード 1s）— 緊迫感の追加
- **マッチ結果**: 勝者側 BGM（短いジングル）。

### 10.3 実装メモ

- ミキサーは `Audio/MasterMixer.mixer`（新規）に Master / BGM / SE / Voice の 4 グループ。
- 音量設定は 0〜100 で調整。`PlayerPrefs` に `vol.master / vol.bgm / vol.se` で保存。
- 同時発音衝突対策として、ブロック衝突 SE は 50ms クールダウン（連打抑制）。1 アリーナごとに `lastBlockSeTime` を保持し、`Time.unscaledTime - lastBlockSeTime < 0.05f` なら無視する。
- SE ピッチ可変は `AudioSource.pitch` の動的書き換え。
- Phase F-Audio で音源を導入。生成優先順位: ブロック衝突 → ボール反射 → アイテム取得 → スキル → 妨害 → ラウンド遷移。

### 10.4 コードトリガーマッピング

各 SE がどこで発火するかを明示する。Phase F-Audio 実装時の参照テーブル。

| SE | 発火コード位置 | 補足 |
|---|---|---|
| `se_ball_wall` | `BallScript.OnCollisionEnter`（Block/PlayerController の GetComponent が両方 null = 壁）| pitch = 1 + (naturalSpeed/baseSpeed - 1) × 0.2 |
| `se_ball_paddle` | `PlayerController.OnCollisionEnter`（ball タグ）| 固定 pitch |
| `se_block_hit_normal` | `Block.OnCollisionEnter`（blockType=Normal で hp > 0）| 50ms クールダウン |
| `se_block_hit_hard` | 同上（blockType=Hard）| ピッチ -2 半音 |
| `se_block_hit_absorb` | 同上（blockType=Absorb）| 低音「ボフッ」 |
| `se_block_break` | `Block.OnDestroyed`（破壊確定時）| Explosive は別 SE |
| `se_block_explosive` | `Block.OnDestroyed` の Explosive 系統 | HitStop と同期 |
| `se_poison_loop` | `ZonePoison.Awake` でループ開始、`OnDestroy` で停止 | 滞在中のみ再生（ループ） |
| `se_item_drop` | `ArenaController.SpawnItem` 実行時 | 軽い「ポロン」 |
| `se_item_buff` | `ItemDrop.Update` パドル接触検出（系統=Buff）| 上昇アルペジオ |
| `se_item_attack` | 同上（系統=Attack）| 攻撃者側で再生（防御者には届く前に） |
| `se_item_trap` | 同上（系統=Trap）| 「アッ」のスリップ音 |
| `se_skill_ready` | `EnergySystem.OnEnergyFull` イベント（Phase F-Audio で追加）| キーン |
| `se_skill_activate` | `SkillController.Activate()` 開始時 | スキル種別ごとに微妙な差 |
| `se_interference_recv` | `GameManager.ApplyInterference` 内 | 種別ごとにラベル発音 |
| `se_round_start` | `GameManager.CountdownRoutine` 開始時に 1 回再生（3-2-1-GO! 全体で 1 ファイル）| 1.5s |
| `se_round_win` | `GameManager.EndRound` の勝者アリーナ決定直後、`ROUND WIN!` オーバーレイ表示と同フレーム | HitStop 30 フレーム中に再生開始（ヒットストップ時間で減衰）|
| `se_match_win` | `GameManager.EndMatch` の勝者決定直後 | より長い勝利フレーズ |
| `se_combo_milestone` | `UIManager.ShowComboMilestone` 呼出と同フレーム（10/20/30）| マイルストーン番号でピッチ +N 半音 |
| `se_addrow_land` | `BlockSpawner.SpawnSabotageRow` の着弾時 | AttackAddRow ドスッ |
| `se_ball_launch` | `LaunchAimer.ConfirmLaunch` 確定発射時 | 短い「シュッ」 |
| `se_center_tick` | `LaunchAimer.CheckCenterPass`（インジケーターが 0°=真上 を符号反転で横切った瞬間, 半周期に 1 回）| 短い「ティック」。発射タイミングの耳コピ用（5.3）|
| `se_ui_move` / `se_ui_confirm` | `SkillSelectUI` / `MatchResultUI` のカーソル移動・確定 | UI 共通 |

### 10.5 BGM クロスフェード規則

- **タイトル → 試合**: タイトル BGM をフェードアウト 0.5s、試合 BGM をフェードイン 0.5s（重なる 0.3s）。
- **試合中の段階クロスフェード**: P1 または P2 のいずれかが **HP ≤ maxHP × 0.3**（30% 帯）に入った瞬間、緊迫レイヤーへクロスフェード 1.0s。両方が 30% 帯から戻った場合のみ通常レイヤーへ戻る（1.0s）。フラッピング防止のため、戻る側のクロスフェードは「両方が 35% 以上」に達した時点で開始（5% のヒステリシス）。
- **試合中ラウンド遷移**: ラウンド決着 → 次ラウンドカウントダウン → Playing の間、BGM は止めない（連続性優先）。決着 SE/勝利ジングル（`se_round_win`）が BGM の上に重なる（ボーカル感を出す）。
- **試合 → マッチ結果**: マッチ決着の瞬間に試合 BGM をフェードアウト 1.0s、結果 BGM ジングル（短）を再生。ジングル終了後はループなし無音（プレイヤーの操作を急かさない）。
- **マッチ結果 → タイトル戻し**: 結果 BGM 終了後 or 「メニューへ戻る」選択時に Stop。タイトル BGM をフェードイン 0.5s。
- **マッチ結果 → 再戦**: 結果画面で「再戦」選択時、結果 BGM フェードアウト 0.3s → スキル選択画面（通常 BGM 継続）。

---

## 11. タイトル / メニュー / 設定

起動時は `GameState.Title` で待機し、START で SkillSelect → Playing へ進む（`TitleUI` 実装済み）。発表（2026-06-05）に向けて最低限の「ゲームとしての枠」を整える。チュートリアル / ポーズ / AI 対戦は 2026-05-28 改訂で廃止済み（このセクションには含めない）。

### 11.1 シーン構成（単一シーン / 状態切替）

**単一シーン方式を採用**（発表規模ではシーン分割しない）。`SampleScene` 内で `GameState` により UI パネルを `SetActive` 切替する：

```
[Title]        起動直後。ロゴ + メニュー（START / SETTINGS / QUIT）
   │ START
   ▼
[SkillSelect]  スキル選択（4枚カード）
   ▼
[Playing → RoundOver → ... → MatchOver]
   │ MatchOver の「メニューへ戻る」/ シーンリロード
   ▼
[Title]        へ戻る
```

実装: `GameManager.GameState` に `Title` を追加（旧 `WaitingToStart` を流用）。起動時は `Title`（`Time.timeScale=0`）。`StartFromTitle()` で `SkillSelect` へ遷移。

### 11.2 タイトル画面（最小）

| 要素 | 内容 |
|---|---|
| ロゴ | 「BurokkuKuzushi」タイトル + サブタイトル（任意） |
| メニュー | **START / SETTINGS / QUIT**（TUTORIAL は廃止）。選択中項目はテキスト色を変えて表現（別カーソル不要） |
| 操作 | 矢印 or W/S で項目選択、Enter / Space で確定。SETTINGS で 11.3 を開く |
| BGM | MainTheme（音声実装は Phase F-Audio。未実装時は無音で可） |

### 11.3 設定（最小）

タイトルの SETTINGS から開くオーバーレイ。発表向けの最小項目は **先取数のみ**。音量（BGM/SE）は音声未実装のため今回は持たない。アクセシビリティ（カメラシェイク/ヒットストップ OFF）も今回スコープ外（Phase G+）。

| 項目 | 内容 | 保存先 |
|---|---|---|
| 先取数 (rounds to win) | 1〜5 本（試合前に変更可） | `PlayerPrefs "match.roundsToWin"` → `GameManager.SetRoundsToWin()` |

---

## 12. エッジケース / 仕様の境界

実装時に曖昧になりやすい境界条件を明文化する。ここに記載がないケースは実装者の判断で決め、発覚したら追記する。

### 12.1 同時 HP 0

- 両プレイヤーが**同一フレーム**で HP 0 になった場合（例: ブロック底到達ダメージが同時に発生）、**先に Update が処理されたプレイヤーが敗者** とする（Unity の Update 順は最初にシーンに置かれた側が先）。
- 引き分けにはしない。極めてレアなケースのため、プレイヤーにとって不公平感はほぼ生じない。

### 12.2 追加ボールとラウンド終了の競合

- `SkillBall_Multi` で追加ボールが出ている最中にラウンドが終了した場合、追加ボールは**即時 Destroy**する。
- 追加ボールの落下は HP ダメージを与えない（落下ダメージはメインボールのみ）。
- 追加ボールを落としてもコンボはリセットしない（コンボはメインボールの DeadZone 通過でのみリセット）。

### 12.3 アイテム落下中のラウンド終了

- ラウンド終了時、まだ落下中のアイテムは**即時 Destroy**する。効果は適用しない。

### 12.4 ZonePoison / ZoneSlow の重ね掛け

- 同一アリーナに複数の ZonePoison が存在した場合、**ダメージは加算**する（2 個あれば 10 HP/秒になる）。
- ただし**同一アリーナの ZonePoison は最大 3 個**まで。4 個目の生成時に最も古いゾーンを Destroy する（インフレ防止）。
- ZoneSlow は最後に生成されたものの `slowFactor` が有効（複数存在しても効果は 1 個分、上書き方式）。

### 12.5 HitStop の重ね掛け

- 既存の HitStop が処理中に新たな HitStop が発火した場合、**残りフレームと新規フレームの大きい方** を採用する。フレームを加算はしない（無限に積み重なるバグを防ぐ）。
- 強シェイク (`strong=true`) は弱シェイクを上書きする。弱→強の途中変更は可能だが、強→弱への格下げはしない。

### 12.6 AttackHarden の対象がない場合

- 相手アリーナに Normal ブロックが 1 個も存在しない場合（すべて Hard / Hardened / Absorb / Explosive / Spike）、AttackHarden は**効果なしで消費**される。通知や返金はしない。
- この状況は試合中盤以降にまれに発生しうるが、攻撃タイミングを読む戦略要素として許容する。

### 12.7 コンボタイマーとボール落下の競合

- ボールが DeadZone に入った瞬間（`OnTriggerEnter`）に、コンボを**即時 0 リセット**する。comboTimer のカウントダウン判定より先に処理する。
- メインボール落下でのみリセット。追加ボール落下ではリセットしない（12.2 参照）。
- `comboTimeout` によるリセットと落下リセットは独立したトリガーとして扱う。

### 12.9 DirectAttack 予告中のラウンド終了（Phase G+）

- 予告マーカーが表示中にラウンドが終了した場合、マーカーと着弾処理を**即時キャンセル**する。ダメージは発生しない。
- ラウンドをまたいでの攻撃は認めない。

### 12.10 コンボマイルストーンの重複発火防止

- コンボ 10 を達成した後、11・12 と続いても `10 COMBO!!` は再表示しない。
- コンボが 0 にリセットされ、再度 10 に到達した場合は改めて演出を発火する（ラウンド内のマイルストーン到達回数は無制限）。

### 12.12 ラウンド開始カウントダウン中の入力

- 3-2-1-GO! のカウントダウン中は `GameState = Countdown`。PlayerController は移動入力を受け付ける（パドルポジショニング許可）が、発射キー（S/K）は無効化する。ボールはパドル上で静止し続ける。
- LaunchAimer は GO! と同時に起動（メトロノーム開始）。
- カウントダウン中にスキルキー（Q/U）を押しても発動しない（ゲージ 0 蓄積も停止しているため）。
- カウントダウン中はブロックも Freeze（落下停止）。スキル蓄積（`SkillController` の gauge 加算）も停止する。

### 12.15 DOUBLE BALL 中のコンボ管理

- `SkillBall_Multi` による追加ボールがブロックに命中した場合、**コンボカウントに加算する**（追加ボールのヒットもコンボ継続と見なす）。
- 追加ボールが DeadZone に落下した場合、**コンボはリセットしない**（12.2 参照。落下ダメージも発生しない）。
- コンボタイマーは「最後にいずれかのボール（メイン / 追加）がブロックを破壊した時点」から計測する。追加ボールが存在する間は実質的にコンボが切れにくくなり、`SkillBall_Multi` の戦略的価値が増す。

**設計意図**: 追加ボールがコンボに貢献できることで「DOUBLE BALL を使いながらコンボを延ばす」プレイスタイルが成立する。単なる防御スキルから「コンボ加速器」としての側面を持たせる。

### 12.17 BlockItem の詳細

- `BlockItem` は `BlockType.Item` の通常行扱いで、標準スポーン時に `specialRowChance`（スペシャル行確率）とは別に混入する可能性がある（または特定の行構成で出現する）。**現状の仕様では「特定の行構成には含まない」— 単独スポーンまたは通常行の一要素として Phase C で実装済み。詳細な出現ロジックは実装者の判断**（Phase C 時点の `BlockSpawner` ロジックに従う）。
- `BlockItem` を破壊すると **確定で 1 個アイテムドロップ** する（通常の確率ドロップではなく確定）。
- ドロップするアイテムの系統分布は通常の抽選と同じ（HP 帯バンドを参照）。強化・攻撃・罠のいずれかをランダムに選ぶ。
- `BlockItem` 自体は HP1（1 撃で破壊）。ブロック IP ドットは表示しない（1 撃確定なので不要）。

### 12.18 TrapBall_Reversed 中の発射操作

- `TrapBall_Reversed` 有効中に LaunchAimer でボールを発射する場合、**発射方向の反転はしない**（入力反転はパドル移動のみ）。
- 発射確定キー（S / K）の機能は変わらない。「発射後に変な方向に飛んだ」のはパドルが逆に動いた混乱によるものであり、発射判定自体は通常通り行われる。
- `TrapBall_Reversed` 中に罠を取ってしまった場合（Shrink など）、重複効果は通常どおり上書き/加算される（特別なインタラクションなし）。

### 12.21 スコア表示の累積 vs ラウンド単位

- **HUD のスコア表示（`$P1ScoreValue`）**: マッチ全体の**累積スコア**を表示する。ラウンドをまたいで加算される。
- **ラウンド終了の簡易リザルト（キー待ち）**: **そのラウンドだけの獲得スコア** を表示する（累積ではなく、そのラウンドの差分）。表示ラベル: `ROUND SCORE: {N}`
- **マッチ結果画面**: **累積スコア**（マッチ全体）を表示する（カンマ区切り）。
- 実装: `GameManager` は `roundScore[pi]` と `matchScore[pi]` を独立して保持する。ラウンド開始時に `roundScore` を 0 クリア、`matchScore` は持ち越し。

### 12.22 Combo Timer Arc と HitStop の時間軸

- `comboTimer[pi]` は**ゲーム時間**（`Time.deltaTime`）で加算する。HitStop 中は `IFreezable.Freeze()` で BallScript / PlayerController / BlockSpawner が止まるが、UIManager は止まらない。
- UIManager.Update() は毎フレーム実行される。HitStop 中も `comboTimer` を更新し、Combo Timer Arc の fillAmount を更新し続ける。
- つまり HitStop 中にもコンボタイマーが進む（フリーズするのはボールとブロックであり、時計は止まらない）。これは意図通り — HitStop は「演出の一時停止」であってゲーム状態の巻き戻しではない。
- `UIManager.Update()` で使う `timeSinceLastBlockHit[pi]` は `Time.deltaTime` 加算版（`Time.unscaledDeltaTime` は使わない）。HitStop によって `Time.timeScale` は変更しないため、両者は同じ値になる（このゲームは timeScale=0 を SkillSelect / MatchOver 等の UI 待機状態でのみ使用。ポーズ機能は廃止済み）。

---

## 13. アクセシビリティ

### 13.1 視覚的配慮

ゲームは色を主要な情報担体として使っているが、色覚異常（日本人男性の約 5%）を持つプレイヤーに向けて追加の識別手段を設ける。

#### アイテムのシェイプ+カラー識別

色だけに頼らず、**形状** も系統の判断材料にする。

| 系統 | 色 | 形状 | 付加記号 |
|---|---|---|---|
| 強化（Buff） | 青系（Cyan） | 円形（○） | 上向き矢印（↑） |
| 攻撃（Attack） | 赤系（Red/Orange） | 星形（★）または棘付き | 矢印＋「!」 |
| 罠（Trap） | 紫系（Purple） | 三角形（△）※ 強化と「形が紛らわしい」ことは意図的だが基本形は異なる | 下向き矢印（↓） |

形状の差により、色を正しく識別できない場合でも「丸は強化、棘は攻撃」と判断できる。

#### ブロックの識別

| ブロック種 | 色 | 追加識別 |
|---|---|---|
| BlockNormal | 白 | 形状のみ（プレーン） |
| BlockHard | 灰 | 横線テクスチャ |
| BlockAbsorb | 緑 | 波線テクスチャ |
| BlockExplosive | 橙 | 爆発記号（☆） |
| BlockHardened | 金 | 金色（輝度で区別、HDR 発光） |

### 13.2 操作・環境的配慮

> ⚠️ 以下のカメラシェイク / ヒットストップ強度設定は **Phase G+ のスコープ外**（11.3 の最小設定には含まれない。発表版の設定画面は先取数のみ）。設定 UI を拡張する際に追加する。
- **カメラシェイク OFF 設定（Phase G+）**: 設定画面に `カメラシェイク: OFF / NORMAL / STRONG` オプションを追加予定（光敏感者・モーションシック配慮）。
- **ヒットストップ強度 OFF 設定（Phase G+）**: 設定画面に `ヒットストップ強度: OFF / LOW / NORMAL / HIGH` オプションを追加予定（動きのスムーズさを優先したいプレイヤー向け）。
- **キーボードゴースト注意**: ローカル 2P で同一キーボードを使う場合、A+D+J+L+S+K+Q+U の同時押しが発生しうる。多くのキーボードで 6 キー以上の同時押しは誤検出が起きる。発表時は「このゲームは 2 つのキーボードでの分担プレイを推奨」と案内するか、ゲームパッドオプション（Phase G+ 以降）を検討する。

### 13.3 Bloom / フラッシュ強度

- 強い発光やフラッシュは光感受性発作のトリガーになりうる。設定画面の `カメラシェイク` を OFF にした場合、以下も自動的に減衰する（連動）:
  - 妨害受信時の赤フラッシュ alpha 最大値を 1.0 → 0.5 に減衰
  - Bloom Intensity 上限を 50% に制限（READY 表示などの脈動）
  - Last Stand 演出の赤化を点滅ではなく静的表示に変更
- 個別に発光のみ抑制したい場合は `fx.bloom` PlayerPrefs（OFF / NORMAL）を Phase G+ で追加予定。

### 13.4 音響アクセシビリティ

- 重要な状況通知は **必ず視覚的にも表示**（音だけに依存しない）。SE で知らせる項目は HUD に同期する:
  - スキル READY → `· READY` テキスト
  - 妨害受信 → 赤フラッシュ + `INCOMING: 〈種別〉` テキスト
  - コンボマイルストーン → `{N} COMBO!!` 大きく
- BGM 音量 0 設定でもゲームの本質的な情報伝達に支障が出ないこと（playtest で確認）。
- 補助テキストフォントは JetBrainsMono（モノスペース）を使い、視認性を上げる。
- 将来的に字幕オプション（妨害受信ラベルを読み上げない代わりにテキスト固定表示）を Phase G+ で検討。

### 13.5 操作形態（Phase G+）

発表時は既定キー固定（変更不可）。Phase G+ で対応予定。要件:

- ゲームパッド対応（Xbox / PS / 任意の HID）— `Unity Input System` 経由

### 13.6 言語

- 発表時点では英語のみ（テキストはハードコード）。
- 日本語対応はフォントの問題あり

---

## 14. 関連ドキュメント

- 実装の As-Built 対応表（本仕様 ↔ 実装の差異・未実装）: [`IMPLEMENTATION.md`](./IMPLEMENTATION.md)
- 開発フェーズ・進捗管理: [`ROADMAP.md`](./ROADMAP.md)
- 実装アーキテクチャ詳細: [`ARCHITECTURE.md`](./ARCHITECTURE.md)
- C# / Unity 学習ロードマップ: [`LEARNING.md`](./LEARNING.md)
- 実装の引継ぎ情報: [`../CLAUDE.md`](../CLAUDE.md)
- バランス詳細: [`BALANCE.md`](./BALANCE.md)
- 発表ガイド: [`PRESENTATION.md`](./PRESENTATION.md)
- アセット一覧: [`ASSETS.md`](./ASSETS.md)
