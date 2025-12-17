using System.Runtime.Serialization;

namespace AlgoRhythm.Services.Users.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException()
        : base("User with this email does not exist.") { }

    public UserNotFoundException(string message)
        : base(message) { }

    public UserNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class InvalidPasswordException : Exception
{
    public InvalidPasswordException()
        : base("The password is incorrect.") { }

    public InvalidPasswordException(string message)
        : base(message) { }

    public InvalidPasswordException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class EmailNotVerifiedException : Exception
{
    public EmailNotVerifiedException()
        : base("Email address has not been verified. Please check your email for the verification code.") { }

    public EmailNotVerifiedException(string message)
        : base(message) { }

    public EmailNotVerifiedException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class InvalidVerificationCodeException : Exception
{
    public InvalidVerificationCodeException()
        : base("The verification code is invalid or has expired.") { }

    public InvalidVerificationCodeException(string message)
        : base(message) { }

    public InvalidVerificationCodeException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException()
        : base("An account with this email address already exists.") { }

    public EmailAlreadyExistsException(string message)
        : base(message) { }

    public EmailAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class EmailAlreadyVerifiedException : Exception
{
    public EmailAlreadyVerifiedException()
        : base("This email address has already been verified.") { }

    public EmailAlreadyVerifiedException(string message)
        : base(message) { }

    public EmailAlreadyVerifiedException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException()
        : base("Too many requests. Please try again later.") { }

    public TooManyRequestsException(string message)
        : base(message) { }

    public TooManyRequestsException(string message, Exception innerException)
        : base(message, innerException) { }
}