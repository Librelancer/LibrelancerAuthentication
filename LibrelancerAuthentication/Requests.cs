namespace LibrelancerAuthentication;
record PasswordRequestData(string username, string password, string captchaToken, long utctime, string nonce, string hash) : IValidateRecord
{
    public bool Validate(string difficulty, out IResult error)
    {
        error = null;
        return Validation.Username(username, ref error) &&
               Validation.PasswordLength(password, ref error) &&
               Validation.Time(utctime, ref error) &&
               Validation.Nonce(nonce, ref error) &&
               Validation.Hash(hash, difficulty, ref error, username, password, captchaToken, utctime.ToString(), nonce);
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

record CaptchaCreateRequest(long utctime, string nonce, string hash) : IValidateRecord
{
    public bool Validate(string difficulty, out IResult error)
    {
        error = null;
        return Validation.Time(utctime, ref error) &&
               Validation.Nonce(nonce, ref error) &&
               Validation.Hash(hash, difficulty, ref error, utctime.ToString(), nonce);
    }
}

record CaptchaCheckData(string id, int x)
{
    
}

record TokenRequestData(string token) : IValidateRecord
{
    public bool Validate(string difficulty, out IResult error)
    {
        error = null;
        return Validation.TokenLength(token, ref error);
    }
}