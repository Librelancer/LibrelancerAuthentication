using LibrelancerAuthentication;
using LibrelancerAuthentication.Captcha;
using Microsoft.EntityFrameworkCore;

var tokens = new TokenProvider(TimeSpan.FromMinutes(2));
var builder = WebApplication.CreateBuilder(args);

var DebugOrigins = "_debugOrigins";

builder.Services.AddCors(options => {
    options.AddPolicy(name: DebugOrigins, policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddSingleton(new CaptchaService(new CaptchaBackground()));

var backend = builder.Configuration.GetValue<string>("Backend", "sqlite");

if (backend == "sqlite")
{
    var dbPath = builder.Configuration.GetValue<string>("AppDb");
    using (var context = new UserDbContext(dbPath))
    {
        if (context.Database.GetPendingMigrations().Any()) context.Database.Migrate();
    }
    builder.Services.AddSingleton<IUserDbService>(_ => new UserDbService(dbPath));
}
else
{
    Console.Error.WriteLine($"Backend not implemented {backend}");
    return;
}

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
var captchaFactor = builder.Configuration.GetValue<int>("CaptchaCreateDifficulty", 2);
var registerEnabled = builder.Configuration.GetValue<bool>("RegisterEnabled", true);

var loginDifficulty = new string('0', loginFactor);
var changePasswordDifficulty = new string('0', changePasswordFactor);
var registerDifficulty = new string('0', registerFactor);
var captchaDifficulty = new string('0', captchaFactor);

if (!string.IsNullOrWhiteSpace(pathBase)) {
    app.UsePathBase(pathBase);
}

app.UseRouting();

if (app.Environment.IsDevelopment()) app.UseCors(DebugOrigins);


IResult AppInfo(IUserDbService users)
{
    var appInfoObject = new Dictionary<string, object>();
    appInfoObject["application"] = "authserver";
    appInfoObject["captchaDifficulty"] = captchaFactor;
    if (registerEnabled && users.CanRegister)
    {
        appInfoObject["registerEnabled"] = true;
        appInfoObject["registerDifficulty"] = registerFactor;
    }
    else
        appInfoObject["registerEnabled"] = false;

    appInfoObject["loginDifficulty"] = loginFactor;
    if (users.CanChangePassword)
    {
        appInfoObject["changePasswordEnabled"] = true;
        appInfoObject["changePasswordDifficulty"] = changePasswordFactor;
    }
    else
    {
        appInfoObject["changePasswordEnabled"] = false;
    }
    return Results.Ok(appInfoObject);
}

app.MapGet("/", AppInfo);
app.MapGet("/info", AppInfo);


app.MapPost("/login", async (PasswordRequestData request, IUserDbService users) =>
{
    if (!request.Validate(loginDifficulty ,out var error)) return error;
    
    var user = await users.Login(request.username, request.password);
    if (user != null)
    {
        return Results.Ok(new {token = tokens.GenerateToken(user.Id, user.Guid)});
    }
    return Results.BadRequest("Invalid username or password");
});

app.MapPost("/changepassword", async (ChangePasswordRequestData request, IUserDbService users) =>
{
    if (!users.CanChangePassword) return Results.BadRequest("Cannot change password");
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

app.MapPost("/createcaptcha", (CaptchaCreateRequest request, CaptchaService captchas) =>
{
    if (!request.Validate(captchaDifficulty, out var error)) return error;
    return Results.Ok(captchas.Create());
});

app.MapPost("/checkcaptcha", (CaptchaCheckData request, CaptchaService captchas) =>
{
    var result = captchas.CheckCaptcha(request.id, request.x, out string token);
    if (result == CaptchaResult.Ok)
        return Results.Ok(new{ token = token });
    else
        return Results.BadRequest(result.ToString());
});

if (registerEnabled)
{
    app.MapPost("/register", async (PasswordRequestData request, IUserDbService users, CaptchaService captchas) =>
    {
        if (!users.CanRegister) return Results.BadRequest("Cannot register user");
        if (!request.Validate(registerDifficulty, out var error)) return error;
        if (!captchas.CheckToken(request.captchaToken)) return Results.BadRequest("Invalid captchaToken");
        
        if (await users.Register(request.username, request.password))
        {
            return Results.Ok("User registered successfully");
        }

        return Results.BadRequest("User already exists");
    });
}

app.Run();