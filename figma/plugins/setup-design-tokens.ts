// ============================================================
// MARIXA FIGMA SETUP PLUGIN
// ============================================================
// Cách chạy:
//   1. Mở Figma file "Marixa001" (browser hoặc desktop)
//   2. Menu: Plugins → Development → New Plugin...
//   3. Đặt tên: "Marixa Setup"
//   4. Paste TOÀN BỘ code này vào editor
//   5. Bấm "Run"
//
// Plugin tạo:
//   1. Variable Collection "Marixa Design Tokens" (mode: Light)
//      - 46 COLOR vars (đủ hex từ marixa_react/src/theme.css)
//      - 9  FLOAT vars (spacing, radius, touch)
//      - 1  STRING var (font family)
//      = 56 variables total
//   2. Page "UI Kit" với 10 component placeholders
//   3. 11 route pages, mỗi page 2 frames:
//      - "-mobile"  (375 × 812)
//      - "-desktop" (1440 × 900)
//      Pre-fill nền th-bg (#F5F6F8) + ghi chú route/module/roles
//
// LƯU Ý:
//   - Shadow values (th-shadow-card, th-shadow-lg, th-header-shadow,
//     th-card-shadow) là CSS box-shadow → KHÔNG tạo thành Variable
//     (Figma dùng Effects → Drop Shadow, không phải variable).
//     Apply thủ công khi thiết kế từng frame.
//   - Script là idempotent: chạy lần 2 sẽ skip các variable đã có
//     (catch error "already exists"), chỉ thêm cái mới.
//   - Sau khi chạy: mở Variables panel (trái, icon cuối) để verify.
//
// CSS variable ↔ Figma Variable mapping:
//   --th-primary         → color/brand/th-primary
//   --th-header-from     → color/header/th-header-from
//   --th-side-bg         → color/sidebar/th-side-bg
//   --th-bg              → color/bg-surface/th-bg
//   --th-text            → color/text/th-text
//   --th-ok              → color/status/th-ok
//   --th-card-bg         → color/card/th-card-bg
//   (shadow/typo/spacing/radius/touch → các nhóm tương ứng)
// ============================================================

// ─── Helpers ─────────────────────────────────────────────────

function hexToRgb(hex: string): { r: number; g: number; b: number } {
  const c = hex.replace("#", "");
  const n = parseInt(c, 16);
  return {
    r: ((n >> 16) & 0xff) / 255,
    g: ((n >> 8) & 0xff) / 255,
    b: (n & 0xff) / 255,
  };
}

function solidPaint(hex: string, alpha = 1): Paint {
  const { r, g, b } = hexToRgb(hex);
  return { type: "SOLID", color: { r, g, b, a: alpha } };
}

// ─── Variable definitions (from marixa_react/src/theme.css) ─

const COLOR_VARS: [string, string][] = [
  // Brand (7)
  ["color/brand/th-primary", "#1647C8"],
  ["color/brand/th-primary-strong", "#0F34A0"],
  ["color/brand/th-primary-light", "#2F6DF6"],
  ["color/brand/th-cyan", "#00BFD0"],
  ["color/brand/th-gold", "#F3B928"],
  ["color/brand/th-green", "#16a34a"],
  ["color/brand/th-green-strong", "#15803d"],
  // Header (8)
  ["color/header/th-header-from", "#FFFFFF"],
  ["color/header/th-header-mid", "#F8FAFC"],
  ["color/header/th-header-to", "#F1F5F9"],
  ["color/header/th-header-text", "#0e1a26"],
  ["color/header/th-header-sub", "#5a6b7b"],
  ["color/header/th-header-surface", "#f0f4f8"],
  ["color/header/th-header-surface-hover", "#e8edf3"],
  ["color/header/th-header-border", "#E2E8F0"],
  // Sidebar (9)
  ["color/sidebar/th-side-bg", "#FFFFFF"],
  ["color/sidebar/th-side-border", "#E4EAF2"],
  ["color/sidebar/th-side-text", "#526174"],
  ["color/sidebar/th-side-text-strong", "#172033"],
  ["color/sidebar/th-side-hover", "#F5F7FA"],
  ["color/sidebar/th-side-active-bg", "#E3ECFF"],
  ["color/sidebar/th-side-active-text", "#1647C8"],
  ["color/sidebar/th-side-active-bar", "#2F6DF6"],
  ["color/sidebar/th-side-scroll", "#C9D3E0"],
  // BG & Surface (5)
  ["color/bg-surface/th-bg", "#F5F6F8"],
  ["color/bg-surface/th-surface", "#FFFFFF"],
  ["color/bg-surface/th-surface-alt", "#F8F9FB"],
  ["color/bg-surface/th-border", "#DCE3EC"],
  ["color/bg-surface/th-divider", "#EEF2F6"],
  // Text (3)
  ["color/text/th-text", "#172033"],
  ["color/text/th-text-2", "#526174"],
  ["color/text/th-text-3", "#8A99AB"],
  // Status (11)
  ["color/status/th-ok", "#167347"],
  ["color/status/th-ok-bg", "#ECFDF5"],
  ["color/status/th-ok-border", "#BBF7D0"],
  ["color/status/th-warn", "#9A5A00"],
  ["color/status/th-warn-bg", "#FEF9C3"],
  ["color/status/th-warn-border", "#FDE68A"],
  ["color/status/th-err", "#B42318"],
  ["color/status/th-err-bg", "#FEF2F2"],
  ["color/status/th-err-border", "#FECACA"],
  ["color/status/th-info", "#1D4ED8"],
  ["color/status/th-info-bg", "#DBEAFE"],
  // Card / Clock / Slot (8)
  ["color/card/th-card-bg", "#FFFFFF"],
  ["color/card/th-card-border", "#E2E8F0"],
  ["color/card/th-clock-bg", "#0e1a26"],
  ["color/card/th-clock-text", "#FFFFFF"],
  ["color/card/th-clock-date", "#94A3B8"],
  ["color/card/th-slot-bg", "#F8FAFC"],
  ["color/card/th-slot-border", "#E2E8F0"],
];

const FLOAT_VARS: [string, number][] = [
  ["spacing/4", 4],
  ["spacing/8", 8],
  ["spacing/12", 12],
  ["spacing/16", 16],
  ["spacing/24", 24],
  ["spacing/32", 32],
  ["radius/input-button", 8],
  ["radius/card", 12],
  ["touch/min-target", 44],
];

const STRING_VARS: [string, string][] = [
  [
    "typo/font",
    '"Be Vietnam Pro", "Inter", ui-sans-serif, system-ui, sans-serif',
  ],
];

// ─── UI Kit component specs (Q6=B: create first) ───────────

interface CompSpec {
  name: string;
  w: number;
  h: number;
  fill: string;
  radius: number;
  label: string;
  labelColor: string;
  cssRef: string;
}

const UI_KIT: CompSpec[] = [
  {
    name: "Button/Primary", w: 140, h: 44,
    fill: "#1647C8", radius: 8,
    label: "Chấm công", labelColor: "#FFFFFF",
    cssRef: "bg: var(--th-primary) · radius: 8px",
  },
  {
    name: "Button/Ghost", w: 140, h: 44,
    fill: "#FFFFFF", radius: 8,
    label: "Hủy", labelColor: "#526174",
    cssRef: "border: var(--th-border) · text: var(--th-text-2)",
  },
  {
    name: "Button/Danger", w: 140, h: 44,
    fill: "#B42318", radius: 8,
    label: "Xóa", labelColor: "#FFFFFF",
    cssRef: "bg: var(--th-err)",
  },
  {
    name: "Button/Success", w: 140, h: 44,
    fill: "#167347", radius: 8,
    label: "Lưu", labelColor: "#FFFFFF",
    cssRef: "bg: var(--th-ok)",
  },
  {
    name: "Input", w: 300, h: 44,
    fill: "#FFFFFF", radius: 8,
    label: "Nhập tài khoản…", labelColor: "#8A99AB",
    cssRef: "bg: var(--th-surface) · border: var(--th-border)",
  },
  {
    name: "Card", w: 400, h: 200,
    fill: "#FFFFFF", radius: 12,
    label: "Nội dung card", labelColor: "#172033",
    cssRef: "bg: var(--th-surface) · border: var(--th-card-border) · shadow: --th-shadow-card (manual)",
  },
  {
    name: "Badge/OK", w: 72, h: 24,
    fill: "#ECFDF5", radius: 4,
    label: "OK", labelColor: "#167347",
    cssRef: "bg: var(--th-ok-bg) · border: var(--th-ok-border) · text: var(--th-ok)",
  },
  {
    name: "Badge/Warn", w: 96, h: 24,
    fill: "#FEF9C3", radius: 4,
    label: "Cảnh báo", labelColor: "#9A5A00",
    cssRef: "bg: var(--th-warn-bg) · border: var(--th-warn-border) · text: var(--th-warn)",
  },
  {
    name: "Dialog", w: 400, h: 300,
    fill: "#FFFFFF", radius: 12,
    label: "Hộp thoại (title + body + 2 buttons)", labelColor: "#172033",
    cssRef: "bg: var(--th-surface) · shadow: --th-shadow-lg (manual) · radius: 12px",
  },
  {
    name: "Toast", w: 320, h: 56,
    fill: "#FFFFFF", radius: 8,
    label: "Thông báo (icon + text)", labelColor: "#172033",
    cssRef: "bg: var(--th-surface) · shadow: --th-shadow-card (manual)",
  },
];

// ─── Route page specs (Q5=B: 2 frames per route) ───────────

interface RouteSpec {
  page: string;
  route: string;
  module: string;
  roles: string;
  notes?: string;
}

const ROUTES: RouteSpec[] = [
  { page: "Login", route: "/login", module: "auth", roles: "Public", notes: "Loading / sai TK / khóa TK / cần kích hoạt" },
  { page: "Activate", route: "/kich-hoat", module: "activate", roles: "Public", notes: "Kích hoạt mã + đặt mật khẩu" },
  { page: "ClockIn", route: "/attendance", module: "me", roles: "Employee", notes: "Camera + GPS + log (09-ui-design §4)" },
  { page: "AttendanceHistory", route: "/attendance/attendance-history", module: "me/hr", roles: "Employee, HR", notes: "HR: đối soát & duyệt công" },
  { page: "Statistics", route: "/attendance/statistics", module: "me/hr", roles: "Employee, HR", notes: "KPI + cảnh báo + bảng chi tiết" },
  { page: "Leave", route: "/attendance/leave", module: "me", roles: "Employee", notes: "Nháp / chờ / duyệt / từ chối / hủy" },
  { page: "Employees", route: "/employees/*", module: "hr/admin", roles: "HR, Admin", notes: "Danh sách nhân sự + đối soát + duyệt (worklog §2 D/E)" },
  { page: "Admin", route: "/admin/*", module: "admin", roles: "Admin", notes: "Dashboard + TK + role + hợp đồng + lương + nghỉ việc + báo cáo" },
  { page: "Work", route: "/work", module: "work", roles: "Đa role", notes: "Dự án / task / hiệu suất" },
  { page: "Profile", route: "/profile", module: "profile", roles: "Đã xác thực", notes: "Hồ sơ cá nhân" },
  { page: "Unauthorized", route: "/unauthorized", module: "—", roles: "—", notes: "Thiếu quyền" },
];

// ─── Main ──────────────────────────────────────────────────

async function main() {
  const originalPage = figma.currentPage;
  let created = 0, skipped = 0;

  // ── 1. VARIABLE COLLECTION ──
  const colName = "Marixa Design Tokens";
  let collection: VariableCollection;
  try {
    collection = await figma.variables.createVariableCollection({
      name: colName,
      modes: [{ name: "Light" }],
    });
  } catch (e) {
    figma.notify("⚠️ Create collection failed: " + (e as Error).message, { timeout: 5000 });
    return;
  }
  const modeId = collection.modes[0].modeId;
  collection.initialModeId = modeId;

  // Color variables
  for (const [name, hex] of COLOR_VARS) {
    try {
      const { r, g, b } = hexToRgb(hex);
      await figma.variables.createVariableAsync({
        name,
        variableType: "COLOR",
        collections: [collection],
        valuesByMode: { [modeId]: { r, g, b, a: 1 } },
      });
      created++;
    } catch { skipped++; }
  }

  // Float variables
  for (const [name, val] of FLOAT_VARS) {
    try {
      await figma.variables.createVariableAsync({
        name,
        variableType: "FLOAT",
        collections: [collection],
        valuesByMode: { [modeId]: val },
      });
      created++;
    } catch { skipped++; }
  }

  // String variables
  for (const [name, val] of STRING_VARS) {
    try {
      await figma.variables.createVariableAsync({
        name,
        variableType: "STRING",
        collections: [collection],
        valuesByMode: { [modeId]: val },
      });
      created++;
    } catch { skipped++; }
  }

  figma.notify(
    `✅ Variables: ${created} created, ${skipped} skipped (already exist)`,
    { timeout: 3000 }
  );

  // ── 2. UI KIT PAGE ──
  await figma.loadFontAsync({ family: "Inter", style: "Regular" });
  await figma.loadFontAsync({ family: "Inter", style: "Medium" });

  let uiPage: Page;
  const existingUi = figma.root.children.find(
    (p) => p.type === "PAGE" && p.name === "UI Kit"
  );
  if (existingUi) {
    uiPage = existingUi as Page;
  } else {
    uiPage = figma.createPage();
    uiPage.name = "UI Kit";
  }
  figma.currentPage = uiPage;

  let compX = 40, compY = 40, rowMaxH = 0;
  for (const spec of UI_KIT) {
    const exists = uiPage.children.find((ch) => ch.name === spec.name);
    if (exists) {
      compX += spec.w + 40;
      if (compX > 1100) { compX = 40; compY += rowMaxH + 80; }
      rowMaxH = Math.max(rowMaxH, spec.h + 32);
      continue;
    }

    const c = figma.createComponent();
    c.name = spec.name;
    c.resize(spec.w, spec.h);
    c.cornerRadius = spec.radius;
    c.fills = [solidPaint(spec.fill)];

    // Label (centered)
    const label = figma.createText();
    label.fontName = { family: "Inter", style: "Regular" };
    label.fontSize = spec.w > 200 ? 14 : 13;
    label.characters = spec.label;
    label.fills = [solidPaint(spec.labelColor)];
    c.appendChild(label);
    label.x = Math.max(8, Math.round((spec.w - label.width) / 2));
    label.y = Math.round((spec.h - label.height) / 2);

    // CSS reference (below component)
    const desc = figma.createText();
    desc.fontName = { family: "Inter", style: "Regular" };
    desc.fontSize = 10;
    desc.characters = spec.cssRef;
    desc.fills = [solidPaint("#8A99AB")];
    uiPage.appendChild(desc);
    desc.x = compX;
    desc.y = compY + spec.h + 8;

    // Position component
    c.x = compX;
    c.y = compY;
    compX += spec.w + 40;
    rowMaxH = Math.max(rowMaxH, spec.h + 32);
    if (compX > 1100) {
      compX = 40;
      compY += rowMaxH + 80;
      rowMaxH = 0;
    }
  }

  figma.notify(`✅ UI Kit: ${UI_KIT.length} components placed on page`, { timeout: 3000 });

  // ── 3. ROUTE PAGES ──
  for (const r of ROUTES) {
    let page: Page;
    const existingPage = figma.root.children.find(
      (p) => p.type === "PAGE" && p.name === r.page
    );
    if (existingPage) {
      page = existingPage as Page;
    } else {
      page = figma.createPage();
      page.name = r.page;
    }
    figma.currentPage = page;

    const bg = solidPaint("#F5F6F8"); // th-bg
    const info = `${r.route}  (${r.module})  —  ${r.roles}${r.notes ? "  |  " + r.notes : ""}`;

    // Mobile frame
    if (!page.children.find((ch) => ch.name === r.page + "-mobile")) {
      const m = figma.createFrame();
      m.name = r.page + "-mobile";
      m.resize(375, 812);
      m.fills = [bg];
      m.x = 20;
      m.y = 20;
      const ml = figma.createText();
      ml.fontName = { family: "Inter", style: "Medium" };
      ml.fontSize = 11;
      ml.characters = "MOBILE 375×812 — " + info;
      ml.fills = [solidPaint("#8A99AB")];
      m.appendChild(ml);
      ml.x = 12;
      ml.y = 12;
    }

    // Desktop frame
    if (!page.children.find((ch) => ch.name === r.page + "-desktop")) {
      const d = figma.createFrame();
      d.name = r.page + "-desktop";
      d.resize(1440, 900);
      d.fills = [bg];
      d.x = 460;
      d.y = 20;
      const dl = figma.createText();
      dl.fontName = { family: "Inter", style: "Medium" };
      dl.fontSize = 11;
      dl.characters = "DESKTOP 1440×900 — " + info;
      dl.fills = [solidPaint("#8A99AB")];
      d.appendChild(dl);
      dl.x = 12;
      dl.y = 12;
    }
  }

  figma.notify(`✅ Route pages: ${ROUTES.length} pages × 2 frames (mobile + desktop)`, { timeout: 3000 });

  // ── Done ──
  figma.currentPage = originalPage;
  figma.notify(
    "🎉 Marixa Figma setup COMPLETE!\n" +
      `  • ${COLOR_VARS.length} color + ${FLOAT_VARS.length} float + ${STRING_VARS.length} string variables\n` +
      `  • UI Kit: ${UI_KIT.length} component placeholders\n` +
      `  • ${ROUTES.length} route pages × 2 frames\n` +
      "  • Shadows: apply manually (Effects → Drop Shadow)\n" +
      "  • Next: open Variables panel (left sidebar) to verify",
    { timeout: 10000 }
  );
}

main();
