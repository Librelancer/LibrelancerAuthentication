namespace LibrelancerAuthentication;
record PasswordRequestData(string username, string password, long utctime, string nonce, string hash) : IValidateRecord
{
    public bool Validate(string difficulty, out IResult error)
    {
        error = null;
        return Validation.Username(username, ref error) &&
               Validation.PasswordLength(password, ref error) &&
               Validation.Time(utctime, ref error) &&
               Validation.Nonce(nonce, ref error) &&
               Validation.Hash(hash, difficulty, ref error, username, password, utctime.ToString(), nonce);
    }
}

record ChangePasswordRequestData(string username, string oldpassword, string newpassword, long utctime, string nonce, string hash) : IValidateRecord
{
    public bool Validate(string difficulty, out IResult error)
    {
        error = null;
        return Validation.Username(username, ref error) &&
               Validation.PasswordLength(oldpassword, ref error) &&
               Validation.PasswordLength(newpassword, ref error) &&
               Validation.Time(utctime, ref error) &&
               Validation.Nonce(nonce, ref error) &&
               Validation.Hash(hash, difficulty, ref error, username, oldpassword, newpassword, utctime.ToString(), nonce);
    }
}

record TokenRequestData(string token) : IValidateRecord
{
    public bool Validate(string difficulty, out IResult error)
    {
        error = null;
        return Validation.TokenLength(token, ref error);
    }
}