# IMPLEMENTATION.md — 実装仕様書（As-Built）

このドキュメントは **「DESIGN.md（目標仕様）に対して、実際にどう実装したか」** を仕様の節番号順にまとめた対応表である。
仕様からの差異・未実装が増えてきたため、両者の乖離を一望できるようにすることが目的。

## ドキュメントの三層運用

| ドキュメント | 役割 | 視点 |
|---|---|---|
| [`DESIGN.md`](./DESIGN.md) | **目標仕様**（最新仕様の真実） | 「こうしたい」 |
| 本ファイル `IMPLEMENTATION.md` | **As-Built 対応表**（仕様↔実装の差分・未実装の一覧） | 「実際こう作った / まだ作っていない」 |
| [`../CLAUDE.md`](../CLAUDE.md) | **現在のコード状態**（スクリプト/シーン単位の技術詳細） | 「コードがどうなっているか」 |

> 仕様を変える時は **まず DESIGN.md を更新** → 実装 → **本ファイルの該当節を更新**。
> 本ファイルは DESIGN.md の節番号（5.2, 5.4, 12.x …）にそのまま対応する。

## 凡例

| 記号 | 意味 |
|---|---|
| ✅ | 仕様どおり実装済み |
| ⚠️ | **仕様と差異あり**（実装側を正とした変更／暫定実装） |
| ❌ | **未実装**（発表スコープ内・要対応） |
| ◐ | コードは実装済みだが **UI 要素が未配置・未バインド**（配置すれば動く） |
| 🔵 | Phase G+（DESIGN が発表後スコープと明記） |

最終更新: 2026-06-03。発表: 2026-06-12。

---

## 差異サマリ（⚠️ 仕様から変えた部分）— まずここを見る

| 節 | 項目 | 仕様（DESIGN） | 実装（As-Built） | 理由 |
|---|---|---|---|---|
| 5.2 | Heavy 属性 | （貫通的な強属性の記述） | **非貫通＝通常反射**（高ダメ・速度0.7倍） | 貫通は Pierce に一本化（2026-06-03） |
| 5.2 | Pierce 貫通 | ブロックを通り抜ける | **`Physics.IgnoreCollision`+overlap ダメージで物理素通り**（旧: `lastVelocity` 復元のみ） | 反発の押し戻しで軌道が折れトレイルがカクついたため（2026-06-03） |
| 5.4 | Explosive | 破壊で周囲に巻き込みダメージ＋連鎖爆発 | ✅ DESIGN 準拠に作り直し済み（旧実装は「周囲を硬くする妨害」で**真逆**だった） | 仕様乖離を是正（2026-06-02） |
| 5.4 | ブロック種別 | （Spike 等の記述があれば） | 実装は **Normal / Hard / Absorb / Explosive / Item** の5種。**Spike はコードに無い** | — |
| 5.9 / 7.3 | ヒットストップ | 「衝突でフリーズ＋シェイク」 | **フリーズはボール衝突のみ**。非衝突イベント（底到達・スライド着地・**妨害受信**・**スキル発動**）は**シェイクのみ（`freeze:false`）** | 飛行中ボールを空中で止めないため（2026-06-03） |
| 5.9 | シェイク対象 | カメラシェイク | **単カメラのため `Arena{N}/ShakeRoot` を揺らす**。ボールは ShakeRoot 外でシェイク非干渉。アリーナ枠 `P{N}ArenaFrame` も同期シェイク | 単カメラ構成＋Rigidbody が親シェイクで teleport される問題（2026-06-03） |
| 5.3 | パドル反射ゾーン | （角度反射ゾーンの記述） | **廃止＝単純物理反射に統一**（2026-05-28 仕様削除） | — |
| 5.6 | スキル | （CATCH & SHOOT 含む構想） | `SkillForceCatch`（CATCH & SHOOT）は**廃止**。実装は4種 | 2026-05-28 仕様改訂 |
| 5.5 | アイテム寿命 | `itemLifetime`=8s で消滅 | **実装しない方針**（2026-05-29 判断） | — |
| 6.1 | UI/カメラ | 画面分割2カメラ | **単一 Ortho カメラ**で両アリーナ横並び | ポスプロ/UI 単純化 |
| 11 | ポーズ/チュートリアル/AI | 構想あり | **廃止**（2026-05-28） | 発表スコープ縮小 |

詳細は各節を参照。

## 未実装サマリ（❌ / ◐）— 発表前の残作業

| 分類 | 項目 | 状態 |
|---|---|---|
| 演出(VFX) | ボール属性ビジュアル（Fire 炎/Thunder 電気のパーティクル・オーラ） | ❌ 色とトレイル色のみ |
| 演出(VFX) | Explosive / Fire の攻撃範囲を示すエフェクト | ❌ 挙動のみ DESIGN 準拠、範囲表示なし |
| 演出(VFX) | ブロック起源オーラ（Neutral/Self/Opponent の無/青/赤） | ❌ 起源を追跡していない |
| 演出 | エイマーの振れ角幅（扇）・予想軌道・センター通過音/ビジュアル | ❌ 現在方向の2点ラインのみ |
| 演出 | ラウンド決着のテキスト overlay（`ROUND WIN!`/`OVER`） | ❌（枠の輝度フラッシュ/敗者暗転は ✅） |
| UI | ROUND {N} ヘッダ + 先取数ドット | ❌ 勝数テキストはあるが中央ヘッダ/ドット無し |
| UI | Last Stand「OPPONENT 危険!」相手 HUD 通知 | ❌（自陣の枠/HPバー/PANIC READY は ✅） |
| UI(◐) | Victory Bar / Energy ゲージ / スキル名(READY) / Incoming / 妨害受信 overlay / SENT ラベル / コンボマイルストーン overlay / Combo Timer Arc / 複数アイテム同時表示 | ◐ コード済・**要素未配置→配置後バインド** |
| Audio | BGM クリップ（4種、クロスフェード/緊迫レイヤーはコード済） | ◐ **全クリップ未配置**（最大の体験ギャップ） |
| Audio | `se_addrow_land` / `se_special_row` クリップ | ◐ 発火点配線済・クリップ未配置 |
| Audio | AudioMixer（Master/BGM/SE バス + Expose Param） | ❌ 未作成 |
| アクセシビリティ | アイテムのシェイプ識別（円/星/三角+記号）・ブロックのテクスチャ識別 | ❌ 色のみ |
| ブロック | Hard クラック段階（残HPでひび） | ❌（HP pip ドットで代替・カット推奨） |

---

## 5. システム仕様

### 5.1 HP ✅
- `HPSystem`（純 C# クラス）でプレイヤーごとに管理。`GameManager.ApplyDamage()` が全ダメージの最終窓口。
- HP 帯（`HPStateBand[]`）で動的パラメータ倍率。`GetCurrentBand` で参照。
- ダメージ表・回復は仕様どおり。

### 5.2 ボール ⚠️
- ✅ 速度の3層管理（`naturalSpeed` × `speedMultiplier` × `slowZoneMul`）、時間加速（メインボールのみ）、軌道補正（`ClampAngle` で壁沿いループ防止）、最小軸成分比率。
- ✅ 属性5種: `Normal / Fire`(範囲) `/ Thunder`(同種連鎖) `/ Ice`(高ダメ) `/ Heavy / Pierce`。
- ⚠️ **Heavy = 非貫通**（通常反射・高ダメ・速度0.7倍）。DESIGN の貫通的解釈から変更し、貫通は Pierce のみに一本化（2026-06-03）。
- ⚠️ **Pierce = 物理素通り**: 旧実装は衝突後に `lastVelocity` を復元するだけで、反発の押し戻しにより軌道が折れトレイルがカクついた。現在は Pierce 中 `FixedUpdate` で `OverlapSphere` 検出 → `Physics.IgnoreCollision(ball, block, true)` で**反発を無効化して直進**、ダメージは overlap で1回だけ（`pierceIgnored` で重複防止／高速衝突時は従来復元がフォールバック）。`RestorePierceCollisions()` で解除（2026-06-03）。
- ✅ **Ball Heat**（5.3 由来の演出）: Normal 時にコンボ段階で白→クリーム→橙→赤、トレイルも追従。
- ❌ **属性ビジュアル**（Fire 炎/Thunder 電気のパーティクル・オーラ）は未実装。色とトレイル色のみ。
- ❌ **属性/範囲の VFX**（範囲ダメージの可視化）未実装。

### 5.3 パドル / 発射 ⚠️
- ✅ パドル: kinematic + `localPosition` 直接操作。一時効果（幅・入力反転）。
- ⚠️ **パドル反射ゾーン（角度反射）は廃止**（2026-05-28、単純物理反射へ統一）＝未実装ではなく仕様削除。
- ✅ **メトロノーム発射**（`LaunchAimer`）: `±metronomeAngleRange°` を sin 波往復、確定キーで発射。発射は `GameState.Playing` 限定。
- ❌ エイマーの**振れ角幅（扇）表示** / **予想軌道（壁反射込み）** / **センター通過音** / **センター通過ビジュアル** は未実装（現在方向の2点 LineRenderer のみ）。

### 5.4 / 5.4.1 ブロック ⚠️
- ✅ 種別: **Normal / Hard / Absorb / Explosive / Item**。⚠️ **Spike はコードに無い**。
- ⚠️ **Explosive**: DESIGN 準拠に作り直し済み（2026-06-02）。破壊で `OverlapSphere(explosionRadius)` 内に巻き込みダメージ、HP0 で各自 `OnDestroyed` → Explosive なら連鎖（`destroyed` フラグで無限再帰防止）、スコア/コンボは破壊数ぶん加算。**旧実装は「周囲ブロックの HP を増やす＝硬くする妨害」で仕様と真逆だった**（撤去済み）。
- ✅ **Item ブロック**: HP1・破壊で**確定**1個ドロップ（`itemBlockChance`=0.08）。
- ✅ **HP pip**（残耐久ドット）: HP>1 に子キューブのドットを hp 個生成し被弾で減少。⚠️ DESIGN の「Hard クラック（ひび）段階」は pip で代替＝**クラックは未実装**。
- ✅ **スペシャル行**（全Item/全Explosive/歯抜け、`specialRowChance`=0.125）、**行スライドイン演出**（通常は控えめ／妨害行は派手＋着弾フラッシュ＋hitstop）。
- ✅ **Danger Proximity**（最下段が死線接近で `P{N}BlockDeadLine` を赤明滅）、底到達ペナルティの白フラッシュ。
- ✅ **Dynamic Escalation**（5.4.1）: 降下速度/行間隔の base/decay/min・base/gain/max ＋ `roundElapsedTime`。
- ❌ **ブロック起源オーラ**（Neutral/Self/Opponent の無/青/赤）: 起源を追跡しておらず未実装。色は種別カラーのみ。
- ❌ **Explosive 爆発エフェクト**（範囲 VFX）未実装。挙動のみ。
- 既知: `Block` のスコア（`normalScore`10/`hardScore`20/`explosiveScore`30）は Prefab 依存でハードコード相当。

### 5.5 アイテム ⚠️ / ◐
- ✅ **15種**を3系統で実装: Buff(属性) `Fire/Ice/Thunder/Heavy/Pierce` / Buff(パドル・回復) `Enlarge/SpeedUp/Heal` / Attack(妨害送付) `AttackHarden/AttackAddRow/AttackPoison/AttackSlow` / Trap `Shrink/Hyper/Reversed`。
- ✅ ドロップ: 確率ドロップ＋ Item ブロックは確定。**ドロップ過多抑制**（同スロット再抽選・`IsEffectSlotActive`）。
- ✅ 取得時のパドルフラッシュ（系統色）。罠アイテムは強化枠に偽装（`trapDisguiseChance`）。
- ⚠️ **アイテム寿命**（`itemLifetime`=8s）は**実装しない方針**（2026-05-29）。落下して底を超えたら破棄のみ。
- ◐ 複数アイテム同時表示 UI は未配置（バックエンドは `GetActiveEffects()` で全件取得可、HUD は末尾1個表示）。
- ❌ アイテムのシェイプ識別（13.1）は色のみ。

### 5.6 スキル ⚠️
- ✅ エナジーゲージ（`SkillController`/`EnergySystem`）、スキルキー（1P:Q / 2P:U）。
- ✅ 4種: `SkillPaddle_Enlarge` / `SkillBall_Attribute_Fire` / `SkillBall_Multi` / `SkillPanic_BlockClear`。
- ⚠️ **`SkillForceCatch`（CATCH & SHOOT）は廃止**（2026-05-28、コードも削除）。
- ✅ スキル選択画面（`SkillSelectUI`、4枚カード GameObject 切替方式）。⚠️ DESIGN/旧記述の「色ハイライト方式」ではない。◐ カーソル/Ready 配列は要バインド。

### 5.7 妨害（攻撃アイテム経由）✅ / ◐
- ✅ `EffectAttack` → `GameManager.SendInterference` 経路。**コンボ自動妨害は撤廃**。4種（Harden/AddRow/Poison/Slow）。
- ✅ `ZonePoison`（毒・端数累積ダメージ）/ `ZoneSlow`（減速ゾーン）。
- ✅ Incoming インジケータのキュー（FIFO 3件・3s 失効、バックエンド）／妨害受信オーバーレイ／SENT ラベル（いずれも ◐ 要素未配置）。
- ⚠️ 妨害受信のヒットストップは **シェイクのみ（`freeze:false`）** に変更（5.9 参照、2026-06-03）。
- ❌ アリーナ間オーブ飛翔エフェクト（高コスト VFX・カット推奨）。

### 5.8 コンボ・スコア ✅
- ✅ **コンボ = ブロック破壊ごとに +1**（2026-06-01 に接触ベースから戻した）。`comboTimeout`(6s)/落下リセット。`RegisterBlockDestroyed` がコンボ++/タイマー/マイルストーン/エナジー蓄積を担当。
- ✅ score/gauge/itemDrop のコンボ倍率。マイルストーン発火（10/20/30）。
- ◐ コンボマイルストーン overlay / Combo Timer Arc は要素未配置。

### 5.9 ヒットストップ ⚠️（2026-06-03 大幅変更）
- ⚠️ **フリーズ/シェイク分離**（`TriggerHitStop(frames, strong, shake, freeze)`）。**フリーズ（ボール停止）はボール衝突イベントのみ**。
  - フリーズあり（衝突）: ブロック衝突 / 壁バウンス / パドル反射 / Explosive 破壊。
  - **シェイクのみ（`freeze:false`）**: 底到達 / スライド着地 / **妨害受信** / **スキル発動**。飛行中ボールを空中で止めないため。
  - 例外: ラウンド/マッチ決着は**意図的にフリーズ**（勝者は `shake:false` でフリーズが唯一の演出、かつ決着済みで飛行中ボールが無い）。
- ⚠️ **シェイク対象 = `Arena{N}/ShakeRoot`**（壁/パドル/DeadZone/BlockSpawner を収める空オブジェクト, local 0,0,0）。**Ball は ShakeRoot 外（Arena 直下）** に置き、シェイクで Rigidbody が teleport される問題を回避。
- ⚠️ **アリーナ枠 `P{N}ArenaFrame` も同期シェイク**（`SetFrameShakeTarget`、world position を同一オフセットで揺らす）。
- ✅ `IFreezable`（Ball/Spawner/Player）、多重発火ガード（`RestoreShakeTarget`/`activeFroze`）。
- 全トリガーの一覧は CLAUDE.md「HitStopController」節を参照。

### 5.10 ラウンド/マッチ終了 ✅ / ❌
- ✅ フロー: `EndRound` → 先取数判定で MatchOver / RoundOver。`RoundResultUI` / `MatchResultUI`（フルスタッツ版・再戦/メニュー）。`RoundIntermissionRemaining`（unscaled カウントダウン）。
- ✅ **勝者枠フラッシュ / 敗者暗転**（`FlashRoundResult`）。
- ❌ `ROUND WIN!` / `OVER` テキスト overlay（要素未配置）。BackdropBlur との協調に注意。
- ✅ マッチ統計（最大コンボ/総破壊/被妨害）。

### 5.11 Phase G+ 拡張 🔵
- Gate / 自陣 Zone(S) / DirectAttack / 罠 ViewDisturb は **発表後スコープ**（DESIGN 明記）。

---

## 6. UI

### 6.1 画面構成 ⚠️
- ⚠️ **単一 Ortho カメラ**（旧: 画面分割2カメラ）。`_UI/_CameraSpace`（Screen Space - Camera）配下に階層化。Figma 準拠の命名（`_Folder` / `$Dynamic` / `P1`/`P2`）。

### 6.2 各表示要素 ✅ / ◐ / ❌
- ✅ HP バー（Sliced を `sizeDelta.x` で削る）/ Combo / Score（×10 表示）/ ActiveItem。
- ◐ Victory Bar / Energy / スキル名(READY) / Round ドット / Incoming / Combo Arc は**要素未配置**。
- ❌ **ROUND {N} ヘッダ + 先取数ドット**、**アイテムアイコン画像**（名前テキストのみ）。

### 6.3 視覚演出 ✅ / ❌
- ✅ Bloom（HDRTint/HDRUnlit シェーダ、`BreathPulse`）、Danger Proximity、**Last Stand**（自陣枠の輝度低下・HPバー赤明滅）、行スライドイン、`BackdropBlur`（メニュー磨りガラス）。
- ❌ **Last Stand の「OPPONENT 危険!」相手 HUD 通知**は未実装（自陣演出のみ）。

---

## 7. アーキテクチャ

### 7.1 パラメータ管理 ⚠️
- ⚠️ **ScriptableObject/Profile は不採用**。各コンポーネントの SerializeField 直接管理 ＋ 左右共通値は **`ArenaSharedConfig`**（シーン内 1 個）に集約し各自が `ApplySharedConfig()` で適用（null セーフ）。

### 7.2 EffectDefinition ✅
- 抽象基底＋実装（`EffectBallAttribute`/`EffectPaddleScale`/`EffectBallSpeed`/`EffectHeal`/`EffectAttack`/`EffectInputReverse`）。

### 7.3 IFreezable ✅（5.9 と連動）
- `Freeze()`/`Unfreeze()` の2メソッド。Ball/Spawner/Player が実装。⚠️ フリーズはボール衝突時のみ発火（5.9）。

### 7.4 InterferencePayload ✅
- 攻撃トリガーは `GameManager.SendInterference`（受信側 index + 種別）に集約。

### 7.5 シーン構造 ⚠️（2026-06-03 変更）
- ⚠️ `Arena{N}` 直下に **`ShakeRoot`** を新設。`Ball` と `ArenaController`（+HitStop/LaunchAimer）は Arena 直下、**壁/Player/DeadZone/BlockSpawner は ShakeRoot 配下**。
- ⚠️ これに伴い `Block`/`BlockSpawner`/`PlayerController` の `GetArena()` を `transform.root` 基準へ修正（ShakeRoot を挟んでも壊れない）。
- ✅ `GameManager` シングルトン中央イベントハブ。ローカル座標系（アリーナ親基準）。

---

## 10. 音響設計 ◐ / ❌
- ✅ `AudioManager`（dB 変換・SE トリガーマッピング・50ms クールダウン・BGM クロスフェード/緊迫レイヤーの**コードは全て実装**）。
- ◐ **BGM クリップ全未配置**（4種）＝最大の体験ギャップ。`se_addrow_land`/`se_special_row` も未配置（発火点は配線済）。
- ❌ **AudioMixer**（Master/BGM/SE バス + Expose Param）未作成。

---

## 11. タイトル / メニュー / 設定 ⚠️
- ✅ `GameState` 7状態: Title / Settings / SkillSelect / Countdown / Playing / RoundOver / MatchOver。`Countdown`（3,2,1,GO!）実装済み。
- ✅ `TitleUI`（最小「PRESS TO START」点滅）/ `SettingsUI`（**先取数のみ**）。
- ⚠️ **ポーズ / チュートリアル / AI対戦（`AIPlayerController`）は廃止**（2026-05-28）。設定は音量/アクセシビリティを含めない最小構成。

---

## 12. エッジケース ✅（2026-06-02 実コード検証で全確定）
- ✅ 12.1 同時HP0（先処理側が敗者・`currentState!=Playing` ガード）/ 12.2 追加ボール落下（Destroy・ダメ無・コンボ非リセット）/ 12.3 落下アイテム破棄 / 12.4 Zone 重ね / 12.5 HitStop 重ね（復元バグ修正済）/ 12.7 落下でコンボ0 / 12.10 マイルストーン重複防止 / 12.12 カウントダウン中入力（移動のみ可・発射不可）/ 12.15 DOUBLE BALL（メインのみ加算）/ 12.17 BlockItem 確定 / 12.18 Reversed（移動のみ反転）/ 12.21 スコア累積（BeginMatch のみ0）/ 12.22 Combo Arc。
- 🔵 12.9 DirectAttack 予告中終了は Phase G+。

---

## 13. アクセシビリティ ❌ / 🔵
- ❌ 13.1 アイテムのシェイプ識別（円/星/三角+記号）・ブロックのテクスチャ識別（横線/波線/爆発記号）は**色のみ**で未実装。
- 🔵 13.2/13.3 カメラシェイク/ヒットストップ/Bloom 強度の**設定 UI** は Phase G+（発表設定は先取数のみ）。
- 🔵 13.5 ゲームパッド / 13.6 多言語は Phase G+。
- ◐ 13.4 音響アクセシビリティ（SE の視覚同期）は対応テキスト類の未バインド分だけ ◐。

---

## 関連
- 詳細なスクリプト/シーン技術情報: [`../CLAUDE.md`](../CLAUDE.md)
- 発表逆算スケジュール: [`./ROADMAP.md`](./ROADMAP.md)
- バランス哲学: [`./BALANCE.md`](./BALANCE.md)
