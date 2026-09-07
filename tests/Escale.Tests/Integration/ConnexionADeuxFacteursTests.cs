using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    public class ConnexionADeuxFacteursTests : SessionAvecDeuxFacteurs
    {
        [Fact]
        public async Task LeMotDePasseSeulNeSuffitPlusEtLeCodeOuvreLaSession()
        {
            //Etant donné un compte dont la double authentification est active
            string demande = await DemanderLeSecondFacteurAsync();

            //Alors la session n'est pas encore ouverte
            using HttpResponseMessage avant = await Client.GetAsync("/Panier/Index");
            Assert.Equal(HttpStatusCode.Redirect, avant.StatusCode);

            //Lorsque le code du vérificateur est fourni
            using HttpResponseMessage validation = await PosterAsync(
                demande, demande,
                ("Input.TwoFactorCode", CodeTotp.Calculer(Cle)),
                ("Input.RememberMachine", "false"));

            //Alors la session s'ouvre
            Assert.Equal(HttpStatusCode.Redirect, validation.StatusCode);

            using HttpResponseMessage apres = await Client.GetAsync("/Panier/Index");
            Assert.Equal(HttpStatusCode.OK, apres.StatusCode);
        }
    }
}
