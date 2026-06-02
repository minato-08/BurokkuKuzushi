// app.jsx — mount the four artboards on the design canvas
(function () {
  const { DesignCanvas, DCSection, DCArtboard } = window;

  function App() {
    return (
      <DesignCanvas>
        <DCSection id="title" title="タイトル (Title)" subtitle="起動時 ･ START / SETTINGS / QUIT ･ W/S 移動 ･ Space/Enter 決定">
          <DCArtboard id="t-a" label="A · 中央 + 括弧カーソル" width={1920} height={1080}>
            <window.TitleA />
          </DCArtboard>
          <DCArtboard id="t-b" label="B · 非対称 + VS エンブレム" width={1920} height={1080}>
            <window.TitleB />
          </DCArtboard>
          <DCArtboard id="settings" label="設定 (Settings) · 先取数オーバーレイ" width={1920} height={1080}>
            <window.SettingsPanel />
          </DCArtboard>
        </DCSection>

        <DCSection id="skillselect" title="スキル選択 (Skill Select)" subtitle="4カード共有 ･ P1/P2 が独立カーソルで選ぶ ･ 互いの選択が見える">
          <DCArtboard id="ss-a" label="A · 横一列 + 枠ブラケット" width={1920} height={1080}>
            <window.SkillSelectA />
          </DCArtboard>
          <DCArtboard id="ss-b" label="B · 扇状 + グロー/塗り" width={1920} height={1080}>
            <window.SkillSelectB />
          </DCArtboard>
        </DCSection>

        <DCSection id="result" title="リザルト (Match Result)" subtitle="勝者を主役に ･ スコア/勝数サマリ ･ REMATCH / MENU 選択">
          <DCArtboard id="r-a" label="A · 左右対称 + 勝者特大" width={1920} height={1080}>
            <window.ResultA />
          </DCArtboard>
          <DCArtboard id="r-b" label="B · 非対称 + 勝者側に色を流す" width={1920} height={1080}>
            <window.ResultB />
          </DCArtboard>
        </DCSection>
      </DesignCanvas>
    );
  }

  ReactDOM.createRoot(document.getElementById("root")).render(<App />);
})();
