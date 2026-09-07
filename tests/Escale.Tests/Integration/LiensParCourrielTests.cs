using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // Les deux formulaires qui déclenchent un envoi de courriel sans être
    // connecté. Ils portent la limitation de débit, d'où une classe à part :
    // chaque classe démarre son propre hôte, donc son propre compteur.
    public class LiensParCourrielTests : SessionHttp
    {
        [Fact]
        public async Task DemanderUnLienDeReinitialisationPourUneAdresseConnue()
        {
            //Etant donné une adresse confirmée
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/ForgotPassword", "/Identity/Account/ForgotPassword",
                ("Input.Email", FabriqueEscale.Administrateur));

            //Alors le lien est produit et la page de confirmation annoncée
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("ForgotPasswordConfirmation", reponse.Headers.Location!.ToString());
        }

        [Fact]
        public async Task RenvoyerLaConfirmationPourUneAdresseConnue()
        {
            //Etant donné un compte dont l'adresse n'est pas encore confirmée
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/ResendEmailConfirmation",
                "/Identity/Account/ResendEmailConfirmation",
                ("Input.Email", FabriqueEscale.Voyageur));

            //Alors la même réponse qu'avec une adresse inconnue est rendue
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("rification", await reponse.Content.ReadAsStringAsync());
        }
    }
}
