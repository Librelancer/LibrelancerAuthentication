using LibrelancerAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var tokens = new TokenProvider(TimeSpan.FromMinutes(2));
var passwordHasher = new PasswordHasher<User>();

var builder = WebApplication.CreateBuilder(args);
var dbPath = builder.Configuration.GetValue<string>("AppDb");

var DebugOrigins = "_debugOrigins";

builder.Services.AddCors(options => {
    options.AddPolicy(name: DebugOrigins, policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

using (var context = new UserDbContext(dbPath))
{
    if (context.Database.GetPendingMigrations().Any()) context.Database.Migrate();
}

builder.Services.AddSingleton(_ => new UserDbService(dbPath, passwordHasher));

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

var pathBase = builder.Configuration.GetValue<string>("PathBase");
var loginFactor = builder.Configuration.GetValue<int>("LoginDifficulty", 3);
var changePasswordFactor = builder.Configuration.GetValue<int>("ChangePasswordDifficulty", 3);
var registerFactor = builder.Configuration.GetValue<int>("RegisterDifficulty", 4);
var registerEnabled = builder.Configuration.GetValue<bool>("RegisterEnabled", true);

var loginDifficulty = new string('0', loginFactor);
var changePasswordDifficulty = new string('0', changePasswordFactor);
var registerDifficulty = new string('0', registerFactor);

if (!string.IsNullOrWhiteSpace(pathBase)) {
    GlobalVars.PathBase = pathBase;
    app.UsePathBase(pathBase);
}

app.UseRouting();

if (app.Environment.IsDevelopment()) app.UseCors(DebugOrigins);

var appInfoObject = new Dictionary<string, object>();
appInfoObject["application"] = "authserver";
if (registerEnabled)
{
    appInfoObject["registerEnabled"] = true;
    appInfoObject["registerDifficulty"] = registerFactor;
}
else
    appInfoObject["registerEnabled"] = false;

appInfoObject["loginDifficulty"] = loginFactor;
appInfoObject["changePasswordDifficulty"] = changePasswordFactor;

var appInfo = Results.Ok(appInfoObject);

app.MapGet("/", () => appInfo);
app.MapGet("/info", () => appInfo);


app.MapPost("/login", async (PasswordRequestData request, UserDbService users) =>
{
    if (!request.Validate(loginDifficulty ,out var error)) return error;
    
    var user = await users.Login(request.username, request.password);
    if (user != null)
    {
        return Results.Ok(new {token = tokens.GenerateToken(user.Id, user.Guid)});
    }
    return Results.BadRequest("Invalid username or password");
});

app.MapPost("/changepassword", async (ChangePasswordRequestData request, UserDbService users) =>
{
    if (!request.Validate(changePasswordDifficulty, out var error)) return error;
    
    if (await users.ChangePassword(request.username, request.oldpassword, request.newpassword))
    {
        return Results.Ok();
    }
    return Results.BadRequest("Invalid username or password");
});

app.MapPost("/verifytoken", (TokenRequestData token) =>
{
    if (!token.Validate(null, out var error)) return error;
    
    if (tokens.VerifyToken(token.token, out var value))
        return Results.Ok(new {guid = value});
    return Results.BadRequest("Invalid token");
});

if (registerEnabled)
{
    app.MapPost("/register", async (PasswordRequestData request, UserDbService users) =>
    {
        if (!request.Validate(registerDifficulty, out var error)) return error;

        if (await users.Register(request.username, request.password))
        {
            return Results.Ok("User registered successfully");
        }

        return Results.BadRequest("User already exists");
    });
}

app.Run();