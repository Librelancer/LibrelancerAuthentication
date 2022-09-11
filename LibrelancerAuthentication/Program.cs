using LibrelancerAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var tokens = new TokenProvider(TimeSpan.FromMinutes(2));
var passwordHasher = new PasswordHasher<User>();
var dbPath = builder.Configuration.GetValue<string>("AppDb");

using (var context = new UserDbContext(dbPath))
{
    if (context.Database.GetPendingMigrations().Any()) context.Database.Migrate();
}

app.MapGet("/", () => Results.Ok(new
{
    application = "authserver",
    registerEnabled = true
}));

app.MapGet("/login", async ([FromQuery(Name = "username")] string username,
    [FromQuery(Name = "password")] string password) =>
{
    await using var context = new UserDbContext(dbPath);
    var user = context.Users.FirstOrDefault(x => x.Username.Equals(username));
    if (user != null)
    {
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await context.SaveChangesAsync();
        }

        if (result == PasswordVerificationResult.Success ||
            result == PasswordVerificationResult.SuccessRehashNeeded)
            return Results.Ok(new {token = tokens.GenerateToken(user.Id, user.Guid)});
    }

    await Task.Delay(3000);
    return Results.BadRequest("Invalid username or password");
});

app.MapGet("/verifytoken", (string token) =>
{
    if (tokens.VerifyToken(token, out var value))
        return Results.Ok(new {guid = value});
    return Results.BadRequest("Invalid token");
});

app.MapGet("/register", async ([FromQuery(Name = "username")] string username,
    [FromQuery(Name = "password")] string password) =>
{
    await using var context = new UserDbContext(dbPath);

    if (context.Users.Any(x => x.Username.Equals(username)))
        return Results.BadRequest("User already exists");
    var newUser = new User {Username = username, Guid = Guid.NewGuid().ToString()};
    newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);
    context.Users.Add(newUser);
    await context.SaveChangesAsync();
    return Results.Ok("User registered successfully");
});

app.Run();