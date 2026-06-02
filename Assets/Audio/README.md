# Audio 配置ガイド

音源をここに置く。**ファイル名を下表どおりにすれば `AudioManager` への自動バインド**ができる
（命名が一致していれば Claude がバインドスクリプトで一括結線する）。

- 短発 SE → `Assets/Audio/SE/*.wav`（Import: Decompress On Load 推奨）
- ループ SE（poison）→ `Assets/Audio/SE/*.ogg`
- BGM → `Assets/Audio/BGM/*.ogg`（Import: Streaming + Loop）
- 各フォルダの `LICENSE.txt` に出典・ライセンスを必ず記録（CC0 / Creative Commons のみ）

## SE: ファイル名 ↔ AudioManager フィールド

| ファイル名（SE/） | AudioManager フィールド | 優先 |
|---|---|---|
| se_block_hit_normal.wav | seBlockHitNormal | ★最優先 |
| se_ball_wall.wav        | seBallWall       | ★最優先 |
| se_block_break.wav      | seBlockBreak     | ★最優先 |
| se_ball_paddle.wav      | seBallPaddle     | ★最優先 |
| se_item_buff.wav        | seItemBuff       | ★最優先 |
| se_skill_activate.wav   | seSkillActivate  | ★最優先 |
| se_round_start.wav      | seRoundStart     | ★最優先 |
| se_round_win.wav        | seRoundWin       | ★最優先 |
| se_block_hit_hard.wav   | seBlockHitHard   | 必須 |
| se_block_hit_absorb.wav | seBlockHitAbsorb | 必須 |
| se_block_explosive.wav  | seBlockExplosive | 必須 |
| se_item_drop.wav        | seItemDrop       | 必須 |
| se_item_attack.wav      | seItemAttack     | 必須 |
| se_item_trap.wav        | seItemTrap       | 必須 |
| se_skill_ready.wav      | seSkillReady     | 必須 |
| se_interference_recv.wav| seInterferenceRecv | 必須 |
| se_match_win.wav        | seMatchWin       | 必須 |
| se_ui_move.wav          | seUiMove         | 必須 |
| se_ui_confirm.wav       | seUiConfirm      | 必須 |
| se_ball_launch.wav      | seBallLaunch     | 中 |
| se_poison_loop.ogg      | sePoisonLoop     | 中 |
| se_combo_milestone.wav  | seComboMilestone | 中 |
| se_addrow_land.wav      | seAddRowLand     | 中（※発火点は未配線。必要なら追補） |

## BGM: ファイル名 ↔ AudioManager フィールド

| ファイル名（BGM/） | AudioManager フィールド | 備考 |
|---|---|---|
| bgm_title.ogg       | bgmTitle        | タイトル。テンポ 100-120 |
| bgm_match_base.ogg  | bgmMatch        | 試合通常。テンポ 130-145 |
| bgm_match_tense.ogg | bgmMatchTense   | HP30% 帯で重ねる緊迫レイヤー |
| bgm_result.ogg      | bgmResultJingle | マッチ結果ジングル（ループ無し） |

## 備考
- **Mixer 未作成でも音は鳴る**（未割り当て時は AudioListener 直結）。Mixer は音量バス用。
- Mixer を使う場合: `Assets/Audio/MasterMixer.mixer` を作成し Master/BGM/SE グループ +
  Expose Param（MasterVol/BGMVol/SEVol）を用意 → AudioManager に結線（Claude が対応）。
- 詳細仕様は `docs/ASSETS.md` §2 / `docs/DESIGN.md` §10。
