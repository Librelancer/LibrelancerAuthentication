namespace LibrelancerAuthentication;

record PasswordRequestData(string username, string password);

record TokenRequestData(string token);