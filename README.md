# 🔐 Hệ Thống IAM (Identity and Access Management) Demo

Một hệ thống quản lý danh tính và quyền truy cập hoàn chỉnh, an toàn và tuân theo các tiêu chuẩn công nghiệp IAM.

## 📋 Mục Lục
1. [Tính Năng](#tính-năng)
2. [Kiến Trúc](#kiến-trúc)
3. [Setup & Installation](#setup--installation)
4. [API Endpoints](#api-endpoints)
5. [Bảo Mật](#bảo-mật)
6. [Ví Dụ Sử Dụng](#ví-dụ-sử-dụng)

---

## 🌟 Tính Năng

### Xác Thực (Authentication)
✅ **JWT Token Based Authentication**
- Tokens hết hạn sau 1 giờ
- Signing key bảo mật 256-bit
- Issuer và Audience validation

✅ **Password Security**
- Sử dụng BCrypt hash (work factor 12)
- Không lưu trữ password plain-text
- Tự động hash password khi khởi động

### Phân Quyền (Authorization)
✅ **Role-Based Access Control (RBAC)**
- Hỗ trợ vai trò: Admin, User
- Kiểm soát truy cập dựa trên role

✅ **Permission-Based Access**
- Kiểm tra chi tiết quyền hạn
- Flexible permission system

### Bảo Mật Tài Khoản (Account Security)
✅ **Account Lockout Mechanism**
- Khóa tài khoản sau 5 lần nhập sai
- Theo dõi số lần nhập sai mật khẩu
- Requires admin intervention để mở khóa

✅ **Session Management**
- Token validation nghiêm ngặt
- Clock skew check = 0 (không cho phép lệch thời gian)
- Lifetime validation bắt buộc

### Audit & Logging
✅ **Comprehensive Logging**
- Ghi log tất cả các lần đăng nhập
- Theo dõi các hành động quan trọng
- Error logging cho debugging

---

## 🏗️ Kiến Trúc

### Cấu Trúc Thư Mục
```
IAMDemoProject/
├── Models/
│   ├── User.cs                    # Model người dùng
│   ├── LoginRequest.cs            # Request model
│   ├── AuthResponse.cs            # Response model
│   └── IAMException.cs            # Custom exceptions
├── Services/
│   ├── IAuthenticationService.cs  # Authentication logic
│   ├── IUserDatabaseService.cs    # Database operations
│   └── ISetupService.cs           # Initialization & seeding
├── Controllers/
│   ├── AuthController.cs          # Authentication endpoints
│   └── DataController.cs          # Protected endpoints (demos)
├── Program.cs                     # Configuration & DI setup
└── appsettings.json              # Configuration
```

### Technology Stack
- **Framework**: ASP.NET Core 10
- **Authentication**: JWT (JSON Web Tokens)
- **Password Hashing**: BCrypt.Net-Next
- **Database**: SQL Server
- **Logging**: Built-in ILogger

---

## 🚀 Setup & Installation

### Yêu Cầu
- SQL Server (LocalDB hoặc Full Edition)
- .NET 10 SDK
- Visual Studio Code hoặc Visual Studio

### Bước 1: Setup Database
```bash
# Mở SQL Server Management Studio hoặc SQL query tool
# Chạy file setup-database.sql
```

Hoặc chạy query này trực tiếp:
```sql
USE master;
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'HeThongBaoMat')
BEGIN
    DROP DATABASE HeThongBaoMat;
END
GO

CREATE DATABASE HeThongBaoMat;
GO

USE HeThongBaoMat;
GO

CREATE TABLE NguoiDung (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhauHash NVARCHAR(MAX) NOT NULL,
    VaiTro NVARCHAR(20) NOT NULL,
    BiKhoa BIT DEFAULT 0,
    SoLanSaiMatKhau INT DEFAULT 0
);

INSERT INTO NguoiDung VALUES ('admin', '123', 'Admin', 0, 0);
INSERT INTO NguoiDung VALUES ('user', '123', 'User', 0, 0);
GO
```

### Bước 2: Cập Nhật Connection String
Chỉnh sửa `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=HeThongBaoMat;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

### Bước 3: Restore Dependencies
```bash
dotnet restore
```

### Bước 4: Chạy Ứng Dụng
```bash
dotnet run
```

Ứng dụng sẽ:
1. Khởi động trên `http://localhost:5000`
2. Tự động hash các password plain-text
3. Sẵn sàng xử lý requests

---

## 📡 API Endpoints

### 1. **Authentication Endpoints**

#### POST `/api/auth/login`
**Đăng nhập và lấy JWT Token**

**Request:**
```json
{
  "tenDangNhap": "admin",
  "matKhau": "123"
}
```

**Success Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "vaiTro": "Admin",
  "tenDangNhap": "admin",
  "message": "Đăng nhập thành công",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

**Error Response (401):**
```json
{
  "message": "Tên đăng nhập hoặc mật khẩu không chính xác",
  "errorCode": "AUTHENTICATION_FAILED",
  "timestamp": "2026-05-12T10:30:00Z"
}
```

**Error Response - Account Locked (401):**
```json
{
  "message": "Tài khoản của bạn đã bị khóa do nhiều lần nhập sai mật khẩu. Vui lòng liên hệ quản trị viên.",
  "errorCode": "ACCOUNT_LOCKED",
  "timestamp": "2026-05-12T10:30:00Z"
}
```

#### GET `/api/auth/status`
**Kiểm tra trạng thái hệ thống**

**Response (200):**
```json
{
  "message": "Hệ thống IAM đang hoạt động bình thường",
  "timestamp": "2026-05-12T10:30:00Z",
  "isHealthy": true
}
```

---

### 2. **Protected Data Endpoints**

#### GET `/api/data/common-info`
**Thông tin chung cho tất cả authenticated users**

**Headers:**
```
Authorization: Bearer <JWT_TOKEN>
```

**Response (200):**
```json
{
  "message": "Đây là thông tin công khai cho mọi User đã đăng nhập",
  "userName": "admin",
  "userId": "1",
  "timestamp": "2026-05-12T10:30:00Z"
}
```

**Response nếu không có token (401):**
```
Unauthorized
```

---

#### GET `/api/data/admin-only-secret`
**Thông tin chỉ Admin mới có quyền truy cập**

**Headers:**
```
Authorization: Bearer <JWT_TOKEN_ADMIN>
```

**Response (200) - Khi user là Admin:**
```json
{
  "message": "CHÚC MỪNG! Bạn đã vào được vùng dữ liệu tối mật của Admin",
  "secretData": ["Dữ liệu 1", "Dữ liệu 2", "Dữ liệu 3"],
  "accessLevel": "ADMIN_ONLY",
  "timestamp": "2026-05-12T10:30:00Z"
}
```

**Response (403) - Khi user không phải Admin:**
```
Forbidden
```

---

#### GET `/api/data/user-profile`
**Thông tin cá nhân của User (không phải Admin)**

**Headers:**
```
Authorization: Bearer <JWT_TOKEN_USER>
```

**Response (200):**
```json
{
  "userName": "user",
  "userId": "2",
  "role": "User",
  "message": "Đây là thông tin cá nhân của người dùng",
  "emailNotifications": true,
  "lastLogin": "2026-05-12T10:30:00Z",
  "timestamp": "2026-05-12T10:30:00Z"
}
```

---

#### GET `/api/data/me`
**Lấy thông tin của người dùng hiện tại**

**Headers:**
```
Authorization: Bearer <JWT_TOKEN>
```

**Response (200):**
```json
{
  "userId": "1",
  "userName": "admin",
  "role": "Admin",
  "status": "active",
  "permissions": [
    "read:all_data",
    "write:all_data",
    "delete:all_data",
    "manage:users",
    "manage:roles",
    "view:admin_panel"
  ],
  "timestamp": "2026-05-12T10:30:00Z"
}
```

---

#### GET `/api/data/check-permission/{permission}`
**Kiểm tra xem user có quyền gì không**

**Headers:**
```
Authorization: Bearer <JWT_TOKEN>
```

**Example:** `/api/data/check-permission/read:all_data`

**Response (200):**
```json
{
  "permission": "read:all_data",
  "hasPermission": true,
  "role": "Admin",
  "timestamp": "2026-05-12T10:30:00Z"
}
```

---

## 🔒 Bảo Mật

### Password Hashing
- **Algorithm**: BCrypt
- **Work Factor**: 12 (cân bằng bảo mật vs performance)
- **Cost Time**: ~100ms per hash
- **Không lưu trữ plain-text passwords**

### JWT Token Security
- **Algorithm**: HMAC-SHA256
- **Key Length**: 256-bit
- **Expiration**: 1 giờ
- **Issuer Validation**: Bắt buộc
- **Audience Validation**: Bắt buộc
- **Clock Skew**: 0 (không cho phép sai lệch thời gian)

### Account Lockout
- **Max Failed Attempts**: 5
- **Action**: Tự động khóa tài khoản
- **Recovery**: Liên hệ Admin để mở khóa

### Connection String Security
- **Encrypted Connection**: TrustServerCertificate=true (dev)
- **Parameterized Queries**: SQL Injection prevention
- **Connection Pooling**: Tối ưu tài nguyên

### Logging & Monitoring
- Ghi log tất cả các lần đăng nhập
- Ghi log các lỗi và ngoại lệ
- Tracking số lần nhập sai mật khẩu

---

## 📝 Ví Dụ Sử Dụng

### 1. Đăng Nhập (Login)
**Sử dụng Postman, curl, hoặc IDE Plugin:**

```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"tenDangNhap":"admin","matKhau":"123"}'
```

**Kết quả:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwibmFtZSI6ImFkbWluIiwicm9sZSI6IkFkbWluIiwidXNlcl9pZCI6IjEiLCJ1c2VyX3N0YXR1cyI6ImFjdGl2ZSIsImlhdCI6IjE3NTI0MTE0NjciLCJleHAiOjE3NTI0MTUwNjcsImlzcyI6IklBTURlbW9Qcm9qZWN0IiwiYXVkIjoiSUFNRGVtb1VzZXJzIn0.Yq...",
  "vaiTro": "Admin",
  "tenDangNhap": "admin",
  "message": "Đăng nhập thành công",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### 2. Truy Cập Protected Endpoint
```bash
# Lấy token từ login response
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Gọi endpoint protected
curl -X GET "http://localhost:5000/api/data/common-info" \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Admin-Only Access
```bash
# Chỉ user admin mới có quyền
curl -X GET "http://localhost:5000/api/data/admin-only-secret" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# User thường sẽ nhận lỗi 403 Forbidden
```

### 4. Kiểm Tra Permissions
```bash
curl -X GET "http://localhost:5000/api/data/check-permission/manage:users" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 🔄 Quy Trình Authentication

```
┌─────────────┐
│   Client    │
└────────┬────┘
         │
         │ 1. POST /api/auth/login
         │    {tenDangNhap, matKhau}
         ▼
┌──────────────────────────────┐
│   AuthController.Login()     │
│   - Validate input           │
│   - Get user from DB         │
│   - Verify password (BCrypt) │
│   - Reset failed attempts    │
│   - Generate JWT token       │
└────────┬─────────────────────┘
         │
         │ 2. Return Token & Role
         ▼
┌─────────────┐
│   Client    │  Token: "eyJ..."
└────────┬────┘
         │
         │ 3. GET /api/data/common-info
         │    Header: Authorization: Bearer eyJ...
         ▼
┌──────────────────────────────┐
│   [Authorize] Middleware     │
│   - Validate JWT signature   │
│   - Check expiration         │
│   - Extract claims           │
│   - Build ClaimsPrincipal    │
└────────┬─────────────────────┘
         │
         │ 4. Request reaches controller
         ▼
┌──────────────────────────────┐
│   DataController.GetInfo()   │
│   - User.FindFirst(claims)   │
│   - Return protected data    │
└────────┬─────────────────────┘
         │
         │ 5. Return response
         ▼
┌─────────────┐
│   Client    │  Response data
└─────────────┘
```

---

## 🛠️ Troubleshooting

### "Connection string không được tìm thấy"
- Kiểm tra `appsettings.json` có đúng `ConnectionStrings:DefaultConnection`
- Kiểm tra tên server SQL Server

### "Login thất bại - Tài khoản bị khóa"
- Cần admin đăng nhập vào SQL và chạy:
```sql
UPDATE NguoiDung SET BiKhoa = 0, SoLanSaiMatKhau = 0 WHERE TenDangNhap = 'username'
```

### "Token hết hạn"
- Token hợp lệ trong 1 giờ
- Cần đăng nhập lại để lấy token mới

### "403 Forbidden"
- Kiểm tra role của user có phù hợp với endpoint
- Admin endpoints yêu cầu role = "Admin"

---

## 📚 Tài Liệu Tham Khảo

- [JWT.io](https://jwt.io) - JWT information
- [BCrypt](https://en.wikipedia.org/wiki/Bcrypt) - Password hashing
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [OWASP Authorization Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html)

---

**Made with ❤️ for Security**
