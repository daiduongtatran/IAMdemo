# 📖 Project Guide - IAM Demo System

## Overview

This is a complete **Identity and Access Management (IAM) system** built with ASP.NET Core. It demonstrates modern authentication and authorization patterns.

**Key Technologies:**
- **.NET Core 10** - Backend framework
- **JWT (JSON Web Tokens)** - Stateless authentication
- **BCrypt** - Password hashing
- **SQL Server** - Database
- **HTML5/CSS3/JavaScript** - Web UI

## Architecture

### Components

```
┌─────────────────────────────────────────┐
│          Web Browser (UI)               │
│  index.html + style.css + app.js        │
└────────────────┬────────────────────────┘
                 │ HTTPS
                 ↓
┌─────────────────────────────────────────┐
│      ASP.NET Core API Server            │
│  Port 5000                              │
├─────────────────────────────────────────┤
│  Controllers/                           │
│  ├─ AuthController (Login)              │
│  └─ DataController (Protected)          │
│                                         │
│  Services/                              │
│  ├─ AuthenticationService (JWT)         │
│  ├─ UserDatabaseService (DB)            │
│  └─ SetupService (Init)                 │
└────────────────┬────────────────────────┘
                 │ ADO.NET
                 ↓
┌─────────────────────────────────────────┐
│        SQL Server Database              │
│  Database: HeThongBaoMat                │
│  Table: NguoiDung (Users)               │
└─────────────────────────────────────────┘
```

## Authentication Flow

```
1. User enters username/password in UI
   ↓
2. POST /api/auth/login
   ↓
3. AuthenticationService validates:
   - Username exists in database
   - Account not locked
   - Password matches (BCrypt verify)
   ↓
4. Generate JWT token with claims:
   - user_id
   - role (Admin/User)
   - permissions
   ↓
5. Send token to browser
   ↓
6. Browser stores token in localStorage
   ↓
7. All API calls include: Authorization: Bearer {token}
```

## Authorization Flow

```
1. Browser sends request with JWT token
   ↓
2. Program.cs TokenValidationParameters validate:
   - Token signature (HMAC-SHA256)
   - Token expiration (1 hour)
   - Issuer (IAMDemoProject)
   - Audience (IAMDemoUsers)
   ↓
3. If valid → extract claims
   ↓
4. DataController checks:
   - [Authorize] - any authenticated user
   - [Authorize(Roles="Admin")] - admin only
   ↓
5. Return data or 403 Forbidden
```

## Security Features

### 1. Password Hashing
- **Algorithm:** BCrypt
- **Work Factor:** 12 (expensive computation)
- **Benefit:** Resistant to brute-force attacks

```csharp
// Hashing
string hash = BCrypt.HashPassword("password123", workFactor: 12);

// Verification
bool isCorrect = BCrypt.Verify("password123", hash);
```

### 2. Account Lockout
- **Max failed attempts:** 5
- **Auto-locks account** in database
- **Prevents brute-force** attacks

```sql
UPDATE NguoiDung SET BiKhoa = 1 WHERE SoLanSaiMatKhau >= 5
```

### 3. JWT Tokens
- **Type:** HMAC-SHA256 signed
- **Expiration:** 1 hour
- **Stateless:** No session storage needed
- **Claims:** user_id, role, permissions

```
Header: eyJhbGciOiJIUzI1NiI...
Payload: eyJ1c2VyX2lkIjoiMSI...
Signature: 8q7k8L9m...
```

### 4. SQL Injection Prevention
- **Parameterized queries** for all database operations
- **No string concatenation** in SQL

```csharp
// ✅ Safe
command.Parameters.AddWithValue("@TenDangNhap", username);

// ❌ Unsafe
string query = $"SELECT * FROM Users WHERE Username = '{username}'";
```

### 5. CORS Configuration
- **Allows** frontend to call backend
- **Configurable** in Program.cs

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
```

## Key Classes

### AuthController.cs
**Purpose:** Handle authentication requests

**Endpoints:**
- `POST /api/auth/login` - Authenticate user
- `GET /api/auth/status` - System health

**Logic:**
1. Validate input
2. Call AuthenticationService
3. Return token or error

### DataController.cs
**Purpose:** Demonstrate authorization with protected endpoints

**Endpoints:**
- `GET /api/data/common-info` - All users
- `GET /api/data/admin-only-secret` - Admin only
- `GET /api/data/user-profile` - Non-admin only
- `GET /api/data/me` - Current user
- `GET /api/data/check-permission/{perm}` - Permission check

### AuthenticationService.cs
**Purpose:** Core authentication and JWT logic

**Methods:**
- `AuthenticateAsync()` - Login flow
- `GenerateJwtToken()` - Create JWT
- `VerifyPassword()` - Check password with BCrypt
- `HashPassword()` - Hash password

### UserDatabaseService.cs
**Purpose:** All database operations

**Methods:**
- `GetUserByUsernameAsync()` - Find user
- `UpdateFailedLoginAttemptsAsync()` - Track failures
- `LockAccountAsync()` - Lock account
- `ResetFailedLoginAttemptsAsync()` - Reset on success

### SetupService.cs
**Purpose:** Initialize on startup

**Logic:**
1. Check all users in database
2. If password is plain text (not BCrypt hash)
3. Hash the password with BCrypt
4. Store back in database

## Database Schema

```sql
CREATE TABLE NguoiDung (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TenDangNhap NVARCHAR(100) NOT NULL UNIQUE,
    MatKhauHash NVARCHAR(255) NOT NULL,     -- BCrypt hash
    VaiTro NVARCHAR(50) DEFAULT 'User',      -- Admin/User
    BiKhoa BIT DEFAULT 0,                    -- Account lock flag
    SoLanSaiMatKhau INT DEFAULT 0            -- Failed attempts
);
```

**Sample Data:**
```sql
INSERT INTO NguoiDung VALUES 
('admin', '$2b$12$...bcrypt_hash...', 'Admin', 0, 0),
('user', '$2b$12$...bcrypt_hash...', 'User', 0, 0);
```

## Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=HeThongBaoMat;..."
  },
  "Jwt": {
    "SecretKey": "your_secret_key_256_bits",
    "Issuer": "IAMDemoProject",
    "Audience": "IAMDemoUsers"
  }
}
```

### Program.cs
- Registers services (Dependency Injection)
- Configures JWT authentication
- Sets up middleware pipeline
- Initializes security on startup

## User Roles & Permissions

### Admin Role
```
Permissions:
- read:all_data
- write:all_data
- delete:all_data
- manage:users
- manage:roles
- view:admin_panel
```

### User Role
```
Permissions:
- read:own_data
- write:own_data
- read:shared_data
```

## Web UI (Frontend)

### Files
- `index.html` - Structure
- `css/style.css` - Dark theme styling
- `js/app.js` - API integration

### Features
- Login form with quick demo buttons
- Real-time response display
- Token storage (localStorage)
- Permission display
- Error handling
- Keyboard shortcuts (Ctrl+L, Ctrl+Q)

### How It Works
1. User enters credentials or clicks quick button
2. JavaScript calls `POST /api/auth/login`
3. Stores JWT in localStorage
4. Updates UI with user info
5. All subsequent requests include token header
6. Test buttons call protected endpoints
7. Displays responses in real-time

## Error Handling

### Custom Exceptions
```csharp
throw new UserNotFoundException("User not found");
throw new AccountLockedException("Account locked");
throw new AuthenticationException("Auth failed");
throw new IAMException("System error");
```

### HTTP Status Codes
- `200 OK` - Success
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Invalid token or credentials
- `403 Forbidden` - Valid token but no permission
- `500 Internal Server Error` - Server error

## Deployment

### Local Development
```bash
dotnet run
# Listens on http://localhost:5000
```

### Production
```bash
dotnet publish -c Release
# Deploy to IIS or Docker
```

## Testing

### Manual Testing
1. Use web UI in browser
2. Test with different user roles
3. Try wrong passwords (account lockout)
4. Check different endpoints

### API Testing (Postman/curl)
```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"tenDangNhap":"admin","matKhau":"123"}'

# Call protected endpoint
curl -X GET http://localhost:5000/api/data/me \
  -H "Authorization: Bearer {token}"
```

## Learning Points

1. **Authentication** - How login works
2. **JWT** - Token-based auth
3. **Authorization** - Role-based access control (RBAC)
4. **Security** - Password hashing, account lockout
5. **Database** - User management
6. **Frontend-Backend** - API integration
7. **Error Handling** - Proper HTTP responses
8. **Dependency Injection** - Service architecture

## Common Questions

**Q: How long does JWT token last?**
A: 1 hour (configurable in AuthenticationService.cs)

**Q: What if token expires?**
A: User gets 401 Unauthorized, must login again

**Q: Can I change user's role?**
A: Yes, update database directly. No admin panel included.

**Q: Is this production-ready?**
A: No, this is a demo. For production:
- Use HTTPS
- Better password policy
- Rate limiting
- Audit logging
- Refresh tokens
- 2FA

**Q: How to unlock locked account?**
A: Update database: `UPDATE NguoiDung SET BiKhoa = 0 WHERE Id = 1`

---

**Summary:** Complete IAM demo showing JWT authentication, role-based authorization, password security, and account lockout mechanisms. Educational project for learning modern authentication patterns.
