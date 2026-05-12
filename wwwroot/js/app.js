// ============================================================
// IAM DEMO - FRONTEND JAVASCRIPT
// ============================================================

const API_URL = `${window.location.origin}/api`;
let currentToken = null;
let currentUser = null;

// ============================================================
// INITIALIZATION
// ============================================================

document.addEventListener('DOMContentLoaded', () => {
    const debugUrlEl = document.getElementById('debugUrl');
    if (debugUrlEl) debugUrlEl.textContent = window.location.origin;

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
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                tenDangNhap: username,
                matKhau: password
            })
        });

        const data = await response.json();

        if (!response.ok) {
            showLoginError(data.message || 'Đăng nhập thất bại');
            return;
        }

        // Success
        currentToken = data.token;
        currentUser = {
            tenDangNhap: data.tenDangNhap,
            vaiTro: data.vaiTro
        };

        localStorage.setItem('iamToken', currentToken);
        document.getElementById('loginError').classList.add('hidden');

        // Update UI
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

        // Reset form
        document.getElementById('loginForm').reset();
        document.getElementById('username').value = 'admin';
        document.getElementById('password').value = '123';

        // Update UI
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
    errorDiv.textContent = '❌ ' + message;
    errorDiv.classList.remove('hidden');
}

// ============================================================
// API CALLS
// ============================================================

async function getCurrentUserInfo() {
    if (!currentToken) return;

    try {
        const response = await fetch(`${API_URL}/data/me`, {
            headers: {
                'Authorization': `Bearer ${currentToken}`
            }
        });

        if (!response.ok) {
            if (response.status === 401) {
                handleLogout();
            }
            return;
        }

        const data = await response.json();

        // Update user info
        document.getElementById('infoUserId').textContent = data.userId;
        document.getElementById('infoUserName').textContent = data.userName;
        document.getElementById('infoUserRole').textContent = data.role;
        
        // Show role badge with color
        const roleElement = document.getElementById('infoUserRole');
        if (data.role === 'Admin') {
            roleElement.parentElement.innerHTML = '<span class="badge badge-admin">' + data.role + '</span>';
        } else {
            roleElement.parentElement.innerHTML = '<span class="badge badge-user">' + data.role + '</span>';
        }

        document.getElementById('infoUserStatus').textContent = data.status;
        document.getElementById('infoToken').textContent = currentToken.substring(0, 20) + '...';
        document.getElementById('infoExpiry').textContent = '1 hour';

        // Update permissions
        updatePermissions(data.permissions);

        // Update dashboard
        showDashboard();

    } catch (error) {
        console.error('Error getting user info:', error);
    }
}

function updatePermissions(permissions) {
    const container = document.getElementById('permissionsList');
    container.innerHTML = '';

    if (!permissions || permissions.length === 0) {
        container.innerHTML = '<p>Không có quyền hạn</p>';
        return;
    }

    permissions.forEach(permission => {
        const badge = document.createElement('span');
        badge.className = 'permission-badge';
        badge.textContent = '✓ ' + permission;
        container.appendChild(badge);
    });
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
            description = 'Thông tin công khai cho tất cả users';
            break;
        case 'admin-secret':
            url += '/admin-only-secret';
            description = 'Dữ liệu tối mật - chỉ Admin';
            break;
        case 'user-profile':
            url += '/user-profile';
            description = 'Hồ sơ người dùng (không phải Admin)';
            break;
        case 'get-me':
            url += '/me';
            description = 'Thông tin người dùng hiện tại';
            break;
        case 'check-permission':
            url += '/check-permission/read:all_data';
            description = 'Kiểm tra quyền: read:all_data';
            break;
        default:
            return;
    }

    updateDebugInfo('Last Request', endpoint);

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

        let data = null;
        let responseText = '';

        try {
            data = await response.json();
            responseText = JSON.stringify(data, null, 2);
        } catch {
            responseText = await response.text();
        }

        // Update response display
        displayResponse(response.status, duration, description, responseText, response.ok);

    } catch (error) {
        const endTime = performance.now();
        const duration = Math.round(endTime - startTime);
        displayResponse(0, duration, description, 'Error: ' + error.message, false);
    }
}

function displayResponse(statusCode, duration, description, body, isSuccess) {
    // Update status
    const statusBadge = document.getElementById('responseStatus');
    if (statusCode === 200) {
        statusBadge.textContent = '✓ 200 OK';
        statusBadge.className = 'status-badge status-success';
    } else if (statusCode === 403) {
        statusBadge.textContent = '✗ 403 Forbidden';
        statusBadge.className = 'status-badge status-error';
    } else if (statusCode === 401) {
        statusBadge.textContent = '✗ 401 Unauthorized';
        statusBadge.className = 'status-badge status-error';
    } else if (statusCode === 0) {
        statusBadge.textContent = '✗ Error';
        statusBadge.className = 'status-badge status-error';
    } else {
        statusBadge.textContent = '✗ ' + statusCode;
        statusBadge.className = 'status-badge status-error';
    }

    // Update time
    document.getElementById('responseTime').textContent = duration + 'ms';

    // Update body
    let content = `╔════════════════════════════════════════════╗
║  Endpoint: ${description}
║  Status: ${statusCode === 200 ? '✓ Success' : '✗ Error'}
║  Duration: ${duration}ms
╚════════════════════════════════════════════╝

`;

    if (typeof body === 'string') {
        content += body;
    } else {
        content += JSON.stringify(body, null, 2);
    }

    document.getElementById('responseContent').textContent = content;
}

// ============================================================
// DEBUG UTILITIES
// ============================================================

function updateDebugInfo(label, value) {
    const debugLabel = {
        'Last Request': 'debugLastRequest',
        'Token Stored': 'debugToken'
    };

    const elementId = debugLabel[label];
    if (elementId) {
        document.getElementById(elementId).textContent = value;
    }
}

// ============================================================
// KEYBOARD SHORTCUTS
// ============================================================

document.addEventListener('keydown', (e) => {
    // Ctrl+L untuk logout
    if (e.ctrlKey && e.key === 'l') {
        e.preventDefault();
        if (currentToken) {
            handleLogout();
        }
    }

    // Ctrl+Q để test quick permission
    if (e.ctrlKey && e.key === 'q') {
        e.preventDefault();
        testEndpoint('check-permission');
    }
});

// ============================================================
// ERROR HANDLING
// ============================================================

window.addEventListener('error', (e) => {
    console.error('Global error:', e.error);
});

// ============================================================
// PERIODIC TOKEN CHECK
// ============================================================

// Check token validity every 5 minutes
setInterval(() => {
    if (currentToken && currentUser) {
        getCurrentUserInfo();
    }
}, 5 * 60 * 1000);

console.log('🔐 IAM Demo UI loaded successfully!');
console.log('💡 Keyboard Shortcuts:');
console.log('   Ctrl+L: Logout');
console.log('   Ctrl+Q: Test Permission');
