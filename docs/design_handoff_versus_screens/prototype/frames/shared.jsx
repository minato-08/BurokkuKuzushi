// shared.jsx — design tokens, blurred-arena background, skill glyphs
// Exports to window: HUDP (palette), ArenaBlurBg, SkillIcon, WinPips
(function () {
  const HUDP = {
    bg: "rgb(5,10,26)",
    bg2: "rgb(10,21,53)",
    panel: "rgb(39,42,49)",
    line: "rgb(42,47,68)",
    line2: "rgb(79,83,96)",
    p1: "rgb(78,195,255)",
    p2: "rgb(255,78,116)",
    accent: "rgb(236,201,47)",
    green: "rgb(123,224,123)",
    orange: "rgb(236,131,26)",
    violet: "rgb(176,150,255)",
    gray: "rgb(154,160,180)",
    ink: "rgb(232,230,223)",
    white: "rgb(255,255,255)",
    mono: "'JetBrains Mono', monospace",
    display: "'Bebas Neue', sans-serif",
  };

  // ── Blurred arena scene used behind both modals ───────────────────
  function brickRows(accent) {
    const rows = [];
    const cols = 7;
    const rowCount = 9;
    for (let r = 0; r < rowCount; r++) {
      const cells = [];
      for (let c = 0; c < cols; c++) {
        // punch some holes so it reads like a match in progress
        const gone = (r * 3 + c * 5) % 7 === 0 && r < 5;
        cells.push(
          <div key={c} style={{
            flex: 1,
            height: 26,
            borderRadius: 3,
            background: gone ? "transparent" : (r < 3
              ? "rgba(120,140,180,0.35)"
              : "rgba(80,100,150,0.55)"),
            boxShadow: gone ? "none" : `inset 0 0 0 1px ${accent}22`,
          }} />
        );
      }
      rows.push(
        <div key={r} style={{ display: "flex", gap: 6 }}>{cells}</div>
      );
    }
    return rows;
  }

  function ArenaColumn({ accent, side }) {
    return (
      <div style={{
        position: "relative",
        flex: 1,
        height: "100%",
        borderRadius: 14,
        background: `linear-gradient(${HUDP.bg} 0%, ${accent}14 100%)`,
        border: `3px solid ${accent}`,
        overflow: "hidden",
        display: "flex",
        flexDirection: "column",
        padding: "40px 34px",
        gap: 6,
      }}>
        {brickRows(accent)}
        {/* paddle */}
        <div style={{
          position: "absolute",
          left: side === "p1" ? "30%" : "44%",
          bottom: 60,
          width: 150, height: 16, borderRadius: 8,
          background: accent, boxShadow: `0 0 24px ${accent}`,
        }} />
        {/* ball */}
        <div style={{
          position: "absolute",
          left: side === "p1" ? "52%" : "38%",
          bottom: 150,
          width: 22, height: 22, borderRadius: "50%",
          background: HUDP.white, boxShadow: `0 0 18px ${HUDP.white}`,
        }} />
      </div>
    );
  }

  function ArenaBlurBg({ dim = 0.74 }) {
    return (
      <div style={{ position: "absolute", inset: 0, overflow: "hidden", background: HUDP.bg }}>
        <div style={{
          position: "absolute", inset: 0,
          display: "flex", gap: 18, padding: 24,
          filter: "blur(7px) saturate(1.05)",
          transform: "scale(1.04)",
        }}>
          <ArenaColumn accent={HUDP.p1} side="p1" />
          <div style={{ width: 120 }} />
          <ArenaColumn accent={HUDP.p2} side="p2" />
        </div>
        {/* darken + vignette */}
        <div style={{
          position: "absolute", inset: 0,
          background: `radial-gradient(120% 90% at 50% 45%, rgba(5,10,26,${dim - 0.12}) 0%, rgba(5,10,26,${Math.min(dim + 0.16, 0.95)}) 100%)`,
        }} />
      </div>
    );
  }

  // ── Skill glyphs (geometric, stroke style) ────────────────────────
  function SkillIcon({ kind, color, size = 56, stroke = 3 }) {
    const common = {
      width: size, height: size, viewBox: "0 0 64 64",
      fill: "none", stroke: color, strokeWidth: stroke,
      strokeLinecap: "round", strokeLinejoin: "round",
    };
    if (kind === "paddle") {
      return (
        <svg {...common}>
          <rect x="10" y="40" width="44" height="12" rx="6" fill={color} stroke="none" />
          <line x1="16" y1="24" x2="48" y2="24" />
          <path d="M16 24 L22 18 M16 24 L22 30" />
          <path d="M48 24 L42 18 M48 24 L42 30" />
        </svg>
      );
    }
    if (kind === "fire") {
      return (
        <svg {...common}>
          <path d="M32 8 C 40 20 46 26 46 38 a14 14 0 1 1 -28 0 c0 -7 4 -11 7 -15 c1 6 4 8 7 9 c-2 -8 0 -16 0 -23 Z" />
          <circle cx="32" cy="40" r="6" fill={color} stroke="none" />
        </svg>
      );
    }
    if (kind === "double") {
      return (
        <svg {...common}>
          <circle cx="24" cy="32" r="13" />
          <circle cx="42" cy="32" r="13" />
        </svg>
      );
    }
    // explosion / clear
    return (
      <svg {...common}>
        <path d="M32 6 L37 24 L55 14 L42 30 L60 34 L42 38 L52 56 L36 44 L32 60 L28 44 L12 56 L22 38 L4 34 L22 30 L9 14 L27 24 Z" />
        <circle cx="32" cy="34" r="5" fill={color} stroke="none" />
      </svg>
    );
  }

  // ── Win pips (best-of indicator) ──────────────────────────────────
  function WinPips({ wins, total, color, align = "left" }) {
    return (
      <div style={{ display: "flex", gap: 8, justifyContent: align === "right" ? "flex-end" : "flex-start" }}>
        {Array.from({ length: total }).map((_, i) => (
          <div key={i} style={{
            width: 16, height: 16, borderRadius: "50%",
            border: `2px solid ${i < wins ? color : HUDP.line2}`,
            background: i < wins ? color : "transparent",
            boxShadow: i < wins ? `0 0 10px ${color}` : "none",
          }} />
        ))}
      </div>
    );
  }

  Object.assign(window, { HUDP, ArenaBlurBg, SkillIcon, WinPips });
})();
