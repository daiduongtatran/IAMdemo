const API_URL = `${window.location.origin}/api`;
let currentToken = null;
let currentUser = null;
let tempMfaToken = null; // Biến giữ vé chờ xác thực MFA

const permissionLabels = {
  "read:all_data": "Đọc Tất Cả Dữ Liệu",
  "write:all_data": "Ghi Tất Cả Dữ Liệu",
  "delete:all_data": "Xóa Tất Cả Dữ Liệu",
  "manage:users": "Quản Lý Người Dùng",
  "manage:roles": "Quản Lý Vai Trò",
  "view:admin_panel": "Xem Bảng Điều Khiển Admin",
  "read:own_data": "Đọc Dữ Liệu Của Tôi",
  "write:own_data": "Ghi Dữ Liệu Của Tôi",
  "read:shared_data": "Đọc Dữ Liệu Được Chia Sẻ",
  "read:limited_data": "Đọc Dữ Liệu Hạn Chế",
};

document.addEventListener("DOMContentLoaded", () => {
  const storedToken = localStorage.getItem("iamToken");
  if (storedToken) {
    currentToken = storedToken;
    getCurrentUserInfo();
  }

  document.getElementById("loginForm").addEventListener("submit", handleLogin);
  document.getElementById("mfaForm").addEventListener("submit", verifyMfa); // Lắng nghe form MFA
  document.getElementById("logoutBtn").addEventListener("click", handleLogout);
});

// BƯỚC 1: ĐĂNG NHẬP LẤY VÉ CHỜ MFA
async function handleLogin(e) {
  e.preventDefault();

  const username = document.getElementById("username").value.trim();
  const password = document.getElementById("password").value;

  if (!username || !password) {
    showLoginError("Vui lòng nhập tên đăng nhập và mật khẩu");
    return;
  }

  try {
    const response = await fetch(`${API_URL}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ tenDangNhap: username, matKhau: password }),
    });

    const data = await response.json();

    if (!response.ok) {
      showLoginError(data.message || "Đăng nhập thất bại");
      return;
    }

    // NẾU HỆ THỐNG YÊU CẦU MFA (Có TempToken)
    if (data.tempToken) {
        tempMfaToken = data.tempToken;
        
        // Ẩn form login, hiện form MFA
        document.getElementById("loginSection").classList.add("hidden");
        document.getElementById("mfaSection").classList.remove("hidden");
        
        // Cấu hình mã QR nếu là lần đầu đăng nhập
        if (data.isSetupRequired) {
            document.getElementById("qrSetupSection").classList.remove("hidden");
            // DÁN DÒNG NÀY VÀO THẾ CHỖ
document.getElementById("qrCodeImg").src = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(data.qrCodeUri)}`;
            document.getElementById("setupCodeText").textContent = data.setupCode;
            showToast("Lần đầu đăng nhập! Vui lòng cài đặt Authenticator.", "success");
        } else {
            document.getElementById("qrSetupSection").classList.add("hidden");
            showToast("Mở ứng dụng Authenticator để lấy mã OTP.", "success");
        }
        
        document.getElementById("otpCode").value = "";
        document.getElementById("otpCode").focus();
        return;
    }

    // (Phòng hờ nếu tắt MFA) Đăng nhập bình thường
    finalizeLogin(data);
  } catch (error) {
    console.error("Lỗi đăng nhập:", error);
    showLoginError("Lỗi kết nối: " + error.message);
  }
}

// BƯỚC 2: XÁC THỰC MÃ OTP
async function verifyMfa(e) {
    e.preventDefault();
    
    const otpCode = document.getElementById("otpCode").value.trim();
    if (!otpCode || otpCode.length !== 6) {
        showMfaError("Vui lòng nhập đủ 6 số OTP");
        return;
    }

    try {
        const response = await fetch(`${API_URL}/auth/verify-mfa`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ tempToken: tempMfaToken, otpCode: otpCode })
        });

        const data = await response.json();

        if (!response.ok) {
            showMfaError(data.message || "Mã OTP không hợp lệ hoặc đã hết hạn!");
            return;
        }

        // Xác thực thành công
        document.getElementById("mfaSection").classList.add("hidden");
        document.getElementById("mfaError").classList.add("hidden");
        tempMfaToken = null;
        
        finalizeLogin(data);
    } catch (error) {
        console.error("Lỗi xác thực MFA:", error);
        showMfaError("Lỗi kết nối: " + error.message);
    }
}

// HÀM CHỐT ĐĂNG NHẬP (LƯU TOKEN CHÍNH THỨC)
function finalizeLogin(data) {
    currentToken = data.token;
    currentUser = { tenDangNhap: data.tenDangNhap, vaiTro: data.vaiTro };
    localStorage.setItem("iamToken", currentToken);
    
    document.getElementById("loginError").classList.add("hidden");
    
    showDashboard();
    getCurrentUserInfo();
    showToast("Đăng nhập thành công!", "success");
}

function cancelMfa() {
    tempMfaToken = null;
    document.getElementById("mfaSection").classList.add("hidden");
    document.getElementById("loginSection").classList.remove("hidden");
    document.getElementById("mfaError").classList.add("hidden");
}

function handleLogout() {
  if (confirm("Bạn có chắc chắn muốn đăng xuất?")) {
    currentToken = null;
    currentUser = null;
    tempMfaToken = null;
    localStorage.removeItem("iamToken");

    document.getElementById("loginForm").reset();
    document.getElementById("username").value = "";
    document.getElementById("password").value = "";

    showLoginForm();
    hideUserInfo();
  }
}

function showLoginForm() {
  document.getElementById("loginSection").classList.remove("hidden");
  document.getElementById("mfaSection").classList.add("hidden");
  document.getElementById("dashboardSection").classList.add("hidden");
  hideUserInfo();
}

function showDashboard() {
  document.getElementById("loginSection").classList.add("hidden");
  document.getElementById("mfaSection").classList.add("hidden");
  document.getElementById("dashboardSection").classList.remove("hidden");
  showUserInfo();
}

function showUserInfo() {
  document.getElementById("userInfo").classList.remove("hidden");
  if (currentUser) {
    document.getElementById("userName").textContent = currentUser.tenDangNhap;
    document.getElementById("userRole").textContent = currentUser.vaiTro;
  }
}

function hideUserInfo() {
  document.getElementById("userInfo").classList.add("hidden");
}

function showLoginError(message) {
  const errorDiv = document.getElementById("loginError");
  errorDiv.textContent = message;
  errorDiv.classList.remove("hidden");
}

function showMfaError(message) {
    const errorDiv = document.getElementById("mfaError");
    errorDiv.textContent = message;
    errorDiv.classList.remove("hidden");
}

async function getCurrentUserInfo() {
  if (!currentToken) return;

  try {
    const response = await fetch(`${API_URL}/data/me`, {
      headers: { Authorization: `Bearer ${currentToken}` },
    });

    if (!response.ok) {
      if (response.status === 401) {
        handleLogout();
      }
      return;
    }

    const data = await response.json();

    document.getElementById("infoUserId").textContent = data.userId;
    document.getElementById("infoUserName").textContent = data.userName;
    document.getElementById("infoUserRole").textContent = data.role;
    document.getElementById("infoToken").textContent = currentToken.substring(0, 20) + "...";

    updatePermissions(data.permissions);

    if (data.role === "Admin") {
      document.getElementById("adminPanel").classList.remove("hidden");
      loadAllUsers();
    } else {
      document.getElementById("adminPanel").classList.add("hidden");
    }

    showDashboard();
  } catch (error) {
    console.error("Lỗi lấy thông tin người dùng:", error);
  }
}

function updatePermissions(permissions) {
  const container = document.getElementById("permissionsList");
  container.innerHTML = "";

  if (!permissions || permissions.length === 0) {
    container.innerHTML = '<div class="permission-badge">Không có quyền hạn</div>';
    return;
  }

  permissions.forEach((permission) => {
    const badge = document.createElement("div");
    badge.className = "permission-badge";
    badge.textContent = permissionLabels[permission] || permission;
    container.appendChild(badge);
  });
}

async function loadAllUsers() {
  if (!currentToken) return;

  try {
    const response = await fetch(`${API_URL}/data/users`, {
      headers: { Authorization: `Bearer ${currentToken}` },
    });

    if (!response.ok) return;

    const users = await response.json();
    displayUsersList(users);
  } catch (error) {
    console.error("Lỗi tải danh sách người dùng:", error);
  }
}

function displayUsersList(users) {
  const container = document.getElementById("usersList");
  container.innerHTML = "";

  if (!users || users.length === 0) {
    container.innerHTML = "<p>Không có người dùng</p>";
    return;
  }

  users.forEach((user) => {
    const userDiv = document.createElement("div");
    userDiv.className = "user-item";

    const info = document.createElement("div");
    info.className = "user-item-info";
    info.innerHTML = `<div class="user-item-name">${user.tenDangNhap}</div>
                          <div class="user-item-role">Vai trò: ${user.vaiTro}</div>`;

    const actions = document.createElement("div");
    actions.className = "user-item-actions";

    if (user.id != 1) {
      const deleteBtn = document.createElement("button");
      deleteBtn.className = "btn";
      deleteBtn.textContent = "Xóa";
      deleteBtn.onclick = () => deleteUser(user.id);
      actions.appendChild(deleteBtn);
    }

    const roleSelect = document.createElement("select");
    roleSelect.className = "form-input";
    roleSelect.style.padding = "6px 8px";
    roleSelect.style.fontSize = "0.85rem";
    roleSelect.innerHTML =
      '<option value="User">User</option><option value="Admin">Admin</option>';
    roleSelect.value = user.vaiTro;
    roleSelect.onchange = () => updateUserRole(user.id, roleSelect.value);
    actions.appendChild(roleSelect);

    userDiv.appendChild(info);
    userDiv.appendChild(actions);
    container.appendChild(userDiv);
  });
}

async function createNewUser() {
  const username = document.getElementById("newUsername").value.trim();
  const password = document.getElementById("newPassword").value;
  const role = document.getElementById("newUserRole").value;

  if (!username || !password) {
    showToast("Vui lòng nhập tên đăng nhập và mật khẩu", "error");
    return;
  }

  try {
    const response = await fetch(`${API_URL}/data/users`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${currentToken}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ tenDangNhap: username, matKhau: password, vaiTro: role }),
    });

    if (!response.ok) {
      const error = await response.json();
      showToast("Lỗi: " + (error.message || "Không thể tạo người dùng"), "error");
      return;
    }

    showToast("Tạo người dùng thành công", "success");
    document.getElementById("newUsername").value = "";
    document.getElementById("newPassword").value = "";
    loadAllUsers();
  } catch (error) {
    console.error("Lỗi tạo người dùng:", error);
    showToast("Lỗi kết nối: " + error.message, "error");
  }
}

async function deleteUser(userId) {
  if (!confirm("Bạn có chắc chắn muốn xóa người dùng này?")) return;

  try {
    const response = await fetch(`${API_URL}/data/users/${userId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${currentToken}` },
    });

    if (!response.ok) {
      const error = await response.json();
      showToast("Lỗi: " + (error.message || "Không thể xóa người dùng"), "error");
      return;
    }

    showToast("Xóa người dùng thành công", "success");
    loadAllUsers();
  } catch (error) {
    console.error("Lỗi xóa người dùng:", error);
    showToast("Lỗi kết nối: " + error.message, "error");
  }
}

async function updateUserRole(userId, newRole) {
  try {
    const response = await fetch(`${API_URL}/data/users/${userId}/role`, {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${currentToken}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ vaiTro: newRole }),
    });

    if (!response.ok) {
      showToast("Không thể cập nhật vai trò", "error");
      return;
    }

    showToast("Cập nhật vai trò thành công", "success");
    loadAllUsers();
  } catch (error) {
    console.error("Lỗi cập nhật vai trò:", error);
    showToast("Lỗi kết nối: " + error.message, "error");
  }
}

async function testEndpoint(endpoint) {
  if (!currentToken) {
    showToast("Vui lòng đăng nhập trước", "error");
    return;
  }

  const startTime = performance.now();
  let url = `${API_URL}/data`;
  let description = "";

  switch (endpoint) {
    case "common-info":
      url += "/common-info";
      description = "Thông Tin Chung - Tất Cả Users";
      break;
    case "admin-secret":
      url += "/admin-only-secret";
      description = "Admin Secret Data";
      break;
    case "user-profile":
      url += "/user-profile";
      description = "User Profile (Không Phải Admin)";
      break;
    case "get-me":
      url += "/me";
      description = "Thông Tin Người Dùng Hiện Tại";
      break;
    default:
      return;
  }

  try {
    const response = await fetch(url, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${currentToken}`,
        "Content-Type": "application/json",
      },
    });

    const endTime = performance.now();
    const duration = Math.round(endTime - startTime);

    let responseText = "";
    try {
      const data = await response.json();
      responseText = JSON.stringify(data, null, 2);
    } catch {
      responseText = await response.text();
    }

    displayResponse(response.status, duration, description, responseText);
    showToast("Kết quả được hiển thị dưới", "success");
  } catch (error) {
    const endTime = performance.now();
    const duration = Math.round(endTime - startTime);
    displayResponse(0, duration, description, "Lỗi: " + error.message);
    showToast("Có lỗi xảy ra", "error");
  }
}

function displayResponse(statusCode, duration, description, body) {
  const statusBadge = document.getElementById("responseStatus");

  if (statusCode === 200) {
    statusBadge.textContent = "✓ 200 OK";
  } else if (statusCode === 403) {
    statusBadge.textContent = "✕ 403 Forbidden";
  } else if (statusCode === 401) {
    statusBadge.textContent = "✕ 401 Unauthorized";
  } else if (statusCode === 0) {
    statusBadge.textContent = "✕ Lỗi";
  } else {
    statusBadge.textContent = statusCode;
  }

  let content = `Điểm cuối: ${description}\n`;
  content += `Trạng thái: ${statusCode}\n`;
  content += `Thời gian: ${duration}ms\n\n`;
  content += "─────────────────────────────────────\n\n";
  content += body;

  document.getElementById("responseContent").textContent = content;
}

function showToast(message, type = "success") {
  const toast = document.createElement("div");
  toast.className = `toast-notification ${type}`;
  toast.textContent = message;
  document.body.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = "0";
    toast.style.transform = "translateY(20px)";
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}