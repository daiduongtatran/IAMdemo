namespace IAMDemoProject.Models;

public class IAMException : Exception
{
    public IAMException(string message) : base(message) { }
}

public class AuthenticationException : IAMException
{
    public AuthenticationException(string message) : base(message) { }
}

public class AccountLockedException : IAMException
{
    public AccountLockedException(string message) : base(message) { }
}
public class UserNotFoundException : IAMException
{
    public UserNotFoundException(string message) : base(message) { }
}
