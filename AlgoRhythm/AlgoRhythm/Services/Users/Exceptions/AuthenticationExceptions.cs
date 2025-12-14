namespace AlgoRhythm.Services.Users.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("User with this email does not exist.") { }
}

public class InvalidPasswordException : Exception
{
    public InvalidPasswordException() : base("The password is incorrect.") { }
}

public class EmailNotVerifiedException : Exception
{
    public EmailNotVerifiedException() : base("Email address has not been verified. Please check your email for the verification code.") { }
}

public class InvalidVerificationCodeException : Exception
{
    public InvalidVerificationCodeException() : base("The verification code is invalid or has expired.") { }
}

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException() : base("An account with this email address already exists.") { }
}

public class EmailAlreadyVerifiedException : Exception
{
    public EmailAlreadyVerifiedException() : base("This email address has already been verified.") { }
}

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message) { }
}