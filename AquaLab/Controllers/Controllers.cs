using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquaLab.Data;
using AquaLab.Models;
using AquaLab.Services;

namespace AquaLab.Controllers;

// ════════════════════════════════════════════════════════════
//  TECHNICIENS
// ════════════════════════════════════════════════════════════
[ApiController, Route("api/[controller]")]
public class TechniciensController : ControllerBase
{
    private readonly AquaLabContext _db;
    public TechniciensController(AquaLabContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Techniciens.OrderBy(t => t.Nom).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _db.Techniciens.FindAsync(id);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Technicien t)
    {
        _db.Techniciens.Add(t);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = t.Id }, t);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Technicien dto)
    {
        var t = await _db.Techniciens.FindAsync(id);
        if (t == null) return NotFound();
        t.Nom = dto.Nom; t.Prenom = dto.Prenom; t.Email = dto.Email;
        t.Telephone = dto.Telephone; t.Role = dto.Role; t.Actif = dto.Actif;
        await _db.SaveChangesAsync();
        return Ok(t);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.Techniciens.FindAsync(id);
        if (t == null) return NotFound();
        t.Actif = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

// ════════════════════════════════════════════════════════════
//  POINTS DE PRELEVEMENT
// ════════════════════════════════════════════════════════════
[ApiController, Route("api/[controller]")]
public class PointsPrelevementController : ControllerBase
{
    private readonly AquaLabContext _db;
    public PointsPrelevementController(AquaLabContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.PointsPrelevement.Where(p => p.Actif).OrderBy(p => p.Nom).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(PointPrelevement p)
    {
        _db.PointsPrelevement.Add(p);
        await _db.SaveChangesAsync();
        return Ok(p);
    }
}

// ════════════════════════════════════════════════════════════
//  PRELEVEMENTS
// ════════════════════════════════════════════════════════════
[ApiController, Route("api/[controller]")]
public class PrelevementsController : ControllerBase
{
    private readonly AquaLabContext _db;
    private readonly AlerteService _alertes;

    public PrelevementsController(AquaLabContext db, AlerteService alertes)
    { _db = db; _alertes = alertes; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var query = _db.Prelevements
            .Include(p => p.Technicien)
            .Include(p => p.PointPrelevement)
            .Include(p => p.Traitement)
            .OrderByDescending(p => p.DateHeure);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        return Ok(new { total, page, size, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await _db.Prelevements
            .Include(x => x.Technicien)
            .Include(x => x.PointPrelevement)
            .Include(x => x.Traitement).ThenInclude(t => t!.Technicien)
            .FirstOrDefaultAsync(x => x.Id == id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Prelevement p)
    {
        p.DateHeure = DateTime.Now;
        p.Statut = StatutPrelevement.Analyse;
        _db.Prelevements.Add(p);
        await _db.SaveChangesAsync();
        await _alertes.GenererAlertesAsync(p);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Prelevement dto)
    {
        var p = await _db.Prelevements.FindAsync(id);
        if (p == null) return NotFound();

        p.pH = dto.pH; p.Turbidite = dto.Turbidite; p.Conductivite = dto.Conductivite;
        p.Temperature = dto.Temperature; p.OxygeneDissous = dto.OxygeneDissous;
        p.DCO = dto.DCO; p.DBO5 = dto.DBO5; p.MES = dto.MES;
        p.Nitrates = dto.Nitrates; p.Nitrites = dto.Nitrites;
        p.Ammonium = dto.Ammonium; p.Phosphore = dto.Phosphore;
        p.ColiformesFecaux = dto.ColiformesFecaux;
        p.VolumeTraiter = dto.VolumeTraiter;
        p.Observations = dto.Observations;
        p.Statut = dto.Statut;

        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Prelevements.FindAsync(id);
        if (p == null) return NotFound();
        _db.Prelevements.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

// ════════════════════════════════════════════════════════════
//  TRAITEMENTS + AIDE A LA DECISION
// ════════════════════════════════════════════════════════════
[ApiController, Route("api/[controller]")]
public class TraitementsController : ControllerBase
{
    private readonly AquaLabContext _db;
    private readonly DecisionTraitementService _decision;

    public TraitementsController(AquaLabContext db, DecisionTraitementService decision)
    { _db = db; _decision = decision; }

    [HttpGet("recommandation/{prelevementId}")]
    public async Task<IActionResult> GetRecommandation(int prelevementId)
    {
        var p = await _db.Prelevements.FindAsync(prelevementId);
        if (p == null) return NotFound("Prélèvement introuvable");
        var reco = _decision.CalculerRecommandation(p);
        return Ok(reco);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Traitement t)
    {
        var prelevement = await _db.Prelevements.FindAsync(t.PrelevementId);
        if (prelevement == null) return NotFound("Prélèvement introuvable");

        t.DateTraitement = DateTime.Now;
        _db.Traitements.Add(t);

        prelevement.Statut = StatutPrelevement.TraitementRecommande;
        await _db.SaveChangesAsync();
        return Ok(t);
    }

    [HttpPut("{id}/appliquer")]
    public async Task<IActionResult> Appliquer(int id, [FromBody] ResultatApplication res)
    {
        var t = await _db.Traitements
            .Include(x => x.Prelevement)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        t.Applique = true;
        t.DateApplication = DateTime.Now;
        t.pHFinal = res.pHFinal;
        t.TurbiditeFinal = res.TurbiditeFinal;
        t.ChloreFinal = res.ChloreFinal;
        t.ConformeNormes = res.ConformeNormes;
        t.NotesOperateur = res.Notes;

        if (t.Prelevement != null)
            t.Prelevement.Statut = StatutPrelevement.Traite;

        await _db.SaveChangesAsync();
        return Ok(t);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Traitements
            .Include(t => t.Prelevement).ThenInclude(p => p!.PointPrelevement)
            .Include(t => t.Technicien)
            .OrderByDescending(t => t.DateTraitement)
            .Take(100)
            .ToListAsync());
}

public record ResultatApplication(double? pHFinal, double? TurbiditeFinal, double? ChloreFinal, bool? ConformeNormes, string? Notes);

// ════════════════════════════════════════════════════════════
//  ALERTES
// ════════════════════════════════════════════════════════════
[ApiController, Route("api/[controller]")]
public class AlertesController : ControllerBase
{
    private readonly AquaLabContext _db;
    public AlertesController(AquaLabContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool nonAcquittees = false)
    {
        var q = _db.Alertes.Include(a => a.Prelevement).OrderByDescending(a => a.DateCreation);
        var items = nonAcquittees
            ? await q.Where(a => !a.Acquittee).ToListAsync()
            : await q.Take(50).ToListAsync();
        return Ok(items);
    }

    [HttpPut("{id}/acquitter")]
    public async Task<IActionResult> Acquitter(int id)
    {
        var a = await _db.Alertes.FindAsync(id);
        if (a == null) return NotFound();
        a.Acquittee = true;
        a.DateAcquittement = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(a);
    }
}

// ════════════════════════════════════════════════════════════
//  RAPPORTS / STATISTIQUES
// ════════════════════════════════════════════════════════════
[ApiController, Route("api/[controller]")]
public class RapportsController : ControllerBase
{
    private readonly AquaLabContext _db;
    public RapportsController(AquaLabContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var now = DateTime.Now;
        var debutMois = new DateTime(now.Year, now.Month, 1);
        var debutSemaine = now.AddDays(-7);

        var prelevements = await _db.Prelevements.ToListAsync();
        var alertesActives = await _db.Alertes.CountAsync(a => !a.Acquittee);
        var traitements = await _db.Traitements.ToListAsync();

        return Ok(new
        {
            total_prelevements = prelevements.Count,
            prelevements_ce_mois = prelevements.Count(p => p.DateHeure >= debutMois),
            prelevements_semaine = prelevements.Count(p => p.DateHeure >= debutSemaine),
            alertes_actives = alertesActives,
            traitements_appliques = traitements.Count(t => t.Applique),
            taux_conformite = traitements.Count > 0
                ? Math.Round(traitements.Count(t => t.ConformeNormes == true) * 100.0 / traitements.Count(t => t.ConformeNormes.HasValue), 1)
                : 0.0,
            en_attente = prelevements.Count(p => p.Statut == StatutPrelevement.EnAttente || p.Statut == StatutPrelevement.Analyse),
        });
    }

    [HttpGet("evolution")]
    public async Task<IActionResult> Evolution([FromQuery] int jours = 30)
    {
        var debut = DateTime.Now.AddDays(-jours);
        var data = await _db.Prelevements
            .Where(p => p.DateHeure >= debut)
            .OrderBy(p => p.DateHeure)
            .Select(p => new {
                date = p.DateHeure.ToString("yyyy-MM-dd"),
                pH = p.pH,
                turbidite = p.Turbidite,
                DCO = p.DCO,
                DBO5 = p.DBO5,
                coliformes = p.ColiformesFecaux
            })
            .ToListAsync();
        return Ok(data);
    }
}
