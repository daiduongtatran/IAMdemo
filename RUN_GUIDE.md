# 🚀 How to Run IAM Demo System

## Prerequisites

- **.NET 10+** installed
- **SQL Server** running (local or remote)
- **appsettings.json** configured with connection string

## Step 1: Setup Database

Run the SQL script to create database and tables:

```bash
# Open SQL Server Management Studio
# Run: setup-database.sql
# Creates database "HeThongBaoMat" with user table
```

**Created users:**
- **admin** / password: `123` (Admin role)
- **user** / password: `123` (User role)

## Step 2: Configure Connection String

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HeThongBaoMat;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Jwt": {
    "SecretKey": "Key_Sieu_Bao_Mat_Nhom_Minh_123456_VeryLongKeyForHigherSecurity",
    "Issuer": "IAMDemoProject",
    "Audience": "IAMDemoUsers"
  }
}
```

## Step 3: Run Backend

```bash
cd d:\anninhmang\IAMDemoProject
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

## Step 4: Open Web UI

Open browser and go to:
```
http://localhost:5000
```

You should see the login page.

## Step 5: Demo

### Login as Admin
```
Username: admin
Password: 123
Click: Login
```

### Test Endpoints

Click the test buttons to see different endpoints:

1. **📊 Common Info** - Works for all authenticated users
2. **👤 User Profile** - Only for non-admin users (403 for admin)
3. **🔍 Get Me** - Current user info with permissions
4. **🔐 Admin Secret** - Only for admin (403 for regular users)
5. **✔️ Check Permission** - Check if user has permission

### Try as User

```
Logout (Ctrl+L)
Username: user
Password: 123
Click: Login
```

Notice the different access patterns - User can't access admin endpoints.

## Step 6: Test Account Lockout (Security)

```
Username: admin
Password: wrong_password
Click: Login 5 times
```

After 5 failed attempts, account locks and shows "Account locked" message.

## API Endpoints

- `POST /api/auth/login` - Login
- `GET /api/auth/status` - Health check
- `GET /api/data/common-info` - Common data (all users)
- `GET /api/data/user-profile` - User profile (non-admin only)
- `GET /api/data/me` - Current user info
- `GET /api/data/admin-only-secret` - Admin data
- `GET /api/data/check-permission/{perm}` - Check permission

## Troubleshooting

### Port 5000 already in use
```bash
# Change port in Properties/launchSettings.json
# Or kill process on port 5000
```

### Connection string error
```
Check appsettings.json ConnectionStrings section
Verify database exists in SQL Server
Verify login credentials
```

### Token errors
```
Clear browser localStorage: F12 → Application → Local Storage → Clear All
Login again
```

### CSS/JS not loading
```
Browser might have cached old files
Press Ctrl+Shift+Delete to clear cache
Or open in Incognito/Private mode
```

## Quick Commands

```bash
# Run in release mode
dotnet run --configuration Release

# Run on specific port
dotnet run --urls http://localhost:8080

# Build only (no run)
dotnet build

# Check for errors
dotnet build

# View logs
dotnet run > app.log 2>&1
```

## Files Structure

```
Controllers/
├── AuthController.cs      # Login endpoint
└── DataController.cs      # Protected endpoints

Models/
├── User.cs               # User entity
└── AuthResponse.cs       # Login response

Services/
├── AuthenticationService.cs    # JWT & Auth logic
├── UserDatabaseService.cs      # Database operations
└── SetupService.cs            # Initialization

wwwroot/
├── index.html            # Web UI
├── css/style.css         # Styling
└── js/app.js             # Frontend logic

appsettings.json          # Configuration
Program.cs                # Startup
```

## Demo Script (5 minutes)

1. Start backend: `dotnet run`
2. Open: `http://localhost:5000`
3. Login as **admin** / `123`
4. Click **🔐 Admin Secret** → 200 OK ✓
5. Logout → Login as **user** / `123`
6. Click **🔐 Admin Secret** → 403 Forbidden ✗
7. Click **👤 User Profile** → 200 OK ✓

Done! You've demonstrated:
- Authentication (login)
- JWT token validation
- Role-based authorization
- Account security

---

**Status:** Ready to run ✅
**Setup Time:** ~5 minutes
**Demo Time:** ~2 minutes
