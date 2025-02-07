using Microsoft.EntityFrameworkCore;
using PlusApi.Models;

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
app.UseAuthorization(); // Required if using authentication and authorization
app.MapControllers(); // Maps controllers instead of Minimal API

app.Run();
