const state = {
  users: [],
  loginUsers: [],
  serviceItems: [],
  activities: [],
  giftRecords: [],
  departments: [],
  players: [],
  bosses: [],
  orders: [],
  payments: [],
  auditLogs: [],
  moneyLogs: [],
  loginHistories: [],
  preferences: null,
  logFilters: {
    audit: { date: "", account: "", keyword: "", type: "", sort: "desc" },
    money: { date: "", account: "", keyword: "", type: "", sort: "desc" },
    login: { date: "", account: "", keyword: "", type: "", sort: "desc" }
  },
  permissionMatrix: null,
  organizations: [],
  activeDepartmentId: null,
  activeMemberPicker: null,
  giftRecordAttachmentFiles: [],
  orderAttachmentFiles: [],
  view: "dashboard",
  serviceCategory: "play",
  orderServiceCategory: "play",
  orderAmountManuallyEdited: false,
  orderBaseAmountManuallyEdited: false,
  responseTime: {
    samples: [],
    lastMs: null,
    averageMs: null,
    maxMs: null
  },
  auth: null
};

const titles = {
  dashboard: ["Dashboard", "總覽"],
  users: ["Users", "成員"],
  loginUsers: ["Accounts", "帳號管理"],
  organization: ["Organization", "組織"],
  orders: ["Orders", "訂單"],
  activities: ["Activities", "活動管理"],
  giftRecords: ["Gift Records", "送禮紀錄"],
  payments: ["Payments", "月結"],
  audit: ["Audit", "紀錄"],
  moneyLogs: ["Money Log", "金流紀錄"],
  loginHistory: ["Login History", "登入紀錄"],
  settings: ["Settings", "個人化"],
  permissions: ["Permissions", "權限管理"]
};

titles.services = ["Services", "服務"];

const money = new Intl.NumberFormat("zh-TW", {
  minimumFractionDigits: 0,
  maximumFractionDigits: 2
});

const labels = {
  systemRole: {
    admin: "管理員",
    staff: "工作人員",
    viewer: "檢視者"
  },
  orderStatus: {
    draft: "草稿",
    completed: "已完成",
    cancelled: "已取消",
    disputed: "爭議中"
  },
  customerPaymentStatus: {
    unpaid: "未收款",
    partial: "部分收款",
    paid: "已收款",
    refunded: "已退款"
  },
  orderType: {
    boosting: "代打",
    farming: "代肝",
    companion: "陪玩",
    prepaid: "預存"
  },
  memberRole: {
    player: "團員",
    leader: "帶團",
    trainer: "教學",
    bonus: "獎金"
  },
  paymentStatus: {
    pending: "待發薪",
    paid: "已發薪",
    cancelled: "已取消"
  },
  auditAction: {
    create: "新增",
    update: "修改",
    deactivate: "停用",
    activate: "啟用",
    leave: "離團",
    cancel: "取消",
    update_status: "修改狀態",
    update_customer_payment_status: "修改收款狀態",
    delete: "刪除",
    generate_monthly: "產生月結",
    mark_paid: "標記已發薪",
    change_password: "變更密碼",
    reverse: "沖正"
  },
  targetType: {
    users: "成員",
    orders: "訂單",
    payments: "發薪",
    login_users: "登入者",
    service_items: "服務項目",
    activities: "活動",
    gift_records: "送禮紀錄",
    departments: "部門",
    department_members: "部門成員",
    audit_logs: "操作紀錄",
    money_logs: "金流紀錄",
    role_permissions: "角色權限"
  },
  moneyLogType: {
    deposit: "儲值",
    deduction: "扣款",
    refund: "退款",
    gift_income: "禮物收入",
    monthly_settlement: "月結",
    manual_adjustment: "手動調帳"
  },
  moneyLogSource: {
    manual: "手動",
    payments: "月結",
    gift_records: "送禮紀錄",
    orders: "訂單"
  },
  loginHistoryAction: {
    login: "登入",
    logout: "登出"
  },
  loginHistoryMethod: {
    password: "帳號密碼",
    discord: "Discord",
    session: "Session"
  }
};

const permissionLabels = {
  "Member.View": "成員 / 查看",
  "Member.Create": "成員 / 新增",
  "Member.Edit": "成員 / 修改",
  "Member.Delete": "成員 / 刪除",
  "Gift.View": "送禮與禮物 / 查看",
  "Gift.Create": "送禮與禮物 / 新增",
  "Gift.Edit": "送禮與禮物 / 修改",
  "Gift.Delete": "送禮與禮物 / 刪除",
  "Order.View": "訂單 / 查看",
  "Order.Create": "訂單 / 新增",
  "Order.Edit": "訂單 / 修改",
  "Order.Cancel": "訂單 / 取消",
  "Settlement.View": "月結 / 查看",
  "Settlement.Close": "月結 / 關帳與重算",
  "Settlement.Export": "月結 / 匯出",
  "Account.Manage": "帳號與權限 / 管理",
  "Organization.Manage": "組織 / 管理",
  "Audit.View": "操作紀錄 / 查看"
};
permissionLabels["Profile.Manage"] = "個人化 / 管理";

document.addEventListener("DOMContentLoaded", async () => {
  bindSidebar();
  bindMobileChrome();
  setupPersonalizationUI();
  bindNavigation();
  bindForms();
  bindOrganizationEditor();
  bindPriceGallery();
  setDefaultDates();
  addMemberRow();
  updateLateNightAddonAvailability(document.getElementById("orderForm"));
  await initializeAuth();
});

function bindMobileChrome() {
  const navToggle = document.getElementById("mobileNavToggle");
  const navBackdrop = document.getElementById("mobileNavBackdrop");
  const moreToggle = document.getElementById("mobileMoreToggle");
  const accountActions = document.getElementById("accountActions");
  const mobileQuery = window.matchMedia("(max-width: 720px)");

  const setMobileNavOpen = (open) => {
    document.body.classList.toggle("mobile-nav-open", open);
    navToggle.setAttribute("aria-expanded", String(open));
    navToggle.setAttribute("aria-label", open ? "關閉主選單" : "開啟主選單");
    navBackdrop.hidden = !open;
  };

  const setAccountActionsOpen = (open) => {
    document.body.classList.toggle("mobile-account-open", open);
    moreToggle.setAttribute("aria-expanded", String(open));
  };

  navToggle.addEventListener("click", () => {
    setAccountActionsOpen(false);
    setMobileNavOpen(!document.body.classList.contains("mobile-nav-open"));
  });
  navBackdrop.addEventListener("click", () => setMobileNavOpen(false));
  moreToggle.addEventListener("click", (event) => {
    event.stopPropagation();
    setAccountActionsOpen(!document.body.classList.contains("mobile-account-open"));
  });
  accountActions.addEventListener("click", () => setAccountActionsOpen(false));

  document.querySelectorAll(".nav-tabs button").forEach((button) => {
    button.addEventListener("click", () => setMobileNavOpen(false));
  });

  document.addEventListener("click", (event) => {
    if (!event.target.closest(".top-actions")) {
      setAccountActionsOpen(false);
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      setMobileNavOpen(false);
      setAccountActionsOpen(false);
    }
  });

  mobileQuery.addEventListener("change", (event) => {
    if (!event.matches) {
      setMobileNavOpen(false);
      setAccountActionsOpen(false);
    }
  });
}

function setupPersonalizationUI() {
  const main = document.querySelector("main.main");
  if (main && !document.getElementById("settingsView")) {
    const section = document.createElement("section");
    section.className = "view";
    section.id = "settingsView";
    section.innerHTML = `
      <div class="grid two settings-layout theme-settings-layout">
        <form class="panel form" id="preferenceForm">
          <h2>個人化配色</h2>
          <label>配色版本
            <select name="themeName">
              <option value="internal-ops">Internal Ops｜營運工作台</option>
              <option value="purple-tech">Cyber Violet｜霓紫科技</option>
              <option value="blue-metal">Aurora Blue｜極光金屬</option>
              <option value="dopamine-candy">Dopamine Candy｜甜感多巴胺</option>
              <option value="mint-energy">Mint Energy｜薄荷能量</option>
              <option value="sunset-neon">Sunset Neon｜落日霓虹</option>
              <option value="light-clean">Light Clean｜清透白</option>
            </select>
          </label>
          <label>主色
            <input name="accentColor" type="color" value="#1f7668">
          </label>
          <div class="form-actions">
            <button class="primary" type="submit">儲存配色</button>
          </div>
        </form>
        <section class="panel settings-preview">
          <div class="panel-head"><h2>預覽</h2></div>
          <div class="theme-preview-card">
            <span>主色</span>
            <strong id="themePreviewName">營運工作台</strong>
            <button class="primary" type="button">主要按鈕</button>
            <button class="ghost" type="button">次要按鈕</button>
          </div>
        </section>
      </div>
    `;
    main.appendChild(section);
  }

  document.getElementById("preferenceForm")?.addEventListener("submit", submitPreferences);
  document.getElementById("preferenceForm")?.addEventListener("input", () => {
    const preference = readPreferenceForm();
    applyPreferences(preference);
    renderPreferencePreview(preference);
  });
  document.getElementById("preferenceForm")?.elements.themeName?.addEventListener("change", (event) => {
    const form = event.currentTarget.form;
    form.elements.accentColor.value = themePreset(event.currentTarget.value).accentColor;
    const preference = readPreferenceForm();
    applyPreferences(preference);
    renderPreferencePreview(preference);
  });
}

function readPreferenceForm() {
  const form = document.getElementById("preferenceForm");
  const data = new FormData(form);
  const current = normalizePreference(state.preferences);
  return {
    themeName: data.get("themeName") || "internal-ops",
    accentColor: data.get("accentColor") || null,
    dashboardLayout: current.dashboardLayout,
    tablePageSize: current.tablePageSize,
    defaultOrderStatusFilter: current.defaultOrderStatusFilter,
    defaultMoneyLogFilter: current.defaultMoneyLogFilter
  };
}

function renderPreferenceForm(preference) {
  const form = document.getElementById("preferenceForm");
  if (!form) {
    return;
  }

  const resolved = normalizePreference(preference);
  form.elements.themeName.value = resolved.themeName;
  form.elements.accentColor.value = resolved.accentColor || themePreset(resolved.themeName).accentColor;
  renderPreferencePreview(resolved);
}

function renderPreferencePreview(preference) {
  const name = document.getElementById("themePreviewName");
  if (!name) {
    return;
  }

  const labels = {
    "internal-ops": "Internal Ops｜營運工作台",
    "purple-tech": "Cyber Violet｜霓紫科技",
    "blue-metal": "Aurora Blue｜極光金屬",
    "dopamine-candy": "Dopamine Candy｜甜感多巴胺",
    "mint-energy": "Mint Energy｜薄荷能量",
    "sunset-neon": "Sunset Neon｜落日霓虹",
    "light-clean": "Light Clean｜清透白"
  };
  name.textContent = labels[preference.themeName] || "Internal Ops｜營運工作台";
}

async function submitPreferences(event) {
  event.preventDefault();
  await runAction(async () => {
    const preference = await api("/api/userpreferences/me", {
      method: "PUT",
      body: JSON.stringify(readPreferenceForm())
    });
    state.preferences = preference;
    state.auth.preferences = preference;
    applyPreferences(preference);
    renderPreferenceForm(preference);
    showAlert("個人化配色已儲存。", false);
  });
}

function applyPreferences(preference) {
  const resolved = normalizePreference(preference);
  const link = document.getElementById("themeStylesheet");
  if (link) {
    link.href = `${themePreset(resolved.themeName).href}?v=20260831-personalization-v2`;
  }
  document.body.dataset.theme = resolved.themeName;
  applyAccentColor(resolved.accentColor || themePreset(resolved.themeName).accentColor);
}

function normalizePreference(preference) {
  return {
    themeName: preference?.themeName || "internal-ops",
    accentColor: preference?.accentColor || null,
    dashboardLayout: preference?.dashboardLayout || null,
    tablePageSize: Number(preference?.tablePageSize || 100),
    defaultOrderStatusFilter: preference?.defaultOrderStatusFilter || null,
    defaultMoneyLogFilter: preference?.defaultMoneyLogFilter || null
  };
}

function themePreset(name) {
  const presets = {
    "internal-ops": {
      href: "/themes/internal-ops.css",
      accentColor: "#1f7668"
    },
    "purple-tech": {
      href: "/themes/purple-tech.css",
      accentColor: "#7c3aed"
    },
    "blue-metal": {
      href: "/themes/blue-metal.css",
      accentColor: "#2563eb"
    },
    "dopamine-candy": {
      href: "/themes/dopamine-candy.css",
      accentColor: "#ff4d9e"
    },
    "mint-energy": {
      href: "/themes/mint-energy.css",
      accentColor: "#10b981"
    },
    "sunset-neon": {
      href: "/themes/sunset-neon.css",
      accentColor: "#ff6b35"
    },
    "light-clean": {
      href: "/themes/light-clean.css",
      accentColor: "#64748b"
    }
  };
  return presets[name] || presets["internal-ops"];
}

function applyAccentColor(hexColor) {
  const color = normalizeHexColor(hexColor);
  if (!color) {
    return;
  }

  const rgb = hexToRgb(color);
  const dark = rgbToHex(scaleRgb(rgb, 0.72));
  const secondary = rgbToHex(mixRgb(rgb, { r: 20, g: 184, b: 166 }, 0.35));
  const gold = rgbToHex(mixRgb(rgb, { r: 185, g: 133, b: 44 }, 0.48));
  const root = document.documentElement;
  root.style.setProperty("--accent", color);
  root.style.setProperty("--accent-dark", dark);
  root.style.setProperty("--accent-2", secondary);
  root.style.setProperty("--accent-soft", `rgb(${rgb.r} ${rgb.g} ${rgb.b} / 12%)`);
  root.style.setProperty("--gold", gold);
  root.style.setProperty("--gold-soft", `rgb(${hexToRgb(gold).r} ${hexToRgb(gold).g} ${hexToRgb(gold).b} / 14%)`);
  root.style.setProperty("--primary-gradient", `linear-gradient(135deg, ${color} 0%, ${secondary} 100%)`);
}

function normalizeHexColor(value) {
  const text = String(value || "").trim();
  return /^#[0-9a-fA-F]{6}$/.test(text) ? text : null;
}

function hexToRgb(hex) {
  const value = hex.replace("#", "");
  return {
    r: parseInt(value.slice(0, 2), 16),
    g: parseInt(value.slice(2, 4), 16),
    b: parseInt(value.slice(4, 6), 16)
  };
}

function scaleRgb(rgb, factor) {
  return {
    r: Math.round(rgb.r * factor),
    g: Math.round(rgb.g * factor),
    b: Math.round(rgb.b * factor)
  };
}

function mixRgb(a, b, weight) {
  return {
    r: Math.round(a.r * (1 - weight) + b.r * weight),
    g: Math.round(a.g * (1 - weight) + b.g * weight),
    b: Math.round(a.b * (1 - weight) + b.b * weight)
  };
}

function rgbToHex(rgb) {
  return `#${[rgb.r, rgb.g, rgb.b]
    .map((value) => Math.max(0, Math.min(255, value)).toString(16).padStart(2, "0"))
    .join("")}`;
}

function bindSidebar() {
  const button = document.getElementById("sidebarToggle");
  if (!button) {
    return;
  }

  const saved = localStorage.getItem("sidebarCollapsed") === "true";
  setSidebarCollapsed(saved);

  button.addEventListener("click", () => {
    setSidebarCollapsed(!document.body.classList.contains("sidebar-collapsed"));
  });
}

function setSidebarCollapsed(collapsed) {
  document.body.classList.toggle("sidebar-collapsed", collapsed);
  localStorage.setItem("sidebarCollapsed", String(collapsed));

  const button = document.getElementById("sidebarToggle");
  if (!button) {
    return;
  }

  button.textContent = collapsed ? "›" : "‹";
  button.setAttribute("aria-expanded", String(!collapsed));
  button.setAttribute("aria-label", collapsed ? "展開側欄" : "收合側欄");
}

function bindNavigation() {
  document.querySelectorAll(".nav-tabs button").forEach((button) => {
    button.addEventListener("click", () => navigateToView(button.dataset.view));
  });

  document.querySelectorAll("[data-log-view]").forEach((button) => {
    button.addEventListener("click", () => navigateToView(button.dataset.logView));
  });

  document.getElementById("refreshBtn").addEventListener("click", refreshAll);
  document.getElementById("personalizationBtn")?.addEventListener("click", () => navigateToView("settings"));
  document.getElementById("currentOrganizationSelect")?.addEventListener("change", switchCurrentOrganization);
  document.querySelectorAll("[data-view-jump]").forEach((button) => {
    button.addEventListener("click", () => navigateToView(button.dataset.viewJump));
  });
  document.getElementById("addMemberBtn").addEventListener("click", () => addMemberRow());
}

async function navigateToView(view) {
  state.view = view;
  document.querySelectorAll(".nav-tabs button").forEach((button) => {
    button.classList.toggle("active", button.dataset.view === view || (["moneyLogs", "loginHistory"].includes(view) && button.dataset.view === "audit"));
  });
  document.querySelectorAll(".view").forEach((section) => section.classList.remove("active"));
  document.getElementById(`${view}View`).classList.add("active");
  document.getElementById("viewEyebrow").textContent = titles[view][0];
  document.getElementById("viewTitle").textContent = titles[view][1];
  document.querySelectorAll("[data-log-view]").forEach((button) => {
    button.classList.toggle("active", button.dataset.logView === view);
  });
  await refreshAll();
}

function bindForms() {
  ensureLoginUserEditControls();
  document.getElementById("loginForm").addEventListener("submit", submitLogin);
  document.getElementById("discordLoginBtn").addEventListener("click", startDiscordLogin);
  document.getElementById("loginUserForm").addEventListener("submit", submitLoginUser);
  document.getElementById("cancelLoginUserEditBtn").addEventListener("click", resetLoginUserForm);
  document.getElementById("changePasswordBtn").addEventListener("click", openChangePasswordModal);
  document.getElementById("discordLinkBtn").addEventListener("click", toggleDiscordLink);
  document.getElementById("changePasswordClose").addEventListener("click", closeChangePasswordModal);
  document.getElementById("changePasswordCancel").addEventListener("click", closeChangePasswordModal);
  document.getElementById("changePasswordForm").addEventListener("submit", submitChangePassword);
  document.getElementById("logoutBtn").addEventListener("click", logout);
  document.getElementById("userForm").addEventListener("submit", submitUser);
  document.getElementById("cancelUserEditBtn").addEventListener("click", resetUserForm);
  document.getElementById("departmentForm").addEventListener("submit", submitDepartment);
  document.getElementById("cancelDepartmentEditBtn").addEventListener("click", resetDepartmentForm);
  document.getElementById("departmentMemberForm").addEventListener("submit", submitDepartmentMember);
  document.getElementById("cancelDepartmentMemberEditBtn").addEventListener("click", resetDepartmentMemberForm);
  document.getElementById("activityForm").addEventListener("submit", submitActivity);
  document.getElementById("cancelActivityEditBtn").addEventListener("click", resetActivityForm);
  document.getElementById("orderForm").addEventListener("submit", submitOrder);
  document.getElementById("orderAttachmentInput").addEventListener("change", handleOrderAttachmentChange);
  document.getElementById("copyOrderBtn").addEventListener("click", copyOrderAsNew);
  document.getElementById("cancelOrderEditBtn").addEventListener("click", resetOrderForm);
  document.getElementById("giftRecordForm").addEventListener("submit", submitGiftRecord);
  document.getElementById("giftRecordAttachmentInput").addEventListener("change", handleGiftRecordAttachmentChange);
  document.getElementById("cancelGiftRecordEditBtn").addEventListener("click", resetGiftRecordForm);
  bindGiftPicker();
  document.getElementById("paymentForm").addEventListener("submit", submitPaymentGeneration);
  document.getElementById("savePermissionsBtn").addEventListener("click", savePermissions);
  document.getElementById("organizationManagementForm").addEventListener("submit", submitOrganization);
  document.getElementById("cancelOrganizationManagementBtn").addEventListener("click", resetOrganizationManagementForm);
  document.getElementById("orderForm").addEventListener("input", handleOrderInput);
  document.getElementById("orderForm").addEventListener("change", handleOrderInput);
  bindMemberPicker();
  bindRecordModal();
  bindAttachmentModal();
  setupLogExperience();
}

function bindOrganizationEditor() {
  bindDepartmentModal();

  document.querySelectorAll("[data-org-tab]").forEach((button) => {
    button.addEventListener("click", () => activateOrganizationTab(button.dataset.orgTab));
  });
}

function bindDepartmentModal() {
  const modal = document.getElementById("departmentModal");
  const closeButton = document.getElementById("departmentModalClose");
  if (!modal || !closeButton) {
    return;
  }

  closeButton.addEventListener("click", closeDepartmentModal);
  modal.addEventListener("click", (event) => {
    if (event.target === modal) {
      closeDepartmentModal();
    }
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && !modal.hidden) {
      closeDepartmentModal();
    }
  });
}

function bindMemberPicker() {
  const modal = document.getElementById("memberPickerModal");
  const search = document.getElementById("memberPickerSearch");

  document.addEventListener("click", (event) => {
    const trigger = event.target.closest("[data-member-picker-trigger]");
    if (trigger) {
      openMemberPicker(trigger.closest("[data-member-picker]"));
    }
  });

  document.getElementById("memberPickerClose").addEventListener("click", closeMemberPicker);
  document.getElementById("memberPickerCancel").addEventListener("click", closeMemberPicker);
  document.getElementById("memberPickerClear").addEventListener("click", () => {
    if (state.activeMemberPicker) {
      setMemberPickerValue(state.activeMemberPicker, "");
    }
    closeMemberPicker();
  });
  search.addEventListener("input", renderMemberPickerOptions);
  modal.addEventListener("click", (event) => {
    if (event.target === modal) {
      closeMemberPicker();
    }
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && !modal.hidden) {
      closeMemberPicker();
    }
  });
}

function bindGiftPicker() {
  const modal = document.getElementById("giftPickerModal");
  document.getElementById("giftPickerTrigger").addEventListener("click", openGiftPicker);
  document.getElementById("giftPickerClose").addEventListener("click", closeGiftPicker);
  document.getElementById("giftPickerCancel").addEventListener("click", closeGiftPicker);
  document.getElementById("giftPickerCustom").addEventListener("click", () => {
    setGiftPickerValue("", { keepCustomFields: true });
    closeGiftPicker();
  });
  document.getElementById("giftPickerSearch").addEventListener("input", renderGiftPickerOptions);
  modal.addEventListener("click", (event) => {
    if (event.target === modal) {
      closeGiftPicker();
    }
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && !modal.hidden) {
      closeGiftPicker();
    }
  });
}

function openGiftPicker() {
  const search = document.getElementById("giftPickerSearch");
  search.value = "";
  document.getElementById("giftPickerModal").hidden = false;
  renderGiftPickerOptions();
  search.focus();
}

function closeGiftPicker() {
  document.getElementById("giftPickerModal").hidden = true;
}

function renderGiftPickerOptions() {
  const list = document.getElementById("giftPickerList");
  const query = document.getElementById("giftPickerSearch").value.trim().toLowerCase();
  const selectedId = Number(document.getElementById("giftItemSelect").value || 0);
  const items = state.serviceItems.filter((item) =>
    item.category === "gift" &&
    item.isActive &&
    (!query || [item.name, item.remark].filter(Boolean).join(" ").toLowerCase().includes(query)));

  const customOption = `
      <button class="gift-picker-option ${selectedId ? "" : "selected"}" type="button" data-gift-picker-custom>
        <span>
          <strong>自訂禮物</strong>
          <small>自訂名稱與金額</small>
        </span>
        <span>自填</span>
      </button>
    `;

  list.innerHTML = customOption || items.length
    ? `${customOption}${items.map((item) => `
      <button class="gift-picker-option ${item.id === selectedId ? "selected" : ""}" type="button" data-gift-picker-value="${item.id}">
        <span>
          <strong>${escapeHtml(item.name)}</strong>
          <small>${escapeHtml(item.remark || "尚未填寫備註")}</small>
        </span>
        <span>${escapeHtml(servicePriceText(item))}</span>
      </button>
    `).join("")}`
    : `<p class="member-picker-empty">找不到符合條件的禮物。</p>`;

  list.querySelectorAll("[data-gift-picker-custom]").forEach((button) => {
    button.addEventListener("click", () => {
      setGiftPickerValue("", { keepCustomFields: true });
      closeGiftPicker();
    });
  });

  list.querySelectorAll("[data-gift-picker-value]").forEach((button) => {
    button.addEventListener("click", () => {
      setGiftPickerValue(button.dataset.giftPickerValue);
      closeGiftPicker();
    });
  });
}

function setGiftPickerValue(value, options = {}) {
  const form = document.getElementById("giftRecordForm");
  const input = document.getElementById("giftItemSelect");
  const trigger = document.getElementById("giftPickerTrigger");
  const giftNameHint = document.getElementById("giftNameHint");
  input.value = value == null ? "" : String(value);
  const item = state.serviceItems.find((serviceItem) => serviceItem.id === Number(input.value));

  trigger.textContent = item?.name || "自訂禮物";
  trigger.classList.toggle("has-value", Boolean(item));
  form.elements.giftName.disabled = Boolean(item);
  giftNameHint.textContent = item
    ? "已選擇固定禮物，名稱不可修改。"
    : "只有選擇「自訂禮物」時可以填寫自訂名稱。";
  if (item) {
    form.elements.giftName.value = "";
    form.elements.amount.value = item.defaultPrice ?? "";
    form.elements.remark.value = item.remark || "";
  } else if (!options.keepCustomFields) {
    form.elements.giftName.value = "";
    form.elements.amount.value = "";
    form.elements.remark.value = "";
  }
}

function bindRecordModal() {
  const modal = document.getElementById("recordModal");
  document.getElementById("recordModalClose").addEventListener("click", closeRecordModal);
  modal.addEventListener("click", (event) => {
    if (event.target === modal) {
      closeRecordModal();
    }
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && !modal.hidden) {
      closeRecordModal();
    }
  });
}

function bindAttachmentModal() {
  const modal = document.getElementById("attachmentModal");
  document.getElementById("attachmentModalClose").addEventListener("click", closeAttachmentModal);
  modal.addEventListener("click", (event) => {
    if (event.target === modal) {
      closeAttachmentModal();
    }
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && !modal.hidden) {
      closeAttachmentModal();
    }
  });
}

function setupLogExperience() {
  const configs = [
    {
      viewId: "auditView",
      kind: "audit",
      title: "📋 操作紀錄",
      subtitle: "所有系統操作皆可追溯",
      accent: "purple",
      tableHeaders: ["時間", "操作者", "功能", "動作", "目標", "摘要", "IP 位址", "詳情"],
      typeLabel: "功能"
    },
    {
      viewId: "moneyLogsView",
      kind: "money",
      title: "💰 金流紀錄",
      subtitle: "所有資金異動紀錄",
      accent: "gold",
      tableHeaders: ["時間", "會員", "類型", "金額", "餘額", "來源", "備註", "詳情"],
      typeLabel: "類型"
    },
    {
      viewId: "loginHistoryView",
      kind: "login",
      title: "🔐 登入紀錄",
      subtitle: "帳號登入與安全紀錄",
      accent: "blue",
      tableHeaders: ["時間", "帳號", "動作", "登入方式", "IP 位址", "裝置", "地區", "詳情"],
      typeLabel: "登入方式"
    }
  ];

  configs.forEach((config) => {
    const view = document.getElementById(config.viewId);
    if (!view || view.dataset.logEnhanced === "true") {
      return;
    }

    view.dataset.logEnhanced = "true";
    view.dataset.logKind = config.kind;
    view.classList.add("log-page", `log-page-${config.accent}`);

    const switcher = view.querySelector(".log-switcher");
    const hero = document.createElement("header");
    hero.className = "log-hero";
    hero.innerHTML = `
      <div>
        <h2>${config.title}</h2>
        <p>${config.subtitle}</p>
      </div>
    `;
    switcher.after(hero);

    const kpis = document.createElement("section");
    kpis.className = "log-kpis";
    kpis.id = `${config.kind}Kpis`;
    hero.after(kpis);

    const filters = document.createElement("section");
    filters.className = "log-filters";
    filters.dataset.logFilter = config.kind;
    filters.innerHTML = `
      <label>日期<input type="date" data-filter-field="date"></label>
      <label>帳號<input type="search" data-filter-field="account" placeholder="帳號或操作者"></label>
      <label>關鍵字<input type="search" data-filter-field="keyword" placeholder="搜尋摘要、IP、備註"></label>
      <label>${config.typeLabel}<select data-filter-field="type"><option value="">全部</option></select></label>
      <label>排序<select data-filter-field="sort"><option value="desc">最新優先</option><option value="asc">最舊優先</option></select></label>
      <button class="primary" type="button" data-log-search>搜尋</button>
    `;
    kpis.after(filters);

    const panel = view.querySelector(".panel");
    panel?.classList.add("log-table-panel");
    const table = view.querySelector("table");
    table?.classList.add("log-table");
    const headerRow = view.querySelector("thead tr");
    if (headerRow) {
      headerRow.innerHTML = config.tableHeaders.map((text) => `<th>${text}</th>`).join("");
    }
  });

  document.addEventListener("input", handleLogFilterEvent);
  document.addEventListener("change", handleLogFilterEvent);
  document.addEventListener("click", (event) => {
    if (event.target.closest("[data-log-search]")) {
      rerenderCurrentLogView();
    }
    if (event.target.closest("[data-log-drawer-close]") || event.target.classList.contains("log-drawer-backdrop")) {
      closeLogDrawer();
    }
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      closeLogDrawer();
    }
  });

  ensureLogDrawer();
}

function handleLogFilterEvent(event) {
  const field = event.target.closest("[data-filter-field]");
  if (!field) {
    return;
  }

  const filter = field.closest("[data-log-filter]");
  const kind = filter?.dataset.logFilter;
  if (!kind || !state.logFilters[kind]) {
    return;
  }

  state.logFilters[kind][field.dataset.filterField] = field.value;
  rerenderCurrentLogView();
}

function rerenderCurrentLogView() {
  if (state.view === "audit") {
    renderAuditLogs(state.auditLogs);
  } else if (state.view === "moneyLogs") {
    renderMoneyLogs(state.moneyLogs);
  } else if (state.view === "loginHistory") {
    renderLoginHistories(state.loginHistories);
  }
}

function ensureLogDrawer() {
  if (document.getElementById("logDrawer")) {
    return;
  }

  const drawer = document.createElement("aside");
  drawer.id = "logDrawer";
  drawer.className = "log-drawer-backdrop";
  drawer.hidden = true;
  drawer.innerHTML = `
    <section class="log-drawer" role="dialog" aria-modal="true" aria-labelledby="logDrawerTitle">
      <div class="log-drawer-head">
        <div>
          <p class="eyebrow" id="logDrawerEyebrow">Details</p>
          <h2 id="logDrawerTitle">詳情</h2>
        </div>
        <button class="ghost small" type="button" data-log-drawer-close>關閉</button>
      </div>
      <div id="logDrawerBody"></div>
    </section>
  `;
  document.body.appendChild(drawer);
}

function openLogDrawer({ title, eyebrow, content }) {
  ensureLogDrawer();
  document.getElementById("logDrawerTitle").textContent = title;
  document.getElementById("logDrawerEyebrow").textContent = eyebrow;
  document.getElementById("logDrawerBody").innerHTML = content;
  document.getElementById("logDrawer").hidden = false;
}

function closeLogDrawer() {
  const drawer = document.getElementById("logDrawer");
  if (drawer) {
    drawer.hidden = true;
  }
}

function openMemberPicker(field) {
  if (!field || field.querySelector("[data-member-picker-trigger]")?.disabled) {
    return;
  }

  state.activeMemberPicker = field;
  document.getElementById("memberPickerTitle").textContent = field.dataset.title || "選擇成員";
  const search = document.getElementById("memberPickerSearch");
  search.value = "";
  search.placeholder = field.dataset.searchPlaceholder || "搜尋暱稱、Discord 名稱或 ID";
  document.getElementById("memberPickerClear").hidden = field.dataset.required === "true";
  const modal = document.getElementById("memberPickerModal");
  document.body.appendChild(modal);
  modal.style.zIndex = "220";
  modal.hidden = false;
  renderMemberPickerOptions();
  search.focus();
}

function closeMemberPicker() {
  const modal = document.getElementById("memberPickerModal");
  modal.hidden = true;
  modal.style.zIndex = "";
  state.activeMemberPicker = null;
}

function memberPickerUsers(field) {
  const source = field?.dataset.source;
  if (source === "bosses") {
    return state.bosses;
  }
  if (source === "players") {
    return state.players;
  }
  if (source === "active-users") {
    return state.users.filter((user) => user.isActive);
  }
  if (source === "login-user-members") {
    const owningForm = field.closest("form");
    const organizationSelectId = field.dataset.organizationSelectId || "";
    const organizationId = Number(owningForm?.elements.organizationId?.value) ||
      Number(organizationSelectId ? document.getElementById(organizationSelectId)?.value : 0) ||
      Number(document.getElementById("loginUserOrganizationSelect")?.value) ||
      state.auth?.user?.organizationId;
    const selectedId = Number(field.querySelector("input[type='hidden']")?.value || 0);
    const currentLoginUserId = Number(owningForm?.elements.loginUserId?.value) ||
      Number(field.dataset.loginUserId || 0);
    const boundUserIds = new Set(state.loginUsers
      .filter((loginUser) =>
        loginUser.userId &&
        (!currentLoginUserId || loginUser.id !== currentLoginUserId))
      .map((loginUser) => loginUser.userId));
    return state.users.filter((user) =>
      user.id === selectedId ||
      (user.isActive &&
        !boundUserIds.has(user.id) &&
        (!organizationId || user.organizationId === organizationId)));
  }
  return state.users.filter((user) => user.isActive);
}

function renderMemberPickerOptions() {
  const field = state.activeMemberPicker;
  const list = document.getElementById("memberPickerList");
  if (!field || !list) {
    return;
  }

  const query = document.getElementById("memberPickerSearch").value.trim().toLowerCase();
  const selectedId = Number(field.querySelector("input[type='hidden']")?.value || 0);
  const searchFields = (field.dataset.searchFields || "nickname,discordName,discordId")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean);
  const displayMode = field.dataset.displayMode || "nickname-discord-id";
  const users = memberPickerUsers(field).filter((user) => {
    const searchable = searchFields
      .map((fieldName) => user[fieldName])
      .filter((value) => value != null)
      .join(" ")
      .toLowerCase();
    return !query || searchable.includes(query);
  });

  list.innerHTML = users.length
    ? users.map((user) => `
      <button class="member-picker-option ${user.id === selectedId ? "selected" : ""}" type="button" data-member-picker-value="${user.id}">
        <span>
          <strong>${escapeHtml(user.nickname)}</strong>
          <small>${displayMode === "nickname-discord-id"
            ? `Discord：${escapeHtml(user.discordName || "未設定")} · ID：${escapeHtml(user.discordId || "未設定")}`
            : escapeHtml(user.discordName || user.discordId || "未設定")}</small>
        </span>
        <span class="member-picker-badges">
          ${user.isBoss ? `<em>老闆</em>` : ""}
          ${user.isPlayer ? `<em>團員</em>` : ""}
        </span>
      </button>
    `).join("")
    : `<p class="member-picker-empty">找不到符合條件的成員。</p>`;

  list.querySelectorAll("[data-member-picker-value]").forEach((button) => {
    button.addEventListener("click", () => {
      setMemberPickerValue(field, button.dataset.memberPickerValue);
      closeMemberPicker();
    });
  });
}

function setMemberPickerValue(fieldOrInput, value) {
  const field = fieldOrInput?.matches?.("[data-member-picker]")
    ? fieldOrInput
    : fieldOrInput?.closest?.("[data-member-picker]");
  const input = field?.querySelector("input[type='hidden']");
  if (!field || !input) {
    return;
  }

  input.value = value == null ? "" : String(value);
  refreshMemberPickerField(field);
  input.dispatchEvent(new Event("change", { bubbles: true }));
  if (input.matches("[data-member-select]")) {
    updateOrderAmountFromService();
    updateOrderCalc();
  }
}

function refreshMemberPickerField(field) {
  if (!field) {
    return;
  }
  const input = field.querySelector("input[type='hidden']");
  const trigger = field.querySelector("[data-member-picker-trigger]");
  const selectedId = Number(input?.value || 0);
  const selected = state.users.find((user) => user.id === selectedId);
  trigger.textContent = selected?.nickname || field.dataset.emptyLabel || "請選擇成員";
  trigger.classList.toggle("has-value", Boolean(selected));
}

function refreshMemberPickerFields(root = document) {
  root.querySelectorAll("[data-member-picker]").forEach(refreshMemberPickerField);
}

function validateRequiredMemberPickers(form) {
  const missing = [...form.querySelectorAll('[data-member-picker][data-required="true"]')]
    .find((field) => !field.querySelector("input[type='hidden']")?.value);
  if (!missing) {
    return true;
  }

  showAlert(`請先${missing.dataset.title || "選擇成員"}。`);
  missing.querySelector("[data-member-picker-trigger]")?.focus();
  return false;
}

function memberPickerLabel(inputId, fallback = "未指定") {
  const id = Number(document.getElementById(inputId)?.value || 0);
  return state.users.find((user) => user.id === id)?.nickname || fallback;
}

function closeRecordModal() {
  document.getElementById("recordModal").hidden = true;
}

function openRecordModal({ title, eyebrow, content }) {
  document.getElementById("recordModalTitle").textContent = title;
  document.getElementById("recordModalEyebrow").textContent = eyebrow;
  document.getElementById("recordModalBody").innerHTML = content;
  document.getElementById("recordModal").hidden = false;
}

function closeAttachmentModal() {
  document.getElementById("attachmentModal").hidden = true;
}

async function openAttachmentModal(title, targetType, targetId, options = {}) {
  document.getElementById("attachmentModalTitle").textContent = title;
  document.getElementById("attachmentModalEyebrow").textContent = label("targetType", targetType) || targetType;
  document.getElementById("attachmentModal").hidden = false;
  await renderAttachmentModalBody(targetType, targetId, options);
}

async function renderAttachmentModalBody(targetType, targetId, options = {}) {
  const body = document.getElementById("attachmentModalBody");
  const canEdit = options.canEdit ?? canEditAttachmentTarget(targetType);
  body.innerHTML = `<div class="attachment-body"><p class="muted">附件載入中...</p></div>`;

  let rows = [];
  try {
    rows = await api(`/api/fileattachments?targetType=${encodeURIComponent(targetType)}&targetId=${encodeURIComponent(targetId)}`);
  } catch (error) {
    body.innerHTML = `<div class="attachment-body"><p class="muted">${escapeHtml(error.message)}</p></div>`;
    return;
  }

  const uploadId = `attachmentUpload_${targetType}_${targetId}`;
  body.innerHTML = `
    <div class="attachment-body">
      <div class="attachment-list">
        ${rows.length ? rows.map((file) => `
          <div class="attachment-row">
            <div>
              <strong>${escapeHtml(file.originalFileName)}</strong>
              <small>${escapeHtml(file.attachmentKind || file.fileExtension || file.contentType)} · ${formatFileSize(file.fileSize)} · ${formatDateTime(file.createdAt)}</small>
            </div>
            <div class="table-actions">
              <a class="ghost small attachment-link" href="/api/fileattachments/${file.id}/preview" target="_blank" rel="noopener">預覽</a>
              <a class="ghost small attachment-link" href="/api/fileattachments/${file.id}/download" target="_blank" rel="noopener">下載</a>
              ${canEdit ? `<button class="ghost small danger-action" type="button" data-attachment-delete="${file.id}">刪除</button>` : ""}
            </div>
          </div>
        `).join("") : `<p class="muted">尚未上傳附件。</p>`}
      </div>
      ${canEdit ? `
        <label class="ghost attachment-trigger attachment-modal-upload" for="${uploadId}">
          <input id="${uploadId}" type="file" multiple accept=".jpg,.jpeg,.png,.webp,.gif,.pdf,.docx,.xlsx,.pptx,.csv,.txt,.log,.mp4,.mov">
          <span data-attachment-label>${escapeHtml(options.uploadLabel || "新增附件")}</span>
        </label>
      ` : ""}
    </div>
  `;

  body.querySelector(`#${uploadId}`)?.addEventListener("change", async (event) => {
    const files = [...(event.currentTarget.files || [])];
    if (files.length === 0) {
      return;
    }

    await runAction(async () => {
      for (const file of files) {
        const formData = new FormData();
        formData.append("targetType", targetType);
        formData.append("targetId", String(targetId));
        formData.append("attachmentKind", options.attachmentKind || "general");
        formData.append("note", options.note || "");
        formData.append("file", file);
        await api("/api/fileattachments", { method: "POST", body: formData });
      }
      await renderAttachmentModalBody(targetType, targetId, options);
    });
  });

  body.querySelectorAll("[data-attachment-delete]").forEach((button) => {
    button.addEventListener("click", async () => {
      if (!window.confirm("確定刪除此附件？")) {
        return;
      }

      await runAction(async () => {
        await api(`/api/fileattachments/${button.dataset.attachmentDelete}`, { method: "DELETE" });
        await renderAttachmentModalBody(targetType, targetId, options);
      });
    });
  });
}

function canEditAttachmentTarget(targetType) {
  return {
    orders: hasPermission("Order.Edit"),
    gift_records: hasPermission("Gift.Edit"),
    payments: hasPermission("Settlement.Close"),
    money_logs: hasAnyPermission(["Settlement.Close", "Account.Manage"]),
    audit_logs: hasAnyPermission(["Account.Manage", "Settlement.Close"])
  }[targetType] ?? false;
}

function formatFileSize(bytes) {
  const value = Number(bytes || 0);
  if (value >= 1024 * 1024) {
    return `${(value / 1024 / 1024).toFixed(1)} MB`;
  }
  if (value >= 1024) {
    return `${(value / 1024).toFixed(1)} KB`;
  }
  return `${value} B`;
}

function recordDetail(labelText, value) {
  return `
    <div class="record-detail">
      <span>${escapeHtml(labelText)}</span>
      <strong>${escapeHtml(value == null || value === "" ? "-" : String(value))}</strong>
    </div>
  `;
}

function organizationDisplayName(organization) {
  if (!organization) {
    return "";
  }

  const hasDuplicateName = state.organizations
    .some((item) => item.id !== organization.id && item.name === organization.name);
  return hasDuplicateName ? `${organization.name} #${organization.id}` : organization.name;
}

function openUserRecordModal(user) {
  const organization = state.organizations.find((item) => item.id === user.organizationId);
  openRecordModal({
    title: user.nickname,
    eyebrow: user.isBoss && user.isPlayer ? "團員 / 老闆" : user.isBoss ? "老闆資料" : "團員資料",
    content: `
      <div class="record-modal-content">
        <div class="record-detail-grid">
          ${recordDetail("Discord 名稱", user.discordName)}
          ${recordDetail("Discord ID", user.discordId)}
          ${recordDetail("所屬組織", organizationDisplayName(organization))}
          ${recordDetail("銀行帳號", user.bankAccount)}
          ${recordDetail("身分類型", [user.isPlayer ? "團員" : "", user.isBoss ? "老闆" : ""].filter(Boolean).join(" / "))}
          ${recordDetail("狀態", user.isActive ? "啟用" : "停用")}
        </div>
        <div class="form-actions">
          ${hasPermission("Member.Edit")
            ? `<button class="primary" id="recordModalEditUser" type="button">編輯資料</button>`
            : ""}
        </div>
      </div>
    `
  });

  document.getElementById("recordModalEditUser")?.addEventListener("click", () => {
    renderUserRecordEdit(user);
  });
}

function renderUserRecordEdit(user) {
  const organizationOptions = state.organizations
    .filter((organization) => organization.isActive || organization.id === user.organizationId)
    .map((organization) =>
      `<option value="${organization.id}" ${organization.id === user.organizationId ? "selected" : ""}>${escapeHtml(organizationDisplayName(organization))}</option>`
    ).join("");
  const canSelectOrganization = state.auth?.user?.systemRole === "admin" || isBootstrapMode();
  document.getElementById("recordModalEyebrow").textContent = "Edit Member";
  document.getElementById("recordModalBody").innerHTML = `
    <form class="form record-edit-form" id="recordUserForm">
      <label>暱稱<input name="nickname" required maxlength="50" value="${escapeHtml(user.nickname || "")}"></label>
      <label ${canSelectOrganization ? "" : "hidden"}>所屬組織
        <select name="organizationId">${organizationOptions}</select>
      </label>
      <label>Discord ID<input value="${escapeHtml(user.discordId || "尚未綁定")}" disabled></label>
      <label>Discord 名稱<input value="${escapeHtml(user.discordName || "尚未綁定")}" disabled></label>
      <label>銀行帳號<input name="bankAccount" maxlength="200" value="${escapeHtml(user.bankAccount || "")}"></label>
      <div class="check-grid">
        <label><input type="checkbox" name="isPlayer" ${user.isPlayer ? "checked" : ""}> 團員</label>
        <label><input type="checkbox" name="isBoss" ${user.isBoss ? "checked" : ""}> 老闆</label>
      </div>
      <div class="form-actions">
        <button class="primary" type="submit">儲存</button>
        <button class="ghost" id="recordUserBack" type="button">返回資料</button>
      </div>
    </form>
  `;

  document.getElementById("recordUserBack").addEventListener("click", () => openUserRecordModal(user));
  document.getElementById("recordUserForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    await runAction(async () => {
      await api(`/api/users/${user.id}`, {
        method: "PUT",
        body: JSON.stringify({
          nickname: data.get("nickname"),
          organizationId: Number(data.get("organizationId")) || user.organizationId,
          bankAccount: emptyToNull(data.get("bankAccount")),
          isPlayer: data.get("isPlayer") === "on",
          isBoss: data.get("isBoss") === "on",
          isActive: user.isActive,
          leftAt: user.leftAt ?? null
        })
      });
      await loadUsers();
      const updated = state.users.find((item) => item.id === user.id);
      if (updated) {
        openUserRecordModal(updated);
      } else {
        closeRecordModal();
      }
      showAlert("成員資料已更新。", false);
    });
  });
}

function openLoginUserRecordModal(loginUser) {
  const organization = state.organizations.find((item) => item.id === loginUser.organizationId);
  const member = state.users.find((item) => item.id === loginUser.userId);
  openRecordModal({
    title: loginUser.displayName,
    eyebrow: "帳號資料",
    content: `
      <div class="record-modal-content">
        <div class="record-detail-grid">
          ${recordDetail("登入帳號", loginUser.loginAccount)}
          ${recordDetail("顯示名稱", loginUser.displayName)}
          ${recordDetail("所屬組織", organizationDisplayName(organization))}
          ${recordDetail("綁定成員", member?.nickname || "不綁定")}
          ${recordDetail("系統權限", label("systemRole", loginUser.systemRole))}
          ${recordDetail("狀態", loginUser.isActive ? "啟用" : "停用")}
        </div>
        <div class="form-actions">
          <button class="primary" id="recordModalEditLoginUser" type="button">編輯帳號</button>
        </div>
      </div>
    `
  });

  document.getElementById("recordModalEditLoginUser").addEventListener("click", () => {
    renderLoginUserRecordEdit(loginUser);
  });
}

function renderLoginUserRecordEdit(loginUser) {
  const organizationOptions = state.organizations
    .filter((organization) => organization.isActive || organization.id === loginUser.organizationId)
    .map((organization) =>
      `<option value="${organization.id}" ${organization.id === loginUser.organizationId ? "selected" : ""}>${escapeHtml(organizationDisplayName(organization))}</option>`
    ).join("");
  const boundMember = state.users.find((user) => user.id === loginUser.userId);
  const boundMemberLabel = boundMember?.nickname || "不綁定";
  document.getElementById("recordModalEyebrow").textContent = "Edit Account";
  document.getElementById("recordModalBody").innerHTML = `
    <form class="form record-edit-form" id="recordLoginUserForm">
      <label>顯示名稱<input name="displayName" required maxlength="50" value="${escapeHtml(loginUser.displayName || "")}"></label>
      <label>登入帳號<input name="loginAccount" required maxlength="50" value="${escapeHtml(loginUser.loginAccount || "")}"></label>
      <label>所屬組織
        <select name="organizationId" id="recordLoginOrganizationSelect">${organizationOptions}</select>
      </label>
      <label>綁定成員
        <span class="member-picker-field" data-member-picker data-source="login-user-members"
          data-organization-select-id="recordLoginOrganizationSelect"
          data-login-user-id="${loginUser.id}"
          data-title="選擇綁定成員" data-empty-label="不綁定">
          <input name="userId" type="hidden" value="${loginUser.userId || ""}">
          <button class="member-picker-trigger ${boundMember ? "has-value" : ""}" type="button" data-member-picker-trigger>${escapeHtml(boundMemberLabel)}</button>
        </span>
      </label>
      <label>系統權限
        <select name="systemRole">
          ${["admin", "staff", "viewer"].map((role) =>
            `<option value="${role}" ${loginUser.systemRole === role ? "selected" : ""}>${label("systemRole", role)}</option>`
          ).join("")}
        </select>
      </label>
      <p class="muted">管理員不可在此修改密碼，密碼只能由登入者本人變更。</p>
      <div class="form-actions">
        <button class="primary" type="submit">儲存</button>
        <button class="ghost" id="recordLoginUserBack" type="button">返回資料</button>
      </div>
    </form>
  `;

  const form = document.getElementById("recordLoginUserForm");
  refreshMemberPickerFields(form);
  form.elements.organizationId.addEventListener("change", () => {
    setMemberPickerValue(form.elements.userId, "");
  });
  document.getElementById("recordLoginUserBack").addEventListener("click", () => openLoginUserRecordModal(loginUser));
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const data = new FormData(form);
    await runAction(async () => {
      await api(`/api/loginusers/${loginUser.id}`, {
        method: "PUT",
        body: JSON.stringify({
          displayName: data.get("displayName"),
          loginAccount: data.get("loginAccount"),
          organizationId: Number(data.get("organizationId")) || loginUser.organizationId,
          userId: Number(data.get("userId")) || null,
          systemRole: data.get("systemRole"),
          isActive: loginUser.isActive
        })
      });
      await loadLoginUsers();
      const updated = state.loginUsers.find((item) => item.id === loginUser.id);
      if (updated) {
        openLoginUserRecordModal(updated);
      } else {
        closeRecordModal();
      }
      showAlert("帳號資料已更新。", false);
    });
  });
}

function activateOrganizationTab(tab) {
  document.querySelectorAll("[data-org-tab]").forEach((button) => {
    button.classList.toggle("active", button.dataset.orgTab === tab);
  });
  document.querySelectorAll("[data-org-panel]").forEach((panel) => {
    panel.classList.toggle("active", panel.dataset.orgPanel === tab);
  });
}

function bindPriceGallery() {
  document.querySelectorAll(".price-gallery").forEach((gallery) => {
    const links = [...gallery.querySelectorAll("[data-price-preview]")];
    if (links.length === 0) {
      return;
    }

    const board = document.createElement("section");
    board.className = "price-board";

    const display = document.createElement("section");
    display.className = "price-display";
    display.innerHTML = `
      <button class="price-nav price-nav-prev" type="button" aria-label="上一張價目表">‹</button>
      <img alt="">
      <button class="price-nav price-nav-next" type="button" aria-label="下一張價目表">›</button>
      <p class="price-edge-hint" aria-live="polite"></p>
    `;

    gallery.parentNode?.insertBefore(board, gallery);
    board.append(gallery, display);

    const displayImg = display.querySelector("img");
    const edgeHint = display.querySelector(".price-edge-hint");
    let activeIndex = 0;
    let hintTimer;
    let touchStartX = null;

    const showEdgeHint = (message) => {
      clearTimeout(hintTimer);
      edgeHint.textContent = message;
      edgeHint.classList.add("show");
      hintTimer = setTimeout(() => edgeHint.classList.remove("show"), 1800);
    };

    const setActive = (index, direction = 0) => {
      const link = links[index];
      activeIndex = index;
      links.forEach((item) => item.classList.toggle("active", item === link));
      const image = link.querySelector("img");
      displayImg.src = link.getAttribute("href");
      displayImg.alt = image?.alt || "";
      displayImg.classList.remove("slide-from-left", "slide-from-right");
      void displayImg.offsetWidth;
      if (direction !== 0) {
        displayImg.classList.add(direction > 0 ? "slide-from-right" : "slide-from-left");
      }
      link.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
    };

    const move = (direction) => {
      const nextIndex = activeIndex + direction;
      if (nextIndex < 0) {
        showEdgeHint("已經是第一張圖片");
        return;
      }
      if (nextIndex >= links.length) {
        showEdgeHint("已經是最後一張圖片");
        return;
      }
      setActive(nextIndex, direction);
    };

    links.forEach((link, index) => {
      link.addEventListener("click", (event) => {
        event.preventDefault();
        setActive(index, index > activeIndex ? 1 : -1);
      });
    });

    display.querySelector(".price-nav-prev").addEventListener("click", () => move(-1));
    display.querySelector(".price-nav-next").addEventListener("click", () => move(1));
    display.addEventListener("keydown", (event) => {
      if (event.key === "ArrowLeft") {
        event.preventDefault();
        move(-1);
      } else if (event.key === "ArrowRight") {
        event.preventDefault();
        move(1);
      }
    });
    display.addEventListener("touchstart", (event) => {
      touchStartX = event.changedTouches[0]?.clientX ?? null;
    }, { passive: true });
    display.addEventListener("touchend", (event) => {
      if (touchStartX === null) {
        return;
      }
      const distance = (event.changedTouches[0]?.clientX ?? touchStartX) - touchStartX;
      touchStartX = null;
      if (Math.abs(distance) >= 48) {
        move(distance < 0 ? 1 : -1);
      }
    }, { passive: true });

    display.tabIndex = 0;
    setActive(0);
  });
}

function ensureLoginUserEditControls() {
  const form = document.getElementById("loginUserForm");
  if (!form) {
    return;
  }

  let idInput = form.elements.loginUserId;
  if (!idInput) {
    idInput = document.createElement("input");
    idInput.type = "hidden";
    idInput.name = "loginUserId";
    form.prepend(idInput);
  }

  const heading = form.querySelector("h2");
  if (heading && !heading.id) {
    heading.id = "loginUserFormTitle";
  }

  const passwordInput = form.elements.password;
  if (passwordInput) {
    passwordInput.required = true;
  }

  const submitButton = form.querySelector("button[type='submit']");
  if (submitButton && !submitButton.id) {
    submitButton.id = "loginUserSubmitBtn";
  }

  if (!document.getElementById("cancelLoginUserEditBtn")) {
    const cancelButton = document.createElement("button");
    cancelButton.className = "ghost";
    cancelButton.id = "cancelLoginUserEditBtn";
    cancelButton.type = "button";
    cancelButton.hidden = true;
    cancelButton.textContent = "取消編輯";

    const actions = document.createElement("div");
    actions.className = "form-actions";

    if (submitButton) {
      submitButton.parentNode?.insertBefore(actions, submitButton);
      actions.append(submitButton, cancelButton);
    } else {
      form.append(cancelButton);
    }
  }
}

function setDefaultDates() {
  const today = new Date().toISOString().slice(0, 10);
  document.querySelector("[name='orderDate']").value = today;
  document.querySelector("[name='giftDate']").value = today;
  document.querySelector("[name='payMonth']").value = today.slice(0, 7);
}

async function initializeAuth() {
  const discordLoginError = takeDiscordLoginError();
  const discordLinkResult = takeDiscordLinkResult();
  try {
    state.auth = await api("/api/auth/me", { skipAuthRedirect: true });
    if (state.auth.authRequired && !state.auth.isAuthenticated) {
      showLogin();
      if (discordLoginError) {
        showLoginError(discordLoginError);
      }
      return;
    }

    showApp();
    await refreshAll();
    if (discordLinkResult) {
      showAlert(discordLinkResult.message, discordLinkResult.isError);
    }
  } catch (error) {
    showLogin();
    showLoginError(discordLoginError || error.message);
  }
}

function takeDiscordLinkResult() {
  const url = new URL(window.location.href);
  const code = url.searchParams.get("discordLink");
  if (!code) {
    return null;
  }

  url.searchParams.delete("discordLink");
  window.history.replaceState({}, document.title, `${url.pathname}${url.search}${url.hash}`);

  const messages = {
    success: { message: "Discord 帳號綁定成功。", isError: false },
    conflict: { message: "此 Discord 已綁定其他帳號。", isError: true },
    member_required: { message: "此登入帳號尚未對應成員，請先在帳號管理指定現有成員。", isError: true },
    denied: { message: "Discord 授權已取消。", isError: true },
    session: { message: "登入狀態已失效，請重新登入後再綁定。", isError: true },
    state: { message: "Discord 綁定驗證失敗，請重新操作。", isError: true },
    config: { message: "Discord 登入尚未完成設定。", isError: true },
    failed: { message: "Discord 綁定失敗，請稍後再試。", isError: true }
  };
  return messages[code] || messages.failed;
}

function takeDiscordLoginError() {
  const url = new URL(window.location.href);
  const code = url.searchParams.get("loginError");
  if (!code) {
    return "";
  }

  url.searchParams.delete("loginError");
  window.history.replaceState({}, document.title, `${url.pathname}${url.search}${url.hash}`);

  const messages = {
    discord_config: "Discord 登入尚未設定，請先設定 Client ID / Client Secret。",
    discord_denied: "Discord 授權已取消。",
    discord_state: "Discord 登入驗證失敗，請重新登入。",
    discord_unbound: "這個 Discord 帳號尚未綁定系統帳號，請聯絡管理員。",
    discord_failed: "Discord 登入失敗，請稍後再試。"
  };
  return messages[code] || "Discord 登入失敗，請稍後再試。";
}

function showLogin() {
  document.body.classList.add("auth-locked");
  document.getElementById("loginView").hidden = false;
  document.getElementById("logoutBtn").hidden = true;
  document.getElementById("changePasswordBtn").hidden = true;
  document.getElementById("discordLinkBtn").hidden = true;
  document.getElementById("personalizationBtn").hidden = true;
  document.getElementById("currentUser").hidden = true;
  document.getElementById("currentOrganizationField").hidden = true;
}

function showApp() {
  document.body.classList.remove("auth-locked");
  document.getElementById("loginView").hidden = true;
  document.getElementById("loginAlert").hidden = true;
  const currentUser = document.getElementById("currentUser");
  if (state.auth?.user) {
    currentUser.textContent = state.auth.user.displayName;
    currentUser.hidden = false;
    document.getElementById("logoutBtn").hidden = false;
    document.getElementById("changePasswordBtn").hidden = false;
    document.getElementById("personalizationBtn").hidden = !hasPermission("Profile.Manage");
    const discordLinkBtn = document.getElementById("discordLinkBtn");
    discordLinkBtn.hidden = false;
    discordLinkBtn.textContent = state.auth.user.discordLinkedAt
      ? `解除 Discord 綁定（${state.auth.user.discordName || state.auth.user.discordId || "已綁定"}）`
      : "綁定 Discord";
  }
  state.preferences = state.auth?.preferences || null;
  applyPreferences(state.preferences);
  renderPreferenceForm(state.preferences);
  renderCurrentOrganizationSwitcher();
  applyNavigationPermissions();
  if (state.auth?.user?.systemRole === "viewer") {
    const userNav = document.querySelector('.nav-tabs button[data-view="users"]');
    const orderNav = document.querySelector('.nav-tabs button[data-view="orders"]');
    const giftNav = document.querySelector('.nav-tabs button[data-view="giftRecords"]');
    if (userNav) userNav.textContent = "我的資料";
    if (orderNav) orderNav.textContent = "我的訂單";
    if (giftNav) giftNav.textContent = "我的送禮紀錄";
  }
}

function currentPermissions() {
  return new Set(state.auth?.user?.permissions || []);
}

function hasPermission(code) {
  return state.auth?.authRequired === false ||
    state.auth?.user?.systemRole === "admin" ||
    currentPermissions().has(code);
}

function isBootstrapMode() {
  return state.auth?.authRequired === false;
}

function currentOrganizationId() {
  return state.auth?.currentOrganizationId || state.auth?.user?.organizationId || 0;
}

function hasAnyPermission(codes) {
  return codes.some((code) => hasPermission(code));
}

function applyNavigationPermissions() {
  const viewPermissions = {
    dashboard: "Order.View",
    users: "Member.View",
    loginUsers: "Account.Manage",
    organization: "Organization.Manage",
    services: "Gift.View",
    activities: "Order.View",
    giftRecords: "Gift.View",
    orders: "Order.View",
    payments: "Settlement.View",
    audit: "Audit.View",
    moneyLogs: "Audit.View",
    settings: "Profile.Manage",
    permissions: null
  };

  document.querySelectorAll(".nav-tabs button").forEach((button) => {
    const permission = viewPermissions[button.dataset.view];
    button.hidden = button.dataset.view === "permissions"
      ? state.auth?.user?.systemRole !== "admin"
      : permission
        ? !hasPermission(permission)
        : false;
    if (state.auth?.user?.systemRole === "viewer" &&
        ["services", "payments"].includes(button.dataset.view)) {
      button.hidden = true;
    }
  });

  document.querySelectorAll(".log-switcher").forEach((switcher) => {
    switcher.hidden = !hasPermission("Audit.View");
  });
}

function applyActionPermissions() {
  const setHidden = (selector, hidden) => {
    document.querySelectorAll(selector).forEach((element) => {
      element.hidden = hidden;
    });
  };

  setHidden("#userForm", !(hasPermission("Member.Create") || hasPermission("Member.Edit")));
  setHidden("[data-user-edit], [data-user-activate], [data-user-deactivate]", !hasPermission("Member.Edit"));
  setHidden("[data-user-delete]", !hasPermission("Member.Delete"));
  setHidden("#giftRecordForm", !(hasPermission("Gift.Create") || hasPermission("Gift.Edit")));
  setHidden("[data-gift-edit]", !hasPermission("Gift.Edit"));
  setHidden("[data-gift-delete]", !hasPermission("Gift.Delete"));
  setHidden("#orderForm", !(hasPermission("Order.Create") || hasPermission("Order.Edit")));
  setHidden("[data-order-edit]", !hasPermission("Order.Edit"));
  setHidden("[data-order-delete]", !hasPermission("Order.Cancel"));
  setHidden("#activityForm, [data-activity-edit], [data-activity-toggle]", !hasPermission("Order.Edit"));
  setHidden("#paymentForm, [data-payment-paid]", !hasPermission("Settlement.Close"));
  setHidden("[data-money-reverse]", !hasAnyPermission(["Settlement.Close", "Account.Manage"]));
}

async function api(path, options = {}) {
  const { skipAuthRedirect, ...fetchOptions } = options;
  const isFormData = fetchOptions.body instanceof FormData;
  const headers = isFormData
    ? { ...(options.headers || {}) }
    : fetchOptions.body == null
      ? { ...(options.headers || {}) }
      : {
          "Content-Type": "application/json",
          ...(options.headers || {})
        };
  const startedAt = typeof performance !== "undefined" ? performance.now() : Date.now();
  let response;

  try {
    response = await fetch(path, {
      headers,
      ...fetchOptions
    });
  } catch (error) {
    recordResponseTime(path, elapsedMs(startedAt), false, 0);
    throw error;
  }

  recordResponseTime(path, elapsedMs(startedAt), response.ok, response.status);

  if (!response.ok) {
    if (response.status === 401 && !skipAuthRedirect) {
      showLogin();
    }

    let message = `${response.status} ${response.statusText}`;
    try {
      const error = await response.json();
      console.error("API request failed", {
        path,
        status: response.status,
        statusText: response.statusText,
        error
      });
      message = error.detail || error.message || message;
      if (error.errors) {
        message += ` ${Object.values(error.errors).flat().join(" ")}`;
      }
    } catch {
      // keep default message
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

function elapsedMs(startedAt) {
  const now = typeof performance !== "undefined" ? performance.now() : Date.now();
  return Math.max(0, Math.round(now - startedAt));
}

function recordResponseTime(path, durationMs, ok, status) {
  if (!state.responseTime) {
    return;
  }

  const samples = state.responseTime.samples;
  samples.unshift({
    path,
    durationMs,
    ok,
    status,
    at: new Date().toISOString()
  });

  if (samples.length > 30) {
    samples.length = 30;
  }

  const completed = samples.filter((sample) => sample.ok);
  const measured = completed.length ? completed : samples;
  const total = measured.reduce((sum, sample) => sum + sample.durationMs, 0);

  state.responseTime.lastMs = durationMs;
  state.responseTime.averageMs = measured.length ? Math.round(total / measured.length) : null;
  state.responseTime.maxMs = measured.length ? Math.max(...measured.map((sample) => sample.durationMs)) : null;

  renderResponseTimePanel();
}

function renderResponseTimePanel() {
  const panel = document.getElementById("responseTimePanel");
  if (!panel) {
    return;
  }

  const samples = state.responseTime?.samples || [];
  const lastMs = state.responseTime?.lastMs;
  const averageMs = state.responseTime?.averageMs;
  const maxMs = state.responseTime?.maxMs;
  const latest = samples[0];
  const status = document.getElementById("responseTimeStatus");

  setText("responseTimeLast", formatMilliseconds(lastMs));
  setText("responseTimeAverage", formatMilliseconds(averageMs));
  setText("responseTimeMax", formatMilliseconds(maxMs));
  setText("responseTimeSampleCount", samples.length);

  if (status) {
    status.className = `response-time-status ${responseTimeTone(averageMs, latest?.ok)}`;
    status.textContent = responseTimeLabel(averageMs, latest?.ok);
  }

  const meter = document.getElementById("responseTimeMeterBar");
  if (meter) {
    const width = averageMs == null ? 0 : Math.min(100, Math.max(6, Math.round((averageMs / 1500) * 100)));
    meter.style.width = `${width}%`;
  }

  const list = document.getElementById("responseTimeList");
  if (!list) {
    return;
  }

  if (!samples.length) {
    list.innerHTML = "<span>尚無 API 樣本</span>";
    return;
  }

  list.innerHTML = samples.slice(0, 6).map((sample) => `
    <article class="${sample.ok ? "" : "failed"}">
      <span>${escapeHtml(shortApiPath(sample.path))}</span>
      <strong>${sample.durationMs} ms</strong>
    </article>
  `).join("");
}

function setText(id, value) {
  const element = document.getElementById(id);
  if (element) {
    element.textContent = value;
  }
}

function formatMilliseconds(value) {
  return value == null ? "-- ms" : `${Math.round(value)} ms`;
}

function responseTimeTone(averageMs, ok) {
  if (ok === false) {
    return "danger";
  }
  if (averageMs == null) {
    return "";
  }
  if (averageMs <= 300) {
    return "good";
  }
  if (averageMs <= 800) {
    return "warning";
  }
  return "danger";
}

function responseTimeLabel(averageMs, ok) {
  if (ok === false) {
    return "錯誤";
  }
  if (averageMs == null) {
    return "待測";
  }
  if (averageMs <= 300) {
    return "快速";
  }
  if (averageMs <= 800) {
    return "正常";
  }
  return "偏慢";
}

function shortApiPath(path) {
  const value = String(path || "");
  return value.replace(/^\/api\//, "");
}

async function submitLogin(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);

  try {
    state.auth = await api("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({
        loginAccount: data.get("loginAccount"),
        password: data.get("password")
      }),
      skipAuthRedirect: true
    });
    form.reset();
    showApp();
    hideAlert();
    await refreshAll();
  } catch (error) {
    showLoginError(error.message);
  }
}

function startDiscordLogin() {
  window.location.href = "/api/auth/discord/login";
}

async function toggleDiscordLink() {
  if (!state.auth?.user?.discordLinkedAt) {
    window.location.href = "/api/auth/discord/link";
    return;
  }

  if (!window.confirm("確定要解除目前的 Discord 綁定嗎？解除後將無法使用 Discord 登入。")) {
    return;
  }

  try {
    await api("/api/auth/discord/link", { method: "DELETE" });
    state.auth = await api("/api/auth/me");
    showApp();
    showAlert("Discord 綁定已解除。", false);
  } catch (error) {
    showAlert(error.message);
  }
}

async function logout() {
  await api("/api/auth/logout", { method: "POST", body: "{}" });
  state.auth = null;
  showLogin();
}

function openChangePasswordModal() {
  const modal = document.getElementById("changePasswordModal");
  document.getElementById("changePasswordForm").reset();
  modal.hidden = false;
  modal.querySelector("[name='currentPassword']")?.focus();
}

function closeChangePasswordModal() {
  document.getElementById("changePasswordModal").hidden = true;
  document.getElementById("changePasswordForm").reset();
}

async function submitChangePassword(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const newPassword = String(data.get("newPassword") || "");
  const confirmPassword = String(data.get("confirmPassword") || "");

  if (newPassword !== confirmPassword) {
    showAlert("新密碼與確認密碼不一致。");
    return;
  }

  await runAction(async () => {
    await api("/api/auth/change-password", {
      method: "POST",
      body: JSON.stringify({
        currentPassword: data.get("currentPassword"),
        newPassword,
        confirmPassword
      })
    });
    closeChangePasswordModal();
    showAlert("密碼已更新。", false);
  });
}

function showLoginError(message) {
  const alert = document.getElementById("loginAlert");
  alert.hidden = false;
  alert.textContent = message;
  alert.style.background = "#fff1df";
  alert.style.color = "var(--warn)";
}

async function refreshAll() {
  try {
    await api("/api/health");
    document.getElementById("apiStatus").classList.add("online");
    hideAlert();

    if ((state.auth?.user?.systemRole === "admin" || isBootstrapMode()) && state.organizations.length === 0) {
      await loadOrganizations();
    }

    if (state.view === "dashboard") {
      await loadDashboard();
    }
    if (state.view === "users" &&
        (state.auth?.user?.systemRole === "admin" || isBootstrapMode())) {
      await loadOrganizations();
    }
    if (state.view === "users" || state.view === "loginUsers" || state.view === "orders" || state.view === "giftRecords" || state.view === "organization") {
      await loadUsers();
    }
    if (state.view === "organization") {
      if (state.auth?.user?.systemRole === "admin" || isBootstrapMode()) {
        await loadOrganizations();
      }
      await loadDepartments();
    }
    if (state.view === "loginUsers") {
      if (state.auth?.user?.systemRole === "admin" || isBootstrapMode()) {
        await loadOrganizations();
      }
      await loadLoginUsers();
    }
    if (state.view === "services" || state.view === "giftRecords" || state.view === "orders") {
      await loadServiceItems();
    }
    if (state.view === "activities" || state.view === "orders") {
      await loadActivities();
    }
    if (state.view === "giftRecords") {
      await loadGiftRecords();
    }
    if (state.view === "orders" || state.view === "dashboard") {
      await loadOrders();
    }
    if (state.view === "payments") {
      await loadPayments();
    }
    if (state.view === "audit") {
      await loadAuditLogs();
    }
    if (state.view === "moneyLogs") {
      await loadMoneyLogs();
    }
    if (state.view === "loginHistory") {
      await loadLoginHistories();
    }
    if (state.view === "permissions") {
      await loadOrganizations();
      await loadPermissions();
    }
    if (state.view === "settings") {
      await loadPreferences();
    }
    applyActionPermissions();
  } catch (error) {
    showAlert(error.message);
    document.getElementById("apiStatus").classList.remove("online");
  }
}

async function loadDashboard() {
  const summary = await api("/api/dashboard/summary");
  const ranking = await api("/api/dashboard/ranking");
  document.getElementById("todayRevenue").textContent = money.format(summary.todayRevenue);
  document.getElementById("monthRevenue").textContent = money.format(summary.monthRevenue);
  document.getElementById("monthCommission").textContent = money.format(summary.monthCommissionAmount);
  document.getElementById("unpaidCount").textContent = summary.unpaidOrderCount;
  renderRanking(ranking);
}

async function loadUsers() {
  state.users = await api("/api/users?activeOnly=false");
  state.players = await api("/api/users/players");
  state.bosses = await api("/api/users/bosses");
  renderUsers();
  renderSelects();
}

async function loadLoginUsers() {
  state.loginUsers = await api("/api/loginusers");
  renderLoginUsers();
}

async function loadServiceItems() {
  state.serviceItems = await api("/api/serviceitems");
  renderServiceItems();
  renderOrderServiceShortcuts();
  renderSelects();
}

async function loadActivities() {
  state.activities = await api("/api/activities");
  renderActivities();
  renderOrderActivityOptions();
}

async function loadGiftRecords() {
  state.giftRecords = await api("/api/giftrecords");
  renderGiftRecords();
}

async function loadDepartments() {
  state.departments = await api("/api/departments?activeOnly=false");
  renderDepartments();
  renderSelects();
}

async function loadOrders() {
  state.orders = await api("/api/orders");
  renderOrders();
  renderRecentOrders();
}

async function loadPayments() {
  state.payments = await api("/api/payments");
  renderPayments();
}

async function loadAuditLogs() {
  state.auditLogs = await api(`/api/auditlogs?take=${preferredTablePageSize()}`);
  renderAuditLogs(state.auditLogs);
}

async function loadMoneyLogs() {
  state.moneyLogs = await api(`/api/moneylogs?take=${preferredTablePageSize()}`);
  renderMoneyLogs(state.moneyLogs);
}

async function loadLoginHistories() {
  state.loginHistories = await api(`/api/loginhistories?take=${preferredTablePageSize()}`);
  renderLoginHistories(state.loginHistories);
}

function preferredTablePageSize() {
  return Math.max(20, Math.min(500, Number(state.preferences?.tablePageSize || 100)));
}

async function loadPermissions() {
  state.permissionMatrix = await api("/api/permissions");
  state.auth.user.permissions = state.permissionMatrix.roles
    .find((role) => role.systemRole === state.auth.user.systemRole)?.permissions || [];
  renderPermissions();
  applyNavigationPermissions();
}

async function loadPreferences() {
  state.preferences = await api("/api/userpreferences/me");
  applyPreferences(state.preferences);
  renderPreferenceForm(state.preferences);
}

async function loadOrganizations() {
  state.organizations = await api("/api/organizations");
  renderCurrentOrganizationSwitcher();
  renderOrganizationManagement();
  renderOrganizationSelect();
  renderUserOrganizationSelect();
}

function renderCurrentOrganizationSwitcher() {
  const field = document.getElementById("currentOrganizationField");
  const select = document.getElementById("currentOrganizationSelect");
  if (!field || !select) {
    return;
  }

  const activeOrganizations = state.organizations.filter((organization) => organization.isActive);
  const canSwitch = state.auth?.user?.systemRole === "admin" && activeOrganizations.length > 1;
  field.hidden = !canSwitch;
  if (!canSwitch) {
    select.innerHTML = "";
    return;
  }

  select.innerHTML = activeOrganizations
    .map((organization) => `<option value="${organization.id}">${escapeHtml(organizationDisplayName(organization))}</option>`)
    .join("");
  select.value = String(currentOrganizationId());
}

async function switchCurrentOrganization(event) {
  const organizationId = Number(event.currentTarget.value);
  if (!organizationId) {
    return;
  }

  await runAction(async () => {
    state.auth = await api("/api/auth/organization", {
      method: "POST",
      body: JSON.stringify({ organizationId })
    });
    clearScopedState();
    renderCurrentOrganizationSwitcher();
    await refreshAll();
    const organization = state.organizations.find((item) => item.id === organizationId);
    showAlert(`已切換到「${organizationDisplayName(organization)}」。`, false);
  });
}

function clearScopedState() {
  state.users = [];
  state.loginUsers = [];
  state.serviceItems = [];
  state.activities = [];
  state.giftRecords = [];
  state.departments = [];
  state.players = [];
  state.bosses = [];
  state.orders = [];
  state.payments = [];
  state.auditLogs = [];
  state.moneyLogs = [];
  state.loginHistories = [];
  state.permissionMatrix = null;
}

function renderOrganizationSelect() {
  const field = document.getElementById("loginUserOrganizationField");
  const select = document.getElementById("loginUserOrganizationSelect");
  if (!field || !select) {
    return;
  }

  field.hidden = state.auth?.user?.systemRole !== "admin" && !isBootstrapMode();
  select.innerHTML = state.organizations
    .filter((organization) => organization.isActive)
    .map((organization) => `<option value="${organization.id}">${escapeHtml(organizationDisplayName(organization))}</option>`)
    .join("");
  const selectedOrganizationId = currentOrganizationId();
  if (selectedOrganizationId && [...select.options].some((option) => Number(option.value) === selectedOrganizationId)) {
    select.value = String(selectedOrganizationId);
  }
  renderLoginUserMemberSelect();
  select.onchange = renderLoginUserMemberSelect;
}

function renderUserOrganizationSelect() {
  const field = document.getElementById("userOrganizationField");
  const select = document.getElementById("userOrganizationSelect");
  if (!field || !select) {
    return;
  }

  field.hidden = state.auth?.user?.systemRole !== "admin" && !isBootstrapMode();
  select.innerHTML = state.organizations
    .filter((organization) => organization.isActive)
    .map((organization) => `<option value="${organization.id}">${escapeHtml(organizationDisplayName(organization))}</option>`)
    .join("");

  const selectedOrganizationId = currentOrganizationId();
  if (selectedOrganizationId && [...select.options].some((option) => Number(option.value) === selectedOrganizationId)) {
    select.value = String(selectedOrganizationId);
  }
}

function renderLoginUserMemberSelect() {
  const organizationSelect = document.getElementById("loginUserOrganizationSelect");
  const memberInput = document.getElementById("loginUserMemberSelect");
  if (!memberInput) {
    return;
  }

  const organizationId = Number(organizationSelect?.value) || currentOrganizationId();
  const selected = state.users.find((user) => user.id === Number(memberInput.value));
  if (selected && organizationId && selected.organizationId !== organizationId) {
    memberInput.value = "";
  }
  refreshMemberPickerField(memberInput.closest("[data-member-picker]"));
}

function renderOrganizationManagement() {
  const body = document.getElementById("organizationManagementRows");
  if (!body) {
    return;
  }

  body.innerHTML = state.organizations.length
    ? state.organizations.map((organization) => `
      <tr>
        <td>${escapeHtml(organization.name)}</td>
        <td>${organization.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
        <td><button class="ghost small" data-organization-edit="${organization.id}">編輯</button></td>
      </tr>
    `).join("")
    : emptyRow(3);

  body.querySelectorAll("[data-organization-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const organization = state.organizations.find((item) => item.id === Number(button.dataset.organizationEdit));
      if (!organization) {
        return;
      }

      const form = document.getElementById("organizationManagementForm");
      form.elements.organizationId.value = organization.id;
      form.elements.name.value = organization.name;
      form.elements.isActive.checked = organization.isActive;
      document.getElementById("organizationManagementTitle").textContent = "編輯組織";
      document.getElementById("cancelOrganizationManagementBtn").hidden = false;
    });
  });
}

async function submitOrganization(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const id = data.get("organizationId");
  await runAction(async () => {
    await api(id ? `/api/organizations/${id}` : "/api/organizations", {
      method: id ? "PUT" : "POST",
      body: JSON.stringify({
        name: data.get("name"),
        isActive: data.get("isActive") === "on"
      })
    });
    resetOrganizationManagementForm();
    await loadOrganizations();
    showAlert("組織設定已儲存。", false);
  });
}

function resetOrganizationManagementForm() {
  const form = document.getElementById("organizationManagementForm");
  form.reset();
  form.elements.organizationId.value = "";
  form.elements.isActive.checked = true;
  document.getElementById("organizationManagementTitle").textContent = "新增組織";
  document.getElementById("cancelOrganizationManagementBtn").hidden = true;
}

function renderPermissions() {
  const body = document.getElementById("permissionRows");
  const matrix = state.permissionMatrix;
  if (!body || !matrix) {
    return;
  }

  const byRole = Object.fromEntries(matrix.roles.map((role) => [
    role.systemRole,
    new Set(role.permissions)
  ]));
  body.innerHTML = matrix.permissionCodes.map((code) => `
    <tr>
      <td>
        <strong>${escapeHtml(permissionLabels[code] || code)}</strong>
        <span class="permission-code">${escapeHtml(code)}</span>
      </td>
      ${["admin", "staff", "viewer"].map((role) => `
        <td>
          <input
            type="checkbox"
            data-role-permission="${role}"
            value="${escapeHtml(code)}"
            ${byRole[role]?.has(code) ? "checked" : ""}
            ${role === "admin" ? "disabled" : ""}>
        </td>
      `).join("")}
    </tr>
  `).join("");
}

async function savePermissions() {
  try {
    for (const role of ["staff", "viewer"]) {
      const permissions = [...document.querySelectorAll(`[data-role-permission="${role}"]:checked`)]
        .map((input) => input.value);
      await api(`/api/permissions/${role}`, {
        method: "PUT",
        body: JSON.stringify({ permissions })
      });
    }

    await loadPermissions();
    showAlert("權限設定已儲存。");
  } catch (error) {
    showAlert(error.message);
  }
}

function renderRanking(rows) {
  const body = document.getElementById("rankingRows");
  body.innerHTML = rows.length ? rows.slice(0, 10).map((row) => `
    <tr>
      <td>${escapeHtml(row.nickname)}</td>
      <td>${money.format(row.totalShareAmount)}</td>
      <td>${row.orderCount}</td>
    </tr>
  `).join("") : emptyRow(3);
}

function renderRecentOrders() {
  const body = document.getElementById("recentOrderRows");
  const rows = state.orders.slice(0, 10);
  body.innerHTML = rows.length ? rows.map((row) => `
    <tr>
      <td>${row.orderDate}</td>
      <td>${escapeHtml(row.orderNo || "")}</td>
      <td>${money.format(row.amount)}</td>
      <td>${paymentPill(row.customerPaymentStatus)}</td>
    </tr>
  `).join("") : emptyRow(4);
}

function renderUsers() {
  renderUserTable("playerRows", state.users.filter((user) => user.isPlayer));
  renderUserTable("bossRows", state.users.filter((user) => user.isBoss));
}

function renderLoginUsers() {
  const body = document.getElementById("loginUserRows");
  if (!body) {
    return;
  }

  ensureLoginUserTableHeader(body);

  const users = state.loginUsers;
  body.innerHTML = users.length ? users.map((user) => `
    <tr>
      <td><button class="record-name-link" type="button" data-login-user-open="${user.id}">${escapeHtml(user.loginAccount || "")}</button></td>
      <td><button class="record-name-link" type="button" data-login-user-open="${user.id}">${escapeHtml(user.displayName)}</button></td>
      <td>${label("systemRole", user.systemRole)}</td>
      <td>${user.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
      <td>${pill("已設定", "good")}</td>
      <td class="actions-col">
        <div class="table-actions">
          ${user.isActive
            ? `<button class="ghost small" data-login-user-deactivate="${user.id}">停用</button>`
            : `<button class="ghost small" data-login-user-activate="${user.id}">啟用</button>`}
          <button class="ghost small" data-login-user-reset-password="${user.id}">重設密碼</button>
          <button class="ghost small danger-action" data-login-user-delete="${user.id}">刪除</button>
        </div>
      </td>
    </tr>
  `).join("") : emptyRow(6);

  bindLoginUserTableActions(body);
}

function ensureLoginUserTableHeader(body) {
  const table = body.closest("table");
  const headerRow = table?.querySelector("thead tr");
  if (!table || !headerRow) {
    return;
  }

  table.classList.add("login-user-table");

  if (headerRow.children.length < 6) {
    const actionHeader = document.createElement("th");
    actionHeader.className = "actions-col";
    actionHeader.textContent = "操作";
    headerRow.appendChild(actionHeader);
  } else {
    headerRow.lastElementChild?.classList.add("actions-col");
  }
}

function bindLoginUserTableActions(body) {
  body.querySelectorAll("[data-login-user-open]").forEach((button) => {
    button.addEventListener("click", () => {
      const loginUser = state.loginUsers.find((item) => item.id === Number(button.dataset.loginUserOpen));
      if (loginUser) {
        openLoginUserRecordModal(loginUser);
      }
    });
  });

  body.querySelectorAll("[data-login-user-deactivate]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/loginusers/${button.dataset.loginUserDeactivate}/deactivate`, { method: "POST", body: "{}" });
        await loadLoginUsers();
        showAlert("登入者已停用。", false);
      });
    });
  });

  body.querySelectorAll("[data-login-user-activate]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/loginusers/${button.dataset.loginUserActivate}/activate`, { method: "POST", body: "{}" });
        await loadLoginUsers();
        showAlert("登入者已啟用。", false);
      });
    });
  });

  body.querySelectorAll("[data-login-user-reset-password]").forEach((button) => {
    button.addEventListener("click", async () => {
      const loginUser = state.loginUsers.find((item) => item.id === Number(button.dataset.loginUserResetPassword));
      const account = loginUser?.loginAccount || "";
      if (!window.confirm(`確定要重設「${account || loginUser?.displayName || "此帳號"}」的密碼嗎？\n\n預設重設密碼為登入帳號。`)) {
        return;
      }

      await runAction(async () => {
        await api(`/api/loginusers/${button.dataset.loginUserResetPassword}/reset-password`, { method: "POST", body: "{}" });
        await loadLoginUsers();
        showAlert("密碼已重設為登入帳號。", false);
      });
    });
  });

  body.querySelectorAll("[data-login-user-delete]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/loginusers/${button.dataset.loginUserDelete}`, { method: "DELETE" });
        await loadLoginUsers();
        showAlert("登入者已刪除。", false);
      });
    });
  });
}

function renderServiceItems() {
  const body = document.getElementById("serviceItemRows");
  if (!body) {
    return;
  }

  renderServiceCategoryTabs();
  renderServiceBranchHead();
  const rows = state.serviceItems
    .filter((item) => item.category === state.serviceCategory && item.isActive)
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.localeCompare(b.name, "zh-Hant"));

  const giftCustomCard = state.serviceCategory === "gift"
    ? `
      <button class="service-item-card" type="button" data-service-gift-custom>
        <div class="service-item-card-main">
          <span>自訂</span>
          <strong>自訂禮物</strong>
          <em>自填 / 金額</em>
          <p>自訂名稱與金額，適合未列在固定禮物表的打賞或禮物。</p>
        </div>
        <span class="service-item-action">贈送</span>
      </button>
    `
    : "";

  body.innerHTML = rows.length || giftCustomCard ? `${rows.map((item) => `
    <button class="service-item-card" type="button" data-${item.category === "gift" ? "service-gift" : "service-order"}="${item.id}">
      <div class="service-item-card-main">
        <span>${escapeHtml(item.subcategory || serviceCategoryText(item.category))}</span>
        <strong>${escapeHtml(item.name)}</strong>
        <em>${escapeHtml(servicePriceText(item))} / ${escapeHtml(unitTypeText(item.unitType))}</em>
        ${item.remark ? `<p>${escapeHtml(item.remark)}</p>` : ""}
      </div>
      <span class="service-item-action">
        ${item.category === "gift" ? "贈送" : "點單"}
      </span>
    </button>
  `).join("")}${giftCustomCard}` : `<p class="muted">這個分類目前沒有可用項目。</p>`;

  body.querySelectorAll("[data-service-order]").forEach((button) => {
    button.addEventListener("click", () => {
      const item = state.serviceItems.find((serviceItem) => serviceItem.id === Number(button.dataset.serviceOrder));
      if (item) {
        startOrderFromService(item);
      }
    });
  });

  body.querySelectorAll("[data-service-gift]").forEach((button) => {
    button.addEventListener("click", () => {
      const item = state.serviceItems.find((serviceItem) => serviceItem.id === Number(button.dataset.serviceGift));
      if (item) {
        startGiftRecordFromService(item);
      }
    });
  });

  body.querySelectorAll("[data-service-gift-custom]").forEach((button) => {
    button.addEventListener("click", () => {
      startCustomGiftRecord();
    });
  });

}

function renderOrderServiceShortcuts() {
  const wrap = document.getElementById("orderServiceShortcuts");
  if (!wrap) {
    return;
  }

  const preferredCategories = ["play", "special_companion", "grind", "boost"];
  const items = state.serviceItems
    .filter((item) => item.isActive && preferredCategories.includes(item.category))
    .sort((a, b) => {
      const categoryDelta = preferredCategories.indexOf(a.category) - preferredCategories.indexOf(b.category);
      return categoryDelta || (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.localeCompare(b.name, "zh-Hant");
    });

  if (!items.length) {
    wrap.innerHTML = `<p class="muted">尚未載入服務價目。</p>`;
    return;
  }

  const categories = preferredCategories.filter((category) => items.some((item) => item.category === category));
  if (!categories.includes(state.orderServiceCategory)) {
    state.orderServiceCategory = categories[0] || "play";
  }
  const categoryItems = items.filter((item) => item.category === state.orderServiceCategory);

  wrap.innerHTML = `
    <div class="order-shortcut-tabs" aria-label="新版價目大類">
      ${categories.map((category) => `
        <button class="${state.orderServiceCategory === category ? "active" : ""}" type="button" data-order-service-category="${category}">
          ${serviceCategoryText(category)}
        </button>
      `).join("")}
    </div>
    <section class="order-shortcut-group">
      <h3>${serviceCategoryText(state.orderServiceCategory)}</h3>
      <div class="order-shortcut-list">
        ${categoryItems.map((item) => `
          <button class="order-shortcut-card" type="button" data-service-order="${item.id}">
            <span>${escapeHtml(item.subcategory || serviceCategoryText(item.category))}</span>
            <strong>${escapeHtml(item.name)}</strong>
            <em>${escapeHtml(servicePriceText(item))} / ${escapeHtml(unitTypeText(item.unitType))}</em>
          </button>
        `).join("")}
      </div>
    </section>
  `;

  wrap.querySelectorAll("[data-order-service-category]").forEach((button) => {
    button.addEventListener("click", () => {
      state.orderServiceCategory = button.dataset.orderServiceCategory;
      renderOrderServiceShortcuts();
    });
  });

  wrap.querySelectorAll("[data-service-order]").forEach((button) => {
    button.addEventListener("click", () => {
      const item = state.serviceItems.find((serviceItem) => serviceItem.id === Number(button.dataset.serviceOrder));
      if (item) {
        applyServiceToOrderForm(item);
      }
    });
  });
}

function renderGiftRecords() {
  const body = document.getElementById("giftRecordRows");
  if (!body) {
    return;
  }

  body.innerHTML = state.giftRecords.length ? state.giftRecords.map((record) => `
    <tr>
      <td>${record.giftDate}</td>
      <td>${escapeHtml(record.bossNickname)}</td>
      <td>${escapeHtml(record.recipientNickname)}</td>
      <td>${escapeHtml(record.giftName)}${record.quantity && record.quantity !== 1 ? ` × ${money.format(record.quantity)}` : ""}</td>
      <td>${money.format(record.amount)}</td>
      <td>${paymentPill(record.customerPaymentStatus)}</td>
      <td>${escapeHtml(record.remark || "—")}</td>
      <td>
        <button class="ghost small" data-gift-edit="${record.id}">編輯</button>
        <button class="ghost small" data-gift-attachments="${record.id}">附件</button>
        <button class="ghost small danger-action" data-gift-delete="${record.id}">刪除</button>
      </td>
    </tr>
  `).join("") : emptyRow(8);

  body.querySelectorAll("[data-gift-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const record = state.giftRecords.find((item) => item.id === Number(button.dataset.giftEdit));
      if (record) {
        startGiftRecordEdit(record);
      }
    });
  });

  body.querySelectorAll("[data-gift-attachments]").forEach((button) => {
    button.addEventListener("click", () => {
      const record = state.giftRecords.find((item) => item.id === Number(button.dataset.giftAttachments));
      openAttachmentModal("送禮紀錄附件", "gift_records", Number(button.dataset.giftAttachments), {
        attachmentKind: record?.customerPaymentStatus === "paid" || record?.customerPaymentStatus === "partial" ? "payment_proof" : "general",
        uploadLabel: "新增付款證明/送禮截圖",
        canEdit: hasPermission("Gift.Edit")
      });
    });
  });

  body.querySelectorAll("[data-gift-delete]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/giftrecords/${button.dataset.giftDelete}`, { method: "DELETE" });
        await loadGiftRecords();
        showAlert("送禮紀錄已刪除。", false);
      });
    });
  });
}

function renderDepartments() {
  const wrap = document.getElementById("departmentCards");
  if (!wrap) {
    return;
  }

  renderDepartmentSummary();

  wrap.innerHTML = state.departments.length ? state.departments.map((department) => `
    <article class="department-card" data-department-open="${department.id}" tabindex="0" role="button">
      <div class="department-head">
        <div>
          <h3 class="department-title">
            <span>${escapeHtml(department.name)}</span>
            ${department.englishName ? `<span class="department-title-en">${escapeHtml(department.englishName)}</span>` : ""}
          </h3>
        </div>
        <div class="department-actions">
          ${department.isActive ? pill("啟用", "good") : pill("停用", "bad")}
          <button class="ghost small" data-department-open-button="${department.id}" type="button">查看</button>
          <button class="ghost small" data-department-edit="${department.id}" type="button">編輯</button>
          <button class="ghost small danger-action" data-department-delete="${department.id}" type="button">刪除</button>
        </div>
      </div>
      <div class="department-meta">
        <span>${(department.members || []).length} 位成員</span>
        <span>排序 ${department.sortOrder ?? 0}</span>
      </div>
    </article>
  `).join("") : `<p class="muted">尚未建立部門。</p>`;

  wrap.querySelectorAll("[data-department-open]").forEach((card) => {
    card.addEventListener("click", (event) => {
      if (event.target.closest("button")) {
        return;
      }

      openDepartmentModal(Number(card.dataset.departmentOpen));
    });
    card.addEventListener("keydown", (event) => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        openDepartmentModal(Number(card.dataset.departmentOpen));
      }
    });
  });

  wrap.querySelectorAll("[data-department-open-button]").forEach((button) => {
    button.addEventListener("click", () => {
      openDepartmentModal(Number(button.dataset.departmentOpenButton));
    });
  });

  wrap.querySelectorAll("[data-department-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const department = state.departments.find((item) => item.id === Number(button.dataset.departmentEdit));
      if (department) {
        startDepartmentEdit(department);
      }
    });
  });

  wrap.querySelectorAll("[data-department-delete]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/departments/${button.dataset.departmentDelete}`, { method: "DELETE" });
        resetDepartmentForm();
        await loadDepartments();
        showAlert("部門已刪除。", false);
      });
    });
  });

}

function openDepartmentModal(departmentId) {
  state.activeDepartmentId = departmentId;
  renderDepartmentModal();
  const modal = document.getElementById("departmentModal");
  if (modal) {
    modal.hidden = false;
  }
}

function closeDepartmentModal() {
  state.activeDepartmentId = null;
  const modal = document.getElementById("departmentModal");
  if (modal) {
    modal.hidden = true;
  }
}

function renderDepartmentModal() {
  const department = state.departments.find((item) => item.id === state.activeDepartmentId);
  const title = document.getElementById("departmentModalTitle");
  const eyebrow = document.getElementById("departmentModalEyebrow");
  const body = document.getElementById("departmentModalBody");
  if (!department || !title || !eyebrow || !body) {
    return;
  }

  const members = department.members || [];
  title.textContent = department.englishName ? `${department.name} ${department.englishName}` : department.name;
  eyebrow.textContent = `${members.length} 位成員 · 排序 ${department.sortOrder ?? 0}`;
  body.innerHTML = `
    <div class="department-modal-summary">
      ${department.isActive ? pill("啟用", "good") : pill("停用", "bad")}
      <span>${escapeHtml(department.description || "尚未填寫職責說明。")}</span>
    </div>
    <div class="department-member-list">
      ${members.length ? members.map((member) => `
        <article class="department-member-row">
          <div>
            <strong>${escapeHtml(member.nickname)}</strong>
            <span>${escapeHtml(member.positionTitle || "未設定職稱")}${member.isManager ? " · 主管" : ""}</span>
          </div>
          <div class="table-actions">
            <button class="ghost small" type="button" data-department-member-edit="${member.id}">編輯</button>
            <button class="ghost small danger-action" type="button" data-department-member-delete="${member.id}">刪除</button>
          </div>
        </article>
      `).join("") : `<p class="muted">尚未加入成員。</p>`}
    </div>
  `;

  body.querySelectorAll("[data-department-member-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const member = members.find((item) => item.id === Number(button.dataset.departmentMemberEdit));
      if (member) startDepartmentMemberEdit(member);
    });
  });

  body.querySelectorAll("[data-department-member-delete]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/departments/members/${button.dataset.departmentMemberDelete}`, { method: "DELETE" });
        await loadDepartments();
        state.activeDepartmentId = department.id;
        renderDepartmentModal();
        showAlert("部門成員已刪除。", false);
      });
    });
  });
}

function renderDepartmentSummary() {
  const total = state.departments.length;
  const active = state.departments.filter((department) => department.isActive).length;
  const members = state.departments.reduce((sum, department) => sum + (department.members || []).length, 0);

  document.getElementById("departmentTotal").textContent = total;
  document.getElementById("departmentActiveTotal").textContent = active;
  document.getElementById("departmentMemberTotal").textContent = members;
}

function renderServiceCategoryTabs() {
  const tabs = document.getElementById("serviceCategoryTabs");
  if (!tabs) {
    return;
  }

  const categories = [
    ["play", "陪玩"],
    ["special_companion", "特殊陪"],
    ["grind", "代肝"],
    ["boost", "代打"],
    ["gift", "禮物"],
    ["deposit_bonus", "預存"],
    ["other", "其他"]
  ];
  const activeCategories = categories.filter(([value]) =>
    state.serviceItems.some((item) => item.category === value && item.isActive));
  if (!activeCategories.some(([value]) => value === state.serviceCategory)) {
    state.serviceCategory = activeCategories[0]?.[0] || "play";
  }

  tabs.innerHTML = activeCategories.map(([value, text]) => {
    const count = state.serviceItems.filter((item) => item.category === value && item.isActive).length;
    return `
    <button class="${state.serviceCategory === value ? "active" : ""}" data-service-category="${value}" type="button">
      <span>${text}</span>
      <strong>${count}</strong>
    </button>
  `;
  }).join("");

  tabs.querySelectorAll("[data-service-category]").forEach((button) => {
    button.addEventListener("click", () => {
      state.serviceCategory = button.dataset.serviceCategory;
      renderServiceItems();
    });
  });
}

function renderUserTable(elementId, users) {
  const body = document.getElementById(elementId);
  body.innerHTML = users.length ? users.map((user) => `
    <tr>
      <td><button class="record-name-link" type="button" data-user-open="${user.id}">${escapeHtml(user.nickname)}</button></td>
      <td>${user.discordId ? escapeHtml(user.discordId) : plainText("未綁定", "muted")}</td>
      <td>${user.discordName ? escapeHtml(user.discordName) : plainText("未綁定", "muted")}</td>
      <td>${user.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
      <td>
        ${user.isActive
          ? `<button class="ghost small" data-user-deactivate="${user.id}">停用</button>`
          : `<button class="ghost small" data-user-activate="${user.id}">啟用</button>`}
        <button class="ghost small danger-action" data-user-delete="${user.id}">刪除</button>
      </td>
    </tr>
  `).join("") : emptyRow(5);

  bindUserTableActions(body);
}

function bindUserTableActions(body) {
  body.querySelectorAll("[data-user-open]").forEach((button) => {
    button.addEventListener("click", () => {
      const user = state.users.find((item) => item.id === Number(button.dataset.userOpen));
      if (user) {
        openUserRecordModal(user);
      }
    });
  });

  body.querySelectorAll("[data-user-deactivate]").forEach((button) => {
    button.addEventListener("click", async () => {
      await api(`/api/users/${button.dataset.userDeactivate}/deactivate`, { method: "POST", body: "{}" });
      await loadUsers();
    });
  });
  body.querySelectorAll("[data-user-activate]").forEach((button) => {
    button.addEventListener("click", async () => {
      await api(`/api/users/${button.dataset.userActivate}/activate`, { method: "POST", body: "{}" });
      await loadUsers();
    });
  });
  body.querySelectorAll("[data-user-delete]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/users/${button.dataset.userDelete}`, { method: "DELETE" });
        await loadUsers();
        showAlert("成員已刪除。", false);
      });
    });
  });
}

function renderActivities() {
  const body = document.getElementById("activityRows");
  if (!body) {
    return;
  }

  body.innerHTML = state.activities.length ? state.activities.map((activity) => `
    <tr>
      <td>${escapeHtml(activity.name)}</td>
      <td>${formatDateTime(activity.startsAt)}<br>${formatDateTime(activity.endsAt)}</td>
      <td>${escapeHtml(activityDiscountText(activity))}</td>
      <td>${escapeHtml(activityCategoriesText(activity.applicableCategories))}</td>
      <td>${pill(activity.isActive ? "啟用" : "停用", activity.isActive ? "success" : "muted")}</td>
      <td>
        <div class="table-actions">
          <button class="ghost small" data-activity-edit="${activity.id}">編輯</button>
          <button class="ghost small" data-activity-toggle="${activity.id}">${activity.isActive ? "停用" : "啟用"}</button>
        </div>
      </td>
    </tr>
  `).join("") : emptyRow(6);

  body.querySelectorAll("[data-activity-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const activity = state.activities.find((item) => item.id === Number(button.dataset.activityEdit));
      if (activity) {
        startActivityEdit(activity);
      }
    });
  });
  body.querySelectorAll("[data-activity-toggle]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/activities/${button.dataset.activityToggle}/toggle`, { method: "POST", body: "{}" });
        await loadActivities();
      });
    });
  });
}

function renderOrders() {
  const body = document.getElementById("orderRows");
  body.innerHTML = state.orders.length ? state.orders.map((order) => `
    <tr>
      <td>${order.id}</td>
      <td>${order.orderDate}</td>
      <td>${escapeHtml(order.orderNo || "")}</td>
      <td>${escapeHtml(orderTypeText(order.orderType))}</td>
      <td>${money.format(orderFinalAmount(order))}</td>
      <td>${money.format(order.commissionAmount)}</td>
      <td>${statusPill(order.status)}</td>
      <td>${paymentPill(order.customerPaymentStatus)}</td>
      <td>
        <div class="table-actions">
          <button class="ghost small" data-order-detail="${order.id}">詳情</button>
          <button class="ghost small" data-order-edit="${order.id}">編輯</button>
          <button class="ghost small" data-order-attachments="${order.id}">附件</button>
          <button class="ghost small danger-action" data-order-delete="${order.id}">刪除</button>
        </div>
      </td>
    </tr>
  `).join("") : emptyRow(9);

  body.querySelectorAll("[data-order-detail]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        const order = await api(`/api/orders/${button.dataset.orderDetail}`);
        openOrderDetail(order);
      });
    });
  });

  body.querySelectorAll("[data-order-edit]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        const order = await api(`/api/orders/${button.dataset.orderEdit}`);
        startOrderEdit(order);
      });
    });
  });

  body.querySelectorAll("[data-order-attachments]").forEach((button) => {
    button.addEventListener("click", () => {
      const order = state.orders.find((item) => item.id === Number(button.dataset.orderAttachments));
      openAttachmentModal("訂單附件", "orders", Number(button.dataset.orderAttachments), {
        attachmentKind: order?.customerPaymentStatus === "paid" || order?.customerPaymentStatus === "partial"
          ? "payment_proof"
          : order?.status === "disputed"
            ? "evidence"
            : "general",
        uploadLabel: "新增收款證明/爭議依據",
        canEdit: hasPermission("Order.Edit")
      });
    });
  });

  body.querySelectorAll("[data-order-delete]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/orders/${button.dataset.orderDelete}`, { method: "DELETE" });
        resetOrderForm();
        await loadOrders();
        showAlert("訂單已刪除。", false);
      });
    });
  });
}

function openOrderDetail(order) {
  const finalAmount = orderFinalAmount(order);
  const baseAmount = Number(order.baseAmount || 0) || finalAmount;
  openLogDrawer({
    title: `訂單 #${order.id}`,
    eyebrow: "訂單詳情",
    content: `
      <div class="log-drawer-section">
        ${recordDetail("日期", order.orderDate)}
        ${recordDetail("單號", order.orderNo)}
        ${recordDetail("類型", orderTypeText(order.orderType))}
        ${recordDetail("老闆", order.ownerNickname)}
        ${recordDetail("狀態", label("orderStatus", order.status))}
        ${recordDetail("客戶收款", label("customerPaymentStatus", order.customerPaymentStatus))}
      </div>
      <h3>金額組成</h3>
      <div class="log-drawer-section">
        ${recordDetail("基礎價格", money.format(baseAmount))}
        ${recordDetail("指定陪陪費", money.format(order.designatedFee || 0))}
        ${recordDetail("帶朋友費", money.format(order.friendFee || 0))}
        ${recordDetail("換人費", money.format(order.replacementFee || 0))}
        ${recordDetail("深夜加價", money.format(order.nightFee || 0))}
        ${recordDetail("其他加價", money.format(order.otherFee || 0))}
        ${recordDetail("活動折扣", `-${money.format(order.discountAmount || 0)}`)}
        ${recordDetail("最終實收", money.format(finalAmount))}
        ${recordDetail("套用活動", order.activityNameSnapshot || "無")}
        ${recordDetail("活動折扣方式", order.activityNameSnapshot ? activityDiscountText({
          discountType: order.activityDiscountType,
          discountValue: order.activityDiscountValue
        }) : "")}
        ${recordDetail("活動包含加價", order.activityNameSnapshot ? (order.activityIncludeFees ? "是" : "否") : "")}
        ${recordDetail("團抽", money.format(order.commissionAmount || 0))}
        ${recordDetail("可分配", money.format(finalAmount - Number(order.commissionAmount || 0)))}
      </div>
      <h3>陪陪分配</h3>
      <div class="log-drawer-section">
        ${(order.members || []).map((member) => recordDetail(member.nickname || `會員${member.userId}`, money.format(member.shareAmount || 0))).join("") || recordDetail("分配", "無")}
      </div>
      <h3>備註</h3>
      <pre class="record-json">${escapeHtml(order.remark || "")}</pre>
    `
  });
}

function orderFinalAmount(order) {
  return Number(order?.finalAmount || order?.amount || 0);
}

function renderPayments() {
  const body = document.getElementById("paymentRows");
  body.innerHTML = state.payments.length ? state.payments.map((payment) => `
    <tr>
      <td>${payment.id}</td>
      <td>${payment.payMonth}</td>
      <td>${escapeHtml(payment.nickname)}</td>
      <td>${money.format(payment.expectedAmount)}</td>
      <td>${payment.actualAmount == null ? plainText("未設定", "muted") : money.format(payment.actualAmount)}</td>
      <td>${paymentStatusPill(payment.paymentStatus)}</td>
      <td>
        <div class="table-actions">
          ${payment.paymentStatus === "paid"
            ? plainText("已發薪", "good")
            : `<button class="ghost small" data-payment-paid="${payment.id}">標記已發</button>`}
          <button class="ghost small" data-payment-attachments="${payment.id}">附件</button>
        </div>
      </td>
    </tr>
  `).join("") : emptyRow(7);

  body.querySelectorAll("[data-payment-paid]").forEach((button) => {
    button.addEventListener("click", async () => {
      await api(`/api/payments/${button.dataset.paymentPaid}/mark-paid`, { method: "POST", body: "{}" });
      await loadPayments();
    });
  });

  body.querySelectorAll("[data-payment-attachments]").forEach((button) => {
    button.addEventListener("click", () => {
      openAttachmentModal("月結附件", "payments", Number(button.dataset.paymentAttachments), {
        attachmentKind: "payment_proof",
        uploadLabel: "新增付款證明",
        canEdit: hasPermission("Settlement.Close")
      });
    });
  });

}

function renderAuditLogs(rows) {
  const body = document.getElementById("auditRows");
  ensureAuditHeader(body);
  body.innerHTML = rows.length ? rows.map((log) => `
    <tr>
      <td>${formatDateTime(log.createdAt)}</td>
      <td>${escapeHtml(log.loginUserDisplayName || "系統")}</td>
      <td>${escapeHtml(label("targetType", log.targetType))}</td>
      <td>${escapeHtml(label("auditAction", log.action))}</td>
      <td>${escapeHtml(auditTargetText(log))}</td>
      <td>${escapeHtml(auditNote(log) || auditSummary(log))}</td>
      <td><button class="ghost small" data-audit-detail="${log.id}" type="button">詳細</button></td>
    </tr>
  `).join("") : emptyRow(7);

  body.querySelectorAll("[data-audit-detail]").forEach((button) => {
    button.addEventListener("click", () => {
      const log = rows.find((item) => item.id === Number(button.dataset.auditDetail));
      if (log) {
        openAuditLogDetail(log);
      }
    });
  });
}

function auditNote(log) {
  for (const json of [log.afterJson, log.beforeJson]) {
    if (!json) {
      continue;
    }

    try {
      const data = JSON.parse(json);
      const note = data?.remark ?? data?.Remark ?? data?.note ?? data?.Note;
      if (note != null && String(note).trim()) {
        return String(note).trim();
      }
    } catch {
      // Older audit rows may contain non-JSON text.
    }
  }

  return "";
}

function ensureAuditHeader(body) {
  const row = body.closest("table")?.querySelector("thead tr");
  if (row && row.children.length === 4) {
    const th = document.createElement("th");
    th.textContent = "操作者";
    row.insertBefore(th, row.children[1]);
  }
  if (row && row.children.length !== 7) {
    row.querySelectorAll("th").forEach((cell) => cell.remove());
    ["時間", "操作者", "功能", "動作", "目標", "摘要", "詳細"].forEach((text) => {
      const th = document.createElement("th");
      th.textContent = text;
      row.appendChild(th);
    });
  }
}

function renderMoneyLogs(rows) {
  const body = document.getElementById("moneyLogRows");
  const canReverse = hasAnyPermission(["Settlement.Close", "Account.Manage"]);
  body.innerHTML = rows.length ? rows.map((log) => `
    <tr>
      <td>${formatDateTime(log.createdAt)}</td>
      <td>${escapeHtml(log.memberNickname || `會員${log.userId}`)}</td>
      <td>${escapeHtml(label("moneyLogType", log.type))}</td>
      <td class="${log.amount < 0 ? "amount-negative" : "amount-positive"}">${formatSignedMoney(log.amount)}</td>
      <td>${money.format(log.balanceAfter)}</td>
      <td>${escapeHtml(moneyLogSourceText(log))}</td>
      <td>${escapeHtml(log.note || "")}</td>
      <td>
        <div class="table-actions">
          <button class="ghost small" data-money-detail="${log.id}" type="button">詳細</button>
          ${canReverse && !log.isReversal && !rows.some((item) => item.reversedMoneyLogId === log.id)
            ? `<button class="ghost small danger" data-money-reverse="${log.id}" type="button">沖正</button>`
            : ""}
        </div>
      </td>
    </tr>
  `).join("") : emptyRow(8);
  applyActionPermissions();

  body.querySelectorAll("[data-money-detail]").forEach((button) => {
    button.addEventListener("click", () => {
      const log = rows.find((item) => item.id === Number(button.dataset.moneyDetail));
      if (log) {
        openMoneyLogDetail(log);
      }
    });
  });

  body.querySelectorAll("[data-money-reverse]").forEach((button) => {
    button.addEventListener("click", async () => {
      const log = rows.find((item) => item.id === Number(button.dataset.moneyReverse));
      if (!log || !window.confirm(`確定要沖正金流紀錄 #${log.id}？系統會新增一筆反向金額紀錄，不會修改原紀錄。`)) {
        return;
      }

      await runAction(async () => {
        await api(`/api/moneylogs/${log.id}/reverse`, {
          method: "POST",
          body: JSON.stringify({ note: `沖正金流紀錄 #${log.id}` })
        });
        await loadMoneyLogs();
        showAlert("金流紀錄已沖正。", false);
      });
    });
  });
}

function renderLoginHistories(rows) {
  const body = document.getElementById("loginHistoryRows");
  if (!body) {
    return;
  }

  body.innerHTML = rows.length ? rows.map((row) => `
    <tr>
      <td>${formatDateTime(row.createdAt)}</td>
      <td>${escapeHtml(row.loginUserDisplayName || row.loginAccount || `#${row.loginUserId}`)}</td>
      <td>${escapeHtml(label("loginHistoryAction", row.action))}</td>
      <td>${escapeHtml(label("loginHistoryMethod", row.method))}</td>
      <td>${escapeHtml(row.ipAddress || "")}</td>
      <td class="truncate-cell" title="${escapeHtml(row.userAgent || "")}">${escapeHtml(row.userAgent || "")}</td>
    </tr>
  `).join("") : emptyRow(6);
}

function auditTargetText(log) {
  const target = label("targetType", log.targetType);
  const id = log.targetId ? ` #${log.targetId}` : "";
  return `${target}${id}`;
}

function auditSummary(log) {
  const data = parseJson(log.afterJson) || parseJson(log.beforeJson);
  if (!data || typeof data !== "object") {
    return "";
  }

  return data.nickname || data.displayName || data.loginAccount || data.orderNo || data.name || data.type || "";
}

function openAuditLogDetail(log) {
  openRecordModal({
    title: `操作紀錄 #${log.id}`,
    eyebrow: "Audit Log",
    content: `
      <div class="record-modal-content">
        <div class="record-detail-grid">
          ${recordDetail("時間", formatDateTime(log.createdAt))}
          ${recordDetail("操作者", log.loginUserDisplayName || "系統")}
          ${recordDetail("功能", label("targetType", log.targetType))}
          ${recordDetail("動作", label("auditAction", log.action))}
          ${recordDetail("目標", auditTargetText(log))}
          ${recordDetail("關聯編號", log.correlationId)}
          ${recordDetail("批次編號", log.batchUuid)}
          ${recordDetail("IP 位址", log.ipAddress)}
        </div>
        <h3>變更前資料</h3>
        <pre class="record-json">${escapeHtml(formatJson(log.beforeJson))}</pre>
        <h3>變更後資料</h3>
        <pre class="record-json">${escapeHtml(formatJson(log.afterJson))}</pre>
      </div>
    `
  });
}

function openMoneyLogDetail(log) {
  openRecordModal({
    title: `金流紀錄 #${log.id}`,
    eyebrow: "Money Log",
    content: `
      <div class="record-modal-content">
        <div class="record-detail-grid">
          ${recordDetail("時間", formatDateTime(log.createdAt))}
          ${recordDetail("會員", log.memberNickname || `會員${log.userId}`)}
          ${recordDetail("類型", label("moneyLogType", log.type))}
          ${recordDetail("金額", formatSignedMoney(log.amount))}
          ${recordDetail("餘額", money.format(log.balanceAfter))}
          ${recordDetail("操作紀錄編號", log.auditLogId)}
          ${recordDetail("來源類型", log.sourceType)}
          ${recordDetail("來源編號", log.sourceId)}
          ${recordDetail("沖正原紀錄編號", log.reversedMoneyLogId)}
          ${recordDetail("是否為沖正", log.isReversal ? "是" : "否")}
          ${recordDetail("關聯編號", log.correlationId)}
          ${recordDetail("備註", log.note)}
        </div>
      </div>
    `
  });
}

function moneyLogSourceText(log) {
  const source = label("moneyLogSource", log.sourceType);
  if (!source) {
    return "-";
  }

  return log.sourceId ? `${source} #${log.sourceId}` : source;
}

function formatSignedMoney(value) {
  const amount = Number(value || 0);
  const prefix = amount > 0 ? "+" : amount < 0 ? "-" : "";
  return `${prefix}${money.format(Math.abs(amount))}`;
}

function parseJson(value) {
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

function formatJson(value) {
  const parsed = parseJson(value);
  return parsed ? JSON.stringify(parsed, null, 2) : (value || "-");
}

function renderAuditLogs(rows) {
  const body = document.getElementById("auditRows");
  updateLogTypeOptions("audit", rows, (log) => log.targetType, (value) => label("targetType", value));
  const filteredRows = filterLogRows("audit", rows, {
    date: (log) => log.createdAt,
    account: (log) => log.loginUserDisplayName || log.loginAccount || "",
    keyword: (log) => [
      log.targetType,
      label("targetType", log.targetType),
      log.action,
      label("auditAction", log.action),
      auditTargetText(log),
      auditNote(log),
      auditSummary(log),
      log.ipAddress,
      log.deviceInfo,
      log.sessionId
    ].join(" "),
    type: (log) => log.targetType
  });
  renderAuditKpis(rows);
  body.innerHTML = filteredRows.length ? filteredRows.map((log) => `
    <tr class="log-row" data-log-row="audit" data-log-id="${log.id}">
      <td>${formatDateTime(log.createdAt)}</td>
      <td>${escapeHtml(log.loginUserDisplayName || log.loginAccount || "系統")}</td>
      <td>${escapeHtml(label("targetType", log.targetType))}</td>
      <td>${escapeHtml(label("auditAction", log.action))}</td>
      <td>${escapeHtml(auditTargetText(log))}</td>
      <td>${escapeHtml(auditNote(log) || auditSummary(log))}</td>
      <td>${escapeHtml(log.ipAddress || "")}</td>
      <td>
        <div class="table-actions">
          <button class="ghost small" data-audit-detail-new="${log.id}" type="button">詳情</button>
          <button class="ghost small" data-audit-attachments="${log.id}" type="button">附件</button>
        </div>
      </td>
    </tr>
  `).join("") : emptyRow(8);

  bindLogRowDetails(body, filteredRows, "audit", openAuditLogDetail);

  body.querySelectorAll("[data-audit-attachments]").forEach((button) => {
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      openAttachmentModal("操作紀錄附件", "audit_logs", Number(button.dataset.auditAttachments), {
        attachmentKind: "audit_evidence",
        uploadLabel: "新增稽核附件",
        canEdit: hasAnyPermission(["Account.Manage", "Settlement.Close"])
      });
    });
  });
}

function renderServiceBranchHead() {
  const head = document.getElementById("serviceBranchHead");
  if (!head) {
    return;
  }

  const rows = state.serviceItems.filter((item) => item.category === state.serviceCategory && item.isActive);
  head.innerHTML = `
    <div>
      <span>目前大類</span>
      <strong>${escapeHtml(serviceCategoryText(state.serviceCategory))}</strong>
    </div>
    <em>${money.format(rows.length)} 個項目</em>
  `;
}

function renderMoneyLogs(rows) {
  const body = document.getElementById("moneyLogRows");
  const canReverse = hasAnyPermission(["Settlement.Close", "Account.Manage"]);
  updateLogTypeOptions("money", rows, (log) => log.type, (value) => label("moneyLogType", value));
  const filteredRows = filterLogRows("money", rows, {
    date: (log) => log.createdAt,
    account: (log) => log.memberNickname || `會員${log.userId}`,
    keyword: (log) => [
      moneyLogSourceText(log),
      log.note,
      log.amount,
      log.balanceBefore,
      log.balanceAfter,
      log.status,
      log.operatorDisplayName,
      log.operatorLoginAccount
    ].join(" "),
    type: (log) => log.type
  });
  renderMoneyKpis(rows);
  body.innerHTML = filteredRows.length ? filteredRows.map((log) => `
    <tr class="log-row" data-log-row="money" data-log-id="${log.id}">
      <td>${formatDateTime(log.createdAt)}</td>
      <td>${escapeHtml(log.memberNickname || `會員${log.userId}`)}</td>
      <td>${escapeHtml(label("moneyLogType", log.type))}</td>
      <td class="${log.amount < 0 ? "amount-negative" : "amount-positive"}">${formatSignedMoney(log.amount)}</td>
      <td>${money.format(log.balanceAfter)}</td>
      <td>${escapeHtml(moneyLogSourceText(log))}</td>
      <td>${escapeHtml(log.note || "")}</td>
      <td>
        <div class="table-actions">
          <button class="ghost small" data-money-detail-new="${log.id}" type="button">詳情</button>
          <button class="ghost small" data-money-attachments="${log.id}" type="button">附件</button>
          ${canReverse && !log.isReversal && !rows.some((item) => item.reversedMoneyLogId === log.id)
            ? `<button class="ghost small danger" data-money-reverse="${log.id}" type="button">沖正</button>`
            : ""}
        </div>
      </td>
    </tr>
  `).join("") : emptyRow(8);
  applyActionPermissions();
  bindLogRowDetails(body, filteredRows, "money", openMoneyLogDetail);

  body.querySelectorAll("[data-money-attachments]").forEach((button) => {
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      openAttachmentModal("金流紀錄附件", "money_logs", Number(button.dataset.moneyAttachments), {
        attachmentKind: "money_proof",
        uploadLabel: "新增金流依據",
        canEdit: hasAnyPermission(["Settlement.Close", "Account.Manage"])
      });
    });
  });

  body.querySelectorAll("[data-money-reverse]").forEach((button) => {
    button.addEventListener("click", async (event) => {
      event.stopPropagation();
      const log = rows.find((item) => item.id === Number(button.dataset.moneyReverse));
      if (!log || !window.confirm(`確定要沖正金流紀錄 #${log.id}？系統會新增一筆反向金額紀錄，不會修改原紀錄。`)) {
        return;
      }

      await runAction(async () => {
        await api(`/api/moneylogs/${log.id}/reverse`, {
          method: "POST",
          body: JSON.stringify({ note: `沖正金流紀錄 #${log.id}` })
        });
        await loadMoneyLogs();
        showAlert("金流紀錄已沖正。", false);
      });
    });
  });
}

function renderLoginHistories(rows) {
  const body = document.getElementById("loginHistoryRows");
  if (!body) {
    return;
  }

  updateLogTypeOptions("login", rows, (row) => row.method, (value) => label("loginHistoryMethod", value));
  const filteredRows = filterLogRows("login", rows, {
    date: (row) => row.createdAt,
    account: (row) => row.loginUserDisplayName || row.loginAccount || "",
    keyword: (row) => [
      row.ipAddress,
      row.userAgent,
      row.deviceInfo,
      row.sessionId,
      row.failureReason,
      loginRegion(row),
      row.action,
      row.method
    ].join(" "),
    type: (row) => row.method
  });
  renderLoginKpis(rows);
  body.innerHTML = filteredRows.length ? filteredRows.map((row) => `
    <tr class="log-row" data-log-row="login" data-log-id="${row.id}">
      <td>${formatDateTime(row.createdAt)}</td>
      <td>${escapeHtml(row.loginUserDisplayName || row.loginAccount || `#${row.loginUserId}`)}</td>
      <td>${plainText(label("loginHistoryAction", row.action), row.succeeded ? "good" : "bad")}</td>
      <td>${escapeHtml(label("loginHistoryMethod", row.method))}</td>
      <td>${escapeHtml(row.ipAddress || "")}</td>
      <td class="truncate-cell" title="${escapeHtml(row.userAgent || "")}">${escapeHtml(shortUserAgent(row.userAgent))}</td>
      <td>${escapeHtml(loginRegion(row))}</td>
      <td><button class="ghost small" data-login-detail-new="${row.id}" type="button">詳情</button></td>
    </tr>
  `).join("") : emptyRow(8);

  bindLogRowDetails(body, filteredRows, "login", openLoginHistoryDetail);
}

function bindLogRowDetails(body, rows, kind, openDetail) {
  body.querySelectorAll(`[data-log-row="${kind}"], [data-${kind === "audit" ? "audit" : kind === "money" ? "money" : "login"}-detail-new]`).forEach((element) => {
    element.addEventListener("click", (event) => {
      const action = event.target.closest("button");
      if (action && !action.matches(`[data-${kind === "audit" ? "audit" : kind === "money" ? "money" : "login"}-detail-new]`)) {
        return;
      }

      event.stopPropagation();
      const id = Number(element.dataset.logId || element.dataset.auditDetailNew || element.dataset.moneyDetailNew || element.dataset.loginDetailNew);
      const row = rows.find((item) => item.id === id);
      if (row) {
        openDetail(row);
      }
    });
  });
}

function filterLogRows(kind, rows, readers) {
  const filter = state.logFilters[kind] || {};
  const date = filter.date;
  const account = (filter.account || "").trim().toLowerCase();
  const keyword = (filter.keyword || "").trim().toLowerCase();
  const type = filter.type || "";
  const sorted = rows.filter((row) => {
    const rowDate = dateValue(readers.date(row));
    const accountText = String(readers.account(row) || "").toLowerCase();
    const keywordText = String(readers.keyword(row) || "").toLowerCase();
    const rowType = String(readers.type(row) || "");
    return (!date || rowDate === date) &&
      (!account || accountText.includes(account)) &&
      (!keyword || keywordText.includes(keyword)) &&
      (!type || rowType === type);
  });

  sorted.sort((a, b) => {
    const left = new Date(readers.date(a)).getTime();
    const right = new Date(readers.date(b)).getTime();
    return filter.sort === "asc" ? left - right : right - left;
  });
  return sorted;
}

function updateLogTypeOptions(kind, rows, getValue, getLabel) {
  const select = document.querySelector(`[data-log-filter="${kind}"] [data-filter-field="type"]`);
  if (!select) {
    return;
  }

  const current = select.value;
  const options = [...new Set(rows.map(getValue).filter(Boolean))]
    .sort((a, b) => String(getLabel(a)).localeCompare(String(getLabel(b)), "zh-Hant"));
  select.innerHTML = `<option value="">全部</option>` + options.map((value) =>
    `<option value="${escapeHtml(value)}">${escapeHtml(getLabel(value))}</option>`
  ).join("");
  select.value = options.includes(current) ? current : "";
}

function renderAuditKpis(rows) {
  renderKpis("auditKpis", [
    ["今日操作", rows.filter((row) => isToday(row.createdAt)).length],
    ["本月操作", rows.filter((row) => isThisMonth(row.createdAt)).length],
    ["異常操作", rows.filter((row) => ["delete", "cancel", "reverse", "deactivate"].includes(row.action)).length]
  ]);
}

function renderMoneyKpis(rows) {
  const todayRows = rows.filter((row) => isToday(row.createdAt));
  const monthRows = rows.filter((row) => isThisMonth(row.createdAt));
  renderKpis("moneyKpis", [
    ["今日收入", money.format(sumBy(todayRows.filter((row) => row.amount > 0), (row) => row.amount))],
    ["今日支出", money.format(Math.abs(sumBy(todayRows.filter((row) => row.amount < 0), (row) => row.amount)))],
    ["目前餘額", money.format(currentBalance(rows))],
    ["本月淨收入", formatSignedMoney(sumBy(monthRows, (row) => row.amount))]
  ]);
}

function renderLoginKpis(rows) {
  renderKpis("loginKpis", [
    ["今日登入", rows.filter((row) => isToday(row.createdAt) && row.action === "login").length],
    ["失敗登入", rows.filter((row) => row.succeeded === false).length],
    ["Discord登入", rows.filter((row) => row.method === "discord").length],
    ["一般登入", rows.filter((row) => row.method === "password").length]
  ]);
}

function renderKpis(id, items) {
  const wrap = document.getElementById(id);
  if (!wrap) {
    return;
  }

  wrap.innerHTML = items.map(([labelText, value]) => `
    <article class="log-kpi-card">
      <strong>${escapeHtml(String(value))}</strong>
      <span>${escapeHtml(labelText)}</span>
    </article>
  `).join("");
}

function openAuditLogDetail(log) {
  openLogDrawer({
    title: `操作紀錄 #${log.id}`,
    eyebrow: "操作資訊",
    content: `
      <div class="log-drawer-section">
        ${recordDetail("時間", formatDateTime(log.createdAt))}
        ${recordDetail("操作者", log.loginUserDisplayName || log.loginAccount || "系統")}
        ${recordDetail("功能", label("targetType", log.targetType))}
        ${recordDetail("動作", label("auditAction", log.action))}
        ${recordDetail("目標", auditTargetText(log))}
        ${recordDetail("摘要", auditNote(log) || auditSummary(log))}
        ${recordDetail("IP 位址", log.ipAddress)}
        ${recordDetail("裝置", log.deviceInfo)}
        ${recordDetail("工作階段編號", log.sessionId)}
        ${recordDetail("瀏覽器資訊", log.userAgent)}
        ${recordDetail("目標唯一識別碼", log.targetUuid)}
        ${recordDetail("關聯編號", log.correlationId)}
        ${recordDetail("批次編號", log.batchUuid)}
      </div>
      <h3>變更前資料</h3>
      <pre class="record-json">${escapeHtml(formatJson(log.beforeJson))}</pre>
      <h3>變更後資料</h3>
      <pre class="record-json">${escapeHtml(formatJson(log.afterJson))}</pre>
    `
  });
}

function openMoneyLogDetail(log) {
  openLogDrawer({
    title: `金流紀錄 #${log.id}`,
    eyebrow: "金流資訊",
    content: `
      <div class="log-drawer-section">
        ${recordDetail("時間", formatDateTime(log.createdAt))}
        ${recordDetail("會員", log.memberNickname || `會員${log.userId}`)}
        ${recordDetail("操作者", log.operatorDisplayName || log.operatorLoginAccount || "系統")}
        ${recordDetail("類型", label("moneyLogType", log.type))}
        ${recordDetail("狀態", log.status || "completed")}
        ${recordDetail("金額", formatSignedMoney(log.amount))}
        ${recordDetail("交易前餘額", money.format(log.balanceBefore ?? 0))}
        ${recordDetail("餘額", money.format(log.balanceAfter))}
        ${recordDetail("來源", moneyLogSourceText(log))}
        ${recordDetail("備註", log.note)}
        ${recordDetail("來源唯一識別碼", log.sourceUuid)}
        ${recordDetail("操作紀錄編號", log.auditLogId)}
        ${recordDetail("沖正原紀錄編號", log.reversedMoneyLogId)}
        ${recordDetail("關聯編號", log.correlationId)}
      </div>
    `
  });
}

function openLoginHistoryDetail(row) {
  openLogDrawer({
    title: `登入紀錄 #${row.id}`,
    eyebrow: "登入資訊",
    content: `
      <div class="log-drawer-section">
        ${recordDetail("時間", formatDateTime(row.createdAt))}
        ${recordDetail("帳號", row.loginUserDisplayName || row.loginAccount || `#${row.loginUserId}`)}
        ${recordDetail("動作", label("loginHistoryAction", row.action))}
        ${recordDetail("登入方式", label("loginHistoryMethod", row.method))}
        ${recordDetail("狀態", row.succeeded ? "成功" : "失敗")}
        ${recordDetail("失敗原因", row.failureReason)}
        ${recordDetail("IP 位址", row.ipAddress)}
        ${recordDetail("裝置", row.deviceInfo)}
        ${recordDetail("地區", loginRegion(row))}
        ${recordDetail("工作階段編號", row.sessionId)}
        ${recordDetail("登出時間", row.loggedOutAt ? formatDateTime(row.loggedOutAt) : "")}
        ${recordDetail("在線時間", formatDuration(row.durationSeconds))}
        ${recordDetail("瀏覽器資訊", row.userAgent)}
      </div>
    `
  });
}

function currentBalance(rows) {
  const latestByUser = new Map();
  rows.forEach((row) => {
    const current = latestByUser.get(row.userId);
    if (!current || new Date(row.createdAt) > new Date(current.createdAt)) {
      latestByUser.set(row.userId, row);
    }
  });
  return sumBy([...latestByUser.values()], (row) => row.balanceAfter);
}

function formatDuration(seconds) {
  if (seconds == null || seconds === "") {
    return "";
  }

  const total = Math.max(0, Number(seconds) || 0);
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const remainSeconds = Math.floor(total % 60);
  return [
    hours ? `${hours} 小時` : "",
    minutes ? `${minutes} 分` : "",
    !hours && !minutes ? `${remainSeconds} 秒` : ""
  ].filter(Boolean).join(" ");
}

function sumBy(rows, getValue) {
  return rows.reduce((total, row) => total + Number(getValue(row) || 0), 0);
}

function isToday(value) {
  return dateValue(value) === dateValue(new Date());
}

function isThisMonth(value) {
  const date = new Date(value);
  const now = new Date();
  return date.getFullYear() === now.getFullYear() && date.getMonth() === now.getMonth();
}

function dateValue(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

function shortUserAgent(value) {
  if (!value) {
    return "";
  }

  return String(value)
    .replace(/\s+/g, " ")
    .replace(/Mozilla\/5\.0\s*/i, "")
    .slice(0, 80);
}

function loginRegion(row) {
  return row.region || row.country || row.location || "-";
}

function renderSelects() {
  const departmentSelect = document.getElementById("departmentSelect");
  if (departmentSelect) {
    const currentValue = departmentSelect.value;
    departmentSelect.innerHTML = state.departments.filter((department) => department.isActive).map((department) =>
      `<option value="${department.id}">${escapeHtml(department.name)}</option>`
    ).join("");
    if (currentValue) {
      departmentSelect.value = currentValue;
    }
  }

  refreshMemberPickerFields();
}

function addMemberRow(member = null) {
  const wrap = document.getElementById("memberRows");
  const row = document.createElement("div");
  row.className = "member-row";
  row.innerHTML = `
    <label class="member-field member-user">團員
      <span class="member-picker-field" data-member-picker data-source="players" data-title="選擇分潤團員" data-required="true">
        <input data-member-select type="hidden">
        <button class="member-picker-trigger" type="button" data-member-picker-trigger>請選擇團員</button>
      </span>
    </label>
    <label class="member-field member-share">分潤<input data-member-share type="number" step="0.01" min="0" placeholder="0"></label>
    <button class="icon-btn member-remove" type="button" title="移除">×</button>
  `;
  row.querySelector(".member-remove").addEventListener("click", () => {
    row.remove();
    updateOrderCalc();
  });
  row.addEventListener("input", updateOrderCalc);
  wrap.appendChild(row);
  if (member) {
    setMemberPickerValue(row.querySelector("[data-member-select]"), member.userId);
    row.querySelector("[data-member-share]").value = member.shareAmount;
  } else {
    refreshMemberPickerFields(row);
  }
  updateOrderCalc();
}

async function submitUser(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const userId = data.get("userId");
  const isEdit = Boolean(userId);
  const existingUser = isEdit ? state.users.find((user) => user.id === Number(userId)) : null;

  await runAction(async () => {
    if ((state.auth?.user?.systemRole === "admin" || isBootstrapMode()) && state.organizations.length === 0) {
      await loadOrganizations();
    }

    const selectedOrganizationId = Number(form.elements.organizationId?.value) ||
      existingUser?.organizationId ||
      currentOrganizationId();

    if (!selectedOrganizationId) {
      showAlert("請先建立或選擇有效的組織。");
      return;
    }

    const payload = {
      organizationId: selectedOrganizationId,
      nickname: data.get("nickname"),
      bankAccount: emptyToNull(data.get("bankAccount")),
      isPlayer: data.get("isPlayer") === "on",
      isBoss: data.get("isBoss") === "on"
    };

    if (isEdit) {
      payload.isActive = existingUser?.isActive ?? true;
      payload.leftAt = existingUser?.leftAt ?? null;
    }

    await api(isEdit ? `/api/users/${userId}` : "/api/users", {
      method: isEdit ? "PUT" : "POST",
      body: JSON.stringify(payload)
    });
    resetUserForm();
    await loadUsers();
    showAlert(isEdit ? "成員已更新。" : "成員已新增。", false);
  });
}

async function submitLoginUser(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const loginUserId = data.get("loginUserId");
  const isEdit = Boolean(loginUserId);
  const initialPassword = String(data.get("password") || "");
  const confirmPassword = String(data.get("confirmPassword") || "");
  const existingLoginUser = isEdit
    ? state.loginUsers.find((loginUser) => loginUser.id === Number(loginUserId))
    : null;

  if (!isEdit && initialPassword !== confirmPassword) {
    showAlert("初始密碼與確認密碼不一致。");
    form.elements.confirmPassword.focus();
    return;
  }

  await runAction(async () => {
    if ((state.auth?.user?.systemRole === "admin" || isBootstrapMode()) && state.organizations.length === 0) {
      await loadOrganizations();
    }

    const selectedOrganizationId = Number(form.elements.organizationId?.value) ||
      existingLoginUser?.organizationId ||
      currentOrganizationId();

    if (!selectedOrganizationId) {
      showAlert("請先建立或選擇有效的組織。");
      return;
    }

    const payload = {
      displayName: data.get("nickname"),
      loginAccount: data.get("loginAccount"),
      systemRole: data.get("systemRole"),
      organizationId: selectedOrganizationId,
      userId: Number(data.get("userId")) || null
    };

    if (isEdit) {
      payload.isActive = existingLoginUser?.isActive ?? true;
    } else {
      payload.password = initialPassword;
      payload.confirmPassword = confirmPassword;
    }

    await api(isEdit ? `/api/loginusers/${loginUserId}` : "/api/loginusers", {
      method: isEdit ? "PUT" : "POST",
      body: JSON.stringify(payload)
    });
    resetLoginUserForm();
    await loadLoginUsers();
    showAlert(isEdit ? "帳號已更新。" : "帳號已新增。", false);
  });
}

function startLoginUserEdit(loginUser) {
  const form = document.getElementById("loginUserForm");
  form.elements.loginUserId.value = loginUser.id;
  form.elements.nickname.value = loginUser.displayName || "";
  form.elements.loginAccount.value = loginUser.loginAccount || "";
  form.elements.systemRole.value = loginUser.systemRole || "staff";
  if (form.elements.organizationId) {
    form.elements.organizationId.value = loginUser.organizationId || "";
    renderLoginUserMemberSelect();
  }
  if (form.elements.userId) {
    form.elements.userId.value = loginUser.userId || "";
    refreshMemberPickerFields(form);
  }
  setLoginUserEditMode(true);
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

function resetLoginUserForm() {
  const form = document.getElementById("loginUserForm");
  form.reset();
  form.elements.loginUserId.value = "";
  form.elements.systemRole.value = "admin";
  setLoginUserEditMode(false);
  refreshMemberPickerFields(form);
}

function setLoginUserEditMode(isEdit) {
  const title = document.getElementById("loginUserFormTitle");
  const submitButton = document.getElementById("loginUserSubmitBtn");
  const cancelButton = document.getElementById("cancelLoginUserEditBtn");
  const passwordInput = document.getElementById("loginUserForm").elements.password;
  const confirmPasswordInput = document.getElementById("loginUserForm").elements.confirmPassword;
  const passwordField = document.getElementById("loginUserPasswordField");
  const confirmPasswordField = document.getElementById("loginUserConfirmPasswordField");

  if (title) {
    title.textContent = isEdit ? "編輯帳號" : "新增帳號";
  }
  if (submitButton) {
    submitButton.textContent = isEdit ? "儲存" : "新增帳號";
  }
  if (cancelButton) {
    cancelButton.hidden = !isEdit;
  }
  passwordInput.required = !isEdit;
  confirmPasswordInput.required = !isEdit;
  passwordInput.value = "";
  confirmPasswordInput.value = "";
  passwordField.hidden = isEdit;
  confirmPasswordField.hidden = isEdit;
}

function startUserEdit(user) {
  const form = document.getElementById("userForm");
  form.elements.userId.value = user.id;
  form.elements.nickname.value = user.nickname || "";
  if (form.elements.organizationId) {
    form.elements.organizationId.value = user.organizationId || "";
  }
  form.elements.bankAccount.value = user.bankAccount || "";
  form.elements.isPlayer.checked = Boolean(user.isPlayer);
  form.elements.isBoss.checked = Boolean(user.isBoss);
  document.getElementById("userFormTitle").textContent = "編輯成員";
  document.getElementById("userSubmitBtn").textContent = "儲存";
  document.getElementById("cancelUserEditBtn").hidden = false;
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

function resetUserForm() {
  const form = document.getElementById("userForm");
  form.reset();
  form.elements.userId.value = "";
  renderUserOrganizationSelect();
  form.elements.isPlayer.checked = true;
  document.getElementById("userFormTitle").textContent = "新增成員";
  document.getElementById("userSubmitBtn").textContent = "新增";
  document.getElementById("cancelUserEditBtn").hidden = true;
}

async function submitDepartment(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const departmentId = data.get("departmentId");
  const isEdit = Boolean(departmentId);

  await runAction(async () => {
    const payload = {
      name: data.get("name"),
      englishName: emptyToNull(data.get("englishName")),
      description: emptyToNull(data.get("description")),
      sortOrder: Number(data.get("sortOrder") || 0),
      isActive: data.get("isActive") === "on"
    };

    if (!isEdit) {
      delete payload.isActive;
    }

    await api(isEdit ? `/api/departments/${departmentId}` : "/api/departments", {
      method: isEdit ? "PUT" : "POST",
      body: JSON.stringify(payload)
    });

    resetDepartmentForm();
    await loadDepartments();
    showAlert(isEdit ? "部門已更新。" : "部門已新增。", false);
  });
}

async function submitDepartmentMember(event) {
  event.preventDefault();
  const form = event.currentTarget;
  if (!validateRequiredMemberPickers(form)) {
    return;
  }
  const data = new FormData(form);
  const departmentId = data.get("departmentId");
  const memberId = data.get("memberId");
  const isEdit = Boolean(memberId);

  await runAction(async () => {
    await api(isEdit ? `/api/departments/members/${memberId}` : `/api/departments/${departmentId}/members`, {
      method: isEdit ? "PUT" : "POST",
      body: JSON.stringify({
        ...(isEdit ? {} : { userId: Number(data.get("userId")) }),
        positionTitle: emptyToNull(data.get("positionTitle")),
        isManager: data.get("isManager") === "on",
        ...(isEdit ? { leftAt: null } : {})
      })
    });

    resetDepartmentMemberForm();
    await loadDepartments();
    showAlert(isEdit ? "部門成員已更新。" : "部門成員已加入。", false);
  });
}

function startDepartmentMemberEdit(member) {
  closeDepartmentModal();
  activateOrganizationTab("member");
  const form = document.getElementById("departmentMemberForm");
  form.elements.memberId.value = member.id;
  form.elements.departmentId.value = member.departmentId;
  form.elements.userId.value = member.userId;
  refreshMemberPickerFields(form);
  form.elements.departmentId.disabled = true;
  form.querySelector("[data-member-picker-trigger]").disabled = true;
  form.elements.positionTitle.value = member.positionTitle || "";
  form.elements.isManager.checked = Boolean(member.isManager);
  document.getElementById("departmentMemberSubmitBtn").textContent = "更新";
  document.getElementById("cancelDepartmentMemberEditBtn").hidden = false;
  form.scrollIntoView({ behavior: "smooth", block: "nearest" });
}

function resetDepartmentMemberForm() {
  const form = document.getElementById("departmentMemberForm");
  form.reset();
  form.elements.memberId.value = "";
  form.elements.departmentId.disabled = false;
  form.querySelector("[data-member-picker-trigger]").disabled = false;
  document.getElementById("departmentMemberSubmitBtn").textContent = "加入 / 更新";
  document.getElementById("cancelDepartmentMemberEditBtn").hidden = true;
  renderSelects();
}

function startDepartmentEdit(department) {
  activateOrganizationTab("department");
  const form = document.getElementById("departmentForm");
  form.elements.departmentId.value = department.id;
  form.elements.name.value = department.name;
  form.elements.englishName.value = department.englishName || "";
  form.elements.sortOrder.value = department.sortOrder || 0;
  form.elements.description.value = department.description || "";
  form.elements.isActive.checked = department.isActive;
  document.getElementById("departmentFormTitle").textContent = "編輯部門";
  document.getElementById("departmentSubmitBtn").textContent = "更新部門";
  document.getElementById("cancelDepartmentEditBtn").hidden = false;
  document.querySelector(".org-editor")?.scrollIntoView({ behavior: "smooth", block: "nearest" });
}

function resetDepartmentForm() {
  const form = document.getElementById("departmentForm");
  form.reset();
  form.elements.departmentId.value = "";
  form.elements.sortOrder.value = 0;
  form.elements.isActive.checked = true;
  document.getElementById("departmentFormTitle").textContent = "新增部門";
  document.getElementById("departmentSubmitBtn").textContent = "新增部門";
  document.getElementById("cancelDepartmentEditBtn").hidden = true;
}

function handleGiftRecordAttachmentChange(event) {
  state.giftRecordAttachmentFiles = [...(event.currentTarget.files || [])];
  renderGiftRecordAttachmentButton();
}

function renderGiftRecordAttachmentButton() {
  const button = document.getElementById("giftRecordAttachmentBtn");
  if (!button) {
    return;
  }

  const count = state.giftRecordAttachmentFiles.length;
  const label = button.querySelector("[data-attachment-label]");
  if (label) {
    label.textContent = count ? `附件 (${count})：付款證明/送禮截圖` : "附件：付款證明/送禮截圖";
  }
  button.title = count
    ? state.giftRecordAttachmentFiles.map((file) => file.name).join("\n")
    : "用途：付款證明、送禮截圖、交易紀錄";
}

function clearGiftRecordAttachmentDraft() {
  state.giftRecordAttachmentFiles = [];
  const input = document.getElementById("giftRecordAttachmentInput");
  if (input) {
    input.value = "";
  }
  renderGiftRecordAttachmentButton();
}

async function submitGiftRecord(event) {
  event.preventDefault();
  const form = event.currentTarget;
  if (!validateRequiredMemberPickers(form)) {
    return;
  }
  const data = new FormData(form);
  const giftRecordId = data.get("giftRecordId");
  const isEdit = Boolean(giftRecordId);

  await runAction(async () => {
    const payload = {
      giftDate: data.get("giftDate"),
      bossUserId: Number(data.get("bossUserId")),
      recipientUserId: Number(data.get("recipientUserId")),
      serviceItemId: data.get("serviceItemId") ? Number(data.get("serviceItemId")) : null,
      giftName: emptyToNull(data.get("giftName")),
      amount: Number(data.get("amount")),
      quantity: Number(data.get("quantity") || 1),
      customerPaymentStatus: data.get("customerPaymentStatus"),
      status: data.get("status"),
      remark: emptyToNull(data.get("remark"))
    };

    if (isEdit) {
      await api(`/api/giftrecords/${giftRecordId}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      });
    } else {
      const requiresAttachment = payload.customerPaymentStatus === "paid"
        || payload.customerPaymentStatus === "partial";
      if (requiresAttachment && state.giftRecordAttachmentFiles.length === 0) {
        showAlert("此送禮收款狀態需要附件，請先選取付款證明或截圖。");
        return;
      }

      if (state.giftRecordAttachmentFiles.length > 0) {
        const createData = new FormData();
        createData.append("giftDate", payload.giftDate);
        createData.append("bossUserId", String(payload.bossUserId));
        createData.append("recipientUserId", String(payload.recipientUserId));
        createData.append("serviceItemId", payload.serviceItemId == null ? "" : String(payload.serviceItemId));
        createData.append("giftName", payload.giftName || "");
        createData.append("amount", String(payload.amount));
        createData.append("quantity", String(payload.quantity));
        createData.append("customerPaymentStatus", payload.customerPaymentStatus);
        createData.append("status", payload.status);
        createData.append("remark", payload.remark || "");
        state.giftRecordAttachmentFiles.forEach((file) => {
          createData.append("attachments", file);
        });

        await api("/api/giftrecords", {
          method: "POST",
          body: createData
        });
      } else {
        await api("/api/giftrecords", {
          method: "POST",
          body: JSON.stringify(payload)
        });
      }
    }

    resetGiftRecordForm();
    await loadGiftRecords();
    showAlert(isEdit ? "送禮紀錄已更新。" : "送禮紀錄已新增。", false);
    document.getElementById("giftRecordRows").closest(".panel").scrollIntoView({ behavior: "smooth", block: "start" });
  });
}

function startGiftRecordEdit(record) {
  const form = document.getElementById("giftRecordForm");
  form.elements.giftRecordId.value = record.id;
  form.elements.giftDate.value = record.giftDate;
  form.elements.bossUserId.value = record.bossUserId;
  form.elements.recipientUserId.value = record.recipientUserId;
  refreshMemberPickerFields(form);
  setGiftPickerValue(record.serviceItemId || "");
  form.elements.giftName.value = record.serviceItemId ? "" : record.giftName;
  form.elements.amount.value = record.amount;
  form.elements.quantity.value = record.quantity || 1;
  form.elements.customerPaymentStatus.value = record.customerPaymentStatus || "unpaid";
  form.elements.status.value = record.status || "completed";
  form.elements.remark.value = record.remark || "";
  document.getElementById("giftRecordFormTitle").textContent = "編輯送禮紀錄";
  document.getElementById("giftRecordSubmitBtn").textContent = "更新紀錄";
  document.getElementById("cancelGiftRecordEditBtn").hidden = false;
  clearGiftRecordAttachmentDraft();
  document.getElementById("giftRecordAttachmentBtn").hidden = true;
  form.scrollIntoView({ behavior: "smooth", block: "start" });
  void openAttachmentModal("送禮紀錄附件", "gift_records", record.id, {
    attachmentKind: record.customerPaymentStatus === "paid" || record.customerPaymentStatus === "partial" ? "payment_proof" : "general",
    uploadLabel: "新增付款證明/送禮截圖",
    canEdit: hasPermission("Gift.Edit")
  });
}

function resetGiftRecordForm() {
  const form = document.getElementById("giftRecordForm");
  form.reset();
  form.elements.giftRecordId.value = "";
  form.elements.quantity.value = 1;
  form.elements.customerPaymentStatus.value = "unpaid";
  form.elements.status.value = "completed";
  form.elements.giftName.disabled = false;
  setGiftPickerValue("");
  document.getElementById("giftRecordFormTitle").textContent = "新增送禮紀錄";
  document.getElementById("giftRecordSubmitBtn").textContent = "新增紀錄";
  document.getElementById("cancelGiftRecordEditBtn").hidden = true;
  clearGiftRecordAttachmentDraft();
  document.getElementById("giftRecordAttachmentBtn").hidden = false;
  setDefaultDates();
  renderSelects();
}

async function startOrderFromService(item) {
  const navButton = document.querySelector('.nav-tabs button[data-view="orders"]');
  if (navButton) {
    navButton.click();
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  applyServiceToOrderForm(item);
  showAlert(`已帶入「${item.name}」到新增訂單。`, false);
  const form = document.getElementById("orderForm");
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

function applyServiceToOrderForm(item) {
  resetOrderForm();
  state.orderAmountManuallyEdited = false;
  state.orderBaseAmountManuallyEdited = false;

  const form = document.getElementById("orderForm");
  const price = item.defaultPrice ?? 0;
  const unitText = unitTypeText(item.unitType);
  form.elements.orderType.value = orderTypeFromServiceCategory(item.category);
  form.elements.pricingCategory.value = item.category || "";
  form.elements.serviceName.value = item.name;
  form.elements.serviceSeedKey.value = item.seedKey || "";
  form.elements.serviceUnitPrice.value = item.defaultPrice ?? "";
  form.elements.serviceUnitType.value = item.unitType || "";
  form.elements.serviceUnitLabel.value = item.defaultPrice == null
    ? `${servicePriceText(item)} / ${unitText}`
    : `${money.format(item.defaultPrice)} / ${unitText}`;
  form.elements.serviceQuantity.value = 1;
  form.elements.baseAmount.value = price || 0;
  form.elements.designatedFee.value = 0;
  form.elements.friendFee.value = 0;
  form.elements.replacementCount.value = 0;
  form.elements.replacementFee.value = 0;
  form.elements.nightFee.value = 0;
  form.elements.otherFee.value = 0;
  form.elements.discountAmount.value = 0;
  form.elements.amount.value = calculateOrderQuotedAmount(form) || price || "";
  form.elements.commissionAmount.value = calculateDefaultCommission(Number(form.elements.amount.value || 0));
  form.elements.status.value = "draft";
  form.elements.customerPaymentStatus.value = "unpaid";
  form.elements.remark.value = item.remark || "";

  updateLateNightAddonAvailability(form, item);
  renderOrderActivityOptions();
  updateOrderCalc();
}

async function startGiftRecordFromService(item) {
  const navButton = document.querySelector('.nav-tabs button[data-view="giftRecords"]');
  if (navButton) {
    navButton.click();
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  resetGiftRecordForm();
  setGiftPickerValue(item.id);

  const form = document.getElementById("giftRecordForm");
  showAlert(`已帶入「${item.name}」到送禮紀錄。`, false);
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

async function startCustomGiftRecord() {
  const navButton = document.querySelector('.nav-tabs button[data-view="giftRecords"]');
  if (navButton) {
    navButton.click();
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  resetGiftRecordForm();
  setGiftPickerValue("", { keepCustomFields: true });

  const form = document.getElementById("giftRecordForm");
  form.elements.giftName.focus();
  showAlert("已切換為自訂禮物，請填名稱與金額。", false);
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

async function submitActivity(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const activityId = data.get("activityId");
  const payload = {
    name: data.get("name"),
    startsAt: toIsoDateTime(data.get("startsAt")),
    endsAt: toIsoDateTime(data.get("endsAt")),
    discountType: data.get("discountType"),
    discountValue: Number(data.get("discountValue") || 0),
    applicableCategories: [...form.querySelectorAll('input[name="activityCategory"]:checked')]
      .map((input) => input.value)
      .join(","),
    includeFees: data.get("includeFees") === "on",
    isActive: data.get("isActive") === "on",
    note: emptyToNull(data.get("note"))
  };

  await runAction(async () => {
    if (activityId) {
      await api(`/api/activities/${activityId}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      });
    } else {
      await api("/api/activities", {
        method: "POST",
        body: JSON.stringify(payload)
      });
    }
    resetActivityForm();
    await loadActivities();
    showAlert(activityId ? "活動已更新。" : "活動已新增。", false);
  });
}

function startActivityEdit(activity) {
  const form = document.getElementById("activityForm");
  form.elements.activityId.value = activity.id;
  form.elements.name.value = activity.name || "";
  form.elements.startsAt.value = toDateTimeLocalValue(activity.startsAt);
  form.elements.endsAt.value = toDateTimeLocalValue(activity.endsAt);
  form.elements.discountType.value = activity.discountType || "percent";
  form.elements.discountValue.value = activity.discountValue ?? 0;
  form.elements.includeFees.checked = Boolean(activity.includeFees);
  form.elements.isActive.checked = Boolean(activity.isActive);
  form.elements.note.value = activity.note || "";
  const categories = new Set(String(activity.applicableCategories || "").split(",").filter(Boolean));
  form.querySelectorAll('input[name="activityCategory"]').forEach((input) => {
    input.checked = categories.has(input.value);
  });
  document.getElementById("activityFormTitle").textContent = "編輯活動";
  document.getElementById("activitySubmitBtn").textContent = "更新活動";
  document.getElementById("cancelActivityEditBtn").hidden = false;
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

function resetActivityForm() {
  const form = document.getElementById("activityForm");
  form.reset();
  form.elements.activityId.value = "";
  form.elements.isActive.checked = true;
  form.querySelectorAll('input[name="activityCategory"]').forEach((input) => {
    input.checked = false;
  });
  document.getElementById("activityFormTitle").textContent = "新增活動";
  document.getElementById("activitySubmitBtn").textContent = "新增活動";
  document.getElementById("cancelActivityEditBtn").hidden = true;
}

function handleOrderAttachmentChange(event) {
  state.orderAttachmentFiles = [...(event.currentTarget.files || [])];
  renderOrderAttachmentButton();
}

function renderOrderAttachmentButton() {
  const button = document.getElementById("orderAttachmentBtn");
  if (!button) {
    return;
  }

  const count = state.orderAttachmentFiles.length;
  const label = button.querySelector("[data-attachment-label]");
  if (label) {
    label.textContent = count ? `附件 (${count})：收款證明/爭議依據` : "附件：收款證明/爭議依據";
  }
  button.title = count
    ? state.orderAttachmentFiles.map((file) => file.name).join("\n")
    : "用途：收款證明、付款截圖、爭議依據";
}

function clearOrderAttachmentDraft() {
  state.orderAttachmentFiles = [];
  const input = document.getElementById("orderAttachmentInput");
  if (input) {
    input.value = "";
  }
  renderOrderAttachmentButton();
}

async function submitOrder(event) {
  event.preventDefault();
  const form = event.currentTarget;
  if (!validateRequiredMemberPickers(form)) {
    return;
  }
  applyAutoLateNightDetection(form);
  updateOrderAmountFromService();
  const data = new FormData(form);
  let orderId = data.get("orderId");
  const activityValue = data.get("activityId");
  const editControlsVisible = !document.getElementById("copyOrderBtn").hidden;
  if (orderId && !editControlsVisible) {
    form.elements.orderId.value = "";
    orderId = "";
  }
  const isEdit = Boolean(orderId);
  const amount = Number(data.get("amount"));
  const commissionAmount = Number(data.get("commissionAmount"));
  const serviceRemark = buildOrderServiceRemark(form);
  const remark = [serviceRemark, emptyToNull(data.get("remark"))].filter(Boolean).join("\n");
  const memberRows = [...document.querySelectorAll(".member-row")];
  fillBlankMemberShares(memberRows, amount - commissionAmount);
  const members = memberRows.map((row) => ({
    userId: Number(row.querySelector("[data-member-select]").value),
    role: "player",
    shareAmount: Number(row.querySelector("[data-member-share]").value || 0)
  }));
  const shareTotal = roundMoney(members.reduce((sum, member) => sum + member.shareAmount, 0));
  const expectedShareTotal = roundMoney(amount - commissionAmount);

  if (shareTotal !== expectedShareTotal) {
    showAlert(`分潤總額必須等於金額扣掉團抽。應分配 ${money.format(expectedShareTotal)}，目前分配 ${money.format(shareTotal)}。`);
    return;
  }

  await runAction(async () => {
    const payload = {
      orderNo: emptyToNull(data.get("orderNo")),
      orderType: data.get("orderType") || "boosting",
      pricingCategory: emptyToNull(data.get("pricingCategory")),
      orderDate: data.get("orderDate"),
      ownerUserId: data.get("ownerUserId") ? Number(data.get("ownerUserId")) : null,
      amount,
      serviceQuantity: Number(data.get("serviceQuantity") || 0),
      baseAmount: Number(data.get("baseAmount") || 0),
      designatedFee: Number(data.get("designatedFee") || 0),
      friendFee: Number(data.get("friendFee") || 0),
      replacementFee: Number(data.get("replacementFee") || 0),
      nightFee: Number(data.get("nightFee") || 0),
      otherFee: Number(data.get("otherFee") || 0),
      discountAmount: Number(data.get("discountAmount") || 0),
      finalAmount: amount,
      activityId: activityValue && activityValue !== "none" ? Number(activityValue) : null,
      ignoreActivity: activityValue === "none",
      commissionRate: amount > 0 ? roundRate(commissionAmount / amount) : 0,
      commissionAmount,
      status: data.get("status"),
      customerPaymentStatus: data.get("customerPaymentStatus"),
      remark: emptyToNull(remark),
      members
    };

    if (isEdit) {
      await api(`/api/orders/${orderId}`, {
        method: "PUT",
        body: JSON.stringify(payload)
      });
    } else {
      const requiresAttachment = payload.status === "disputed"
        || payload.customerPaymentStatus === "paid"
        || payload.customerPaymentStatus === "partial";
      if (requiresAttachment && state.orderAttachmentFiles.length === 0) {
        showAlert("此訂單狀態需要附件，請先選取付款證明或依據。");
        return;
      }

      if (state.orderAttachmentFiles.length > 0) {
        const createData = new FormData();
        createData.append("orderNo", payload.orderNo || "");
        createData.append("orderType", payload.orderType);
        createData.append("pricingCategory", payload.pricingCategory || "");
        createData.append("orderDate", payload.orderDate);
        createData.append("ownerUserId", payload.ownerUserId == null ? "" : String(payload.ownerUserId));
        createData.append("amount", String(payload.amount));
        createData.append("serviceQuantity", String(payload.serviceQuantity));
        createData.append("baseAmount", String(payload.baseAmount));
        createData.append("designatedFee", String(payload.designatedFee));
        createData.append("friendFee", String(payload.friendFee));
        createData.append("replacementFee", String(payload.replacementFee));
        createData.append("nightFee", String(payload.nightFee));
        createData.append("otherFee", String(payload.otherFee));
        createData.append("discountAmount", String(payload.discountAmount));
        createData.append("finalAmount", String(payload.finalAmount));
        createData.append("activityId", payload.activityId == null ? "" : String(payload.activityId));
        createData.append("ignoreActivity", String(payload.ignoreActivity));
        createData.append("commissionRate", String(payload.commissionRate));
        createData.append("commissionAmount", String(payload.commissionAmount));
        createData.append("status", payload.status);
        createData.append("customerPaymentStatus", payload.customerPaymentStatus);
        createData.append("remark", payload.remark || "");
        createData.append("membersJson", JSON.stringify(payload.members));
        state.orderAttachmentFiles.forEach((file) => {
          createData.append("attachments", file);
        });

        await api("/api/orders", {
          method: "POST",
          body: createData
        });
      } else {
        await api("/api/orders", {
          method: "POST",
          body: JSON.stringify(payload)
        });
      }
    }
    resetOrderForm();
    await loadOrders();
    await loadDashboard();
    showAlert(isEdit ? "訂單已更新。" : "訂單已新增。", false);
    document.getElementById("orderRows").closest(".panel").scrollIntoView({ behavior: "smooth", block: "start" });
  });
}

function startOrderEdit(order) {
  const form = document.getElementById("orderForm");
  state.orderAmountManuallyEdited = true;
  state.orderBaseAmountManuallyEdited = true;
  form.elements.orderId.value = order.id;
  form.elements.orderDate.value = order.orderDate;
  form.elements.orderNo.value = order.orderNo || "";
  form.elements.orderType.value = order.orderType || "boosting";
  form.elements.pricingCategory.value = order.pricingCategory || "";
  form.elements.ownerUserId.value = order.ownerUserId || "";
  refreshMemberPickerFields(form);
  form.elements.amount.value = orderFinalAmount(order);
  form.elements.serviceName.value = "";
  form.elements.serviceSeedKey.value = "";
  form.elements.serviceUnitPrice.value = "";
  form.elements.serviceUnitType.value = "";
  form.elements.serviceUnitLabel.value = "";
  form.elements.serviceQuantity.value = order.serviceQuantity || 1;
  form.elements.baseAmount.value = Number(order.baseAmount || 0) || orderFinalAmount(order);
  form.elements.designatedFee.value = order.designatedFee || 0;
  form.elements.friendFee.value = order.friendFee || 0;
  form.elements.replacementCount.value = 0;
  form.elements.replacementFee.value = order.replacementFee || 0;
  form.elements.nightFee.value = order.nightFee || 0;
  form.elements.otherFee.value = order.otherFee || 0;
  form.elements.discountAmount.value = order.discountAmount || 0;
  form.elements.activityId.value = order.activityId || "none";
  form.elements.assignedPlayerCount.value = 0;
  form.elements.friendCount.value = 0;
  form.elements.isLateNight.checked = false;
  form.elements.autoDetectLateNight.checked = false;
  form.elements.commissionAmount.value = order.commissionAmount;
  form.elements.status.value = order.status || "completed";
  form.elements.customerPaymentStatus.value = order.customerPaymentStatus || "unpaid";
  form.elements.remark.value = order.remark || "";
  document.getElementById("memberRows").innerHTML = "";
  (order.members || []).forEach((member) => addMemberRow(member));
  if (!order.members || order.members.length === 0) {
    addMemberRow();
  }
  document.getElementById("orderFormTitle").textContent = "編輯訂單";
  document.getElementById("orderSubmitBtn").textContent = "更新此訂單";
  document.getElementById("copyOrderBtn").hidden = false;
  document.getElementById("cancelOrderEditBtn").hidden = false;
  clearOrderAttachmentDraft();
  document.getElementById("orderAttachmentBtn").hidden = true;
  updateOrderCalc();
  form.scrollIntoView({ behavior: "smooth", block: "start" });
  void openAttachmentModal("訂單附件", "orders", order.id, {
    attachmentKind: order.customerPaymentStatus === "paid" || order.customerPaymentStatus === "partial"
      ? "payment_proof"
      : order.status === "disputed"
        ? "evidence"
        : "general",
    uploadLabel: "新增收款證明/爭議依據",
    canEdit: hasPermission("Order.Edit")
  });
}

function copyOrderAsNew() {
  const form = document.getElementById("orderForm");
  state.orderAmountManuallyEdited = true;
  state.orderBaseAmountManuallyEdited = true;
  form.elements.orderId.value = "";
  form.elements.orderType.value = form.elements.orderType.value || "boosting";
  clearOrderAttachmentDraft();
  document.getElementById("orderAttachmentBtn").hidden = false;
  document.getElementById("orderFormTitle").textContent = "新增訂單";
  document.getElementById("orderSubmitBtn").textContent = "新增訂單";
  document.getElementById("copyOrderBtn").hidden = true;
  document.getElementById("cancelOrderEditBtn").hidden = true;
  showAlert("已切換成新增訂單，送出後會建立新資料，不會覆蓋原訂單。", false);
}

function resetOrderForm() {
  const form = document.getElementById("orderForm");
  state.orderAmountManuallyEdited = false;
  state.orderBaseAmountManuallyEdited = false;
  form.reset();
  form.elements.orderId.value = "";
  form.elements.orderType.value = "companion";
  form.elements.pricingCategory.value = "";
  form.elements.serviceName.value = "";
  form.elements.serviceSeedKey.value = "";
  form.elements.serviceUnitPrice.value = "";
  form.elements.serviceUnitType.value = "";
  form.elements.serviceUnitLabel.value = "";
  form.elements.serviceQuantity.value = 1;
  form.elements.baseAmount.value = 0;
  form.elements.designatedFee.value = 0;
  form.elements.friendFee.value = 0;
  form.elements.replacementCount.value = 0;
  form.elements.replacementFee.value = 0;
  form.elements.nightFee.value = 0;
  form.elements.otherFee.value = 0;
  form.elements.discountAmount.value = 0;
  form.elements.activityId.value = "";
  form.elements.assignedPlayerCount.value = 0;
  form.elements.friendCount.value = 0;
  form.elements.isLateNight.checked = false;
  form.elements.autoDetectLateNight.checked = false;
  refreshMemberPickerFields(form);
  document.getElementById("memberRows").innerHTML = "";
  setDefaultDates();
  addMemberRow();
  document.getElementById("orderFormTitle").textContent = "新增訂單";
  document.getElementById("orderSubmitBtn").textContent = "新增訂單";
  document.getElementById("copyOrderBtn").hidden = true;
  document.getElementById("cancelOrderEditBtn").hidden = true;
  clearOrderAttachmentDraft();
  document.getElementById("orderAttachmentBtn").hidden = false;
  updateLateNightAddonAvailability(form);
  renderOrderActivityOptions();
  updateOrderCalc();
}

function calculateDefaultCommission(amount) {
  const form = document.getElementById("orderForm");
  return calculateOrderCommissionAmount(form, amount);
}

function fillBlankMemberShares(rows, distributableAmount) {
  const blankInputs = rows
    .map((row) => row.querySelector("[data-member-share]"))
    .filter((input) => !String(input.value || "").trim());

  if (blankInputs.length === 0) {
    updateOrderCalc();
    return;
  }

  const usedAmount = rows
    .map((row) => row.querySelector("[data-member-share]"))
    .filter((input) => !blankInputs.includes(input))
    .reduce((sum, input) => sum + Number(input.value || 0), 0);
  const remainingCents = Math.round(roundMoney(distributableAmount - usedAmount) * 100);
  if (remainingCents < 0) {
    updateOrderCalc();
    return;
  }

  const baseCents = Math.trunc(remainingCents / blankInputs.length);
  let extraCents = remainingCents - baseCents * blankInputs.length;

  blankInputs.forEach((input) => {
    const cents = baseCents + (extraCents > 0 ? 1 : 0);
    extraCents -= extraCents > 0 ? 1 : 0;
    input.value = (cents / 100).toFixed(2).replace(/\.00$/, "");
  });

  updateOrderCalc();
}

async function submitPaymentGeneration(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  await runAction(async () => {
    await api("/api/payments/generate-monthly", {
      method: "POST",
      body: JSON.stringify({
        payMonth: data.get("payMonth"),
        overwriteExisting: data.get("overwriteExisting") === "on"
      })
    });
    await loadPayments();
    showAlert("月結已產生。", false);
  });
}

async function runAction(action) {
  try {
    hideAlert();
    await action();
  } catch (error) {
    showAlert(error.message);
  }
}

function handleOrderInput(event) {
  if ([
    "serviceQuantity",
    "orderDate",
    "amount",
    "baseAmount",
    "assignedPlayerCount",
    "designatedFee",
    "friendCount",
    "friendFee",
    "replacementCount",
    "replacementFee",
    "isLateNight",
    "nightFee",
    "otherFee",
    "discountAmount",
    "activityId",
    "autoDetectLateNight",
    "orderType"
  ].includes(event.target?.name)) {
    if (event.target?.name === "amount") {
      state.orderAmountManuallyEdited = true;
    } else if (event.target?.name === "baseAmount") {
      state.orderAmountManuallyEdited = false;
      state.orderBaseAmountManuallyEdited = true;
    } else if ([
      "serviceQuantity",
      "assignedPlayerCount",
      "designatedFee",
      "friendCount",
      "friendFee",
      "replacementCount",
      "replacementFee",
      "isLateNight",
      "nightFee",
      "otherFee",
      "discountAmount",
      "activityId",
      "autoDetectLateNight"
    ].includes(event.target?.name)) {
      state.orderAmountManuallyEdited = false;
      if (event.target?.name === "serviceQuantity") {
        state.orderBaseAmountManuallyEdited = false;
      }
    }
    if (event.target?.name === "orderType") {
      updateLateNightAddonAvailability(event.currentTarget);
    }
    if (event.currentTarget.elements.autoDetectLateNight.checked) {
      applyAutoLateNightDetection(event.currentTarget);
    }
    if (event.target?.name === "amount") {
      const amount = Number(event.currentTarget.elements.amount.value || 0);
      event.currentTarget.elements.commissionAmount.value = calculateDefaultCommission(amount);
    } else {
      updateOrderAmountFromService();
    }
  }

  updateOrderCalc();
}

function updateOrderAmountFromService() {
  const form = document.getElementById("orderForm");
  renderOrderActivityOptions();
  const amount = calculateOrderQuotedAmount(form);
  if (amount <= 0) {
    return;
  }

  form.elements.amount.value = amount;
  form.elements.commissionAmount.value = calculateDefaultCommission(amount);
}

function calculateOrderQuotedAmount(form) {
  const unitPrice = Number(form.elements.serviceUnitPrice.value || 0);
  const quantity = Number(form.elements.serviceQuantity.value || 0);
  if (!state.orderBaseAmountManuallyEdited && unitPrice > 0 && quantity > 0) {
    form.elements.baseAmount.value = roundMoney(unitPrice * quantity);
  }

  const baseAmount = nonNegativeMoney(form.elements.baseAmount.value);
  const assignedPlayerCount = nonNegativeInteger(form.elements.assignedPlayerCount.value);
  const friendCount = nonNegativeInteger(form.elements.friendCount.value);
  const replacementCount = nonNegativeInteger(form.elements.replacementCount.value);
  const lateNightCharge = form.elements.isLateNight.checked ? 30 : 0;
  const designatedFee = roundMoney(assignedPlayerCount * 20);
  const friendFee = roundMoney(friendCount * 20);
  const replacementFee = roundMoney(replacementCount * 10);
  const nightFee = roundMoney(lateNightCharge);
  const otherFee = nonNegativeMoney(form.elements.otherFee.value);
  const subtotal = roundMoney(baseAmount + designatedFee + friendFee + replacementFee + nightFee + otherFee);
  const appliedActivity = selectedOrderActivity(form);
  const discountAmount = appliedActivity
    ? Math.min(calculateActivityDiscount(appliedActivity, baseAmount, subtotal), subtotal)
    : Math.min(nonNegativeMoney(form.elements.discountAmount.value), subtotal);

  form.elements.designatedFee.value = designatedFee;
  form.elements.friendFee.value = friendFee;
  form.elements.replacementFee.value = replacementFee;
  form.elements.nightFee.value = nightFee;
  form.elements.discountAmount.value = discountAmount;

  return roundMoney(subtotal - discountAmount);
}

function renderOrderActivityOptions() {
  const select = document.getElementById("orderActivitySelect");
  const form = document.getElementById("orderForm");
  if (!select || !form) {
    return;
  }

  const currentValue = select.value;
  const category = form.elements.pricingCategory.value;
  const orderDate = form.elements.orderDate.value;
  const options = activeActivitiesForOrder(category, orderDate);
  select.innerHTML = `
    <option value="">自動套用</option>
    <option value="none">不套活動</option>
    ${options.map((activity) => `
      <option value="${activity.id}">${escapeHtml(activity.name)}｜${escapeHtml(activityDiscountText(activity))}</option>
    `).join("")}
  `;
  if ([...select.options].some((option) => option.value === currentValue)) {
    select.value = currentValue;
  }
}

function selectedOrderActivity(form) {
  const value = form.elements.activityId.value;
  if (value === "none") {
    return null;
  }
  if (value) {
    return state.activities.find((activity) => activity.id === Number(value) && activity.isActive) || null;
  }
  return activeActivitiesForOrder(form.elements.pricingCategory.value, form.elements.orderDate.value)[0] || null;
}

function activeActivitiesForOrder(category, orderDate) {
  return state.activities
    .filter((activity) => activity.isActive && activityAppliesToOrder(activity, category, orderDate))
    .sort((a, b) => new Date(b.startsAt) - new Date(a.startsAt) || b.id - a.id);
}

function activityAppliesToOrder(activity, category, orderDate) {
  if (!activity || !orderDate) {
    return false;
  }
  const now = new Date();
  const targetDate = new Date(`${orderDate}T${String(now.getHours()).padStart(2, "0")}:${String(now.getMinutes()).padStart(2, "0")}:00`);
  if (targetDate < new Date(activity.startsAt) || targetDate > new Date(activity.endsAt)) {
    return false;
  }
  const categories = String(activity.applicableCategories || "").split(",").filter(Boolean);
  return categories.length === 0 || categories.includes(category);
}

function calculateActivityDiscount(activity, baseAmount, subtotal) {
  const discountBase = activity.includeFees ? subtotal : baseAmount;
  const value = Number(activity.discountValue || 0);
  const discount = activity.discountType === "percent"
    ? discountBase * value / 100
    : activity.discountType === "fixed_amount"
      ? value
      : activity.discountType === "fixed_price"
        ? discountBase - value
        : 0;
  return nonNegativeMoney(Math.min(discount, subtotal));
}

function calculateOrderCommissionAmount(form, amount) {
  if (!form || amount <= 0) {
    return "";
  }

  if (form.elements.orderType.value !== "companion") {
    return Number(form.elements.commissionAmount.value || 0);
  }

  const quantity = Number(form.elements.serviceQuantity.value || 0);
  const primaryMemberId = Number(document.querySelector("[data-member-select]")?.value || 0);
  if (quantity <= 0 || !primaryMemberId) {
    return roundMoney(amount * 0.25);
  }

  const completedHours = companionCompletedHoursForMonth(
    primaryMemberId,
    form.elements.orderDate.value,
    Number(form.elements.orderId.value || 0));
  return calculateCompanionTierCommission(amount, quantity, completedHours);
}

function companionCompletedHoursForMonth(userId, orderDate, excludeOrderId = 0) {
  if (!userId || !orderDate) {
    return 0;
  }

  const month = orderDate.slice(0, 7);
  return state.orders
    .filter((order) =>
      order.id !== excludeOrderId &&
      order.status === "completed" &&
      order.orderType === "companion" &&
      String(order.orderDate || "").startsWith(month) &&
      (order.memberUserIds || []).includes(userId))
    .reduce((sum, order) => sum + Number(order.serviceQuantity || 0), 0);
}

function calculateCompanionTierCommission(amount, hours, completedHours) {
  if (amount <= 0 || hours <= 0) {
    return 0;
  }

  const hourlyAmount = amount / hours;
  let remainingHours = hours;
  let cursor = completedHours;
  let commission = 0;

  while (remainingHours > 0) {
    const rate = companionCommissionRate(cursor);
    const nextBoundary = cursor < 15 ? 15 : cursor < 30 ? 30 : Number.POSITIVE_INFINITY;
    const tierHours = Number.isFinite(nextBoundary)
      ? Math.min(remainingHours, nextBoundary - cursor)
      : remainingHours;
    commission += hourlyAmount * tierHours * rate;
    remainingHours -= tierHours;
    cursor += tierHours;
  }

  return roundMoney(commission);
}

function companionCommissionRate(completedHours) {
  if (completedHours < 15) {
    return 0.25;
  }

  if (completedHours < 30) {
    return 0.20;
  }

  return 0.10;
}

function roundRate(value) {
  return Math.round(Number(value || 0) * 10000) / 10000;
}

function nonNegativeInteger(value) {
  return Math.max(0, Math.floor(Number(value || 0)));
}

function nonNegativeMoney(value) {
  return Math.max(0, roundMoney(Number(value || 0)));
}

function supportsLateNightAddon(item) {
  if (!item) {
    return false;
  }

  const text = `${item.seedKey || ""} ${item.name || ""} ${item.priceNote || ""} ${item.remark || ""}`;
  return /深夜|00:00-06:00|00:00–06:00/.test(text);
}

function updateLateNightAddonAvailability(form, item = null) {
  const selectedItem = item || state.serviceItems.find((serviceItem) => (
    serviceItem.seedKey === form.elements.serviceSeedKey.value
  ));
  const enabled = supportsLateNightAddon(selectedItem) || (!selectedItem && form.elements.orderType.value === "companion");
  toggleLateNightField(form, "lateNightField", "isLateNight", enabled, "此服務可套用深夜加價");
  toggleLateNightField(form, "autoLateNightField", "autoDetectLateNight", enabled, "依照送出訂單當下時間自動判斷深夜");
}

function toggleLateNightField(form, fieldId, inputName, enabled, enabledTitle) {
  const field = document.getElementById(fieldId);
  const input = form.elements[inputName];
  if (!field || !input) {
    return;
  }

  input.disabled = !enabled;
  if (!enabled) {
    input.checked = false;
  }
  field.classList.toggle("disabled", !enabled);
  field.title = enabled ? enabledTitle : "只有有深夜加價規則的服務可勾選";
}

function applyAutoLateNightDetection(form) {
  if (!form.elements.autoDetectLateNight?.checked || form.elements.isLateNight.disabled) {
    return;
  }

  form.elements.isLateNight.checked = isCurrentLateNight();
}

function isCurrentLateNight(now = new Date()) {
  const hour = now.getHours();
  return hour >= 0 && hour < 6;
}

function buildOrderServiceRemark(form) {
  const serviceName = String(form.elements.serviceName.value || "").trim();
  if (!serviceName) {
    return null;
  }

  const unitLabel = String(form.elements.serviceUnitLabel.value || "").trim();
  const quantity = Number(form.elements.serviceQuantity.value || 0);
  const unitType = String(form.elements.serviceUnitType.value || "").trim();
  const quantityText = quantity > 0
    ? `${money.format(quantity)} ${unitTypeText(unitType)}`
    : "";
  const assignedPlayerCount = nonNegativeInteger(form.elements.assignedPlayerCount.value);
  const friendCount = nonNegativeInteger(form.elements.friendCount.value);
  const replacementCount = nonNegativeInteger(form.elements.replacementCount.value);
  const otherFee = nonNegativeMoney(form.elements.otherFee.value);
  const discountAmount = nonNegativeMoney(form.elements.discountAmount.value);
  const surcharges = [
    assignedPlayerCount > 0 ? `指定陪陪 +20／位 × ${assignedPlayerCount}` : "",
    friendCount > 0 ? `帶朋友 +20／位 × ${friendCount}` : "",
    replacementCount > 0 ? `換人 +10／位 × ${replacementCount}` : "",
    form.elements.isLateNight.checked ? "深夜 00:00-06:00 +30" : "",
    otherFee > 0 ? `其他加價 ${money.format(otherFee)}` : "",
    discountAmount > 0 ? `活動折扣 -${money.format(discountAmount)}` : "",
    form.elements.autoDetectLateNight.checked ? "深夜判斷：自動" : ""
  ].filter(Boolean);

  return [
    `服務項目：${serviceName}`,
    unitLabel ? `計價：${unitLabel}` : "",
    quantityText ? `數量：${quantityText}` : "",
    surcharges.length ? `加價／標記：${surcharges.join("、")}` : ""
  ].filter(Boolean).join("；");
}

function updateOrderCalc() {
  const form = document.getElementById("orderForm");
  const amount = Number(form.elements.amount.value || 0);
  const commission = Number(form.elements.commissionAmount.value || 0);
  const allocated = [...document.querySelectorAll("[data-member-share]")]
    .reduce((sum, input) => sum + Number(input.value || 0), 0);
  document.getElementById("distributableAmount").textContent = money.format(amount - commission);
  document.getElementById("allocatedAmount").textContent = money.format(allocated);
}

function identityText(user) {
  const parts = [];
  if (user.isPlayer) parts.push("團員");
  if (user.isBoss) parts.push("老闆");
  return parts.join(" / ") || "-";
}

function serviceCategoryText(category) {
  return {
    boost: "代打",
    grind: "代肝",
    play: "陪玩",
    special_companion: "特殊陪",
    gift: "禮物",
    deposit_bonus: "預存",
    other: "其他"
  }[category] || category;
}

function orderTypeFromServiceCategory(category) {
  return {
    boost: "boosting",
    grind: "farming",
    play: "companion",
    special_companion: "companion",
    deposit_bonus: "prepaid"
  }[category] || "boosting";
}

function orderTypeText(orderType) {
  return label("orderType", orderType || "boosting");
}

function servicePriceText(item) {
  if (item.defaultPrice != null && item.unitType !== "amount") {
    return money.format(item.defaultPrice);
  }

  return item.priceNote || (item.defaultPrice == null ? "另議" : money.format(item.defaultPrice));
}

function unitTypeText(unitType) {
  return {
    custom: "自訂",
    week: "週",
    day: "日",
    match: "場",
    star: "星",
    hour_person: "小時 / 人",
    item: "項",
    amount: "金額"
  }[unitType] || unitType;
}

function statusPill(status) {
  const type = status === "completed" ? "good" : status === "cancelled" ? "bad" : "warn";
  return pill(label("orderStatus", status), type);
}

function paymentPill(status) {
  const type = status === "paid" ? "good" : status === "unpaid" ? "warn" : "";
  return pill(label("customerPaymentStatus", status), type);
}

function paymentStatusPill(status) {
  const type = status === "paid" ? "good" : status === "cancelled" ? "bad" : "warn";
  return pill(label("paymentStatus", status), type);
}

function label(group, value) {
  return labels[group]?.[value] || value || "";
}

function pill(text, type = "") {
  return plainText(text, type);
}

function plainText(text, type = "") {
  return `<span class="plain-status ${type}">${escapeHtml(text)}</span>`;
}

function emptyRow(colspan) {
  return `<tr><td colspan="${colspan}">沒有資料</td></tr>`;
}

function emptyToNull(value) {
  return value && String(value).trim() ? String(value).trim() : null;
}

function roundMoney(value) {
  return Math.round((Number(value) + Number.EPSILON) * 100) / 100;
}

function formatDateTime(value) {
  return new Date(value).toLocaleString("zh-TW", { hour12: false });
}

function toIsoDateTime(value) {
  if (!value) {
    return null;
  }
  return value.length === 16 ? `${value}:00` : value;
}

function toDateTimeLocalValue(value) {
  if (!value) {
    return "";
  }
  const date = new Date(value);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

function activityDiscountText(activity) {
  if (!activity) {
    return "";
  }
  if (activity.discountType === "percent") {
    return `${activity.discountValue}%`;
  }
  if (activity.discountType === "fixed_amount") {
    return `折 ${money.format(activity.discountValue)}`;
  }
  if (activity.discountType === "fixed_price") {
    return `活動價 ${money.format(activity.discountValue)}`;
  }
  return money.format(activity.discountValue);
}

function activityCategoriesText(value) {
  const categories = String(value || "")
    .split(",")
    .filter(Boolean);
  return categories.length
    ? categories.map(serviceCategoryText).join("、")
    : "全部";
}

function showAlert(message, isError = true) {
  const alert = document.getElementById("alert");
  alert.hidden = false;
  alert.textContent = message;
  alert.style.background = isError ? "#fff1df" : "#e8f5ef";
  alert.style.color = isError ? "var(--warn)" : "var(--success)";
}

function hideAlert() {
  document.getElementById("alert").hidden = true;
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
