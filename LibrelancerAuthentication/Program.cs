using LibrelancerAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var tokens = new TokenProvider(TimeSpan.FromMinutes(2));
var passwordHasher = new PasswordHasher<User>();

var builder = WebApplication.CreateBuilder(args);
var dbPath = builder.Configuration.GetValue<string>("AppDb");

using (var context = new UserDbContext(dbPath))
{
    if (context.Database.GetPendingMigrations().Any()) context.Database.Migrate();
}

builder.Services.AddSingleton(_ => new UserDbService(dbPath, passwordHasher));
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
var pathBase = builder.Configuration.GetValue<string>("PathBase");
if (!string.IsNullOrWhiteSpace(pathBase)) {
    GlobalVars.PathBase = pathBase;
    app.UsePathBase(pathBase);
}

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/info", () => Results.Ok(new
{
    application = "authserver",
    registerEnabled = true
}));


app.MapPost("/login", async (PasswordRequestData request, UserDbService users) =>
{
    if (!request.Validate(out var error)) return error;
    
    var user = await users.Login(request.username, request.password);
    if (user != null)
    {
        return Results.Ok(new {token = tokens.GenerateToken(user.Id, user.Guid)});
    }
    return Results.BadRequest("Invalid username or password");
});

app.MapPost("/changepassword", async (ChangePasswordRequestData request, UserDbService users) =>
{
    if (!request.Validate(out var error)) return error;
    
    if (await users.ChangePassword(request.username, request.oldpassword, request.newpassword))
    {
        return Results.Ok();
    }
    return Results.BadRequest("Invalid username or password");
});

app.MapPost("/verifytoken", (TokenRequestData token) =>
{
    if (tokens.VerifyToken(token.token, out var value))
        return Results.Ok(new {guid = value});
    return Results.BadRequest("Invalid token");
});

app.MapPost("/register", async (PasswordRequestData request, UserDbService users) =>
{
    if (!request.Validate(out var error)) return error;
    
    if (await users.Register(request.username, request.password))
    {
        return Results.Ok("User registered successfully");
    }
    return Results.BadRequest("User already exists");
});

app.Run();