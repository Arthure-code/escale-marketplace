using Escale.Web.Entites;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Escale.Web.Repositories
{
    public class ContexteEscale : IdentityDbContext<Utilisateur>
    {
        public ContexteEscale(DbContextOptions<ContexteEscale> options) : base(options)
        {
        }

        public DbSet<Annonce> Annonces => Set<Annonce>();

        public DbSet<Equipement> Equipements => Set<Equipement>();

        public DbSet<AnnonceEquipement> AnnonceEquipements => Set<AnnonceEquipement>();

        public DbSet<LignePanier> LignesPanier => Set<LignePanier>();

        public DbSet<Commande> Commandes => Set<Commande>();

        public DbSet<LigneCommande> LignesCommande => Set<LigneCommande>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AnnonceEquipement>().HasKey(ae => new { ae.AnnonceId, ae.EquipementId });

            builder.Entity<Annonce>()
                .HasOne(a => a.Loueur)
                .WithMany()
                .HasForeignKey(a => a.LoueurId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LignePanier>()
                .HasOne(l => l.Annonce)
                .WithMany()
                .HasForeignKey(l => l.AnnonceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Une ligne de commande garde ses valeurs même si l'annonce disparaît.
            builder.Entity<LigneCommande>()
                .HasOne(l => l.Annonce)
                .WithMany()
                .HasForeignKey(l => l.AnnonceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Commande>().HasIndex(c => c.Reference).IsUnique();

            builder.Entity<Equipement>().HasIndex(e => e.Code).IsUnique();

            // Abonnement et blocage restent des colonnes de la table des
            // comptes : deux objets dans le code, une seule ligne en base.
            builder.Entity<Utilisateur>().OwnsOne(u => u.Abonnement);
            builder.Entity<Utilisateur>().OwnsOne(u => u.Blocage);
        }
    }
}
