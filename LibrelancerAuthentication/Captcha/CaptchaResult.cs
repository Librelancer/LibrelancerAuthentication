namespace LibrelancerAuthentication.Captcha;

public enum CaptchaResult
{
    Ok,
    Invalid, 
    Expired,
    IncorrectSolution
}