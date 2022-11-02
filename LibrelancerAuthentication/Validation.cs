namespace LibrelancerAuthentication;

public interface IValidateRecord
{
    bool Validate(out IResult error);
}

public static class Validation
{
    public static bool PasswordLength(string password, ref IResult error)
    {
        if (password.Length > 128)
        {
            error = Results.BadRequest("PasswordTooLong");
            return false;
        }
        return true;
    }

    public static bool TokenLength(string token, ref IResult error)
    {
        if (token.Length > 40)
        {
            error = Results.BadRequest("TokenTooLong");
            return false;
        }
        return true;
    }
    
    public static bool Username(string username, ref IResult error)
    {
        if (username.Length > 64)
        {
            error = Results.BadRequest("UsernameTooLong");
            return false;
        }
        foreach (var c in username) {
            //ASCII letters, numbers and punctuation (not space)
            if (c < 33 || c > 126)
            {
                error = Results.BadRequest("UsernameInvalidCharacter");
                return false;
            }
        }
        return true;
    }
}