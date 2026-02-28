using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartHome.Data;
using SmartHome.Data.Repositories;
using SmartHome.Middlewares;
using SmartHome.Services;

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["key"]);


builder.Services.AddSingleton<DbContext>();
builder.Services.AddScoped<IUserRepository , UserRepository>();
builder.Services.AddScoped<IUserService,UserService>();
builder.Services.AddScoped<IAuthService , AuthService>();
builder.Services.AddAuthentication(ops =>
{
   ops.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
   ops.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; 
})
.AddJwtBearer(ops =>
{
    ops.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Author"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();



var app = builder.Build();
app.UseHttpsRedirection();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();