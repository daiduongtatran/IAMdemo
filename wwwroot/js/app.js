// ============================================================
// IAM DEMO - FRONTEND JAVASCRIPT
// ============================================================

const API_URL = `${window.location.origin}/api`;
let currentToken = null;
let currentUser = null;

// Map permission codes to Vietnamese labels
const permissionLabels = {
    'read:all_data': 'Đọc Tất Cả Dữ Liệu',
    'write:all_data': 'Ghi Tất Cả Dữ Liệu',
    'delete:all_data': 'Xóa Tất Cả Dữ Liệu',
    'manage:users': 'Quản Lý Người Dùng',
    'manage:roles': 'Quản Lý Vai Trò',
    'view:admin_panel': 'Xem Bảng Điều Khiển Admin',
    'read:own_data': 'Đọc Dữ Liệu Của Tôi',
    'write:own_data': 'Ghi Dữ Liệu Của Tôi',
    'read:shared_data': 'Đọc Dữ Liệu Được Chia Sẻ',
    'read:limited_data': 'Đọc Dữ Liệu Hạn Chế'
};

// ============================================================
// INITIALIZATION
// ============================================================

document.addEventListener('DOMContentLoaded', () => {
    // Check if already logged in
    const storedToken = localStorage.getItem('iamToken');
    if (storedToken) {
        currentToken = storedToken;
        getCurrentUserInfo();
    }

    // Event listeners
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
    document.getElementById('logoutBtn').addEventListener('click', handleLogout);
});

// ============================================================
// LOGIN / LOGOUT
// ============================================================

async function handleLogin(e) {
    e.preventDefault();

    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value;

    if (!username || !password) {
        showLoginError('Vui lòng nhập tên đăng nhập và mật khẩu');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ tenDangNhap: username, matKhau: password })
        });

        const data = await response.json();

        if (!response.ok) {
            showLoginError(data.message || 'Đăng nhập thất bại');
            return;
        }

        currentToken = data.token;
        currentUser = { tenDangNhap: data.tenDangNhap, vaiTro: data.vaiTro };
        localStorage.setItem('iamToken', currentToken);
        document.getElementById('loginError').classList.add('hidden');

        showDashboard();
        getCurrentUserInfo();

    } catch (error) {
        console.error('Login error:', error);
        showLoginError('Lỗi kết nối: ' + error.message);
    }
}

function handleLogout() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        currentToken = null;
        currentUser = null;
        localStorage.removeItem('iamToken');

        document.getElementById('loginForm').reset();
        document.getElementById('username').value = 'admin';
        document.getElementById('password').value = '123';

        showLoginForm();
        hideUserInfo();
    }
}

function quickLogin(username, password) {
    document.getElementById('username').value = username;
    document.getElementById('password').value = password;
    document.getElementById('loginForm').dispatchEvent(new Event('submit'));
}

// ============================================================
// UI UPDATES
// ============================================================

function showLoginForm() {
    document.getElementById('loginSection').classList.remove('hidden');
    document.getElementById('dashboardSection').classList.add('hidden');
    hideUserInfo();
}

function showDashboard() {
    document.getElementById('loginSection').classList.add('hidden');
    document.getElementById('dashboardSection').classList.remove('hidden');
    showUserInfo();
}

function showUserInfo() {
    document.getElementById('userInfo').classList.remove('hidden');
    if (currentUser) {
        document.getElementById('userName').textContent = currentUser.tenDangNhap;
        document.getElementById('userRole').textContent = currentUser.vaiTro;
    }
}

function hideUserInfo() {
    document.getElementById('userInfo').classList.add('hidden');
}

function showLoginError(message) {
    const errorDiv = document.getElementById('loginError');
    errorDiv.textContent = message;
    errorDiv.classList.remove('hidden');
}

// ============================================================
// API CALLS
// ============================================================

async function getCurrentUserInfo() {
    if (!currentToken) return;

    try {
        const response = await fetch(`${API_URL}/data/me`, {
            headers: { 'Authorization': `Bearer ${currentToken}` }
        });

        if (!response.ok) {
            if (response.status === 401) {
                handleLogout();
            }
            return;
        }

        const data = await response.json();

        document.getElementById('infoUserId').textContent = data.userId;
        document.getElementById('infoUserName').textContent = data.userName;
        document.getElementById('infoUserRole').textContent = data.role;
        document.getElementById('infoToken').textContent = currentToken.substring(0, 20) + '...';

        updatePermissions(data.permissions);

        // Show admin panel if user is admin
        if (data.role === 'Admin') {
            document.getElementById('adminPanel').classList.remove('hidden');
            loadAllUsers();
        } else {
            document.getElementById('adminPanel').classList.add('hidden');
        }

        showDashboard();

    } catch (error) {
        console.error('Error getting user info:', error);
    }
}

function updatePermissions(permissions) {
    const container = document.getElementById('permissionsList');
    container.innerHTML = '';

    if (!permissions || permissions.length === 0) {
        container.innerHTML = '<div class="permission-badge">Không có quyền hạn</div>';
        return;
    }

    permissions.forEach(permission => {
        const badge = document.createElement('div');
        badge.className = 'permission-badge';
        badge.textContent = permissionLabels[permission] || permission;
        container.appendChild(badge);
    });
}

// ============================================================
// USER MANAGEMENT
// ============================================================

async function loadAllUsers() {
    if (!currentToken) return;

    try {
        const response = await fetch(`${API_URL}/data/users`, {
            headers: { 'Authorization': `Bearer ${currentToken}` }
        });

        if (!response.ok) return;

        const users = await response.json();
        displayUsersList(users);

    } catch (error) {
        console.error('Error loading users:', error);
    }
}

function displayUsersList(users) {
    const container = document.getElementById('usersList');
    container.innerHTML = '';

    if (!users || users.length === 0) {
        container.innerHTML = '<p>Không có người dùng</p>';
        return;
    }

    users.forEach(user => {
        const userDiv = document.createElement('div');
        userDiv.className = 'user-item';
        
        const info = document.createElement('div');
        info.className = 'user-item-info';
        info.innerHTML = `<div class="user-item-name">${user.tenDangNhap}</div>
                          <div class="user-item-role">Vai trò: ${user.vaiTro}</div>`;
        
        const actions = document.createElement('div');
        actions.className = 'user-item-actions';

        // Delete button
        if (user.id != 1) { // Don't allow deleting admin
            const deleteBtn = document.createElement('button');
            deleteBtn.className = 'btn';
            deleteBtn.textContent = 'Xóa';
            deleteBtn.onclick = () => deleteUser(user.id);
            actions.appendChild(deleteBtn);
        }

        // Role change dropdown
        const roleSelect = document.createElement('select');
        roleSelect.className = 'form-input';
        roleSelect.style.padding = '6px 8px';
        roleSelect.style.fontSize = '0.85rem';
        roleSelect.innerHTML = '<option value="User">User</option><option value="Admin">Admin</option>';
        roleSelect.value = user.vaiTro;
        roleSelect.onchange = () => updateUserRole(user.id, roleSelect.value);
        actions.appendChild(roleSelect);

        userDiv.appendChild(info);
        userDiv.appendChild(actions);
        container.appendChild(userDiv);
    });
}

async function createNewUser() {
    const username = document.getElementById('newUsername').value.trim();
    const password = document.getElementById('newPassword').value;
    const role = document.getElementById('newUserRole').value;

    if (!username || !password) {
        alert('Vui lòng nhập tên đăng nhập và mật khẩu');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/data/users`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${currentToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                tenDangNhap: username,
                matKhau: password,
                vaiTro: role
            })
        });

        if (!response.ok) {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể tạo người dùng'));
            return;
        }

        alert('Tạo người dùng thành công');
        document.getElementById('newUsername').value = '';
        document.getElementById('newPassword').value = '';
        loadAllUsers();

    } catch (error) {
        console.error('Error creating user:', error);
        alert('Lỗi kết nối: ' + error.message);
    }
}

async function deleteUser(userId) {
    if (!confirm('Bạn có chắc chắn muốn xóa người dùng này?')) return;

    try {
        const response = await fetch(`${API_URL}/data/users/${userId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${currentToken}` }
        });

        if (!response.ok) {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể xóa người dùng'));
            return;
        }

        alert('Xóa người dùng thành công');
        loadAllUsers();

    } catch (error) {
        console.error('Error deleting user:', error);
        alert('Lỗi kết nối: ' + error.message);
    }
}

async function updateUserRole(userId, newRole) {
    try {
        const response = await fetch(`${API_URL}/data/users/${userId}/role`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${currentToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ vaiTro: newRole })
        });

        if (!response.ok) {
            alert('Không thể cập nhật vai trò');
            return;
        }

        alert('Cập nhật vai trò thành công');
        loadAllUsers();

    } catch (error) {
        console.error('Error updating user role:', error);
        alert('Lỗi kết nối: ' + error.message);
    }
}

// ============================================================
// TEST ENDPOINTS
// ============================================================

async function testEndpoint(endpoint) {
    if (!currentToken) {
        alert('Vui lòng đăng nhập trước');
        return;
    }

    const startTime = performance.now();
    let url = `${API_URL}/data`;
    let description = '';

    switch (endpoint) {
        case 'common-info':
            url += '/common-info';
            description = 'Thông Tin Chung - Tất Cả Users';
            break;
        case 'admin-secret':
            url += '/admin-only-secret';
            description = 'Admin Secret Data';
            break;
        case 'user-profile':
            url += '/user-profile';
            description = 'User Profile (Không Phải Admin)';
            break;
        case 'get-me':
            url += '/me';
            description = 'Thông Tin Người Dùng Hiện Tại';
            break;
        default:
            return;
    }

    try {
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${currentToken}`,
                'Content-Type': 'application/json'
            }
        });

        const endTime = performance.now();
        const duration = Math.round(endTime - startTime);

        let responseText = '';
        try {
            const data = await response.json();
            responseText = JSON.stringify(data, null, 2);
        } catch {
            responseText = await response.text();
        }

        displayResponse(response.status, duration, description, responseText);

    } catch (error) {
        const endTime = performance.now();
        const duration = Math.round(endTime - startTime);
        displayResponse(0, duration, description, 'Error: ' + error.message);
    }
}

function displayResponse(statusCode, duration, description, body) {
    const statusBadge = document.getElementById('responseStatus');
    
    if (statusCode === 200) {
        statusBadge.textContent = '200 OK';
    } else if (statusCode === 403) {
        statusBadge.textContent = '403 Forbidden';
    } else if (statusCode === 401) {
        statusBadge.textContent = '401 Unauthorized';
    } else if (statusCode === 0) {
        statusBadge.textContent = 'Error';
    } else {
        statusBadge.textContent = statusCode;
    }

    let content = `Endpoint: ${description}\n`;
    content += `Status: ${statusCode}\n`;
    content += `Duration: ${duration}ms\n\n`;
    content += '─────────────────────────────────────\n\n';
    content += body;

    document.getElementById('responseContent').textContent = content;
}
