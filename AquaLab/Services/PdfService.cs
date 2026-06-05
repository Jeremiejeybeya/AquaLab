using AquaLab.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AquaLab.Services;

public class PdfService
{
    public byte[] GenererRapportPrelevement(Prelevement p, RecommandationTraitement? reco = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                // ── En-tête ───────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("💧 AquaLab")
                                .FontSize(22).Bold().FontColor("#0a7ea4");
                            c.Item().Text("Rapport de prélèvement et traitement")
                                .FontSize(11).FontColor("#666666");
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Text($"#{p.Id}").FontSize(18).Bold().FontColor("#333");
                            c.Item().Text(p.DateHeure.ToString("dd/MM/yyyy HH:mm"))
                                .FontSize(9).FontColor("#888");
                        });
                    });
                    col.Item().PaddingTop(5).BorderBottom(1).BorderColor("#cccccc").Text("");
                });

                // ── Corps ─────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(col =>
                {
                    // Infos générales
                    col.Item().Background("#f0f8ff").Padding(10).Column(info =>
                    {
                        info.Item().Text("INFORMATIONS GÉNÉRALES").FontSize(9)
                            .Bold().FontColor("#0a7ea4").LetterSpacing(1);
                        info.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Site : {p.PointPrelevement?.Nom ?? "—"}").Bold();
                                c.Item().Text($"Technicien : {p.Technicien?.Prenom} {p.Technicien?.Nom}");
                                c.Item().Text($"Date/Heure : {p.DateHeure:dd/MM/yyyy HH:mm}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Volume à traiter : {p.VolumeTraiter?.ToString("F0") ?? "—"} m³").Bold();
                                c.Item().Text($"Statut : {p.Statut}");
                                if (!string.IsNullOrEmpty(p.Observations))
                                    c.Item().Text($"Observations : {p.Observations}");
                            });
                        });
                    });

                    col.Item().PaddingTop(14).Text("PARAMÈTRES MESURÉS").FontSize(9)
                        .Bold().FontColor("#0a7ea4").LetterSpacing(1);

                    // Tableau paramètres
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        // Header
                        table.Header(h =>
                        {
                            h.Cell().Background("#0a7ea4").Padding(5)
                                .Text("Paramètre").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Background("#0a7ea4").Padding(5)
                                .Text("Valeur mesurée").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Background("#0a7ea4").Padding(5)
                                .Text("Unité").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Background("#0a7ea4").Padding(5)
                                .Text("Norme").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        void LigneParam(string nom, double? val, string unite, string norme, bool hors = false)
                        {
                            var bg = hors ? "#fff3cd" : Colors.White;
                            var fg = hors ? "#856404" : Colors.Black;
                            table.Cell().Background(bg).Padding(5).Text(nom).FontSize(9);
                            table.Cell().Background(bg).Padding(5)
                                .Text(val?.ToString("F2") ?? "—").FontSize(9).FontColor(fg).Bold();
                            table.Cell().Background(bg).Padding(5).Text(unite).FontSize(9);
                            table.Cell().Background(bg).Padding(5).Text(norme).FontSize(9).FontColor("#666");
                        }

                        LigneParam("pH", p.pH, "", "6.5 – 8.5",
                            p.pH.HasValue && (p.pH < 6.5 || p.pH > 8.5));
                        LigneParam("Turbidité", p.Turbidite, "NTU", "≤ 5",
                            p.Turbidite > 5);
                        LigneParam("Température", p.Temperature, "°C", "—");
                        LigneParam("Conductivité", p.Conductivite, "µS/cm", "—");
                        LigneParam("O₂ Dissous", p.OxygeneDissous, "mg/L", "≥ 2",
                            p.OxygeneDissous < 2);
                        LigneParam("DCO", p.DCO, "mg/L", "≤ 125",
                            p.DCO > 125);
                        LigneParam("DBO5", p.DBO5, "mg/L", "≤ 25",
                            p.DBO5 > 25);
                        LigneParam("MES", p.MES, "mg/L", "≤ 35",
                            p.MES > 35);
                        LigneParam("Nitrates (NO₃)", p.Nitrates, "mg/L", "≤ 50",
                            p.Nitrates > 50);
                        LigneParam("Ammonium (NH₄)", p.Ammonium, "mg/L", "≤ 10",
                            p.Ammonium > 10);
                        LigneParam("Phosphore total", p.Phosphore, "mg/L", "≤ 2",
                            p.Phosphore > 2);
                        LigneParam("Coliformes fécaux", p.ColiformesFecaux, "UFC/100mL", "≤ 1000",
                            p.ColiformesFecaux > 1000);
                        LigneParam("Chlore résiduel", p.ChloreTotalResiduel, "mg/L", "0.2 – 0.5");
                    });

                    // Recommandation traitement
                    if (reco != null)
                    {
                        col.Item().PaddingTop(14).Text("RECOMMANDATION DE TRAITEMENT").FontSize(9)
                            .Bold().FontColor("#0a7ea4").LetterSpacing(1);

                        col.Item().PaddingTop(6).Background("#f8f9fa").Padding(10).Column(r =>
                        {
                            r.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Score qualité : {reco.ScoreQualite}/100")
                                    .FontSize(14).Bold()
                                    .FontColor(reco.ScoreQualite >= 80 ? "#198754" :
                                               reco.ScoreQualite >= 50 ? "#fd7e14" : "#dc3545");
                                row.RelativeItem().AlignRight()
                                    .Text($"Risque : {reco.NiveauRisque}").Bold().FontSize(12);
                            });

                            r.Item().PaddingTop(8).Text("Doses recommandées :").Bold().FontSize(9);

                            if (reco.DoseChloreMgL > 0)
                                r.Item().Text($"  • Chlore : {reco.DoseChloreMgL:F2} mg/L → {reco.DoseChloreTotaleKg:F3} kg total").FontSize(9);
                            if (reco.DoseCoagulantMgL > 0)
                                r.Item().Text($"  • Coagulant : {reco.DoseCoagulantMgL:F1} mg/L → {reco.DoseCoagulantTotaleKg:F2} kg total").FontSize(9);
                            if (reco.DoseFloquantMgL > 0)
                                r.Item().Text($"  • Floquant : {reco.DoseFloquantMgL:F2} mg/L → {reco.DoseFloquantTotaleKg:F3} kg total").FontSize(9);
                            if (reco.DoseChaux > 0)
                                r.Item().Text($"  • Chaux Ca(OH)₂ : {reco.DoseChaux:F2} kg").FontSize(9);

                            r.Item().PaddingTop(6).Text("Actions recommandées :").Bold().FontSize(9);
                            foreach (var action in reco.ActionsRecommandees)
                                r.Item().Text($"  → {action}").FontSize(9);
                        });
                    }

                    // Traitement appliqué
                    if (p.Traitement != null && p.Traitement.Applique)
                    {
                        col.Item().PaddingTop(14).Text("RÉSULTAT DU TRAITEMENT").FontSize(9)
                            .Bold().FontColor("#0a7ea4").LetterSpacing(1);
                        col.Item().PaddingTop(6).Background(
                            p.Traitement.ConformeNormes == true ? "#d1e7dd" : "#f8d7da")
                            .Padding(10).Column(r =>
                            {
                                r.Item().Text(p.Traitement.ConformeNormes == true
                                    ? "✓ EAU CONFORME AUX NORMES" : "✗ EAU NON CONFORME")
                                    .Bold().FontSize(11)
                                    .FontColor(p.Traitement.ConformeNormes == true ? "#0f5132" : "#842029");
                                r.Item().PaddingTop(4).Text($"Date application : {p.Traitement.DateApplication:dd/MM/yyyy HH:mm}").FontSize(9);
                                if (p.Traitement.pHFinal.HasValue)
                                    r.Item().Text($"pH final : {p.Traitement.pHFinal:F1}").FontSize(9);
                                if (p.Traitement.TurbiditeFinal.HasValue)
                                    r.Item().Text($"Turbidité finale : {p.Traitement.TurbiditeFinal:F1} NTU").FontSize(9);
                                if (!string.IsNullOrEmpty(p.Traitement.NotesOperateur))
                                    r.Item().Text($"Notes : {p.Traitement.NotesOperateur}").FontSize(9);
                            });
                    }
                });

                // ── Pied de page ──────────────────────────────────────
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("AquaLab — Rapport généré le ").FontSize(8).FontColor("#888");
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor("#888");
                    x.Span(" — Page ").FontSize(8).FontColor("#888");
                    x.CurrentPageNumber().FontSize(8).FontColor("#888");
                    x.Span("/").FontSize(8).FontColor("#888");
                    x.TotalPages().FontSize(8).FontColor("#888");
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenererRapportMensuel(List<Prelevement> prelevements, DateTime mois)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("💧 AquaLab — Rapport Mensuel")
                        .FontSize(20).Bold().FontColor("#0a7ea4");
                    col.Item().Text($"Période : {mois:MMMM yyyy}")
                        .FontSize(12).FontColor("#666");
                    col.Item().PaddingTop(4).BorderBottom(1).BorderColor("#ccc").Text("");
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    // Statistiques
                    col.Item().Text("STATISTIQUES DU MOIS").FontSize(9)
                        .Bold().FontColor("#0a7ea4").LetterSpacing(1);

                    var traites = prelevements.Count(p => p.Statut == StatutPrelevement.Traite);
                    var conformes = prelevements.Count(p => p.Traitement?.ConformeNormes == true);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        void StatBox(string label, string val, string color)
                        {
                            row.RelativeItem().Background(color).Padding(12).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor("#555");
                                c.Item().Text(val).FontSize(20).Bold().FontColor("#222");
                            });
                        }
                        StatBox("Prélèvements", prelevements.Count.ToString(), "#e8f4f8");
                        StatBox("Traités", traites.ToString(), "#e8f8f0");
                        StatBox("Conformes", conformes.ToString(), "#fff8e1");
                        StatBox("Taux conformité",
                            traites > 0 ? $"{conformes * 100 / traites}%" : "—", "#fce8e8");
                    });

                    col.Item().PaddingTop(14).Text("DÉTAIL DES PRÉLÈVEMENTS").FontSize(9)
                        .Bold().FontColor("#0a7ea4").LetterSpacing(1);

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "Date", "Site", "pH", "Turb.", "DCO", "Statut" })
                                h.Cell().Background("#0a7ea4").Padding(4)
                                    .Text(t).FontColor(Colors.White).Bold().FontSize(8);
                        });

                        bool alt = false;
                        foreach (var p in prelevements.OrderBy(x => x.DateHeure))
                        {
                            var bg = alt ? "#f9f9f9" : Colors.White;
                            alt = !alt;
                            table.Cell().Background(bg).Padding(4).Text(p.Id.ToString()).FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(p.DateHeure.ToString("dd/MM HH:mm")).FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(p.PointPrelevement?.Nom ?? "—").FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(p.pH?.ToString("F1") ?? "—").FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(p.Turbidite?.ToString("F0") ?? "—").FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(p.DCO?.ToString("F0") ?? "—").FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(p.Statut.ToString()).FontSize(8);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"AquaLab — Rapport mensuel généré le {DateTime.Now:dd/MM/yyyy} — Page ").FontSize(8).FontColor("#888");
                    x.CurrentPageNumber().FontSize(8);
                    x.Span("/").FontSize(8);
                    x.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf();
    }
}
