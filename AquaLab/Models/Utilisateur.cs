using System.ComponentModel.DataAnnotations;

namespace AquaLab.Models;

public class Utilisateur
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = "";

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = "";  // BCrypt hash

    [Required, MaxLength(100)]
    public string NomComplet { get; set; } = "";

    [MaxLength(150)]
    public string? Email { get; set; }

    // Roles : Admin | ChefLabo | Technicien
    [Required, MaxLength(30)]
    public string Role { get; set; } = "Technicien";

    public bool Actif { get; set; } = true;

    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DerniereConnexion { get; set; }
}
