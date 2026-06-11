# 発表スライド用スクリーンショット置き場

PowerPoint に貼るスクショの集積場所。スライド10「開発の弧（4段階）」に使う。

## 4段階（撮ったらここに置く）

| # | 段階 | ファイル名 | 撮影元 | コミット |
|---|---|---|---|---|
| ① | 原型 | `stage1_proto.png`   | worktree `_bk_snap_proto_0510` | 5/10 adf0b44 |
| ② | 大刷新（転機） | `stage2_renewal.png` | worktree `_bk_snap_renewal_0519` | 5/19 40c086a |
| ③ | 演出充実 | `stage3_polish.png`  | worktree `_bk_snap_polish_0606` | 6/6 6ec01a3 |
| ④ | 完成形 | `stage4_now.png`     | 今の main プロジェクト | now |

- `before_legacy.png` … 開発初期の実Game画面（保険。①が起動しない時の代替に使える）。

## 撮影順（おすすめ）

**④今 → ③6/6 → ②5/19 → ①5/10**。新しい＝確実に動く版から撮る。古い版はコンパイルが通らない可能性があるため最後に試す。

## 各段階の撮り方

1. **1つずつ**開く（ディスク節約。並行に複数開かない）。Unity Hub → Open → 対象フォルダを選ぶ → 6000.4.2f1 で開く（初回は再インポートで数分）。
2. **Game** ビューを表示（Scene ではなく Game。Bloom が乗るのは Game 側）。アスペクト 16:9 推奨。
3. ▶ Play → 対戦中まで進める（コンボ・アイテム落下中が映える）。
4. `Cmd + Shift + 4` → スペース → Game ビューのウィンドウをクリックで撮影。
5. `stageN_*.png` でこのフォルダに保存し、PowerPoint に貼る。
6. **撮り終えたら一声** → こちらでその版の `Library` を削除して次の容量を空ける。

## 注意

- MCP からは Game ビューを撮れない（SceneView/Canvas のみ）。発光込みの絵は必ず Unity の Game ビューから手動で撮る。
- 古い版が起動しない/エラーの時は、出た範囲を撮る or その段階は飛ばす（`before_legacy.png` で代替可）。
- proto(5/10) は `Assets/SampleScene.unity` と `Assets/Scenes/SampleScene.unity` の両方がある。ゲーム画面が出る方を開く。
