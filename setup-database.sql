-- XÓA DATABASE CŨ NẾU TỒN TẠI
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'HeThongBaoMat')
BEGIN
    ALTER DATABASE HeThongBaoMat SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE HeThongBaoMat;
    PRINT N'✓ Database cũ đã được xóa';
END
GO

-- TẠO DATABASE MỚI
CREATE DATABASE HeThongBaoMat;
PRINT N'✓ Database mới đã được tạo';
GO

USE HeThongBaoMat;
GO

-- 1. TẠO BẢNG VÀ TỰ ĐỘNG NÂNG CẤP CỘT
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NguoiDung' and xtype='U')
BEGIN
    CREATE TABLE NguoiDung (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
        MatKhauHash NVARCHAR(MAX) NOT NULL,
        VaiTro NVARCHAR(20) NOT NULL,
        BiKhoa BIT DEFAULT 0,
        SoLanSaiMatKhau INT DEFAULT 0,
        TotpSecret NVARCHAR(MAX) NULL
    );
    PRINT N'✓ Bảng NguoiDung đã tạo thành công (Đã bao gồm TOTP)';
END
ELSE
BEGIN
    PRINT N'ℹ Bảng NguoiDung đã tồn tại. Đang kiểm tra cấu trúc...';
    -- Cực kỳ tối ưu: Tự động thêm cột nếu database cũ chưa có
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'TotpSecret' AND Object_ID = Object_ID(N'NguoiDung'))
    BEGIN
        ALTER TABLE NguoiDung ADD TotpSecret NVARCHAR(MAX) NULL;
        PRINT N'✓ Đã tự động nâng cấp: Bổ sung cột TotpSecret thành công!';
    END
END
GO

-- 2. ĐỔ DỮ LIỆU MẪU (Mật khẩu: 123)
-- BCrypt hash của "123" với cost 12
INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, VaiTro, BiKhoa, SoLanSaiMatKhau, TotpSecret)
VALUES
    ('admin', '$2a$12$NnlMOYtbr20r.tCVETJUfO8Uai.8cUiiL4CSN1rdisXinJqeIt4GS', 'Admin', 0, 0, NULL),
    ('user', '$2a$12$NnlMOYtbr20r.tCVETJUfO8Uai.8cUiiL4CSN1rdisXinJqeIt4GS', 'User', 0, 0, NULL);

PRINT N'✓ Admin và User đã được thêm (Mật khẩu: 123)';
GO

-- 3. IN THÔNG BÁO VÀ KIỂM TRA
PRINT N'
╔════════════════════════════════════════════════════════════════╗
║ DỮ LIỆU NGƯỜI DÙNG HIỆN TẠI                                    ║
╚════════════════════════════════════════════════════════════════╝';

SELECT
    Id,
    TenDangNhap,
    VaiTro,
    CASE WHEN BiKhoa = 1 THEN N'Bị khóa' ELSE N'Hoạt động' END AS [Trạng thái],
    SoLanSaiMatKhau AS [Lần sai],
    CASE WHEN TotpSecret IS NULL THEN N'Chưa cài đặt' ELSE N'Đã kích hoạt' END AS [MFA]
FROM NguoiDung
ORDER BY Id;

PRINT N'
╔════════════════════════════════════════════════════════════════╗
║ THÔNG TIN LƯU Ý QUAN TRỌNG                                     ║
╚════════════════════════════════════════════════════════════════╝
• Database đã sẵn sàng, hỗ trợ xác thực TOTP (Authenticator).
• Các mật khẩu sẽ tự động được hash (BCrypt) khi ứng dụng khởi động.
• Hệ thống sẽ tự động khóa tài khoản sau 5 lần nhập sai mật khẩu.
';
GO