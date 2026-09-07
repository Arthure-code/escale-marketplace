using System;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Escale.Web.Repositories
{
    /// <summary>
    /// Ce que l'application prépare au démarrage. Les rôles et le compte
    /// d'administration existent dans tous les environnements ; le jeu de
    /// démonstration n'existe qu'en développement.
    /// </summary>
    public static class Amorcage
    {
        public static async Task PreparerAsync(IServiceProvider services,
            IWebHostEnvironment environnement, IConfiguration configuration)
        {
            ILogger journal = services.GetRequiredService<ILoggerFactory>().CreateLogger("Escale.Amorcage");
            ContexteEscale contexte = services.GetRequiredService<ContexteEscale>();
            RoleManager<IdentityRole> roles = services.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<Utilisateur> comptes = services.GetRequiredService<UserManager<Utilisateur>>();

            // Le schéma ne se modifie tout seul qu'en développement. En
            // production, on génère un script idempotent que l'on fait relire
            // avant de l'appliquer, ce que recommande Microsoft.
            if (environnement.IsDevelopment())
            {
                await contexte.Database.MigrateAsync();
            }
            else if ((await contexte.Database.GetPendingMigrationsAsync()).Any())
            {
                // On ne modifie pas une base de production au démarrage. Tant
                // que le script n'est pas passé, rien d'autre ne peut être
                // préparé : on le dit et on s'arrête là plutôt que d'échouer
                // sur une table absente.
                journal.LogError(
                    "Migrations en attente. Appliquez le script idempotent avant de servir du trafic.");
                return;
            }

            foreach (string role in new[] { Utilisateur.RoleVoyageur, Utilisateur.RoleLoueur, Utilisateur.RoleAdministrateur })
            {
                if (!await roles.RoleExistsAsync(role))
                {
                    await roles.CreateAsync(new IdentityRole(role));
                }
            }

            await CreerAdministrateurAsync(comptes, configuration, journal);

            if (environnement.IsDevelopment())
            {
                await Semences.RemplirAsync(services, configuration);
            }
        }

        // Le compte d'administration vient de la configuration, jamais du
        // code : user-secrets en local, variables d'application ou Key Vault
        // en production. Sans configuration, aucun compte n'est créé.
        private static async Task CreerAdministrateurAsync(UserManager<Utilisateur> comptes,
            IConfiguration configuration, ILogger journal)
        {
            string? courriel = configuration["Administrateur:Courriel"];
            string? motDePasse = configuration["Administrateur:MotDePasse"];
            string nom = configuration["Administrateur:NomComplet"] ?? "Administration";

            if (string.IsNullOrWhiteSpace(courriel) || string.IsNullOrWhiteSpace(motDePasse))
            {
                journal.LogInformation("Aucun compte d'administration configuré, aucun compte créé.");
                return;
            }

            if (await comptes.FindByEmailAsync(courriel) is not null)
            {
                return;
            }

            Utilisateur administrateur = new Utilisateur
            {
                UserName = courriel,
                Email = courriel,
                EmailConfirmed = true,
                NomComplet = nom
            };

            IdentityResult resultat = await comptes.CreateAsync(administrateur, motDePasse);

            if (!resultat.Succeeded)
            {
                journal.LogError("Création du compte d'administration refusée : {Motifs}",
                    string.Join(", ", resultat.Errors.Select(e => e.Code)));
                return;
            }

            await comptes.AddToRoleAsync(administrateur, Utilisateur.RoleAdministrateur);

            journal.LogInformation(JournalDeSecurite.CompteCree,
                "Compte d'administration créé, identifiant {UtilisateurId}", administrateur.Id);
        }
    }
}
