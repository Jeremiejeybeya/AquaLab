using Microsoft.EntityFrameworkCore;
using AquaLab.Models;

namespace AquaLab.Data;

public class AquaLabContext : DbContext
{
    public AquaLabContext(DbContextOptions<AquaLabContext> options) : base(options) { }

    public DbSet<Technicien> Techniciens => Set<Technicien>();
    public DbSet<PointPrelevement> PointsPrelevement => Set<PointPrelevement>();
    public DbSet<Prelevement> Prelevements => Set<Prelevement>();
    public DbSet<Traitement> Traitements => Set<Traitement>();
    public DbSet<Alerte> Alertes => Set<Alerte>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique relation Prelevement <-> Traitement
        modelBuilder.Entity<Traitement>()
            .HasOne(t => t.Prelevement)
            .WithOne(p => p.Traitement)
            .HasForeignKey<Traitement>(t => t.PrelevementId);

        // Seed data
        modelBuilder.Entity<Technicien>().HasData(
            new Technicien { Id = 1, Nom = "Dubois", Prenom = "Marie", Email = "m.dubois@aquaong.org", Role = "Chef de Labo", Actif = true, DateCreation = new DateTime(2024, 1, 1) },
            new Technicien { Id = 2, Nom = "Koné", Prenom = "Ibrahima", Email = "i.kone@aquaong.org", Role = "Technicien", Actif = true, DateCreation = new DateTime(2024, 1, 1) },
            new Technicien { Id = 3, Nom = "Traoré", Prenom = "Aminata", Email = "a.traore@aquaong.org", Role = "Technicien", Actif = true, DateCreation = new DateTime(2024, 1, 1) }
        );

        modelBuilder.Entity<PointPrelevement>().HasData(
            new PointPrelevement { Id = 1, Nom = "Entrée Station", Description = "Eau brute arrivant à la station", Localisation = "Bassin A - Entrée", Actif = true },
            new PointPrelevement { Id = 2, Nom = "Décanteur Primaire", Description = "Après décantation primaire", Localisation = "Bassin B", Actif = true },
            new PointPrelevement { Id = 3, Nom = "Bassin Biologique", Description = "Zone de traitement biologique", Localisation = "Bassin C", Actif = true },
            new PointPrelevement { Id = 4, Nom = "Sortie Station", Description = "Eau traitée avant rejet", Localisation = "Point de rejet - Rivière", Actif = true }
        );
    }
}
