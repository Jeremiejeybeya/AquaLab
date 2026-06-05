using AquaLab.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AquaLab.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private EmailConfig GetConfig() => new(
        Host:     _config["Email:Host"]     ?? "",
        Port:     int.Parse(_config["Email:Port"] ?? "587"),
        Username: _config["Email:Username"] ?? "",
        Password: _config["Email:Password"] ?? "",
        From:     _config["Email:From"]     ?? "aqualab@ong.org",
        AdminTo:  _config["Email:AdminTo"]  ?? ""
    );

    public async Task EnvoyerAlerteAsync(Alerte alerte, string destinataire)
    {
        var cfg = GetConfig();
        if (string.IsNullOrEmpty(cfg.Host) || string.IsNullOrEmpty(cfg.Username))
        {
            _logger.LogWarning("Email non configuré — alerte non envoyée par email");
            return;
        }

        var couleur = alerte.Niveau == NiveauAlerte.Critique ? "#dc3545" :
                      alerte.Niveau == NiveauAlerte.Avertissement ? "#fd7e14" : "#0a7ea4";
        var emoji   = alerte.Niveau == NiveauAlerte.Critique ? "🔴" :
                      alerte.Niveau == NiveauAlerte.Avertissement ? "⚠️" : "ℹ️";

        var html = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <div style='background: {couleur}; padding: 20px; border-radius: 8px 8px 0 0;'>
    <h1 style='color: white; margin: 0; font-size: 20px;'>{emoji} Alerte AquaLab — {alerte.Niveau}</h1>
  </div>
  <div style='background: #f8f9fa; padding: 20px; border-radius: 0 0 8px 8px; border: 1px solid #dee2e6;'>
    <h2 style='color: #333; font-size: 16px;'>{alerte.Titre}</h2>
    <p style='color: #666;'>{alerte.Message}</p>
    <table style='width: 100%; border-collapse: collapse; margin-top: 16px;'>
      <tr><td style='padding: 8px; background: #fff; border: 1px solid #dee2e6; font-weight: bold;'>Paramètre</td>
          <td style='padding: 8px; background: #fff; border: 1px solid #dee2e6;'>{alerte.Parametre}</td></tr>
      <tr><td style='padding: 8px; background: #f8f9fa; border: 1px solid #dee2e6; font-weight: bold;'>Valeur mesurée</td>
          <td style='padding: 8px; background: #f8f9fa; border: 1px solid #dee2e6; color: {couleur}; font-weight: bold;'>{alerte.ValeurMesuree?.ToString("F2") ?? "—"}</td></tr>
      {(alerte.SeuilMaximum.HasValue ? $"<tr><td style='padding:8px;background:#fff;border:1px solid #dee2e6;font-weight:bold;'>Seuil maximum</td><td style='padding:8px;background:#fff;border:1px solid #dee2e6;'>{alerte.SeuilMaximum}</td></tr>" : "")}
      <tr><td style='padding: 8px; background: #f8f9fa; border: 1px solid #dee2e6; font-weight: bold;'>Prélèvement</td>
          <td style='padding: 8px; background: #f8f9fa; border: 1px solid #dee2e6;'>#{alerte.PrelevementId}</td></tr>
      <tr><td style='padding: 8px; background: #fff; border: 1px solid #dee2e6; font-weight: bold;'>Date/Heure</td>
          <td style='padding: 8px; background: #fff; border: 1px solid #dee2e6;'>{alerte.DateCreation:dd/MM/yyyy HH:mm}</td></tr>
    </table>
    <div style='margin-top: 20px; padding: 12px; background: #fff3cd; border-radius: 6px;'>
      <strong>Action requise :</strong> Connectez-vous à AquaLab pour traiter cette alerte.
    </div>
    <p style='color: #999; font-size: 12px; margin-top: 20px;'>
      — AquaLab, système de gestion de laboratoire eau usée
    </p>
  </div>
</body>
</html>";

        await EnvoyerEmailAsync(destinataire, $"[AquaLab] {emoji} {alerte.Niveau} — {alerte.Titre}", html, cfg);
    }

    public async Task EnvoyerRapportPdfAsync(string destinataire, byte[] pdf, string nomFichier, string mois)
    {
        var cfg = GetConfig();
        if (string.IsNullOrEmpty(cfg.Host)) return;

        var html = $@"
<html><body style='font-family:Arial;max-width:600px;margin:0 auto;'>
  <div style='background:#0a7ea4;padding:20px;border-radius:8px 8px 0 0;'>
    <h1 style='color:white;margin:0;font-size:18px;'>💧 AquaLab — Rapport mensuel {mois}</h1>
  </div>
  <div style='background:#f8f9fa;padding:20px;border:1px solid #dee2e6;border-radius:0 0 8px 8px;'>
    <p>Bonjour,</p>
    <p>Veuillez trouver ci-joint le rapport mensuel du laboratoire AquaLab pour la période <strong>{mois}</strong>.</p>
    <p style='color:#999;font-size:12px;'>— AquaLab</p>
  </div>
</body></html>";

        await EnvoyerEmailAvecPjAsync(destinataire, $"[AquaLab] Rapport mensuel {mois}", html, pdf, nomFichier, cfg);
    }

    // ── Méthodes privées ──────────────────────────────────────────────────
    private async Task EnvoyerEmailAsync(string to, string subject, string html, EmailConfig cfg)
    {
        try
        {
            var message = BuildMessage(to, subject, html, cfg.From);
            await SendAsync(message, cfg);
            _logger.LogInformation("Email envoyé à {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi email à {To}", to);
        }
    }

    private async Task EnvoyerEmailAvecPjAsync(string to, string subject, string html, byte[] attachment, string fileName, EmailConfig cfg)
    {
        try
        {
            var message = BuildMessage(to, subject, html, cfg.From);
            var builder = new BodyBuilder { HtmlBody = html };
            builder.Attachments.Add(fileName, attachment, new ContentType("application", "pdf"));
            message.Body = builder.ToMessageBody();
            await SendAsync(message, cfg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi email avec PJ à {To}", to);
        }
    }

    private static MimeMessage BuildMessage(string to, string subject, string html, string from)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(from));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = html };
        return msg;
    }

    private static async Task SendAsync(MimeMessage message, EmailConfig cfg)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(cfg.Host, cfg.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(cfg.Username, cfg.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

record EmailConfig(string Host, int Port, string Username, string Password, string From, string AdminTo);
