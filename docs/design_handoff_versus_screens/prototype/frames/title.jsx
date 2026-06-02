// title.jsx — Title screen (A/B) + Settings overlay
// Exports to window: TitleA, TitleB, SettingsPanel
(function () {
  const P = window.HUDP;
  const jp = "'JetBrains Mono','Hiragino Kaku Gothic ProN','Noto Sans JP',sans-serif";

  const MENU = [
    { key: "Start",    label: "START",    jp: "はじめる" },
    { key: "Settings", label: "SETTINGS", jp: "せってい" },
    { key: "Quit",     label: "QUIT",     jp: "おわる" },
  ];

  // logo lockup, reused by both variations
  function Logo({ align = "center", scale = 1 }) {
    return (
      <div style={{ display: "flex", flexDirection: "column", alignItems: align === "left" ? "flex-start" : "center", gap: 14 }}>
        <div style={{
          fontFamily: P.mono, fontWeight: 700, fontSize: 18 * scale, letterSpacing: ".42em",
          color: P.accent, paddingLeft: ".42em",
        }}>2P&nbsp;&nbsp;VERSUS&nbsp;&nbsp;ARCADE</div>
        <div style={{ position: "relative", display: "inline-block" }}>
          <div style={{
            fontFamily: P.display, fontSize: 150 * scale, lineHeight: .82, letterSpacing: ".01em",
            color: P.white, textAlign: align === "left" ? "left" : "center",
          }}>
            BUROKKU<br /><span style={{ color: P.accent }}>KUZUSHI</span>
          </div>
          {/* skewed accent bar behind, like the in-game VS mark */}
          <div style={{
            position: "absolute", left: -10, right: -10, top: "46%", height: 10 * scale,
            background: P.accent, transform: "skewX(-14deg)", opacity: .9, zIndex: -1,
          }} />
        </div>
        <div style={{
          fontFamily: jp, fontWeight: 700, fontSize: 22 * scale, letterSpacing: ".24em",
          color: P.gray, paddingLeft: ".24em",
        }}>対戦ブロック崩し</div>
      </div>
    );
  }

  const Hint = ({ text, align = "center" }) => (
    <div style={{
      fontFamily: P.mono, fontWeight: 700, fontSize: 16, letterSpacing: ".1em", color: P.gray,
      textAlign: align,
    }}>{text}</div>
  );

  // ════════════════════════ TITLE A — centered arcade ════════════════════════
  function MenuItemA({ item, i, selected }) {
    return (
      <div data-name={`Menu${i}${item.key}`} style={{
        position: "relative", display: "flex", alignItems: "center", justifyContent: "center",
        gap: 22, padding: "8px 0", width: 460,
      }}>
        {/* $Menu{i}Cursor */}
        {selected && (
          <div data-name={`$Menu${i}Cursor`} style={{ position: "absolute", inset: 0, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <span style={{ position: "absolute", left: 30, fontFamily: P.display, fontSize: 40, color: P.accent }}>‹</span>
            <span style={{ position: "absolute", right: 30, fontFamily: P.display, fontSize: 40, color: P.accent }}>›</span>
            <div style={{ position: "absolute", bottom: 2, width: 200, height: 4, background: P.accent, boxShadow: `0 0 14px ${P.accent}` }} />
          </div>
        )}
        <span style={{
          fontFamily: P.display, fontSize: 52, letterSpacing: ".1em", lineHeight: 1,
          color: selected ? P.white : P.gray,
          textShadow: selected ? `0 0 22px ${P.accent}55` : "none",
        }}>{item.label}</span>
      </div>
    );
  }

  function TitleA() {
    return (
      <div data-screen-label="Title-A" style={frame}>
        <window.ArenaBlurBg dim={0.8} />
        {/* P1/P2 versus accent stripe at very top */}
        <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: 5, background: `linear-gradient(90deg, ${P.p1} 0 50%, ${P.p2} 50% 100%)`, opacity: .9 }} />
        <div data-name="_TitlePanel" style={{
          position: "absolute", inset: 0, display: "flex", flexDirection: "column",
          alignItems: "center", justifyContent: "center", gap: 64,
        }}>
          <Logo />
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
            {MENU.map((m, i) => <MenuItemA key={i} item={m} i={i} selected={i === 0} />)}
          </div>
        </div>
        <div style={{ position: "absolute", left: 0, right: 0, bottom: 48, display: "flex", justifyContent: "center" }}>
          <Hint text="W / S  ･  ↑ / ↓   選択　　　SPACE / ENTER   決定" />
        </div>
        <div style={{ position: "absolute", right: 40, bottom: 40, fontFamily: P.mono, fontWeight: 700, fontSize: 13, letterSpacing: ".14em", color: P.line2 }}>v0.3 ･ LOCAL 2P</div>
      </div>
    );
  }

  // ════════════════════════ TITLE B — asymmetric kinetic ════════════════════════
  function MenuItemB({ item, i, selected }) {
    return (
      <div data-name={`Menu${i}${item.key}`} style={{
        position: "relative", display: "flex", alignItems: "center", gap: 20,
        padding: "10px 0 10px 0", height: 64,
      }}>
        {/* $Menu{i}Cursor */}
        {selected && (
          <div data-name={`$Menu${i}Cursor`} style={{ display: "flex", alignItems: "center", gap: 20 }}>
            <div style={{ width: 10, height: 44, background: P.accent, boxShadow: `0 0 16px ${P.accent}` }} />
            <span style={{ fontFamily: P.display, fontSize: 34, color: P.accent, marginLeft: -6 }}>►</span>
          </div>
        )}
        {!selected && <div style={{ width: 10, height: 44 }} />}
        <span style={{
          fontFamily: P.display, fontSize: 56, letterSpacing: ".08em", lineHeight: 1,
          color: selected ? P.white : P.gray,
          textShadow: selected ? `0 0 22px ${P.accent}55` : "none",
        }}>{item.label}</span>
      </div>
    );
  }

  function TitleB() {
    return (
      <div data-screen-label="Title-B" style={frame}>
        <window.ArenaBlurBg dim={0.74} />
        {/* diagonal darkening on the left to seat the type */}
        <div style={{ position: "absolute", inset: 0, background: `linear-gradient(105deg, rgba(5,10,26,0.9) 0%, rgba(5,10,26,0.65) 42%, rgba(5,10,26,0.1) 70%)` }} />
        <div data-name="_TitlePanel" style={{
          position: "absolute", inset: 0, display: "flex", flexDirection: "column",
          justifyContent: "center", gap: 56, padding: "0 0 0 130px",
        }}>
          <Logo align="left" />
          <div style={{ display: "flex", flexDirection: "column", gap: 2 }}>
            {MENU.map((m, i) => <MenuItemB key={i} item={m} i={i} selected={i === 0} />)}
          </div>
          <Hint text="W / S   選択　　SPACE / ENTER   決定" align="left" />
        </div>
        {/* P1 vs P2 emblem, right side */}
        <div style={{ position: "absolute", right: 120, top: "50%", transform: "translateY(-50%)", display: "flex", flexDirection: "column", alignItems: "center", gap: 18, opacity: .92 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 22 }}>
            <Emblem color={P.p1} label="P1" />
            <span style={{ fontFamily: P.display, fontSize: 64, color: P.white, letterSpacing: ".04em" }}>VS</span>
            <Emblem color={P.p2} label="P2" />
          </div>
        </div>
        <div style={{ position: "absolute", right: 40, bottom: 40, fontFamily: P.mono, fontWeight: 700, fontSize: 13, letterSpacing: ".14em", color: P.line2 }}>v0.3 ･ LOCAL 2P</div>
      </div>
    );
  }

  const Emblem = ({ color, label }) => (
    <div style={{
      width: 132, height: 132, borderRadius: 20, border: `3px solid ${color}`,
      background: `radial-gradient(circle at 50% 35%, ${color}26, transparent 70%)`,
      display: "flex", alignItems: "center", justifyContent: "center",
      fontFamily: P.display, fontSize: 64, color, letterSpacing: ".04em",
      boxShadow: `0 0 30px ${color}55, inset 0 0 26px ${color}22`,
    }}>{label}</div>
  );

  // ════════════════════════ SETTINGS overlay ════════════════════════
  function SettingsPanel() {
    const rounds = 3;
    return (
      <div data-screen-label="Settings" style={frame}>
        {/* the title sits behind, dimmed */}
        <window.ArenaBlurBg dim={0.86} />
        <div style={{ position: "absolute", inset: 0, background: "rgba(5,10,26,0.55)" }} />
        <div style={{ position: "absolute", inset: 0, display: "flex", alignItems: "center", justifyContent: "center" }}>
          <div data-name="_SettingsPanel" style={{
            width: 760, borderRadius: 18,
            background: "rgba(12,17,34,0.94)",
            border: `1.5px solid ${P.line2}`,
            boxShadow: `0 0 60px rgba(0,0,0,0.6), 0 0 0 1px ${P.accent}22`,
            padding: "56px 64px 48px", display: "flex", flexDirection: "column", alignItems: "center", gap: 12,
          }}>
            <div style={{ fontFamily: P.display, fontSize: 56, letterSpacing: ".08em", color: P.white }}>SETTINGS</div>
            <div style={{ fontFamily: jp, fontWeight: 700, fontSize: 15, letterSpacing: ".3em", color: P.gray, marginBottom: 30 }}>せってい</div>

            <div data-name="RoundsLabel" style={{
              fontFamily: P.mono, fontWeight: 700, fontSize: 17, letterSpacing: ".18em", color: P.gray,
              display: "flex", alignItems: "baseline", gap: 12,
            }}>
              <span style={{ color: P.white }}>先取数</span> ROUNDS
            </div>

            {/* stepper */}
            <div style={{ display: "flex", alignItems: "center", gap: 44, marginTop: 14 }}>
              <span style={{ fontFamily: P.display, fontSize: 64, color: P.accent, lineHeight: 1 }}>‹</span>
              <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 4, minWidth: 130 }}>
                <span data-name="$RoundsValue" style={{
                  fontFamily: P.display, fontSize: 132, lineHeight: .9, color: P.white,
                  textShadow: `0 0 30px ${P.accent}55`,
                }}>{rounds}</span>
                <span style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 13, letterSpacing: ".2em", color: P.gray }}>本先取</span>
              </div>
              <span style={{ fontFamily: P.display, fontSize: 64, color: P.accent, lineHeight: 1 }}>›</span>
            </div>

            {/* pips preview */}
            <div style={{ display: "flex", gap: 12, marginTop: 18 }}>
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} style={{
                  width: 18, height: 18, borderRadius: "50%",
                  border: `2px solid ${i < rounds ? P.accent : P.line2}`,
                  background: i < rounds ? P.accent : "transparent",
                  boxShadow: i < rounds ? `0 0 10px ${P.accent}` : "none",
                }} />
              ))}
            </div>
            <div style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 13, letterSpacing: ".14em", color: P.line2, marginTop: 6 }}>FIRST TO {rounds} WINS THE MATCH</div>

            <div style={{ width: "100%", height: 1, background: P.line, margin: "30px 0 18px" }} />
            <div data-name="Hint" style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 15, letterSpacing: ".1em", color: P.gray }}>
              <b style={{ color: P.white }}>A / D</b> ( ← / → ) で変更　　<b style={{ color: P.white }}>ENTER</b> で戻る
            </div>
          </div>
        </div>
      </div>
    );
  }

  const frame = {
    position: "relative", width: 1920, height: 1080, overflow: "hidden",
    background: P.bg, fontFamily: P.mono,
  };

  Object.assign(window, { TitleA, TitleB, SettingsPanel });
})();
