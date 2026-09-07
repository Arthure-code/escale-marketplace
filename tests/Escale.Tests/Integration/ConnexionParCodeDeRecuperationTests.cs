using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    public class ConnexionParCodeDeRecuperationTests : SessionAvecDeuxFacteurs
    {
        [Fact]
        public async Task UnCodeDeRecuperationRemplaceLeVerificateurPerdu()
        {
            //Etant donné un compte protégé dont l'appareil est perdu
            await DemanderLeSecondFacteurAsync();

            //Lorsque le visiteur passe par les codes de récupération
            using HttpResponseMessage validation = await PosterAsync(
                "/Identity/Account/LoginWithRecoveryCode",
                "/Identity/Account/LoginWithRecoveryCode",
                ("Input.RecoveryCode", Codes[0]));

            //Alors la session s'ouvre
            Assert.Equal(HttpStatusCode.Redirect, validation.StatusCode);

            using HttpResponseMessage apres = await Client.GetAsync("/Panier/Index");
            Assert.Equal(HttpStatusCode.OK, apres.StatusCode);
        }

        [Fact]
        public async Task UnCodeDeRecuperationInventeEstRefuse()
        {
            //Etant donné
            await DemanderLeSecondFacteurAsync();

            //Lorsque
            using HttpResponseMessage refus = await PosterAsync(
                "/Identity/Account/LoginWithRecoveryCode",
                "/Identity/Account/LoginWithRecoveryCode",
                ("Input.RecoveryCode", "aaaaa-bbbbb"));

            //Alors la page revient sans ouvrir de session
            Assert.Equal(HttpStatusCode.OK, refus.StatusCode);
            Assert.Contains("validation-summary-errors", await refus.Content.ReadAsStringAsync());
        }
    }
}
