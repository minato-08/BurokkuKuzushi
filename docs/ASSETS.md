# BurokkuKuzushi アセットチェックリスト

最終更新: 2026-05-20

「何のアセットが必要か」「どこに置くか」「どのフェーズで作るか」をまとめる。音響設計の詳細は [`DESIGN.md` 10節](./DESIGN.md) を参照。

---

## 1. アセット管理方針

| カテゴリ | 格納パス | フォーマット |
|---|---|---|
| SE | `Assets/Audio/SE/` | `.wav`（短発）/ `.ogg`（ループ） |
| BGM | `Assets/Audio/BGM/` | `.ogg` |
| アイテムアイコン | `Assets/Sprites/Items/` | `.png`（128×128 推奨） |
| ブロックテクスチャ | `Assets/Sprites/Blocks/` | `.png` |
| パーティクル | `Assets/Particles/` | Unity Particle System |
| フォント | `Assets/` | `.ttf` + TMP SDF Asset |
| シェーダー | `Assets/Shaders/` | `.shader` |

**ライセンス管理**: CC0 / Creative Commons 素材のみ使用。素材と同フォルダに `LICENSE.txt` を配置し素材名と出典 URL を記録する。

---

## 2. SE（効果音）

### 2.1 必須 SE（Phase F-Audio: 〜2026-05-29）

実装優先順位順。上から着手する。

| ファイル名 | 内容 | 長さの目安 | 音像 |
|---|---|---|---|
| `se_block_hit_normal.wav` | BlockNormal 衝突（非破壊） | 0.10〜0.15s | 短い「カチッ」 |
| `se_block_hit_hard.wav` | BlockHard 衝突（非破壊） | 0.15s | 鈍い「ゴッ」 |
| `se_block_hit_absorb.wav` | BlockAbsorb 衝突 | 0.20s | 低音・吸収感「ボッ」 |
| `se_block_break.wav` | ブロック破壊 | 0.20〜0.30s | 弾けるパッ + 弱残響 |
| `se_block_explosive.wav` | BlockExplosive 爆発 | 0.40s | 低音ボン（ヒットストップと同期） |
| `se_block_spike.wav` | BlockSpike 接触 | 0.25s | ザリッ + 高音ピリッ |
| `se_ball_wall.wav` | ボール壁バウンス | 0.05〜0.10s | 軽い「コッ」 |
| `se_ball_paddle.wav` | ボールパドル反射 | 0.10s | やや重い「コッ」 |
| `se_item_drop.wav` | アイテムドロップ出現 | 0.20s | ポロン |
| `se_item_buff.wav` | 強化アイテム取得 | 0.30s | 上昇アルペジオ |
| `se_item_attack.wav` | 攻撃アイテム取得 | 0.35s | 不穏な下降アルペジオ + 低音ヒット |
| `se_item_trap.wav` | 罠アイテム取得 | 0.25s | 滑り音「スベッ」 |
| `se_skill_ready.wav` | スキルゲージ満タン（READY 点灯瞬間） | 0.30s | 「キーン」チャージ完了 |
| `se_skill_activate.wav` | スキル発動 | 0.40s | 「シャッ」 |
| `se_interference_recv.wav` | 妨害受信（汎用） | 0.40s | 低音ドン + ノイズ短発 |
| `se_round_start.wav` | ラウンド開始カウントダウン（3-2-1-GO） | 1.50s | ビープ × 3 + 短いアクセント |
| `se_round_win.wav` | ラウンド勝利 | 0.60s | 短い勝利フレーズ |
| `se_match_win.wav` | マッチ勝利 | 1.50s | より長い勝利フレーズ |
| `se_ui_move.wav` | UI カーソル移動 | 0.05s | カチ |
| `se_ui_confirm.wav` | UI 確定 | 0.10s | ピッ |

### 2.2 中優先 SE（Phase F-Audio 後半 〜 F-Polish）

| ファイル名 | 内容 | 長さの目安 |
|---|---|---|
| `se_poison_loop.ogg` | 毒エリアダメージ中（1 秒ループ） | 1.00s ループ |
| `se_ball_launch.wav` | 発射確定 | 0.15s |
| `se_hitstop_strong.wav` | 強ヒットストップ（ラウンド / マッチ決着） | 0.50s |
| `se_combo_milestone.wav` | コンボ 10, 20, 30... 到達時の通知音（ピッチ差分付き） | 0.20s |
| `se_addrow_land.wav` | AttackAddRow の妨害行着弾（ドスッとした着地音） | 0.30s |
| `se_retaliation.wav` | 反撃ウィンドウ中の攻撃アイテム取得成功（上昇系タダン音） | 0.40s |

### 2.3 SE 実装メモ

- **ピッチ可変（ボール反射）**: `AudioSource.pitch` を `naturalSpeed / baseSpeed` の 1.0〜1.5 倍範囲で動的変更する。
- **ブロック衝突クールダウン**: 50ms の衝突音クールダウン（`AudioManager` で管理）。連打による音割れを防ぐ。
- **属性ピッチ差分（任意）**: Fire=+2 半音、Ice=−1 半音、Thunder=+3 半音、Heavy=−2 半音。`AudioSource.pitch` の固定差分で実装。
- **妨害種別ラベル音**: `se_interference_recv.wav` の再生後に種別名を TTS or 声ファイルで overlay 再生（Phase G+ 以降、F 発表では省略可）。

---

## 3. BGM

| ファイル名 | 内容 | BPM 目安 | ループ | 実装フェーズ |
|---|---|---|---|---|
| `bgm_title.ogg` | タイトル画面 BGM | 100〜120 | ○ | F-Audio |
| `bgm_match_base.ogg` | 試合中 BGM（通常レイヤー） | 130〜145 | ○ | F-Audio |
| `bgm_match_tense.ogg` | 試合中 BGM（緊迫レイヤー、HP 1/3 以下で追加） | 同上 | ○ | F-Audio |
| `bgm_result.ogg` | マッチ結果ジングル | — | × | F-Title |

### BGM クロスフェード実装メモ

- `bgm_match_base` と `bgm_match_tense` は同じ BPM / 同じ小節開始で同時再生を開始し、`bgm_match_tense` の Volume を 0 → 1 に 1 秒かけてフェード（`AudioMixer` の Exposed Parameter 経由）。
- HP 条件: `GameManager.GetHPRatio(0) < 0.33 || GameManager.GetHPRatio(1) < 0.33` を毎フレーム監視。1 度でも発火したら戻さない（試合終了まで緊迫レイヤーを維持）。
- タイトル → 試合: `bgm_title` を 0.5s でフェードアウトしつつ `bgm_match_base` を 0.5s でフェードイン。
- `Audio/MasterMixer.mixer` に Master / BGM / SE / Voice の 4 グループを作成。Exposed Parameters: `vol.master`, `vol.bgm`, `vol.se` を Inspector / PlayerPrefs と連動させる。

---

## 4. ビジュアルアセット

### 4.1 アイテムアイコン

ドロップ中のスプライトと HUD の `$P1ItemName` 近くに表示するアイコン（Phase F-Combat 以降）。

| カテゴリ | ファイル名パターン | 色系 | 優先度 |
|---|---|---|---|
| 強化（Buff） | `icon_buff_[name].png` | 青系（HDR Cyan / Blue） | F-Combat |
| 攻撃（Attack） | `icon_attack_[name].png` | 赤系（HDR Red / Orange）+ 棘装飾 | F-Combat |
| 罠（Trap） | `icon_trap_[name].png` | 強化と「同形・違う色」（薄紫系） | F-Combat |

**最低ライン（F 発表時点）**: カテゴリ別に丸形 Sprite 3 種を色だけ変えて代用可能。正式アイコンは Figma で作成 or フリー素材から調達。

### 4.2 ブロックビジュアル

現状はコードで色付けのみ。テクスチャ追加は Phase F-Polish 以降。

| ブロック種 | 現状 | 追加予定（F-Polish） |
|---|---|---|
| BlockNormal | 白 | 薄い石目テクスチャ |
| BlockHard | 灰 | ヒビ（HP に応じて段階変化） |
| BlockAbsorb | 緑 | ゼリー状吸収エフェクト |
| BlockExplosive | 橙 | 爆発マークアイコン overlay |
| BlockSpike | 赤 | 棘アイコン |
| BlockHardened | 金 | 金色オーラ（既実装） |

### 4.3 パーティクルエフェクト

| エフェクト名（Prefab） | 内容 | 実装フェーズ |
|---|---|---|
| `FX_BlockBreak` | ブロック破壊破片 | F-Polish |
| `FX_BlockExplosive` | 爆発リング | F-Polish |
| `FX_BallTrail` | ボール軌跡（属性カラー変化） | F-Polish |
| `FX_ItemPickup_Buff` | 強化アイテム取得パーティクル（青） | F-Polish |
| `FX_ItemPickup_Attack` | 攻撃アイテム取得パーティクル（赤） | F-Combat |
| `FX_InterferenceRecv` | 妨害受信フラッシュ（画面縁赤） | F-Polish |
| `FX_SkillActivate` | スキル発動縁取り光 | F-Polish |
| `FX_GatePassed` | Gate 通過パーティクル | G+ |
| `FX_DirectAttackLand` | DirectAttack 着弾円 | G+ |

### 4.4 UI テクスチャ

| ファイル名 | 内容 | 現状 |
|---|---|---|
| `bg_arena.png` | アリーナ背景（Figma 出力） | 配置済 |
| `frame_arena_p1.png` | P1 アリーナ枠（青系 HDR Bloom） | 配置済 |
| `frame_arena_p2.png` | P2 アリーナ枠（橙系 HDR Bloom） | 配置済 |
| `bar_hp_fill.png` | HP バー Fill テクスチャ | 単色で代用中 |
| `bar_energy_fill.png` | Energy バー Fill テクスチャ | 未作成 |
| `icon_skill_[name].png` | スキルアイコン（HUD 表示） | 未作成 |

---

## 5. フォント（導入済み）

| フォント | ファイル | 用途 |
|---|---|---|
| Bebas Neue Regular | `BebasNeue-Regular.ttf` + SDF | 数字（HP / Score / Combo） |
| JetBrainsMono Regular | `JetBrainsMono-Regular.ttf` + SDF | ラベル・固定文言 |
| JetBrainsMono Bold | `JetBrainsMono-Bold.ttf` + SDF | 強調ラベル |
| JetBrainsMono ExtraBold | `JetBrainsMono-ExtraBold.ttf` + SDF | タイトル・見出し |

TMP Font Asset は Custom Characters 指定で生成済み。

---

## 6. シェーダー（実装済み）

| シェーダー | パス | 用途 |
|---|---|---|
| `UI/HDRTint` | `Assets/Shaders/UI_HDRTint.shader` | UI Image の HDR 発光 |
| `Custom/HDRUnlit` | `Assets/Shaders/HDRUnlit.shader` | 3D Mesh / SpriteRenderer の HDR 発光 |

---

## 7. 素材調達ガイド

### SE を素材サイトから調達する場合

| サイト | URL | 特徴 |
|---|---|---|
| Freesound | freesound.org | CC0 フィルタ付き大型ライブラリ |
| 魔王魂 | maoudamashii.jikkyo.org | ゲーム向け SE / BGM、日本語 |
| DOVA-SYNDROME | dova-s.jp | BGM 多数、日本語 |
| OpenGameArt | opengameart.org | CC0 ゲームアセット多数 |

### SE をツールで生成する場合（推奨）

| ツール | 特徴 | 用途 |
|---|---|---|
| jsfxr (sfxr.me) | ブラウザ完結。WAV 書き出し可 | ブロック衝突 / ボール反射 / アイテム系 |
| BFXR (bfxr.net) | jsfxr の高機能版 | より凝った SE 生成 |
| AudioCraft (Meta) | テキストから生成（Python 環境必要） | 長めの SE / ジングル |

**推奨手順**: まず jsfxr でブロック衝突 SE とボール反射 SE を生成して仮当てし、バランス調整と並行して正式素材を探す。

### BGM を調達する場合

- **DOVA-SYNDROME**: テンポ・ジャンルで検索しやすい。`ゲーム BGM` タグで絞る。
- **魔王魂**: `エレクトロニカ / テクノ` など区分から探す。

---

## 8. フェーズ別の最低ライン

| フェーズ | 必要アセット |
|---|---|
| F-Combat（〜05-23） | アイテムアイコン 3 種（Buff/Attack/Trap 色違い丸形）、`se_item_attack.wav` 仮 |
| F-Audio（〜05-29） | 2.1 必須 SE 全20種、BGM 2 曲（bgm_title + bgm_match_base） |
| F-Title（〜06-01） | `bgm_result.ogg`、UI ナビ SE |
| F-Polish（〜06-03） | FX_BlockBreak、FX_BallTrail（最低限の破片演出） |
| 発表（06-05） | 上記すべて揃っている状態。未完は省略 or ダミー音で対応 |

---

## 9. 関連ドキュメント

- 音響設計詳細: [`DESIGN.md` 10節](./DESIGN.md#10-音響設計se--bgm)
- 開発フェーズ: [`ROADMAP.md`](./ROADMAP.md)
- 発表ガイド: [`PRESENTATION.md`](./PRESENTATION.md)
