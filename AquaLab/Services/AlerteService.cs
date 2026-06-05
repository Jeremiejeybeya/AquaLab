using AquaLab.Data;
using AquaLab.Models;
using Microsoft.EntityFrameworkCore;

namespace AquaLab.Services;

public class AlerteService
{
    private readonly AquaLabContext _db;

    public AlerteService(AquaLabContext db) => _db = db;

    public async Task GenererAlertesAsync(Prelevement p)
    {
        var nouvelles = new List<Alerte>();

        void Verifier(string param, double? valeur, double? min, double? max, NiveauAlerte niveau, string message)
        {
            if (!valeur.HasValue) return;
            if ((min.HasValue && valeur < min) || (max.HasValue && valeur > max))
            {
                nouvelles.Add(new Alerte
                {
                    PrelevementId = p.Id,
                    Niveau = niveau,
                    Titre = $"Paramètre {param} hors normes",
                    Message = message,
                    Parametre = param,
                    ValeurMesuree = valeur,
                    SeuilMinimum = min,
                    SeuilMaximum = max,
                    DateCreation = DateTime.Now
                });
            }
        }

        Verifier("pH", p.pH, 6.5, 8.5, NiveauAlerte.Avertissement, $"pH mesuré à {p.pH:F1} (norme : 6.5–8.5)");
        Verifier("Turbidité", p.Turbidite, null, 100, NiveauAlerte.Critique, $"Turbidité critique : {p.Turbidite:F0} NTU");
        Verifier("Turbidité", p.Turbidite, null, 5, NiveauAlerte.Avertissement, $"Turbidité élevée : {p.Turbidite:F1} NTU (max 5 NTU)");
        Verifier("DCO", p.DCO, null, 500, NiveauAlerte.Critique, $"DCO critique : {p.DCO:F0} mg/L");
        Verifier("DCO", p.DCO, null, 125, NiveauAlerte.Avertissement, $"DCO élevée : {p.DCO:F0} mg/L (max 125 mg/L)");
        Verifier("Coliformes", p.ColiformesFecaux, null, 10000, NiveauAlerte.Critique, $"Contamination fécale critique : {p.ColiformesFecaux:F0} UFC/100mL");
        Verifier("Ammonium", p.Ammonium, null, 10, NiveauAlerte.Avertissement, $"Ammonium élevé : {p.Ammonium:F1} mg/L");
        Verifier("Phosphore", p.Phosphore, null, 2, NiveauAlerte.Avertissement, $"Phosphore total élevé : {p.Phosphore:F2} mg/L");
        Verifier("O₂ Dissous", p.OxygeneDissous, 2.0, null, NiveauAlerte.Critique, $"Oxygène dissous insuffisant : {p.OxygeneDissous:F1} mg/L (min 2 mg/L)");

        if (nouvelles.Count > 0)
        {
            // Éviter les doublons pour le même prélèvement
            var existants = await _db.Alertes
                .Where(a => a.PrelevementId == p.Id)
                .Select(a => a.Parametre)
                .ToListAsync();

            foreach (var al in nouvelles.Where(n => !existants.Contains(n.Parametre)))
                _db.Alertes.Add(al);

            await _db.SaveChangesAsync();
        }
    }
}
