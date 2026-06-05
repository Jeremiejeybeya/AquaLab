using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AquaLab.Data;
using AquaLab.Models;
using AquaLab.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Connexion SQLite : /tmp en fallback pour Railway ─────────────────────
var connStr = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? "Data Source=/tmp/aqualab.db";

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.WriteIndented = true;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AquaLabContext>(opt => opt.UseSqlite(connStr));

// ── JWT ───────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? "HorizonVertSecretKey2024!ChangeInProduction!AtLeast32Chars";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "HorizonVert",
            ValidAudience = "HorizonVertApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

// ── Init base de données + compte admin ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AquaLabContext>();
    var authSvc = scope.ServiceProvider.GetRequiredService<AuthService>();

    // Crée la DB si elle n'existe pas
    db.Database.EnsureCreated();

    // Crée le compte admin si inexistant
    if (!db.Utilisateurs.Any())
    {
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@2024";
        db.Utilisateurs.Add(new Utilisateur
        {
            Username = "admin",
            PasswordHash = authSvc.HashPassword(adminPassword),
            NomComplet = "Administrateur",
            Email = "admin@horizonvert.org",
            Role = "Admin",
            Actif = true,
            DateCreation = DateTime.Now
        });
        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Compte admin créé — DB: {connStr}");
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
