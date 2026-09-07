using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Escale.Tests.Integration
{
    // Démarre l'application entière sur une base SQLite jetable. Ces tests
    // traversent le composeur, les dépôts, les modèles de page et les vues,
    // c'est-à-dire tout ce qu'une doublure ne peut pas atteindre.
    public class FabriqueEscale : WebApplicationFactory<Program>
    {
        public const string MotDePasse = "Escale.Test.2026";
        public const string Voyageur = "camille@escale.test";
        public const string Loueuse = "marie@escale.test";
        public const string Administrateur = "admin@escale.test";

        private readonly string _base;
        private readonly string _environnement;
        private readonly bool _sienne;

        public FabriqueEscale() : this(null, "Development")
        {
        }

        // Interne et non publique : xUnit n'accepte qu'un seul constructeur
        // public sur une fixture de classe. Une base fournie de l'extérieur
        // n'est pas supprimée à la fin ; elle appartient à l'appelant, qui
        // peut vouloir la rouvrir avec un autre environnement.
        internal FabriqueEscale(string? base_, string environnement)
        {
            _sienne = base_ is null;
            _base = base_ ?? Path.Combine(Path.GetTempPath(),
                "escale-test-" + Guid.NewGuid().ToString("N") + ".db");
            _environnement = environnement;
        }

        public string CheminDeLaBase => _base;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // L'environnement de développement est celui qui applique les
            // migrations et remplit le jeu de démonstration au démarrage : les
            // tests disposent donc de vraies annonces et de vrais comptes.
            builder.UseEnvironment(_environnement);

            builder.UseSetting("ConnectionStrings:Escale", "Data Source=" + _base);
            builder.UseSetting("Semences:MotDePasse", MotDePasse);
            builder.UseSetting("Administrateur:Courriel", Administrateur);
            builder.UseSetting("Administrateur:MotDePasse", MotDePasse);
            builder.UseSetting("Administrateur:NomComplet", "Administration");
        }

        // Le témoin d'authentification est marqué Secure : sur une adresse en
        // http le client le jetterait sans rien dire, et toute connexion
        // échouerait sans message. Le serveur de test ne chiffre rien, seul le
        // schéma de l'adresse compte.
        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        public HttpClient ClientQuiSuit() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = true
        });

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && _sienne && File.Exists(_base))
            {
                try
                {
                    File.Delete(_base);
                }
                catch (IOException)
                {
                    // La base reste ouverte par un pool de connexions : le
                    // dossier temporaire du système s'en chargera.
                }
            }
        }
    }
}
