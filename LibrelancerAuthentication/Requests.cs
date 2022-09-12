namespace LibrelancerAuthentication;

record PasswordRequestData(string username, string password);

record ChangePasswordRequestData(string username, string oldpassword, string newpassword);

record TokenRequestData(string token);