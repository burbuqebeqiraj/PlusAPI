using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; 
using PlusApi.Models;
using Microsoft.AspNetCore.Identity;
using PlusApi.Models.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // Enables MVC-style controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get connection string from app settings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register DbContext with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register SqlService<T> with Dependency Injection (DI)
builder.Services.AddScoped(typeof(ISqlService<>), typeof(SqlService<>));

// Register the password hasher service
builder.Services.AddScoped<IPasswordHasher<Users>, PasswordHasher<Users>>();

// JWT Authentication configuration
var jwtKey = builder.Configuration["Jwt:SecretKey"]; 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:PlusApi"], // Set in appsettings.json
            ValidAudience = builder.Configuration["Jwt:PlusClient"], // Set in appsettings.json
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("jwtKey")),
        };
    });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";  // Redirect users to this path when they aren't authenticated
        options.SlidingExpiration = true;     // Make sure the cookie doesn't expire until a specific period of inactivity
        options.Cookie.HttpOnly = true;       // Makes the cookie accessible only via HTTP requests (not JavaScript)
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Only send cookie over HTTPS (important for production)
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.WithOrigins("https://example.com") // Replace with your allowed domain(s)
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Ensures Swagger JSON is available
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PlusAPI v1");
        options.RoutePrefix = string.Empty; // Makes Swagger available at root URL
    });
}

app.UseHttpsRedirection();
// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();


// Enable CORS middleware
app.UseCors("AllowSpecificOrigin");

app.MapControllers();
app.Run();