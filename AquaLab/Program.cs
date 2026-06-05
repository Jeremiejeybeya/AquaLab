using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AquaLab.Data;
using AquaLab.Models;
using AquaLab.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.WriteIndented = true;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AquaLabContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=aqualab.db"));

// ── JWT Auth ──────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? "AquaLabSecretKey2024!ChangeInProduction!AtLeast32Chars";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = "AquaLab",
            ValidAudience            = "AquaLabApp",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── App services ──────────────────────────────────────────────────────────
builder.Services.AddScoped<DecisionTraitementService>();
builder.Services.AddScoped<AlerteService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Database init ─────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db      = scope.ServiceProvider.GetRequiredService<AquaLabContext>();
    var authSvc = scope.ServiceProvider.GetRequiredService<AuthService>();

    db.Database.EnsureCreated();

    // Créer le compte admin si inexistant
    if (!db.Utilisateurs.Any())
    {
        db.Utilisateurs.Add(new Utilisateur
        {
            Username     = "admin",
            PasswordHash = authSvc.HashPassword("Admin@2024"),
            NomComplet   = "Administrateur",
            Email        = "admin@aqualab.org",
            Role         = "Admin",
            Actif        = true,
            DateCreation = DateTime.Now
        });
        await db.SaveChangesAsync();
        Console.WriteLine("✅ Compte admin créé — username: admin / password: Admin@2024");
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
