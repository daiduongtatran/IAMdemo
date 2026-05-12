-- ============================================================
-- HỆTHỐNG IAM - SETUP DATABASE
-- Database: HeThongBaoMat
-- Mục đích: Tạo bảng người dùng với hỗ trợ bảo mật IAM
-- ============================================================

-- BƯỚC 1: TẠO DATABASE (nếu chưa có)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HeThongBaoMat')
BEGIN
    CREATE DATABASE HeThongBaoMat;
END
GO

USE HeThongBaoMat;
GO

-- BƯỚC 2: TẠO BẢNG NGƯỜI DÙNG (nếu chưa có)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NguoiDung' and xtype='U')
BEGIN
    CREATE TABLE NguoiDung (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
        MatKhauHash NVARCHAR(MAX) NOT NULL,
        VaiTro NVARCHAR(20) NOT NULL, -- 'Admin' hoặc 'User'
        BiKhoa BIT DEFAULT 0, -- 0: Hoạt động, 1: Bị khóa
        SoLanSaiMatKhau INT DEFAULT 0 -- Đếm số lần nhập sai mật khẩu
    );
    
    PRINT '✓ Bảng NguoiDung đã được tạo thành công';
END
ELSE
BEGIN
    PRINT 'ℹ Bảng NguoiDung đã tồn tại';
END
GO

-- BƯỚC 3: KIỂM TRA VÀ INSERT DỮ LIỆU SAMPLE (nếu chưa có)
-- LƯU Ý: Các mật khẩu sau đây là PLAIN TEXT và sẽ được hash tự động khi ứng dụng khởi động
-- admin: 123
-- user: 123

IF NOT EXISTS (SELECT * FROM NguoiDung WHERE TenDangNhap = 'admin')
BEGIN
    INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, VaiTro, BiKhoa, SoLanSaiMatKhau)
    VALUES ('admin', '123', 'Admin', 0, 0);
    
    PRINT '✓ User admin đã được thêm';
END
ELSE
BEGIN
    PRINT 'ℹ User admin đã tồn tại';
END
GO

IF NOT EXISTS (SELECT * FROM NguoiDung WHERE TenDangNhap = 'user')
BEGIN
    INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, VaiTro, BiKhoa, SoLanSaiMatKhau)
    VALUES ('user', '123', 'User', 0, 0);
    
    PRINT '✓ User user đã được thêm';
END
ELSE
BEGIN
    PRINT 'ℹ User user đã tồn tại';
END
GO

-- BƯỚC 4: KIỂM TRA DỮ LIỆU HIỆN TẠI
PRINT '
╔════════════════════════════════════════╗
║ DỮ LIỆU NGƯỜI DÙNG HIỆN TẠI            ║
╚════════════════════════════════════════╝';

SELECT 
    Id,
    TenDangNhap,
    VaiTro,
    CASE WHEN BiKhoa = 1 THEN 'Bị khóa' ELSE 'Hoạt động' END AS [Trạng thái],
    SoLanSaiMatKhau AS [Lần sai]
FROM NguoiDung
ORDER BY Id;

-- BƯỚC 5: THÔNG TIN LƯU Ý
PRINT '
╔════════════════════════════════════════════════════════════════╗
║ THÔNG TIN LƯU Ý QUAN TRỌNG                                    ║
╚════════════════════════════════════════════════════════════════╝
✓ Database đã sẵn sàng
✓ Các mật khẩu sẽ tự động được hash (BCrypt) khi ứng dụng khởi động
✓ Hệ thống sẽ tự động khóa tài khoản sau 5 lần nhập sai mật khẩu
✓ Để thay đổi mật khẩu, hãy cập nhật MatKhauHash bằng BCrypt hash
✓ Vai trò hiện có: Admin, User (có thể mở rộng)
';
GO
