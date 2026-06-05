using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AquaLab.Data;
using AquaLab.Models;
using AquaLab.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var connStr = "Data Source=/tmp/horizonvert.db";
Console.WriteLine($"DB: {connStr}");

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.WriteIndented = true;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AquaLabContext>(opt => opt.UseSqlite(connStr));

var jwtKey = "HorizonVertSecretKey2024!ChangeInProduction!AtLeast32Chars";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = "HorizonVert",
            ValidAudience            = "HorizonVertApp",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<DecisionTraitementService>();
builder.Services.AddScoped<AlerteService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db      = scope.ServiceProvider.GetRequiredService<AquaLabContext>();
    var authSvc = scope.ServiceProvider.GetRequiredService<AuthService>();
    try
    {
        db.Database.EnsureCreated();
        Console.WriteLine("DB creee OK");
        if (!db.Utilisateurs.Any())
        {
            db.Utilisateurs.Add(new Utilisateur
            {
                Username     = "admin",
                PasswordHash = authSvc.HashPassword("Admin@2024"),
                NomComplet   = "Administrateur",
                Email        = "admin@horizonvert.org",
                Role         = "Admin",
                Actif        = true,
                DateCreation = DateTime.Now
            });
            await db.SaveChangesAsync();
            Console.WriteLine("Compte admin cree : admin / Admin@2024");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur DB : {ex.Message}");
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => "OK");
app.MapFallbackToFile("index.html");

Console.WriteLine($"HorizonVert APP demarre port {port}");
app.Run();
