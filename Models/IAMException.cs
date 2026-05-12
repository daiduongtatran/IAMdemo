namespace IAMDemoProject.Models;

/// <summary>
/// Exception cho các lỗi IAM
/// </summary>
public class IAMException : Exception
{
    public IAMException(string message) : base(message) { }
}

/// <summary>
/// Exception cho lỗi xác thực
/// </summary>
public class AuthenticationException : IAMException
{
    public AuthenticationException(string message) : base(message) { }
}

/// <summary>
/// Exception cho lỗi tài khoản bị khóa
/// </summary>
public class AccountLockedException : IAMException
{
    public AccountLockedException(string message) : base(message) { }
}

/// <summary>
/// Exception cho lỗi tài khoản không tồn tại
/// </summary>
public class UserNotFoundException : IAMException
{
    public UserNotFoundException(string message) : base(message) { }
}
