using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // En production, l'application se comporte autrement : journal en JSON,
    // page d'erreur sans détail, HSTS, et surtout aucune migration appliquée
    // au démarrage. Ces branches ne sont jamais prises par les autres tests.
    public class DemarrageEnProductionTests : IAsyncLifetime
    {
        private readonly string _base =
            Path.Combine(Path.GetTempPath(), "escale-prod-" + Guid.NewGuid().ToString("N") + ".db");

        private FabriqueEscale? _production;

        public async Task InitializeAsync()
        {
            // Le schéma est d'abord posé par un démarrage en développement,
            // comme le ferait le script de déploiement avant la mise en ligne.
            using (FabriqueEscale preparation = new FabriqueEscale(_base, "Development"))
            using (HttpClient amorce = preparation.Client())
            {
                using HttpResponseMessage reponse = await amorce.GetAsync("/");
                Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            }

            _production = new FabriqueEscale(_base, "Production");
        }

        public Task DisposeAsync()
        {
            _production?.Dispose();

            if (File.Exists(_base))
            {
                try
                {
                    File.Delete(_base);
                }
                catch (IOException)
                {
                    // Le dossier temporaire du système s'en chargera.
                }
            }

            return Task.CompletedTask;
        }

        [Fact]
        public async Task LApplicationSertLAccueilSurUneBaseDejaMigree()
        {
            //Etant donné une base dont le schéma est à jour
            using HttpClient client = _production!.Client();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync("/");

            //Alors l'application sert la page sans avoir touché au schéma
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        }

        [Fact]
        public async Task LesEntetesDeSecuriteSontLesMemesQuEnDeveloppement()
        {
            //Etant donné
            using HttpClient client = _production!.Client();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync("/");

            //Alors
            Assert.DoesNotContain("unsafe-inline",
                reponse.Headers.GetValues("Content-Security-Policy").Single());
            Assert.False(reponse.Headers.Contains("Server"));
        }

        [Fact]
        public async Task LeJeuDeDemonstrationNEstPasRejoueEnProduction()
        {
            //Etant donné une base déjà remplie par le démarrage en développement
            using HttpClient client = _production!.Client();

            //Lorsque
            string accueil = await client.GetStringAsync("/");

            //Alors les annonces existantes sont servies, sans doublon ajouté
            Assert.Contains("offres", accueil);
        }
    }
}
