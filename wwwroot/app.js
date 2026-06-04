const state = {
  users: [],
  players: [],
  bosses: [],
  orders: [],
  payments: [],
  view: "dashboard"
};

const titles = {
  dashboard: ["Dashboard", "總覽"],
  users: ["Users", "成員"],
  orders: ["Orders", "訂單"],
  payments: ["Payments", "月結"],
  audit: ["Audit", "紀錄"]
};

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
    audit_logs: "操作紀錄"
  }
};

document.addEventListener("DOMContentLoaded", async () => {
  bindNavigation();
  bindForms();
  setDefaultDates();
  addMemberRow();
  await refreshAll();
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
  document.getElementById("userForm").addEventListener("submit", submitUser);
  document.getElementById("orderForm").addEventListener("submit", submitOrder);
  document.getElementById("paymentForm").addEventListener("submit", submitPaymentGeneration);
  document.getElementById("orderForm").addEventListener("input", updateOrderCalc);
}

function setDefaultDates() {
  const today = new Date().toISOString().slice(0, 10);
  document.querySelector("[name='orderDate']").value = today;
  document.querySelector("[name='payMonth']").value = today.slice(0, 7);
}

async function api(path, options = {}) {
  const response = await fetch(path, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {})
    },
    ...options
  });

  if (!response.ok) {
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

async function refreshAll() {
  try {
    await api("/api/health");
    document.getElementById("apiStatus").classList.add("online");
    hideAlert();

    if (state.view === "dashboard") {
      await loadDashboard();
    }
    if (state.view === "users" || state.view === "orders") {
      await loadUsers();
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

function renderUserTable(elementId, users) {
  const body = document.getElementById(elementId);
  body.innerHTML = users.length ? users.map((user) => `
    <tr>
      <td>${user.id}</td>
      <td>${escapeHtml(user.nickname)}</td>
      <td>${label("systemRole", user.systemRole)}</td>
      <td>${user.isActive ? pill("啟用", "good") : pill("停用", "bad")}</td>
      <td>
        ${user.isActive
          ? `<button class="ghost small" data-user-deactivate="${user.id}">停用</button>`
          : `<button class="ghost small" data-user-activate="${user.id}">啟用</button>`}
      </td>
    </tr>
  `).join("") : emptyRow(5);

  bindUserTableActions(body);
}

function bindUserTableActions(body) {
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
    </tr>
  `).join("") : emptyRow(7);
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

function addMemberRow() {
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
}

async function submitUser(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  await runAction(async () => {
    await api("/api/users", {
      method: "POST",
      body: JSON.stringify({
        nickname: data.get("nickname"),
        discordId: emptyToNull(data.get("discordId")),
        discordName: emptyToNull(data.get("discordName")),
        bankAccount: emptyToNull(data.get("bankAccount")),
        systemRole: data.get("systemRole"),
        isPlayer: data.get("isPlayer") === "on",
        isBoss: data.get("isBoss") === "on"
      })
    });
    form.reset();
    form.elements.isPlayer.checked = true;
    await loadUsers();
    showAlert("成員已新增。", false);
  });
}

async function submitOrder(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const members = [...document.querySelectorAll(".member-row")].map((row) => ({
    userId: Number(row.querySelector("[data-member-select]").value),
    role: row.querySelector("[data-member-role]").value,
    shareAmount: Number(row.querySelector("[data-member-share]").value || 0)
  }));

  await runAction(async () => {
    await api("/api/orders", {
      method: "POST",
      body: JSON.stringify({
        orderNo: emptyToNull(data.get("orderNo")),
        orderDate: data.get("orderDate"),
        ownerUserId: data.get("ownerUserId") ? Number(data.get("ownerUserId")) : null,
        amount: Number(data.get("amount")),
        commissionRate: 0.1,
        commissionAmount: Number(data.get("commissionAmount")),
        status: data.get("status"),
        customerPaymentStatus: data.get("customerPaymentStatus"),
        remark: emptyToNull(data.get("remark")),
        members
      })
    });
    form.reset();
    document.getElementById("memberRows").innerHTML = "";
    setDefaultDates();
    addMemberRow();
    await loadOrders();
    await loadDashboard();
    showAlert("訂單已新增。", false);
  });
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
