using AquaLab.Models;

namespace AquaLab.Services;

/// <summary>
/// Moteur d'aide à la décision pour le traitement des eaux usées.
/// Calcule les doses de produits chimiques selon les paramètres mesurés.
/// </summary>
public class DecisionTraitementService
{
    // ─── Normes OMS / standard traitement eau usée (mg/L) ───────────────────
    private static class Normes
    {
        public const double pH_Min = 6.5;
        public const double pH_Max = 8.5;
        public const double Turbidite_Max = 5.0;           // NTU sortie
        public const double Turbidite_Urgence = 100.0;     // NTU
        public const double DCO_Max = 125.0;               // mg/L
        public const double DCO_Urgence = 500.0;
        public const double DBO5_Max = 25.0;               // mg/L
        public const double MES_Max = 35.0;                // mg/L
        public const double Nitrates_Max = 50.0;           // mg/L
        public const double Ammonium_Max = 10.0;           // mg/L
        public const double Phosphore_Max = 2.0;           // mg/L
        public const double Coliformes_Max = 1000.0;       // UFC/100mL
        public const double Chlore_Min = 0.2;              // mg/L résiduel
        public const double Chlore_Max = 0.5;              // mg/L résiduel
    }

    public RecommandationTraitement CalculerRecommandation(Prelevement p)
    {
        var reco = new RecommandationTraitement();
        var alertes = new List<string>();
        var actions = new List<string>();
        double volume = p.VolumeTraiter ?? 1.0; // m³

        // ── 1. Vérification pH ───────────────────────────────────────────────
        if (p.pH.HasValue)
        {
            double ph = p.pH.Value;
            if (ph < Normes.pH_Min)
            {
                double ecart = Normes.pH_Min - ph;
                reco.DoseChaux = CalculerDoseChaux(ph, volume);
                alertes.Add($"⚠️ pH trop bas ({ph:F1}) — correction alcalinisation requise");
                actions.Add($"Ajouter {reco.DoseChaux:F2} kg de chaux vive (Ca(OH)₂)");
            }
            else if (ph > Normes.pH_Max)
            {
                reco.DoseAcide = CalculerDoseAcide(ph, volume);
                alertes.Add($"⚠️ pH trop élevé ({ph:F1}) — acidification requise");
                actions.Add($"Ajouter {reco.DoseAcide:F2} L d'acide chlorhydrique (HCl 33%)");
            }
        }

        // ── 2. Turbidité → Coagulation/Floculation ──────────────────────────
        if (p.Turbidite.HasValue)
        {
            double turb = p.Turbidite.Value;
            if (turb > Normes.Turbidite_Urgence)
            {
                reco.DecisionSuggere = DecisionTraitement.TraitementUrgence;
                reco.DoseCoagulantMgL = 60.0;
                reco.DoseFloquantMgL = 2.0;
                alertes.Add($"🔴 URGENCE — Turbidité critique ({turb:F0} NTU)");
            }
            else if (turb > Normes.Turbidite_Max)
            {
                reco.DoseCoagulantMgL = CalculerDoseCoagulant(turb);
                reco.DoseFloquantMgL = reco.DoseCoagulantMgL * 0.03;
                alertes.Add($"⚠️ Turbidité élevée ({turb:F1} NTU)");
            }
            reco.DoseCoagulantTotaleKg = (reco.DoseCoagulantMgL ?? 0) * volume / 1000.0;
            reco.DoseFloquantTotaleKg = (reco.DoseFloquantMgL ?? 0) * volume / 1000.0;

            if (reco.DoseCoagulantMgL.HasValue && reco.DoseCoagulantMgL > 0)
                actions.Add($"Coagulant (sulfate d'aluminium) : {reco.DoseCoagulantMgL:F1} mg/L → {reco.DoseCoagulantTotaleKg:F2} kg pour {volume} m³");
        }

        // ── 3. Désinfection au chlore ────────────────────────────────────────
        double besoinsChloreTotaux = CalculerBesoinChloreTotaux(p);
        reco.DoseChloreMgL = besoinsChloreTotaux;
        reco.DoseChloreTotaleKg = besoinsChloreTotaux * volume / 1000.0;

        if (p.ColiformesFecaux.HasValue && p.ColiformesFecaux > Normes.Coliformes_Max)
            alertes.Add($"🔴 Contamination bactérienne élevée ({p.ColiformesFecaux:F0} UFC/100mL)");

        if (reco.DoseChloreMgL > 0)
            actions.Add($"Chlore (eau de javel) : {reco.DoseChloreMgL:F2} mg/L → {reco.DoseChloreTotaleKg:F3} kg pour {volume} m³");

        // ── 4. DCO / DBO5 → traitement biologique intensifié ─────────────────
        if (p.DCO.HasValue && p.DCO > Normes.DCO_Max)
        {
            if (p.DCO > Normes.DCO_Urgence)
            {
                alertes.Add($"🔴 DCO critique ({p.DCO:F0} mg/L) — traitement biologique renforcé obligatoire");
                reco.DecisionSuggere = DecisionTraitement.TraitementUrgence;
            }
            else
            {
                alertes.Add($"⚠️ DCO élevée ({p.DCO:F0} mg/L) — prolonger aération biologique");
                if (reco.DecisionSuggere == DecisionTraitement.TraitementStandard)
                    reco.DecisionSuggere = DecisionTraitement.TraitementRenforce;
            }
            actions.Add("Augmenter temps de rétention bassin biologique (+2h minimum)");
        }

        if (p.DBO5.HasValue && p.DBO5 > Normes.DBO5_Max)
        {
            alertes.Add($"⚠️ DBO5 élevée ({p.DBO5:F0} mg/L)");
            actions.Add("Vérifier charge organique et ajuster débit d'entrée");
        }

        // ── 5. Phosphore → précipitation chimique ────────────────────────────
        if (p.Phosphore.HasValue && p.Phosphore > Normes.Phosphore_Max)
        {
            double dosePhosphore = p.Phosphore.Value * 3.5; // ratio molaire FeCl3
            alertes.Add($"⚠️ Phosphore total élevé ({p.Phosphore:F2} mg/L)");
            actions.Add($"Ajouter FeCl₃ (chlorure ferrique) : {dosePhosphore:F1} mg/L pour précipitation du phosphore");
            if (reco.DoseCoagulantMgL == null)
            {
                reco.DoseCoagulantMgL = dosePhosphore;
                reco.DoseCoagulantTotaleKg = dosePhosphore * volume / 1000.0;
            }
        }

        // ── 6. Ammonium / Azote ──────────────────────────────────────────────
        if (p.Ammonium.HasValue && p.Ammonium > Normes.Ammonium_Max)
        {
            alertes.Add($"⚠️ Ammonium élevé ({p.Ammonium:F1} mg/L) — nitrification insuffisante");
            actions.Add("Vérifier aération bassin de nitrification, ajuster apport O₂");
        }

        // ── 7. Score qualité global ───────────────────────────────────────────
        reco.ScoreQualite = CalculerScore(p);
        reco.NiveauRisque = reco.ScoreQualite >= 80 ? "Faible" :
                           reco.ScoreQualite >= 50 ? "Modéré" :
                           reco.ScoreQualite >= 30 ? "Élevé" : "Critique";

        // ── 8. Décision finale ────────────────────────────────────────────────
        if (reco.DecisionSuggere == DecisionTraitement.TraitementStandard && alertes.Count == 0)
        {
            actions.Add("✅ Tous les paramètres sont dans les normes");
        }

        if (reco.ScoreQualite < 20)
        {
            reco.DecisionSuggere = DecisionTraitement.RejetRejection;
            actions.Add("⛔ Eau non conforme — nouveau cycle de traitement ou rejet");
        }

        reco.Alertes = alertes;
        reco.ActionsRecommandees = actions;
        reco.Justification = GenererJustification(reco, p);

        return reco;
    }

    // ─── Formules de calcul ──────────────────────────────────────────────────

    private double CalculerDoseChaux(double pH, double volume)
    {
        // Approximation : 1 unité pH nécessite ~10-50 mg/L de Ca(OH)2 selon alcalinité
        double ecart = 7.0 - pH;
        double concentration = Math.Min(ecart * 20.0, 80.0); // mg/L
        return concentration * volume / 1000.0; // kg
    }

    private double CalculerDoseAcide(double pH, double volume)
    {
        double ecart = pH - 7.5;
        double concentration = Math.Min(ecart * 15.0, 60.0); // mL/L → L/m³
        return concentration * volume / 1000.0;
    }

    private double CalculerDoseCoagulant(double turbidite)
    {
        // Sulfate d'aluminium Al2(SO4)3 : 0.3-0.5 mg/L par NTU
        if (turbidite <= 20) return turbidite * 0.4;
        if (turbidite <= 50) return turbidite * 0.5;
        return Math.Min(turbidite * 0.6, 80.0);
    }

    private double CalculerBesoinChloreTotaux(Prelevement p)
    {
        // Chlore de base pour désinfection
        double base_dose = 2.0; // mg/L minimum

        // Augmentation selon contamination
        if (p.ColiformesFecaux.HasValue)
        {
            if (p.ColiformesFecaux > 10000) base_dose = 5.0;
            else if (p.ColiformesFecaux > 1000) base_dose = 3.5;
        }

        // Demande en chlore selon matières organiques (DCO)
        double demande_organique = 0;
        if (p.DCO.HasValue) demande_organique = p.DCO.Value * 0.005;

        // Demande en chlore selon ammonium (chloramines)
        double demande_ammonium = 0;
        if (p.Ammonium.HasValue) demande_ammonium = p.Ammonium.Value * 7.6; // ratio stœchiométrique

        double total = base_dose + demande_organique + Math.Min(demande_ammonium, 8.0);
        return Math.Round(Math.Min(total, 15.0), 2); // Plafond sécurité 15 mg/L
    }

    private double CalculerScore(Prelevement p)
    {
        var scores = new List<(double valeur, double min, double max, double poids)>();
        double score = 100.0;

        void Penaliser(double? val, double? min, double? max, double penalite)
        {
            if (!val.HasValue) return;
            if (min.HasValue && val < min) score -= penalite * (1.0 - val.Value / min.Value);
            if (max.HasValue && val > max) score -= penalite * Math.Min((val.Value - max.Value) / max.Value, 2.0);
        }

        Penaliser(p.pH, Normes.pH_Min, Normes.pH_Max, 20);
        Penaliser(p.Turbidite, null, Normes.Turbidite_Max, 15);
        Penaliser(p.DCO, null, Normes.DCO_Max, 20);
        Penaliser(p.DBO5, null, Normes.DBO5_Max, 15);
        Penaliser(p.MES, null, Normes.MES_Max, 10);
        Penaliser(p.ColiformesFecaux, null, Normes.Coliformes_Max, 15);
        Penaliser(p.Ammonium, null, Normes.Ammonium_Max, 10);
        Penaliser(p.Phosphore, null, Normes.Phosphore_Max, 5);

        return Math.Max(0, Math.Round(score));
    }

    private string GenererJustification(RecommandationTraitement reco, Prelevement p)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Score qualité : {reco.ScoreQualite}/100 — Risque {reco.NiveauRisque}");
        sb.AppendLine();
        if (reco.Alertes.Count > 0)
        {
            sb.AppendLine("Paramètres hors normes :");
            foreach (var a in reco.Alertes) sb.AppendLine($"  {a}");
        }
        return sb.ToString().Trim();
    }
}

public class RecommandationTraitement
{
    public double? DoseChloreMgL { get; set; }
    public double? DoseChloreTotaleKg { get; set; }
    public double? DoseCoagulantMgL { get; set; }
    public double? DoseCoagulantTotaleKg { get; set; }
    public double? DoseFloquantMgL { get; set; }
    public double? DoseFloquantTotaleKg { get; set; }
    public double? DoseChaux { get; set; }
    public double? DoseAcide { get; set; }
    public DecisionTraitement DecisionSuggere { get; set; } = DecisionTraitement.TraitementStandard;
    public string Justification { get; set; } = "";
    public List<string> Alertes { get; set; } = new();
    public List<string> ActionsRecommandees { get; set; } = new();
    public double ScoreQualite { get; set; }
    public string NiveauRisque { get; set; } = "Inconnu";
}
