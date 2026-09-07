using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Escale.Tests.Integration
{
    // Les refus de connexion, dans leur propre hôte : la page est limitée à
    // huit requêtes par minute et chaque tentative en consomme deux.
    public class ConnexionRefuseeTests : SessionHttp
    {
        [Fact]
        public async Task UnMauvaisMotDePasseNeDitPasSiLeCompteExiste()
        {
            //Etant donné une adresse connue et un mot de passe faux
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Login", "/Identity/Account/Login",
                ("Input.Email", FabriqueEscale.Voyageur),
                ("Input.Password", "Mauvais.Mot.2026"),
                ("Input.RememberMe", "false"));

            //Alors le message est le même que pour un compte inexistant
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            string html = await reponse.Content.ReadAsStringAsync();
            Assert.Contains("validation-summary-errors", html);
            Assert.DoesNotContain("existe pas", html);
        }

        [Fact]
        public async Task UnCompteBloqueNePeutPlusSeConnecter()
        {
            //Etant donné un compte bloqué par l'administration
            using (IServiceScope portee = Fabrique.Services.CreateScope())
            {
                IUtilisateurService comptes =
                    portee.ServiceProvider.GetRequiredService<IUtilisateurService>();
                IUtilisateurRepository depot =
                    portee.ServiceProvider.GetRequiredService<IUtilisateurRepository>();

                Utilisateur compte = (await depot.ObtenirParCourrielAsync(FabriqueEscale.Loueuse))!;
                ResultatAjout blocage = await comptes.BloquerAsync(
                    compte.Id, DateTime.Today, DateTime.Today.AddDays(7), "Test d'intégration");
                Assert.True(blocage.Reussi);
            }

            //Lorsqu'il tente de se connecter
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Login", "/Identity/Account/Login",
                ("Input.Email", FabriqueEscale.Loueuse),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.RememberMe", "false"));

            //Alors la connexion est refusée avant même la vérification du mot de passe
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("validation-summary-errors", await reponse.Content.ReadAsStringAsync());
        }
    }
}
