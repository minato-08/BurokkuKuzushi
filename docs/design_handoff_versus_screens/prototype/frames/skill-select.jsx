// skill-select.jsx — Skill Select screen, variations A (row+frame) & B (fan+glow)
// Exports to window: SkillSelectA, SkillSelectB
(function () {
  const P = window.HUDP;

  // index order MUST match code (spec): 0..3
  const SKILLS = [
    { name: "BIG PADDLE",      desc: "パドルを10秒拡大",        kind: "paddle",    color: P.green },
    { name: "FIRE BALL",       desc: "着弾点を10秒爆破",        kind: "fire",      color: P.orange },
    { name: "DOUBLE BALL",     desc: "ボールを10秒2個に",       kind: "double",    color: P.violet },
    { name: "EMERGENCY CLEAR", desc: "下半分を即消去 ･ HP≤10%", kind: "explosion", color: P.accent, locked: true },
  ];

  const Header = ({ note }) => (
    <div style={{ textAlign: "center", marginBottom: 8 }}>
      <div data-name="Header" style={{
        fontFamily: P.display, fontSize: 64, letterSpacing: ".08em",
        color: P.white, lineHeight: 1, whiteSpace: "nowrap",
        textShadow: `0 0 24px ${P.accent}55`,
      }}>SELECT SKILL</div>
      <div style={{
        fontFamily: P.mono, fontSize: 15, fontWeight: 700, letterSpacing: ".34em",
        color: P.gray, marginTop: 10,
      }}>{note}</div>
    </div>
  );

  // status guide pinned bottom corners
  const StatusBar = ({ side, color, label, confirmed, keys }) => (
    <div data-name={side === "p1" ? "_P1Status" : "_P2Status"} style={{
      position: "absolute", bottom: 56,
      [side === "p1" ? "left" : "right"]: 80,
      display: "flex", flexDirection: "column", gap: 10,
      alignItems: side === "p1" ? "flex-start" : "flex-end",
    }}>
      <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
        <span style={{
          fontFamily: P.mono, fontWeight: 700, fontSize: 18, letterSpacing: ".04em",
          color: "#000", background: color, padding: "4px 14px", borderRadius: 8,
        }}>{label}</span>
      </div>
      <div data-name={side === "p1" ? "$P1Status" : "$P2Status"} style={{
        fontFamily: P.mono, fontWeight: 700, fontSize: 16, letterSpacing: ".1em",
        color: confirmed ? color : P.gray,
      }}>
        {confirmed
          ? <span>✓ READY <span style={{ color: P.gray }}>･ 相手を待機中…</span></span>
          : <span><b style={{ color: P.white }}>{keys[0]}</b> SELECT&nbsp;&nbsp;&nbsp;<b style={{ color: P.white }}>{keys[1]}</b> CONFIRM</span>}
      </div>
    </div>
  );

  // ════════════════════════ VARIATION A — row + frame ════════════════════════
  function CardA({ skill, i, p1, p2, p1confirmed, p2confirmed }) {
    const active = p1 || p2;
    return (
      <div data-name={`_SkillCard${i}`} style={{ position: "relative", width: 300, height: 416 }}>
        {/* P1 bracket (top, blue) */}
        {p1 && <Bracket color={P.p1} edge="top" confirmed={p1confirmed} tag="P1" name={`$Card${i}P1Cursor`} />}
        {/* P2 bracket (bottom, pink) */}
        {p2 && <Bracket color={P.p2} edge="bottom" confirmed={p2confirmed} tag="P2" name={`$Card${i}P2Cursor`} />}

        <div style={{
          position: "absolute", inset: 0, borderRadius: 14,
          background: skill.locked ? "rgba(39,42,49,0.5)" : "rgba(16,21,40,0.82)",
          border: `1.5px solid ${active ? skill.color + "00" : P.line}`,
          opacity: skill.locked ? 0.55 : 1,
          display: "flex", flexDirection: "column", alignItems: "center",
          padding: "44px 22px 28px", gap: 18,
        }}>
          <span style={{
            position: "absolute", top: 14, left: 16,
            fontFamily: P.mono, fontWeight: 700, fontSize: 14, color: P.gray, letterSpacing: ".1em",
          }}>0{i + 1}</span>
          <div style={{
            width: 92, height: 92, borderRadius: 12, display: "flex",
            alignItems: "center", justifyContent: "center",
            background: skill.color + "14", border: `1.5px solid ${skill.color}55`,
          }}>
            <window.SkillIcon kind={skill.kind} color={skill.color} size={54} />
          </div>
          <div data-name={`Card${i}Name`} style={{
            fontFamily: P.display, fontSize: 40, letterSpacing: ".03em", lineHeight: .9,
            color: P.white, textAlign: "center",
          }}>{skill.name.split(" ").map((w, k) => <div key={k}>{w}</div>)}</div>
          <div data-name={`Card${i}Desc`} style={{
            fontFamily: P.mono, fontSize: 14, lineHeight: 1.5, color: P.gray, textAlign: "center",
            textWrap: "balance",
          }}>{skill.desc}</div>
          {skill.locked && (
            <div style={{
              marginTop: "auto", fontFamily: P.mono, fontWeight: 700, fontSize: 12,
              letterSpacing: ".14em", color: P.accent,
              border: `1px solid ${P.accent}55`, borderRadius: 6, padding: "4px 10px",
            }}>🔒 CONDITION LOCKED</div>
          )}
        </div>
      </div>
    );
  }

  function Bracket({ color, edge, confirmed, tag, name }) {
    const top = edge === "top";
    return (
      <div data-name={name} style={{ position: "absolute", inset: -12, pointerEvents: "none", zIndex: 3 }}>
        {/* full glow frame when confirmed */}
        <div style={{
          position: "absolute", inset: 0, borderRadius: 18,
          border: `2.5px solid ${color}`,
          boxShadow: confirmed ? `0 0 26px ${color}, inset 0 0 22px ${color}33` : `0 0 14px ${color}88`,
          opacity: confirmed ? 1 : 0.9,
          clipPath: confirmed ? "none"
            : (top ? "polygon(0 0,100% 0,100% 34%,0 34%)" : "polygon(0 66%,100% 66%,100% 100%,0 100%)"),
        }} />
        {/* player tab */}
        <div style={{
          position: "absolute", [top ? "top" : "bottom"]: -16, left: "50%", transform: "translateX(-50%)",
          background: color, color: "#000", fontFamily: P.mono, fontWeight: 700, fontSize: 15,
          letterSpacing: ".06em", padding: "3px 16px", borderRadius: 7,
          boxShadow: `0 0 12px ${color}`,
        }}>{tag}{confirmed ? " ✓" : ""}</div>
      </div>
    );
  }

  function SkillSelectA() {
    return (
      <div data-screen-label="SkillSelect-A" style={frameStyle}>
        <window.ArenaBlurBg dim={0.78} />
        <div data-name="_SkillSelectPanel" style={{ ...panelStyle, justifyContent: "flex-start", paddingTop: 110 }}>
          <Header note="同じスキルも選択可 ･ 互いの選択が見える" />
          <div data-name="_SkillCards" style={{ display: "flex", gap: 30, marginTop: 64 }}>
            {SKILLS.map((s, i) => (
              <CardA key={i} skill={s} i={i}
                p1={i === 2} p2={i === 1} p2confirmed={i === 1} />
            ))}
          </div>
          <StatusBar side="p1" color={P.p1} label="PLAYER 1" keys={["A / D", "S"]} />
          <StatusBar side="p2" color={P.p2} label="PLAYER 2" keys={["J / L", "K"]} confirmed />
        </div>
      </div>
    );
  }

  // ════════════════════════ VARIATION B — fan + glow ════════════════════════
  function CardB({ skill, i, rot, dy, p1, p2, p1confirmed, p2confirmed }) {
    const glow = [];
    if (p1) glow.push(`0 0 0 3px ${P.p1}`, `0 0 34px ${P.p1}aa`);
    if (p2) glow.push(`0 0 0 3px ${P.p2}`, `0 0 34px ${P.p2}aa`);
    const lift = (p1 || p2) ? -34 : 0;
    const fillColor = p1confirmed ? P.p1 : p2confirmed ? P.p2 : null;
    return (
      <div data-name={`_SkillCard${i}`} style={{
        position: "absolute", left: "50%", top: "50%",
        width: 256, height: 360, marginLeft: -128, marginTop: -180,
        transform: `translateX(${(i - 1.5) * 232}px) translateY(${dy + lift}px) rotate(${rot}deg)`,
        transformOrigin: "bottom center",
        transition: "transform .2s",
        zIndex: (p1 || p2) ? 5 : i,
      }}>
        <div style={{
          position: "absolute", inset: 0, borderRadius: 16,
          background: fillColor
            ? `linear-gradient(180deg, ${fillColor}33, rgba(16,21,40,0.95))`
            : "rgba(16,21,40,0.92)",
          border: `1.5px solid ${fillColor || (skill.locked ? P.line : P.line2)}`,
          boxShadow: glow.length ? glow.join(",") : "0 18px 40px rgba(0,0,0,0.5)",
          opacity: skill.locked ? 0.6 : 1,
          display: "flex", flexDirection: "column", alignItems: "center",
          padding: "34px 20px 22px", gap: 16,
        }}>
          <div style={{
            width: 84, height: 84, borderRadius: 12, display: "flex",
            alignItems: "center", justifyContent: "center",
            background: skill.color + "16", border: `1.5px solid ${skill.color}66`,
          }}>
            <window.SkillIcon kind={skill.kind} color={skill.color} size={48} />
          </div>
          <div data-name={`Card${i}Name`} style={{
            fontFamily: P.display, fontSize: 34, letterSpacing: ".03em", lineHeight: .9,
            color: P.white, textAlign: "center",
          }}>{skill.name.split(" ").map((w, k) => <div key={k}>{w}</div>)}</div>
          <div data-name={`Card${i}Desc`} style={{
            fontFamily: P.mono, fontSize: 13, lineHeight: 1.5, color: P.gray, textAlign: "center",
            textWrap: "balance",
          }}>{skill.desc}</div>
          {/* cursor chips */}
          <div style={{ marginTop: "auto", display: "flex", gap: 8 }}>
            {p1 && <Chip color={P.p1} text={p1confirmed ? "P1 ✓ READY" : "P1"} name={`$Card${i}P1Cursor`} />}
            {p2 && <Chip color={P.p2} text={p2confirmed ? "P2 ✓ READY" : "P2"} name={`$Card${i}P2Cursor`} />}
            {skill.locked && !p1 && !p2 && (
              <span style={{ fontFamily: P.mono, fontWeight: 700, fontSize: 11, color: P.accent, letterSpacing: ".1em" }}>🔒 HP≤10%</span>
            )}
          </div>
        </div>
      </div>
    );
  }

  const Chip = ({ color, text, name }) => (
    <span data-name={name} style={{
      background: color, color: "#000", fontFamily: P.mono, fontWeight: 700,
      fontSize: 13, letterSpacing: ".05em", padding: "3px 12px", borderRadius: 7,
      boxShadow: `0 0 12px ${color}`,
    }}>{text}</span>
  );

  function SkillSelectB() {
    const rots = [-10, -3.5, 3.5, 10];
    const dys = [44, 8, 8, 44];
    return (
      <div data-screen-label="SkillSelect-B" style={frameStyle}>
        <window.ArenaBlurBg dim={0.8} />
        <div data-name="_SkillSelectPanel" style={{ ...panelStyle, justifyContent: "flex-start", paddingTop: 96 }}>
          <Header note="カーソルを左右に ･ 決めたら確定" />
          <div data-name="_SkillCards" style={{ position: "relative", width: "100%", height: 520, marginTop: 40 }}>
            {SKILLS.map((s, i) => (
              <CardB key={i} skill={s} i={i} rot={rots[i]} dy={dys[i]}
                p1={i === 0} p2={i === 2} p2confirmed={i === 2} />
            ))}
          </div>
          <StatusBar side="p1" color={P.p1} label="PLAYER 1" keys={["A / D", "S"]} />
          <StatusBar side="p2" color={P.p2} label="PLAYER 2" keys={["J / L", "K"]} confirmed />
        </div>
      </div>
    );
  }

  const frameStyle = {
    position: "relative", width: 1920, height: 1080, overflow: "hidden",
    background: P.bg, fontFamily: P.mono,
  };
  const panelStyle = {
    position: "absolute", inset: 0, display: "flex", flexDirection: "column",
    alignItems: "center", justifyContent: "center", padding: "80px 60px",
  };

  Object.assign(window, { SkillSelectA, SkillSelectB });
})();
