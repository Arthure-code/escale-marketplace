using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // Le second facteur refusé. Classe à part de celle du cas passant : la
    // page de connexion porte la limitation de débit, et chaque classe démarre
    // son propre hôte, donc son propre compteur.
    public class ConnexionADeuxFacteursRefuseeTests : SessionAvecDeuxFacteurs
    {
        [Fact]
        public async Task UnCodeErroneNOuvrePasLaSession()
        {
            //Etant donné un compte dont la double authentification est active
            string demande = await DemanderLeSecondFacteurAsync();

            //Lorsqu'un code qui n'est pas celui du vérificateur est fourni
            using HttpResponseMessage validation = await PosterAsync(
                demande, demande,
                ("Input.TwoFactorCode", "000000"),
                ("Input.RememberMachine", "false"));

            //Alors la page revient avec son motif
            Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
            Assert.Contains("validation-summary-errors",
                await validation.Content.ReadAsStringAsync());

            //Et la session reste fermée
            using HttpResponseMessage apres = await Client.GetAsync("/Panier/Index");
            Assert.Equal(HttpStatusCode.Redirect, apres.StatusCode);
        }
    }
}
