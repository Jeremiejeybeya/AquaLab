using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AquaLab.Data;
using AquaLab.Models;
using AquaLab.Services;
using System.Security.Claims;

namespace AquaLab.Controllers;

[ApiController, Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AquaLabContext _db;
    private readonly AuthService _auth;

    public AuthController(AquaLabContext db, AuthService auth)
    { _db = db; _auth = auth; }

    // ── Login ──────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Utilisateurs
            .FirstOrDefaultAsync(u => u.Username == req.Username && u.Actif);

        if (user == null || !_auth.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe incorrect" });

        user.DerniereConnexion = DateTime.Now;
        await _db.SaveChangesAsync();

        var token = _auth.GenererToken(user);

        return Ok(new
        {
            token,
            user = new {
                user.Id, user.Username, user.NomComplet,
                user.Email, user.Role
            }
        });
    }

    // ── Créer compte (Admin seulement) ─────────────────────────
    [HttpPost("register"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Utilisateurs.AnyAsync(u => u.Username == req.Username))
            return BadRequest(new { message = "Ce nom d'utilisateur existe déjà" });

        var user = new Utilisateur
        {
            Username    = req.Username,
            PasswordHash = _auth.HashPassword(req.Password),
            NomComplet  = req.NomComplet,
            Email       = req.Email,
            Role        = req.Role ?? "Technicien",
            Actif       = true
        };

        _db.Utilisateurs.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.Role, message = "Compte créé avec succès" });
    }

    // ── Changer mot de passe ───────────────────────────────────
    [HttpPut("password"), Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _db.Utilisateurs.FindAsync(userId);
        if (user == null) return NotFound();

        if (!_auth.VerifyPassword(req.AncienMotDePasse, user.PasswordHash))
            return BadRequest(new { message = "Ancien mot de passe incorrect" });

        user.PasswordHash = _auth.HashPassword(req.NouveauMotDePasse);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mot de passe modifié avec succès" });
    }

    // ── Profil connecté ────────────────────────────────────────
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _db.Utilisateurs.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new {
            user.Id, user.Username, user.NomComplet,
            user.Email, user.Role, user.DerniereConnexion
        });
    }

    // ── Liste utilisateurs (Admin) ─────────────────────────────
    [HttpGet("utilisateurs"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUtilisateurs() =>
        Ok(await _db.Utilisateurs
            .Select(u => new { u.Id, u.Username, u.NomComplet, u.Email, u.Role, u.Actif, u.DerniereConnexion })
            .ToListAsync());

    // ── Désactiver utilisateur (Admin) ─────────────────────────
    [HttpDelete("utilisateurs/{id}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Desactiver(int id)
    {
        var user = await _db.Utilisateurs.FindAsync(id);
        if (user == null) return NotFound();
        user.Actif = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Utilisateur désactivé" });
    }
}

// ── Controllers PDF ────────────────────────────────────────────
[ApiController, Route("api/[controller]")]
[Authorize]
public class PdfController : ControllerBase
{
    private readonly AquaLabContext _db;
    private readonly PdfService _pdf;
    private readonly DecisionTraitementService _decision;

    public PdfController(AquaLabContext db, PdfService pdf, DecisionTraitementService decision)
    { _db = db; _pdf = pdf; _decision = decision; }

    [HttpGet("prelevement/{id}")]
    public async Task<IActionResult> PrelevementPdf(int id)
    {
        var p = await _db.Prelevements
            .Include(x => x.Technicien)
            .Include(x => x.PointPrelevement)
            .Include(x => x.Traitement).ThenInclude(t => t!.Technicien)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound();

        RecommandationTraitement? reco = null;
        if (p.Statut != StatutPrelevement.EnAttente)
            reco = _decision.CalculerRecommandation(p);

        var bytes = _pdf.GenererRapportPrelevement(p, reco);
        return File(bytes, "application/pdf", $"prelevement_{id}_{DateTime.Now:yyyyMMdd}.pdf");
    }

    [HttpGet("rapport-mensuel")]
    public async Task<IActionResult> RapportMensuel([FromQuery] int annee = 0, [FromQuery] int mois = 0)
    {
        if (annee == 0) annee = DateTime.Now.Year;
        if (mois == 0)  mois  = DateTime.Now.Month;

        var debut = new DateTime(annee, mois, 1);
        var fin   = debut.AddMonths(1);

        var prelevements = await _db.Prelevements
            .Include(p => p.PointPrelevement)
            .Include(p => p.Technicien)
            .Include(p => p.Traitement)
            .Where(p => p.DateHeure >= debut && p.DateHeure < fin)
            .OrderBy(p => p.DateHeure)
            .ToListAsync();

        var bytes = _pdf.GenererRapportMensuel(prelevements, debut);
        return File(bytes, "application/pdf", $"rapport_{annee}_{mois:D2}.pdf");
    }
}

// ── Records ────────────────────────────────────────────────────
public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password, string NomComplet, string? Email, string? Role);
public record ChangePasswordRequest(string AncienMotDePasse, string NouveauMotDePasse);
