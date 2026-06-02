# Handoff: 対戦ブロック崩し — Title / Skill Select / Result（採用案）

## Overview
ローカル2P対戦ブロック崩しの 3 つの全画面 UI（1920×1080, 16:9）の確定デザイン。

採用バリエーション:
| 画面 | 採用案 | プロトタイプ内アートボード |
|---|---|---|
| **タイトル** | **A · 中央＋括弧カーソル** | `t-a` / `TitleA` |
| **スキル選択** | **B · 扇状＋グロー** | `ss-b` / `SkillSelectB` |
| **リザルト** | **A · 左右対称＋勝者特大** | `r-a` / `ResultA` |

> プロトタイプには各画面 2 案が並んでいますが、**実装対象は上記の採用案のみ**です。他の案（Title B / Skill A / Result B）は参考・不採用。

## About the Design Files
`prototype/` の HTML/JSX は **HTML で作られたデザインリファレンス**（見た目と挙動を示すプロトタイプ）であり、そのまま製品コードにコピーするものではありません。

このプロジェクトの実装環境は **Unity（uGUI）+ Figma ノードのバインド**です。ワークフローは:
1. この README とプロトタイプを元に **Figma で各画面を作成**（要素名は下表の `Figma 要素 (data-name)` 列に合わせる）
2. Figma → Unity に取り込み、各 `$`/`_` 要素を下表の **Unity バインド先**（既存 `TitleUI` / `SkillSelectUI` / `MatchResultUI` の SerializeField）へ接続

つまりこの HTML は「ピクセルの正解」であって、出力先は Unity の uGUI 階層です。HTML をそのまま使うのではなく、**この見た目を Unity 上で再現**してください。

各 JSX 要素には Figma/Unity 命名に対応する `data-name` 属性が付いています（例 `data-name="$MatchWinner"`）。ブラウザの DevTools で実寸・実色を直接拾えます。

## Fidelity
**High-fidelity (hifi)。** 色・タイポ・余白・状態（選択/確定/ロック）まで最終仕様です。下記の数値どおりに再現してください。

---

## Design Tokens

### Colors
| 用途 | 名前 | RGB | HEX |
|---|---|---|---|
| 背景（最暗） | bg | rgb(5,10,26) | `#050A1A` |
| 背景グラデ下端 | bg2 | rgb(10,21,53) | `#0A1535` |
| パネル塗り | panel | rgb(39,42,49) | `#272A31` |
| 罫線（細） | line | rgb(42,47,68) | `#2A2F44` |
| 罫線（太/枠） | line2 | rgb(79,83,96) | `#4F5360` |
| **P1 色** | p1 | rgb(78,195,255) | `#4EC3FF` |
| **P2 色** | p2 | rgb(255,78,116) | `#FF4E74` |
| **アクセント（選択/黄）** | accent | rgb(236,201,47) | `#ECC92F` |
| スキル色: BIG PADDLE | green | rgb(123,224,123) | `#7BE07B` |
| スキル色: FIRE BALL | orange | rgb(236,131,26) | `#EC831A` |
| スキル色: DOUBLE BALL | violet | rgb(176,150,255) | `#B096FF` |
| スキル色: EMERGENCY CLEAR | accent | rgb(236,201,47) | `#ECC92F` |
| 補助テキスト（グレー） | gray | rgb(154,160,180) | `#9AA0B4` |
| 本文（オフホワイト） | ink | rgb(232,230,223) | `#E8E6DF` |
| 強調テキスト（白） | white | rgb(255,255,255) | `#FFFFFF` |

### Typography
- **JetBrains Mono**（weight 400 / 500 / 700）— 既定。ラベル・操作ヒント・ステータス・数値以外すべて。
- **Bebas Neue** — ディスプレイ専用。ロゴ、`P{N} WINS!`、特大数値（スコア/コンボ/先取数）、メニュー項目、`VS`。
- 日本語テキストは `'JetBrains Mono','Hiragino Kaku Gothic ProN','Noto Sans JP',sans-serif` のフォールバック。Unity では日本語対応フォント（Noto Sans JP 等）を割当。

### 共通背景処理
全画面の背景は **ぼかしたアリーナ＋暗転ビネット**（`ArenaBlurBg`）。「試合の続き」感を出す演出。Unity ではゲーム画面を blur したものか、同等の暗い背景テクスチャで代替可。装飾なので厳密な再現は不要。
- ビネット: `radial-gradient(120% 90% at 50% 45%, …)` で中央 dim ≒ 0.74–0.86、外周を強く暗く。

---

## 画面 1 — タイトル（Title A · 中央＋括弧カーソル）
プロトタイプ: `TitleA`（`frames/title.jsx`）/ アートボード `t-a`

### Purpose
起動時の画面（`GameState.Title`, `timeScale=0`）。W/S（↑↓）でメニュー移動、Space/Enter で確定。

### Layout
- 1920×1080、`_TitlePanel` が全面、中央寄せ縦積み（ロゴ群とメニュー群を gap 64 で）。
- 最上部に高さ 5px の P1↔P2 ストライプ: `linear-gradient(90deg, #4EC3FF 0 50%, #FF4E74 50% 100%)` opacity .9。

### Components
**ロゴ群**（中央、縦 gap 14）
- キッカー: `2P  VERSUS  ARCADE` — JetBrains Mono 700, 18px, letter-spacing .42em, color `#ECC92F`。
- ロゴ: `BUROKKU`（白 `#FFFFFF`）改行 `KUZUSHI`（`#ECC92F`） — Bebas Neue 150px, line-height .82, letter-spacing .01em。背後に skew(-14deg) の黄色アクセントバー（高さ10px, opacity .9, z 後ろ）。
- サブタイトル: `対戦ブロック崩し` — 700, 22px, letter-spacing .24em, color `#9AA0B4`。

**メニュー**（中央、縦, START/SETTINGS/QUIT, 各 width 460, 中央寄せ）
- 各項目ラベル: Bebas Neue 52px, letter-spacing .1em。
  - 選択中: color `#FFFFFF` ＋ text-shadow グロー（accent 55）。
  - 非選択: color `#9AA0B4`。
- **$Menu{i}Cursor（選択ハイライト）= 動的表示**:
  - 左 `‹` / 右 `›`（Bebas 40px, `#ECC92F`）を項目の左右 30px に。
  - 下にアクセント下線バー: 幅 200 × 高さ 4, `#ECC92F`, グロー。

**操作ヒント**（下から 48px, 中央）: `W / S  ･  ↑ / ↓   選択　　　SPACE / ENTER   決定` — JetBrains Mono 700, 16px, letter-spacing .1em, `#9AA0B4`。

**バージョンタグ**（右下 40/40）: `v0.3 ･ LOCAL 2P` — 13px, letter-spacing .14em, `#4F5360`。

### バインド表（メニュー順 0=START / 1=SETTINGS / 2=QUIT 固定）
| Figma 要素 (data-name) | Unity バインド先 | 種別 |
|---|---|---|
| `_TitlePanel` | `TitleUI.panel` | パネル |
| ロゴ `BUROKKU KUZUSHI` | （静的） | 見出し |
| `Menu0Start` / `Menu1Settings` / `Menu2Quit` | （静的ラベル） | メニュー項目 |
| `$Menu0Cursor` / `$Menu1Cursor` / `$Menu2Cursor` | `TitleUI.menuCursors[0..2]` | 選択中ハイライト（括弧＋下線, 動的表示） |

---

## 画面 2 — スキル選択（Skill Select B · 扇状＋グロー）
プロトタイプ: `SkillSelectB`（`frames/skill-select.jsx`）/ アートボード `ss-b`

### Purpose
4 枚のスキルカードを **共有**し、P1/P2 がそれぞれ独立カーソルで選ぶ（互いの選択が見える）。1P: A/D 移動・S 確定 / 2P: J/L 移動・K 確定。

### Layout
- `_SkillSelectPanel` 全面、上寄せ（padding-top 96）縦積み。
- ヘッダー → カード群（扇状）→ P1/P2 ステータス（下両端）。

### Components
**ヘッダー**（中央）
- `SELECT SKILL` — Bebas Neue 64px, letter-spacing .08em, `#FFFFFF`, グロー（accent 55）。
- 注記: `カーソルを左右に ･ 決めたら確定` — JetBrains Mono 700, 15px, letter-spacing .34em, `#9AA0B4`。

**カード群 `_SkillCards`（扇状）**
4 枚を `transform: translateX((i-1.5)*232px) translateY(dy) rotate(rot)` で弧状配置（transform-origin: bottom center）。
| index | rotate | dy(下げ) | スキル名 | 説明 | アイコン色 |
|---|---|---|---|---|---|
| 0 | -10° | 44 | **BIG PADDLE** | パドルを10秒拡大 | green `#7BE07B` |
| 1 | -3.5° | 8 | **FIRE BALL** | 着弾点を10秒爆破 | orange `#EC831A` |
| 2 | +3.5° | 8 | **DOUBLE BALL** | ボールを10秒2個に | violet `#B096FF` |
| 3 | +10° | 44 | **EMERGENCY CLEAR** | 下半分を即消去 ･ HP≤10% | accent `#ECC92F` |

> index 順はコードと一致必須（`cardP1Cursors[i]` / `cardP2Cursors[i]`）。

各カード（256×360, radius 16, padding 34/20/22）:
- 通常背景 `rgba(16,21,40,0.92)`, border 1.5px `#4F5360`, box-shadow `0 18px 40px rgba(0,0,0,.5)`。
- アイコン枠: 84×84, radius 12, 背景 スキル色16%、border スキル色66%。中に幾何グリフ（下記）。
- `Card{i}Name`: Bebas Neue 32px, 2 行スタック（語ごとに改行）, `#FFFFFF`, 中央。
- `Card{i}Desc`: JetBrains Mono 13px, line-height 1.5, `#9AA0B4`, 中央, text-wrap balance。
- ロック（index 3, EMERGENCY CLEAR）: opacity .6、チップ `🔒 HP≤10%`（11px, accent）。発動条件は自 HP 10% 以下のみ。

**カーソル/確定状態（動的）**
- **ホバー中**: カードが上に持ち上がる（translateY -34）＋ z 最前面＋プレイヤー色のグローリング `0 0 0 3px <色>, 0 0 34px <色>aa`。下部にチップ表示。
  - `$Card{i}P1Cursor`: P1色 `#4EC3FF` リング＋チップ「P1」。
  - `$Card{i}P2Cursor`: P2色 `#FF4E74` リング＋チップ「P2」。
- **確定後**: カード背景が `linear-gradient(180deg, <色>33, rgba(16,21,40,.95))`＋border 該当色、チップが「P1 ✓ READY」/「P2 ✓ READY」。
- 同じカードを両者が選んでも可（リング/チップが両方点灯）。

**ステータス `_P1Status`（左下）/ `_P2Status`（右下）**（bottom 56, 左右 80）
- プレイヤータグ: JetBrains Mono 700, 18px, 黒文字 on プレイヤー色, radius 8。`PLAYER 1` / `PLAYER 2`。
- ステータス行 `$P1Status` / `$P2Status`: JetBrains Mono 700, 16px, letter-spacing .1em。
  - 未確定: `A / D SELECT   S CONFIRM`（2P: `J / L … K`）。キー部分は白。
  - 確定: `✓ READY ･ 相手を待機中…`（プレイヤー色）。

### バインド表
| Figma 要素 (data-name) | Unity バインド先 | 種別 |
|---|---|---|
| `_SkillSelectPanel` | `SkillSelectUI.panel` | パネル |
| `$Card0P1Cursor`…`$Card3P1Cursor` | `SkillSelectUI.cardP1Cursors[0..3]` | P1 カーソル（リング/チップ, 動的表示） |
| `$Card0P2Cursor`…`$Card3P2Cursor` | `SkillSelectUI.cardP2Cursors[0..3]` | P2 カーソル（同上） |
| `$P1Status` / `$P2Status` | `SkillSelectUI.p1StatusText` / `p2StatusText` | ステータス文字列 |
| `Card{i}Name` / `Card{i}Desc` | （静的） | カード名/説明 |

> 旧「1個ずつサイクル」方式の `p1SkillText/p2SkillText` は、カード化に伴い `cardP{n}Cursors[4]` 配列＋確定ハイライトへ置換（Claude Code 側で既にカード化コミット済みの想定）。

### スキル幾何グリフ（自作 SVG, stroke 系）
`frames/shared.jsx` の `SkillIcon` 参照。viewBox 64×64, stroke 3, round。Unity ではこの形状を再現した SVG/スプライトを用意。
- **paddle**: 下に角丸の太いバー（塗り）＋上に左右両矢印（拡大の含意）。
- **fire**: 炎のパスシルエット＋下に塗り円（ボール）。
- **double**: 円 2 つを重ねる。
- **explosion**: 8 方向の星形バースト＋中心の小さい塗り四角。

---

## 画面 3 — リザルト（Result A · 左右対称＋勝者特大）
プロトタイプ: `ResultA`（`frames/result.jsx`）/ アートボード `r-a`

### Purpose
`MatchOver` 時に表示。勝者を主役に。A/D（J/L）で REMATCH/MENU 選択、Space で確定。

### Layout
`_MatchResultPanel` 全面、中央寄せ縦積み。上から: MATCH OVER → 勝者 → スコア対比 → 勝数 → 選択肢 → ヒント。

### Components
- キッカー `MATCH OVER`: JetBrains Mono 700, 18px, letter-spacing .4em, `#9AA0B4`。
- **`$MatchWinner`** `P{N} WINS!`: Bebas Neue 168px, line-height .9, letter-spacing .02em, **white-space: nowrap（1行厳守）**, color = 勝者色（例 P1 `#4EC3FF`）, text-shadow `0 0 48px <勝者色>88`。margin-top 6。
- **`$ScoreSummary`**（横並び, align center, gap 30, margin-top 44）:
  - P1 カラム（width 270, flex-shrink 0, **中央寄せ**）: タグ `P1 · WIN`（勝者は ` · WIN` 付き, 黒 on P1色, radius 7, 16px）/ スコア `1,240`（Bebas 84px, 勝者=白＋グロー / 敗者=`#9AA0B4`）/ `PTS`（mono 700, 14px, letter-spacing .2em, gray）。
  - VS: Bebas 44px, `#9AA0B4`, width 80（固定, 中央）。
  - P2 カラム（同 270, 中央寄せ）: タグ `P2` / `980` / `PTS`。
  - ※カラムは固定幅＋中央寄せで数値と VS を必ず離す。
- **`$WinsSummary`**（横並び, gap 26, margin-top 34, mono 700 15px gray）: `P1` ｜ P1 ピップ ｜ `BEST OF 3` ｜ P2 ピップ ｜ `P2`。
  - 勝数ピップ: 16×16 円。勝ち分は塗り＋該当色グロー、未取得は border `#4F5360` 透明。例 best of 3 → P1: ●●○(2勝) / P2: ●○○(1勝)。
- **`_SelectionPanel`**（横並び, gap 26, margin-top 58）:
  - `$RematchText` / `$MenuText`: Bebas Neue 36px, letter-spacing .08em, padding 14/20, radius 12, **width 280, 中央, nowrap**。
    - 選択中: 黒文字 on `#ECC92F`, border accent, グロー。
    - 非選択: `#9AA0B4`, border `#4F5360`, 背景なし。
- **`HintText`**（margin-top 26）: `A / D ( J / L ) SELECT   SPACE CONFIRM` — mono 700, 16px, letter-spacing .12em, gray（キーは白）。

### バインド表
| Figma 要素 (data-name) | Unity バインド先 | 種別 |
|---|---|---|
| `_MatchResultPanel` | `MatchResultUI.matchResultPanel` | パネル |
| `$MatchWinner` | `MatchResultUI.matchWinnerText` | 勝者（特大, `P{N} WINS!`） |
| `$ScoreSummary` | `MatchResultUI.scoreSummaryText` | スコア対比（P1/P2 pts） |
| `$WinsSummary` | `MatchResultUI.winsSummaryText` | 勝数（pips + best of） |
| `$RematchText` / `$MenuText` | `MatchResultUI.rematchText` / `menuText` | 選択肢（文言固定・色のみ選択で黄↔白） |
| `HintText` | `MatchResultUI.hintText` | 操作ヒント（コード上書きあり, 長さ確保） |

> `scoreSummaryText` / `winsSummaryText` を単一 TMP で扱う場合は「P1: {n} pts / P2: {n} pts」「P1: {n} wins / P2: {n} wins」形式。pips をオブジェクト表現にするなら別途配列バインドに分解可（要相談）。

---

## Interactions & Behavior
- **Title**: W/S（↑↓）でメニュー index 移動 → 該当 `menuCursors[i]` のみ表示。Space/Enter で確定。START→SkillSelect、SETTINGS→Settings オーバーレイ、QUIT→終了。
- **Skill Select**: A/D（J/L）で各プレイヤーの index 移動 → `card{P}Cursors[index]` を点灯・他消灯。S/K で確定スタイルへ。両者確定で次フェーズ。
- **Result**: A/D（J/L）で REMATCH↔MENU の選択色トグル（黄↔白）。Space で確定。
- 状態遷移は単一シーン内のステート切替（`GameState.Title → SkillSelect → … → MatchOver`）。
- アニメ: ホバー時カードの lift は transform .2s。グロー/色変化は .15s 程度のフェードで可（プロトタイプ準拠）。

## State Management（参考・コード側）
- `GameManager.GameState`（Title 含む）、`SetRoundsToWin/GetRoundsToWin`（1〜5 クランプ, PlayerPrefs `match.roundsToWin`）。
- スキル選択: 各プレイヤーの選択中 index と確定フラグ。
- リザルト: 勝者 N、P1/P2 スコア、P1/P2 勝数、best of。

## Assets
- 画像アセットなし。スキルアイコンは自作の幾何 SVG（`frames/shared.jsx` の `SkillIcon`）。Unity 用にスプライト化して用意。
- フォント: JetBrains Mono / Bebas Neue（Google Fonts）。日本語は Noto Sans JP 等。

## Files
- `prototype/versus_screens.html` — 全 7 アートボード（採用＋不採用）を並べた Design Canvas。ブラウザで開き、各カード右上の拡大ボタンでフルスクリーン確認。
- `prototype/frames/title.jsx` — `TitleA`（採用）/ `TitleB` / `SettingsPanel`。
- `prototype/frames/skill-select.jsx` — `SkillSelectB`（採用）/ `SkillSelectA`。
- `prototype/frames/result.jsx` — `ResultA`（採用）/ `ResultB`。
- `prototype/frames/shared.jsx` — トークン（`HUDP`）、背景 `ArenaBlurBg`、`SkillIcon`、`WinPips`。
- `prototype/frames/design-canvas.jsx` — 比較用キャンバス（実装不要）。

> 設定（Settings）画面はこの採用セットには未確定（タイトル A の SETTINGS から開くオーバーレイ）。プロトタイプの `SettingsPanel` を別途確定する場合は追って指示します。
