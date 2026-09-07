using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    public class PagesPubliquesTests : IClassFixture<FabriqueEscale>
    {
        private readonly FabriqueEscale _fabrique;

        public PagesPubliquesTests(FabriqueEscale fabrique)
        {
            _fabrique = fabrique;
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/?categorie=Chambre")]
        [InlineData("/?categorie=Voiture")]
        [InlineData("/Annonces/Details/1")]
        [InlineData("/Identity/Account/Login")]
        [InlineData("/Identity/Account/Register")]
        [InlineData("/Identity/Account/ForgotPassword")]
        [InlineData("/Identity/Account/ResendEmailConfirmation")]
        [InlineData("/Identity/Account/Lockout")]
        [InlineData("/Identity/Account/AccessDenied")]
        [InlineData("/Identity/Account/ForgotPasswordConfirmation")]
        [InlineData("/Identity/Account/ResetPasswordConfirmation")]
        [InlineData("/health")]
        public async Task LesPagesPubliquesRepondent(string adresse)
        {
            //Etant donné une application démarrée sur une base neuve
            using HttpClient client = _fabrique.Client();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync(adresse);

            //Alors
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        }

        [Fact]
        public async Task LAccueilAfficheLesAnnoncesSemees()
        {
            //Etant donné
            using HttpClient client = _fabrique.Client();

            //Lorsque
            string html = await client.GetStringAsync("/");

            //Alors le jeu de démonstration est bien en base et rendu par la vue
            Assert.Contains("offres", html);
            Assert.Contains("Chambre", html);
            Assert.Contains("Voiture", html);
        }

        [Fact]
        public async Task ChaqueReponsePorteLesEntetesDeSecurite()
        {
            //Etant donné
            using HttpClient client = _fabrique.Client();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync("/");

            //Alors
            string csp = reponse.Headers.GetValues("Content-Security-Policy").Single();
            Assert.DoesNotContain("unsafe-inline", csp);
            Assert.DoesNotContain("unsafe-eval", csp);
            Assert.Equal("nosniff", reponse.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", reponse.Headers.GetValues("X-Frame-Options").Single());
            Assert.False(reponse.Headers.Contains("Server"));
        }

        [Fact]
        public async Task UneAdresseInconnueRendLaPageDeCodeStatut()
        {
            //Etant donné
            using HttpClient client = _fabrique.ClientQuiSuit();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync("/page-qui-nexiste-pas");

            //Alors le code d'origine est conservé et la page maison est servie
            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
            Assert.Contains("404", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UneAnnonceInconnueRendUnQuatreCentQuatre()
        {
            //Etant donné
            using HttpClient client = _fabrique.Client();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync("/Annonces/Details/9999");

            //Alors
            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }

        [Theory]
        [InlineData("/Panier/Index")]
        [InlineData("/Favoris/Index")]
        [InlineData("/Commandes/Paiement")]
        [InlineData("/Tableau/Index")]
        [InlineData("/Administration/Index")]
        [InlineData("/Identity/Account/Manage/Index")]
        public async Task LesPagesProtegeesRenvoientVersLaConnexion(string adresse)
        {
            //Etant donné un visiteur non connecté
            using HttpClient client = _fabrique.Client();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync(adresse);

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("/Identity/Account/Login", reponse.Headers.Location!.ToString());
        }

        [Fact]
        public async Task LaFeuilleDeStyleEstServieAvecUnCacheLong()
        {
            //Etant donné
            using HttpClient client = _fabrique.Client();

            //Lorsque
            using HttpResponseMessage reponse = await client.GetAsync("/css/site.css");

            //Alors
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("immutable", reponse.Headers.CacheControl!.ToString());
        }
    }
}
