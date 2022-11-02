namespace LibrelancerAuthentication;
record PasswordRequestData(string username, string password) : IValidateRecord
{
    public bool Validate(out IResult error)
    {
        error = null;
        return Validation.Username(username, ref error) &&
               Validation.PasswordLength(username, ref error);
    }
}

record ChangePasswordRequestData(string username, string oldpassword, string newpassword) : IValidateRecord
{
    public bool Validate(out IResult error)
    {
        error = null;
        return Validation.Username(username, ref error) &&
               Validation.PasswordLength(oldpassword, ref error) &&
               Validation.PasswordLength(newpassword, ref error);
    }
}

record TokenRequestData(string token) : IValidateRecord
{
    public bool Validate(out IResult error)
    {
        error = null;
        return Validation.TokenLength(token, ref error);
    }
}