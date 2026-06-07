const state = {
  users: [],
  loginUsers: [],
  serviceItems: [],
  giftRecords: [],
  players: [],
  bosses: [],
  orders: [],
  payments: [],
  view: "dashboard",
  serviceCategory: "boost",
  auth: null
};

const titles = {
  dashboard: ["Dashboard", "總覽"],
  users: ["Users", "成員"],
  loginUsers: ["Login Users", "登入者"],
  orders: ["Orders", "訂單"],
  gifts: ["Gifts", "禮物"],
  payments: ["Payments", "月結"],
  audit: ["Audit", "紀錄"]
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
    generate_monthly: "產生月結",
    mark_paid: "標記已發薪"
  },
  targetType: {
    users: "成員",
    orders: "訂單",
    payments: "發薪",
    gift_records: "送禮紀錄",
    audit_logs: "操作紀錄"
  }
};

document.addEventListener("DOMContentLoaded", async () => {
  bindNavigation();
  bindForms();
  bindPriceGallery();
  setDefaultDates();
  addMemberRow();
  await initializeAuth();
});

function bindNavigation() {
  document.querySelectorAll(".nav-tabs button").forEach((button) => {
    button.addEventListener("click", async () => {
      state.view = button.dataset.view;
      document.querySelectorAll(".nav-tabs button").forEach((x) => x.classList.remove("active"));
      button.classList.add("active");
      document.querySelectorAll(".view").forEach((x) => x.classList.remove("active"));
      document.getElementById(`${state.view}View`).classList.add("active");
      document.getElementById("viewEyebrow").textContent = titles[state.view][0];
      document.getElementById("viewTitle").textContent = titles[state.view][1];
      await refreshAll();
    });
  });

  document.getElementById("refreshBtn").addEventListener("click", refreshAll);
  document.getElementById("addMemberBtn").addEventListener("click", () => addMemberRow());
}

function bindForms() {
  ensureLoginUserEditControls();
  document.getElementById("loginForm").addEventListener("submit", submitLogin);
  document.getElementById("loginUserForm").addEventListener("submit", submitLoginUser);
  document.getElementById("cancelLoginUserEditBtn").addEventListener("click", resetLoginUserForm);
  document.getElementById("logoutBtn").addEventListener("click", logout);
  document.getElementById("userForm").addEventListener("submit", submitUser);
  document.getElementById("cancelUserEditBtn").addEventListener("click", resetUserForm);
  document.getElementById("orderForm").addEventListener("submit", submitOrder);
  document.getElementById("copyOrderBtn").addEventListener("click", copyOrderAsNew);
  document.getElementById("cancelOrderEditBtn").addEventListener("click", resetOrderForm);
  document.getElementById("giftRecordForm").addEventListener("submit", submitGiftRecord);
  document.getElementById("cancelGiftRecordEditBtn").addEventListener("click", resetGiftRecordForm);
  document.getElementById("giftItemSelect").addEventListener("change", applySelectedGiftItem);
  document.getElementById("paymentForm").addEventListener("submit", submitPaymentGeneration);
  document.getElementById("orderForm").addEventListener("input", updateOrderCalc);
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
      <img alt="">
    `;

    gallery.parentNode?.insertBefore(board, gallery);
    board.append(gallery, display);

    const displayImg = display.querySelector("img");
    const setActive = (link) => {
      links.forEach((item) => item.classList.toggle("active", item === link));
      const image = link.querySelector("img");
      displayImg.src = link.getAttribute("href");
      displayImg.alt = image?.alt || "";
    };

    links.forEach((link) => {
      link.addEventListener("click", (event) => {
        event.preventDefault();
        setActive(link);
      });
    });

    setActive(links[0]);
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
  try {
    state.auth = await api("/api/auth/me", { skipAuthRedirect: true });
    if (state.auth.authRequired && !state.auth.isAuthenticated) {
      showLogin();
      return;
    }

    showApp();
    await refreshAll();
  } catch (error) {
    showLogin();
    showLoginError(error.message);
  }
}

function showLogin() {
  document.body.classList.add("auth-locked");
  document.getElementById("loginView").hidden = false;
  document.getElementById("logoutBtn").hidden = true;
  document.getElementById("currentUser").hidden = true;
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
  }
}

async function api(path, options = {}) {
  const { skipAuthRedirect, ...fetchOptions } = options;
  const response = await fetch(path, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {})
    },
    ...fetchOptions
  });

  if (!response.ok) {
    if (response.status === 401 && !skipAuthRedirect) {
      showLogin();
    }

    let message = `${response.status} ${response.statusText}`;
    try {
      const error = await response.json();
      message = error.message || message;
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

async function logout() {
  await api("/api/auth/logout", { method: "POST", body: "{}" });
  state.auth = null;
  showLogin();
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

    if (state.view === "dashboard") {
      await loadDashboard();
    }
    if (state.view === "users" || state.view === "orders" || state.view === "gifts") {
      await loadUsers();
    }
    if (state.view === "loginUsers") {
      await loadLoginUsers();
    }
    if (state.view === "services" || state.view === "gifts") {
      await loadServiceItems();
    }
    if (state.view === "gifts") {
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
  renderGiftItems();
  renderSelects();
}

async function loadGiftRecords() {
  state.giftRecords = await api("/api/giftrecords");
  renderGiftRecords();
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
  const logs = await api("/api/auditlogs?take=100");
  renderAuditLogs(logs);
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

  const users = state.loginUsers;
  body.innerHTML = users.length ? users.map((user) => `
    <tr>
      <td>${escapeHtml(user.loginAccount || "")}</td>
      <td>${escapeHtml(user.displayName)}</td>
      <td>${label("systemRole", user.systemRole)}</td>
      <td>${user.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
      <td>${pill("已設定", "good")}</td>
      <td>
        <button class="ghost small" data-login-user-edit="${user.id}">編輯</button>
        ${user.isActive
          ? `<button class="ghost small" data-login-user-deactivate="${user.id}">停用</button>`
          : `<button class="ghost small" data-login-user-activate="${user.id}">啟用</button>`}
      </td>
    </tr>
  `).join("") : emptyRow(6);

  bindLoginUserTableActions(body);
}

function bindLoginUserTableActions(body) {
  body.querySelectorAll("[data-login-user-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const loginUser = state.loginUsers.find((item) => item.id === Number(button.dataset.loginUserEdit));
      if (loginUser) {
        startLoginUserEdit(loginUser);
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
}

function renderServiceItems() {
  const body = document.getElementById("serviceItemRows");
  if (!body) {
    return;
  }

  renderServiceCategoryTabs();
  const rows = state.serviceItems.filter((item) => item.category === state.serviceCategory && item.isActive);

  body.innerHTML = rows.length ? rows.map((item) => `
    <tr>
      <td>${escapeHtml(item.subcategory || serviceCategoryText(item.category))}</td>
      <td>${escapeHtml(item.name)}</td>
      <td>${escapeHtml(servicePriceText(item))}</td>
      <td>${escapeHtml(unitTypeText(item.unitType))}</td>
      <td>${escapeHtml(item.remark || "")}</td>
      <td>${item.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
    </tr>
  `).join("") : emptyRow(6);
}

function renderGiftItems() {
  const body = document.getElementById("giftItemRows");
  if (!body) {
    return;
  }

  const rows = state.serviceItems.filter((item) => item.category === "gift" && item.isActive);
  body.innerHTML = rows.length ? rows.map((item) => `
    <tr>
      <td>${escapeHtml(item.name)}</td>
      <td>${escapeHtml(servicePriceText(item))}</td>
      <td>${escapeHtml(unitTypeText(item.unitType))}</td>
      <td>${escapeHtml(item.remark || "")}</td>
      <td>${item.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
    </tr>
  `).join("") : emptyRow(5);
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
      <td>
        <button class="ghost small" data-gift-edit="${record.id}">編輯</button>
        ${record.status === "cancelled" ? pill("已取消", "bad") : `<button class="ghost small" data-gift-cancel="${record.id}">取消</button>`}
      </td>
    </tr>
  `).join("") : emptyRow(7);

  body.querySelectorAll("[data-gift-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const record = state.giftRecords.find((item) => item.id === Number(button.dataset.giftEdit));
      if (record) {
        startGiftRecordEdit(record);
      }
    });
  });

  body.querySelectorAll("[data-gift-cancel]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        await api(`/api/giftrecords/${button.dataset.giftCancel}/cancel`, { method: "POST", body: "{}" });
        await loadGiftRecords();
        showAlert("送禮紀錄已取消。", false);
      });
    });
  });
}

function renderServiceCategoryTabs() {
  const tabs = document.getElementById("serviceCategoryTabs");
  if (!tabs) {
    return;
  }

  const categories = [
    ["boost", "代打"],
    ["grind", "代肝"],
    ["play", "陪玩"],
    ["deposit_bonus", "預存"]
  ];

  tabs.innerHTML = categories.map(([value, text]) => `
    <button class="ghost small ${state.serviceCategory === value ? "active" : ""}" data-service-category="${value}" type="button">${text}</button>
  `).join("");

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
      <td>${escapeHtml(user.discordName || "")}</td>
      <td>${escapeHtml(user.nickname)}</td>
      <td>${label("systemRole", user.systemRole)}</td>
      <td>${user.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
      <td>
        <button class="ghost small" data-user-edit="${user.id}">編輯</button>
        ${user.isActive
          ? `<button class="ghost small" data-user-deactivate="${user.id}">停用</button>`
          : `<button class="ghost small" data-user-activate="${user.id}">啟用</button>`}
      </td>
    </tr>
  `).join("") : emptyRow(5);

  bindUserTableActions(body);
}

function bindUserTableActions(body) {
  body.querySelectorAll("[data-user-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const user = state.users.find((item) => item.id === Number(button.dataset.userEdit));
      if (user) {
        startUserEdit(user);
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
}

function renderOrders() {
  const body = document.getElementById("orderRows");
  body.innerHTML = state.orders.length ? state.orders.map((order) => `
    <tr>
      <td>${order.id}</td>
      <td>${order.orderDate}</td>
      <td>${escapeHtml(order.orderNo || "")}</td>
      <td>${money.format(order.amount)}</td>
      <td>${money.format(order.shareTotalAmount)}</td>
      <td>${statusPill(order.status)}</td>
      <td>${paymentPill(order.customerPaymentStatus)}</td>
      <td><button class="ghost small" data-order-edit="${order.id}">編輯</button></td>
    </tr>
  `).join("") : emptyRow(8);

  body.querySelectorAll("[data-order-edit]").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(async () => {
        const order = await api(`/api/orders/${button.dataset.orderEdit}`);
        startOrderEdit(order);
      });
    });
  });
}

function renderPayments() {
  const body = document.getElementById("paymentRows");
  body.innerHTML = state.payments.length ? state.payments.map((payment) => `
    <tr>
      <td>${payment.id}</td>
      <td>${payment.payMonth}</td>
      <td>${escapeHtml(payment.nickname)}</td>
      <td>${money.format(payment.expectedAmount)}</td>
      <td>${payment.actualAmount == null ? "" : money.format(payment.actualAmount)}</td>
      <td>${paymentStatusPill(payment.paymentStatus)}</td>
      <td>${payment.paymentStatus === "paid" ? "" : `<button class="ghost small" data-payment-paid="${payment.id}">標記已發</button>`}</td>
    </tr>
  `).join("") : emptyRow(7);

  body.querySelectorAll("[data-payment-paid]").forEach((button) => {
    button.addEventListener("click", async () => {
      await api(`/api/payments/${button.dataset.paymentPaid}/mark-paid`, { method: "POST", body: "{}" });
      await loadPayments();
    });
  });
}

function renderAuditLogs(rows) {
  const body = document.getElementById("auditRows");
  body.innerHTML = rows.length ? rows.map((log) => `
    <tr>
      <td>${formatDateTime(log.createdAt)}</td>
      <td>${escapeHtml(label("auditAction", log.action))}</td>
      <td>${escapeHtml(label("targetType", log.targetType))}</td>
      <td>${log.targetId || ""}</td>
    </tr>
  `).join("") : emptyRow(4);
}

function renderSelects() {
  const bossSelect = document.getElementById("bossSelect");
  bossSelect.innerHTML = `<option value="">未指定</option>${state.bosses.map((boss) =>
    `<option value="${boss.id}">${escapeHtml(boss.nickname)}</option>`
  ).join("")}`;

  const giftBossSelect = document.getElementById("giftBossSelect");
  if (giftBossSelect) {
    const currentValue = giftBossSelect.value;
    giftBossSelect.innerHTML = state.bosses.map((boss) =>
      `<option value="${boss.id}">${escapeHtml(boss.nickname)}</option>`
    ).join("");
    if (currentValue) {
      giftBossSelect.value = currentValue;
    }
  }

  const giftRecipientSelect = document.getElementById("giftRecipientSelect");
  if (giftRecipientSelect) {
    const currentValue = giftRecipientSelect.value;
    giftRecipientSelect.innerHTML = state.players.map((player) =>
      `<option value="${player.id}">${escapeHtml(player.nickname)}</option>`
    ).join("");
    if (currentValue) {
      giftRecipientSelect.value = currentValue;
    }
  }

  const giftItemSelect = document.getElementById("giftItemSelect");
  if (giftItemSelect) {
    const currentValue = giftItemSelect.value;
    const giftItems = state.serviceItems.filter((item) => item.category === "gift" && item.isActive);
    giftItemSelect.innerHTML = `<option value="">自訂打賞</option>${giftItems.map((item) =>
      `<option value="${item.id}">${escapeHtml(item.name)}${item.defaultPrice == null ? "" : ` - ${money.format(item.defaultPrice)}`}</option>`
    ).join("")}`;
    giftItemSelect.value = currentValue;
  }

  document.querySelectorAll("[data-member-select]").forEach((select) => {
    const currentValue = select.value;
    select.innerHTML = state.players.map((player) =>
      `<option value="${player.id}">${escapeHtml(player.nickname)}</option>`
    ).join("");
    if (currentValue) {
      select.value = currentValue;
    }
  });
}

function addMemberRow(member = null) {
  const wrap = document.getElementById("memberRows");
  const row = document.createElement("div");
  row.className = "member-row";
  row.innerHTML = `
    <select data-member-select></select>
    <select data-member-role>
      <option value="player">團員</option>
      <option value="leader">帶團</option>
      <option value="trainer">教學</option>
      <option value="bonus">獎金</option>
    </select>
    <input data-member-share type="number" step="0.01" min="0" placeholder="分潤">
    <button class="icon-btn" type="button" title="移除">×</button>
  `;
  row.querySelector("button").addEventListener("click", () => {
    row.remove();
    updateOrderCalc();
  });
  row.addEventListener("input", updateOrderCalc);
  wrap.appendChild(row);
  renderSelects();
  if (member) {
    row.querySelector("[data-member-select]").value = member.userId;
    row.querySelector("[data-member-role]").value = member.role || "player";
    row.querySelector("[data-member-share]").value = member.shareAmount;
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
  const payload = {
    nickname: data.get("nickname"),
    discordId: emptyToNull(data.get("discordId")),
    discordName: emptyToNull(data.get("discordName")),
    bankAccount: emptyToNull(data.get("bankAccount")),
    systemRole: data.get("systemRole"),
    isPlayer: data.get("isPlayer") === "on",
    isBoss: data.get("isBoss") === "on"
  };

  if (isEdit) {
    payload.isActive = existingUser?.isActive ?? true;
    payload.leftAt = existingUser?.leftAt ?? null;
  }

  await runAction(async () => {
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
  const existingLoginUser = isEdit
    ? state.loginUsers.find((loginUser) => loginUser.id === Number(loginUserId))
    : null;
  const password = emptyToNull(data.get("password"));

  await runAction(async () => {
    const payload = {
      displayName: data.get("nickname"),
      loginAccount: data.get("loginAccount"),
      systemRole: data.get("systemRole")
    };

    if (password) {
      payload.password = password;
    }

    if (isEdit) {
      payload.isActive = existingLoginUser?.isActive ?? true;
    } else {
      payload.password = data.get("password");
    }

    await api(isEdit ? `/api/loginusers/${loginUserId}` : "/api/loginusers", {
      method: isEdit ? "PUT" : "POST",
      body: JSON.stringify(payload)
    });
    resetLoginUserForm();
    await loadLoginUsers();
    showAlert(isEdit ? "登入者已更新。" : "登入者已新增。", false);
  });
}

function startLoginUserEdit(loginUser) {
  const form = document.getElementById("loginUserForm");
  form.elements.loginUserId.value = loginUser.id;
  form.elements.nickname.value = loginUser.displayName || "";
  form.elements.loginAccount.value = loginUser.loginAccount || "";
  form.elements.password.value = "";
  form.elements.systemRole.value = loginUser.systemRole || "staff";
  setLoginUserEditMode(true);
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

function resetLoginUserForm() {
  const form = document.getElementById("loginUserForm");
  form.reset();
  form.elements.loginUserId.value = "";
  form.elements.systemRole.value = "admin";
  setLoginUserEditMode(false);
}

function setLoginUserEditMode(isEdit) {
  const title = document.getElementById("loginUserFormTitle");
  const submitButton = document.getElementById("loginUserSubmitBtn");
  const cancelButton = document.getElementById("cancelLoginUserEditBtn");
  const passwordInput = document.getElementById("loginUserForm").elements.password;

  if (title) {
    title.textContent = isEdit ? "編輯登入者" : "新增登入者";
  }
  if (submitButton) {
    submitButton.textContent = isEdit ? "儲存" : "新增登入者";
  }
  if (cancelButton) {
    cancelButton.hidden = !isEdit;
  }
  passwordInput.required = !isEdit;
  passwordInput.placeholder = isEdit ? "留空代表不變更密碼" : "";
}

function startUserEdit(user) {
  const form = document.getElementById("userForm");
  form.elements.userId.value = user.id;
  form.elements.nickname.value = user.nickname || "";
  form.elements.discordId.value = user.discordId || "";
  form.elements.discordName.value = user.discordName || "";
  form.elements.bankAccount.value = user.bankAccount || "";
  form.elements.systemRole.value = user.systemRole || "staff";
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
  form.elements.isPlayer.checked = true;
  document.getElementById("userFormTitle").textContent = "新增成員";
  document.getElementById("userSubmitBtn").textContent = "新增";
  document.getElementById("cancelUserEditBtn").hidden = true;
}

async function submitGiftRecord(event) {
  event.preventDefault();
  const form = event.currentTarget;
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

    await api(isEdit ? `/api/giftrecords/${giftRecordId}` : "/api/giftrecords", {
      method: isEdit ? "PUT" : "POST",
      body: JSON.stringify(payload)
    });

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
  form.elements.serviceItemId.value = record.serviceItemId || "";
  form.elements.giftName.value = record.serviceItemId ? "" : record.giftName;
  form.elements.amount.value = record.amount;
  form.elements.quantity.value = record.quantity || 1;
  form.elements.customerPaymentStatus.value = record.customerPaymentStatus || "unpaid";
  form.elements.status.value = record.status || "completed";
  form.elements.remark.value = record.remark || "";
  document.getElementById("giftRecordFormTitle").textContent = "編輯送禮紀錄";
  document.getElementById("giftRecordSubmitBtn").textContent = "更新紀錄";
  document.getElementById("cancelGiftRecordEditBtn").hidden = false;
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

function resetGiftRecordForm() {
  const form = document.getElementById("giftRecordForm");
  form.reset();
  form.elements.giftRecordId.value = "";
  form.elements.quantity.value = 1;
  form.elements.customerPaymentStatus.value = "unpaid";
  form.elements.status.value = "completed";
  document.getElementById("giftRecordFormTitle").textContent = "新增送禮紀錄";
  document.getElementById("giftRecordSubmitBtn").textContent = "新增紀錄";
  document.getElementById("cancelGiftRecordEditBtn").hidden = true;
  setDefaultDates();
  renderSelects();
}

function applySelectedGiftItem() {
  const form = document.getElementById("giftRecordForm");
  const itemId = Number(form.elements.serviceItemId.value);
  const item = state.serviceItems.find((serviceItem) => serviceItem.id === itemId);
  if (!item) {
    return;
  }

  form.elements.giftName.value = "";
  if (item.defaultPrice != null) {
    form.elements.amount.value = item.defaultPrice;
  }
}

async function submitOrder(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  let orderId = data.get("orderId");
  const editControlsVisible = !document.getElementById("copyOrderBtn").hidden;
  if (orderId && !editControlsVisible) {
    form.elements.orderId.value = "";
    orderId = "";
  }
  const isEdit = Boolean(orderId);
  const amount = Number(data.get("amount"));
  const commissionAmount = Number(data.get("commissionAmount"));
  const memberRows = [...document.querySelectorAll(".member-row")];
  fillBlankMemberShares(memberRows, amount - commissionAmount);
  const members = memberRows.map((row) => ({
    userId: Number(row.querySelector("[data-member-select]").value),
    role: row.querySelector("[data-member-role]").value,
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
      orderDate: data.get("orderDate"),
      ownerUserId: data.get("ownerUserId") ? Number(data.get("ownerUserId")) : null,
      amount,
      commissionRate: 0.1,
      commissionAmount,
      status: data.get("status"),
      customerPaymentStatus: data.get("customerPaymentStatus"),
      remark: emptyToNull(data.get("remark")),
      members
    };

    await api(isEdit ? `/api/orders/${orderId}` : "/api/orders", {
      method: isEdit ? "PUT" : "POST",
      body: JSON.stringify(payload)
    });
    resetOrderForm();
    await loadOrders();
    await loadDashboard();
    showAlert(isEdit ? "訂單已更新。" : "訂單已新增。", false);
    document.getElementById("orderRows").closest(".panel").scrollIntoView({ behavior: "smooth", block: "start" });
  });
}

function startOrderEdit(order) {
  const form = document.getElementById("orderForm");
  form.elements.orderId.value = order.id;
  form.elements.orderDate.value = order.orderDate;
  form.elements.orderNo.value = order.orderNo || "";
  form.elements.ownerUserId.value = order.ownerUserId || "";
  form.elements.amount.value = order.amount;
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
  updateOrderCalc();
  form.scrollIntoView({ behavior: "smooth", block: "start" });
}

function copyOrderAsNew() {
  const form = document.getElementById("orderForm");
  form.elements.orderId.value = "";
  document.getElementById("orderFormTitle").textContent = "新增訂單";
  document.getElementById("orderSubmitBtn").textContent = "新增訂單";
  document.getElementById("copyOrderBtn").hidden = true;
  document.getElementById("cancelOrderEditBtn").hidden = true;
  showAlert("已切換成新增訂單，送出後會建立新資料，不會覆蓋原訂單。", false);
}

function resetOrderForm() {
  const form = document.getElementById("orderForm");
  form.reset();
  form.elements.orderId.value = "";
  document.getElementById("memberRows").innerHTML = "";
  setDefaultDates();
  addMemberRow();
  document.getElementById("orderFormTitle").textContent = "新增訂單";
  document.getElementById("orderSubmitBtn").textContent = "新增訂單";
  document.getElementById("copyOrderBtn").hidden = true;
  document.getElementById("cancelOrderEditBtn").hidden = true;
  updateOrderCalc();
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
    gift: "禮物",
    deposit_bonus: "預存",
    other: "其他"
  }[category] || category;
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
  return `<span class="pill ${type}">${escapeHtml(text)}</span>`;
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
