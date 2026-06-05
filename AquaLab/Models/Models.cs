using System.ComponentModel.DataAnnotations;

namespace AquaLab.Models;

// ─────────────────────────────────────────────
//  TECHNICIEN
// ─────────────────────────────────────────────
public class Technicien
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nom { get; set; } = "";

    [Required, MaxLength(100)]
    public string Prenom { get; set; } = "";

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telephone { get; set; }

    [MaxLength(50)]
    public string Role { get; set; } = "Technicien";   // Technicien | Chef de Labo | Admin

    public bool Actif { get; set; } = true;

    public DateTime DateCreation { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<Prelevement> Prelevements { get; set; } = new List<Prelevement>();
    public ICollection<Traitement> Traitements { get; set; } = new List<Traitement>();
}

// ─────────────────────────────────────────────
//  POINT DE PRELEVEMENT (site)
// ─────────────────────────────────────────────
public class PointPrelevement
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nom { get; set; } = "";

    [MaxLength(200)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Localisation { get; set; }

    public bool Actif { get; set; } = true;

    // Navigation
    public ICollection<Prelevement> Prelevements { get; set; } = new List<Prelevement>();
}

// ─────────────────────────────────────────────
//  PRELEVEMENT (échantillon)
// ─────────────────────────────────────────────
public class Prelevement
{
    public int Id { get; set; }

    public int PointPrelevementId { get; set; }
    public PointPrelevement? PointPrelevement { get; set; }

    public int TechnicienId { get; set; }
    public Technicien? Technicien { get; set; }

    public DateTime DateHeure { get; set; } = DateTime.Now;

    // Paramètres physico-chimiques
    public double? pH { get; set; }                         // 0-14
    public double? Turbidite { get; set; }                  // NTU
    public double? Conductivite { get; set; }               // µS/cm
    public double? Temperature { get; set; }                // °C
    public double? OxygeneDissous { get; set; }             // mg/L
    public double? DCO { get; set; }                        // mg/L Demande Chimique en Oxygène
    public double? DBO5 { get; set; }                       // mg/L Demande Biochimique en Oxygène 5j
    public double? MES { get; set; }                        // mg/L Matières En Suspension
    public double? Nitrates { get; set; }                   // mg/L NO3
    public double? Nitrites { get; set; }                   // mg/L NO2
    public double? Ammonium { get; set; }                   // mg/L NH4
    public double? Phosphore { get; set; }                  // mg/L P total
    public double? ColiformesFecaux { get; set; }           // UFC/100mL
    public double? ChloreTotalResiduel { get; set; }        // mg/L

    // Volume à traiter (en m³)
    public double? VolumeTraiter { get; set; }

    [MaxLength(500)]
    public string? Observations { get; set; }

    public StatutPrelevement Statut { get; set; } = StatutPrelevement.EnAttente;

    // Navigation
    public Traitement? Traitement { get; set; }
}

public enum StatutPrelevement
{
    EnAttente,
    Analyse,
    TraitementRecommande,
    Traite,
    Rejete
}

// ─────────────────────────────────────────────
//  TRAITEMENT
// ─────────────────────────────────────────────
public class Traitement
{
    public int Id { get; set; }

    public int PrelevementId { get; set; }
    public Prelevement? Prelevement { get; set; }

    public int TechnicienId { get; set; }
    public Technicien? Technicien { get; set; }

    public DateTime DateTraitement { get; set; } = DateTime.Now;

    // Doses recommandées (mg/L ou kg selon contexte)
    public double? DoseChloreMgL { get; set; }
    public double? DoseChloreTotaleKg { get; set; }

    public double? DoseCoagulantMgL { get; set; }
    public double? DoseCoagulantTotaleKg { get; set; }

    public double? DoseFloquantMgL { get; set; }
    public double? DoseFloquantTotaleKg { get; set; }

    public double? DoseChaux { get; set; }                  // pH correction
    public double? DoseAcide { get; set; }                  // pH correction

    // Décision finale
    public DecisionTraitement Decision { get; set; } = DecisionTraitement.TraitementStandard;

    [MaxLength(1000)]
    public string? Justification { get; set; }

    [MaxLength(1000)]
    public string? NotesOperateur { get; set; }

    public bool Applique { get; set; } = false;
    public DateTime? DateApplication { get; set; }

    // Résultat post-traitement
    public double? pHFinal { get; set; }
    public double? TurbiditeFinal { get; set; }
    public double? ChloreFinal { get; set; }

    public bool? ConformeNormes { get; set; }
}

public enum DecisionTraitement
{
    TraitementStandard,
    TraitementRenforce,
    TraitementUrgence,
    RejetRejection,
    NouveauPrelevement
}

// ─────────────────────────────────────────────
//  ALERTE
// ─────────────────────────────────────────────
public class Alerte
{
    public int Id { get; set; }

    public int? PrelevementId { get; set; }
    public Prelevement? Prelevement { get; set; }

    public NiveauAlerte Niveau { get; set; } = NiveauAlerte.Info;

    [Required, MaxLength(200)]
    public string Titre { get; set; } = "";

    [MaxLength(1000)]
    public string? Message { get; set; }

    [MaxLength(50)]
    public string Parametre { get; set; } = "";     // ex: "pH", "Turbidite"

    public double? ValeurMesuree { get; set; }
    public double? SeuilMinimum { get; set; }
    public double? SeuilMaximum { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.Now;
    public bool Acquittee { get; set; } = false;
    public DateTime? DateAcquittement { get; set; }
}

public enum NiveauAlerte
{
    Info,
    Avertissement,
    Critique
}
