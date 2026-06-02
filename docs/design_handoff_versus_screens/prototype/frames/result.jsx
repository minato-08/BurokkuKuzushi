// result.jsx — Match Result screen, variations A (symmetric) & B (color flood)
// Exports to window: ResultA, ResultB
(function () {
  const P = window.HUDP;

  // demo data — winner = P1
  const DATA = {
    winner: 1, winnerColor: P.p1,
    p1: { score: 1240, wins: 2 }, p2: { score: 980, wins: 1 }, bestOf: 3,
  };

  const Choice = ({ text, selected, name }) => (
    <div data-name={name} style={{
      fontFamily: P.display, fontSize: 36, letterSpacing: ".08em",
      padding: "14px 20px", borderRadius: 12, width: 280, textAlign: "center", whiteSpace: "nowrap",
      boxSizing: "border-box",
      color: selected ? "#000" : P.gray,
      background: selected ? P.accent : "transparent",
      border: `2px solid ${selected ? P.accent : P.line2}`,
      boxShadow: selected ? `0 0 26px ${P.accent}99` : "none",
      transition: "all .15s",
    }}>{text}</div>
  );

  const Hint = () => (
    <div data-name="HintText" style={{
      fontFamily: P.mono, fontWeight: 700, fontSize: 16, letterSpacing: ".12em", color: P.gray,
    }}>
      <b style={{ color: P.white }}>A / D</b> ( <b style={{ color: P.white }}>J / L</b> ) SELECT&nbsp;&nbsp;&nbsp;<b style={{ color: P.white }}>SPACE</b> CONFIRM
    </div>
  );

  // ════════════════════════ VARIATION A — symmetric ════════════════════════
  function ResultA() {
    return (
      <div data-screen-label="Result-A" style={frameStyle}>
        <window.ArenaBlurBg dim={0.82} />
        <div data-name="_MatchResultPanel" style={{
          position: "absolute", inset: 0, display: "flex", flexDirection: "column",
          alignItems: "center", justifyContent: "center", gap: 0,
        }}>
          <div style={{
            fontFamily: P.mono, fontWeight: 700, fontSize: 18, letterSpacing: ".4em", color: P.gray,
          }}>MATCH&nbsp;OVER</div>
          <div data-name="$MatchWinner" style={{
            fontFamily: P.display, fontSize: 168, lineHeight: .9, letterSpacing: ".02em",
            color: DATA.winnerColor, marginTop: 6, whiteSpace: "nowrap",
            textShadow: `0 0 48px ${DATA.winnerColor}88`,
          }}>P{DATA.winner} WINS!</div>

          {/* score comparison */}
          <div data-name="$ScoreSummary" style={{
            display: "flex", alignItems: "center", gap: 30, marginTop: 44,
          }}>
            <ScoreCol side="P1" color={P.p1} score={DATA.p1.score} win={DATA.winner === 1} align="right" />
            <div style={{ width: 80, flexShrink: 0, textAlign: "center", fontFamily: P.display, fontSize: 44, color: P.gray, letterSpacing: ".08em" }}>VS</div>
            <ScoreCol side="P2" color={P.p2} score={DATA.p2.score} win={DATA.winner === 2} align="left" />
          </div>

          {/* wins */}
          <div data-name="$WinsSummary" style={{
            display: "flex", alignItems: "center", gap: 26, marginTop: 34,
            fontFamily: P.mono, fontWeight: 700, fontSize: 15, letterSpacing: ".14em", color: P.gray,
          }}>
            <span style={{ color: P.p1 }}>P1</span>
            <window.WinPips wins={DATA.p1.wins} total={DATA.bestOf} color={P.p1} />
            <span style={{ opacity: .6 }}>BEST OF {DATA.bestOf}</span>
            <window.WinPips wins={DATA.p2.wins} total={DATA.bestOf} color={P.p2} align="right" />
            <span style={{ color: P.p2 }}>P2</span>
          </div>

          {/* choices */}
          <div data-name="_SelectionPanel" style={{ display: "flex", gap: 26, marginTop: 58 }}>
            <Choice text="REMATCH" selected name="$RematchText" />
            <Choice text="MENU" name="$MenuText" />
          </div>
          <div style={{ marginTop: 26 }}><Hint /></div>
        </div>
      </div>
    );
  }

  const ScoreCol = ({ side, color, score, win, align = "left" }) => (
    <div style={{ width: 270, flexShrink: 0, display: "flex", flexDirection: "column", alignItems: "center", gap: 4 }}>
      <span style={{
        fontFamily: P.mono, fontWeight: 700, fontSize: 16, letterSpacing: ".06em",
        color: "#000", background: color, padding: "3px 14px", borderRadius: 7,
      }}>{side}{win ? " ･ WIN" : ""}</span>
      <span style={{
        fontFamily: P.display, fontSize: 84, lineHeight: 1, letterSpacing: ".02em",
        color: win ? P.white : P.gray,
        textShadow: win ? `0 0 22px ${color}66` : "none",
      }}>{score.toLocaleString()}</span>
      <span style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 14, letterSpacing: ".2em", color: P.gray }}>PTS</span>
    </div>
  );

  // ════════════════════════ VARIATION B — color flood ════════════════════════
  function ResultB() {
    const wc = DATA.winnerColor;
    return (
      <div data-screen-label="Result-B" style={frameStyle}>
        <window.ArenaBlurBg dim={0.85} />
        <div data-name="_MatchResultPanel" style={{ position: "absolute", inset: 0, display: "flex" }}>
          {/* winner flooded side */}
          <div style={{
            flex: "0 0 60%", position: "relative", overflow: "hidden",
            background: `linear-gradient(120deg, ${wc}2e 0%, rgba(5,10,26,0.2) 70%)`,
            display: "flex", flexDirection: "column", justifyContent: "center",
            padding: "0 80px 0 120px", gap: 18,
            clipPath: "polygon(0 0, 100% 0, 88% 100%, 0 100%)",
          }}>
            <div style={{ position: "absolute", left: 0, top: 0, bottom: 0, width: 8, background: wc, boxShadow: `0 0 30px ${wc}` }} />
            <div style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 18, letterSpacing: ".4em", color: P.gray }}>MATCH&nbsp;OVER</div>
            <div data-name="$MatchWinner" style={{
              fontFamily: P.display, fontSize: 168, lineHeight: .86, letterSpacing: ".01em",
              color: wc, textShadow: `0 0 48px ${wc}77`,
            }}>P{DATA.winner}<br />WINS!</div>
            <div data-name="$ScoreSummary" style={{ display: "flex", alignItems: "baseline", gap: 16, marginTop: 8 }}>
              <span style={{ fontFamily: P.display, fontSize: 100, lineHeight: 1, color: P.white, textShadow: `0 0 22px ${wc}66` }}>
                {DATA.p1.score.toLocaleString()}
              </span>
              <span style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 20, letterSpacing: ".2em", color: P.gray }}>PTS</span>
            </div>
            <div data-name="$WinsSummary" style={{ display: "flex", alignItems: "center", gap: 14, marginTop: 4 }}>
              <window.WinPips wins={DATA.p1.wins} total={DATA.bestOf} color={wc} />
              <span style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 15, letterSpacing: ".14em", color: P.gray }}>
                {DATA.p1.wins} / {DATA.bestOf} WINS
              </span>
            </div>
          </div>

          {/* loser + actions side */}
          <div style={{
            flex: 1, display: "flex", flexDirection: "column",
            justifyContent: "center", padding: "0 90px 0 40px", gap: 40,
          }}>
            <div style={{ display: "flex", flexDirection: "column", gap: 6, opacity: .8 }}>
              <span style={{
                fontFamily: P.mono, fontWeight: 700, fontSize: 16, letterSpacing: ".06em",
                color: "#000", background: P.p2, padding: "3px 14px", borderRadius: 7, alignSelf: "flex-start",
              }}>P2</span>
              <span style={{ fontFamily: P.display, fontSize: 72, lineHeight: 1, color: P.gray }}>
                {DATA.p2.score.toLocaleString()}<span style={{ fontSize: 28, marginLeft: 10 }}>PTS</span>
              </span>
              <div style={{ display: "flex", alignItems: "center", gap: 12, marginTop: 4 }}>
                <window.WinPips wins={DATA.p2.wins} total={DATA.bestOf} color={P.p2} />
                <span style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 14, letterSpacing: ".14em", color: P.gray }}>
                  {DATA.p2.wins} / {DATA.bestOf}
                </span>
              </div>
            </div>

            <div style={{ height: 1, background: P.line }} />

            <div data-name="_SelectionPanel" style={{ display: "flex", flexDirection: "column", gap: 16 }}>
              <Choice text="REMATCH" selected name="$RematchText" />
              <Choice text="MENU" name="$MenuText" />
            </div>
            <Hint />
          </div>
        </div>
      </div>
    );
  }

  const frameStyle = {
    position: "relative", width: 1920, height: 1080, overflow: "hidden",
    background: P.bg, fontFamily: P.mono,
  };

  Object.assign(window, { ResultA, ResultB });
})();
