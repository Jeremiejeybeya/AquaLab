using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AquaLab.Models;
using Microsoft.IdentityModel.Tokens;

namespace AquaLab.Services;

public class AuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config) => _config = config;

    // ── Hachage mot de passe (PBKDF2) ─────────────────────────
    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);
        byte[] combined = new byte[48];
        salt.CopyTo(combined, 0);
        hash.CopyTo(combined, 16);
        return Convert.ToBase64String(combined);
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            byte[] combined = Convert.FromBase64String(storedHash);
            byte[] salt = combined[..16];
            byte[] storedHashBytes = combined[16..];
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
        }
        catch { return false; }
    }

    // ── Génération token JWT ───────────────────────────────────
    public string GenererToken(Utilisateur user)
    {
        var jwtKey = _config["Jwt:Key"] ?? "AquaLabSecretKey2024!ChangeInProduction";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("NomComplet", user.NomComplet),
            new Claim("Email", user.Email ?? "")
        };

        var token = new JwtSecurityToken(
            issuer:   "AquaLab",
            audience: "AquaLabApp",
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(8),   // Session 8h
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
