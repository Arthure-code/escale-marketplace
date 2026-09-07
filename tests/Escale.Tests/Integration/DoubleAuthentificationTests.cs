using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Escale.Tests.Integration
{
    // Le parcours complet de la double authentification : configuration du
    // vérificateur avec un vrai code, codes de récupération, désactivation.
    public partial class DoubleAuthentificationTests : SessionConnectee
    {
        [GeneratedRegex("secret=([A-Z2-7]+)")]
        private static partial Regex Secret();

        protected override string Courriel => FabriqueEscale.Voyageur;

        private async Task<string> CleAsync()
        {
            string html = await Client.GetStringAsync("/Identity/Account/Manage/EnableAuthenticator");
            System.Text.RegularExpressions.Match trouve = Secret().Match(html);

            Assert.True(trouve.Success, "Aucune clé partagée sur la page du vérificateur");
            return trouve.Groups[1].Value;
        }

        [Fact]
        public async Task ActiverPuisDesactiverLaDoubleAuthentification()
        {
            //Etant donné la clé que l'application propose
            string cle = await CleAsync();

            //Lorsqu'on saisit le code que produit cette clé
            using HttpResponseMessage activation = await PosterAsync(
                "/Identity/Account/Manage/EnableAuthenticator",
                "/Identity/Account/Manage/EnableAuthenticator",
                ("Input.Code", CodeTotp.Calculer(cle)));

            //Alors les codes de récupération sont présentés
            Assert.Equal(HttpStatusCode.Redirect, activation.StatusCode);
            Assert.Contains("ShowRecoveryCodes", activation.Headers.Location!.ToString());

            string codes = await Client.GetStringAsync(activation.Headers.Location!.ToString());
            Assert.Contains("recovery-code", codes);

            //Et la page de la double authentification propose désormais de la couper
            string etat = await Client.GetStringAsync("/Identity/Account/Manage/TwoFactorAuthentication");
            Assert.Contains("Disable2fa", etat);

            //Lorsqu'on régénère les codes de récupération
            using HttpResponseMessage regeneration = await PosterAsync(
                "/Identity/Account/Manage/GenerateRecoveryCodes",
                "/Identity/Account/Manage/GenerateRecoveryCodes");
            Assert.Equal(HttpStatusCode.Redirect, regeneration.StatusCode);

            //Lorsqu'on désactive
            using HttpResponseMessage desactivation = await PosterAsync(
                "/Identity/Account/Manage/Disable2fa",
                "/Identity/Account/Manage/Disable2fa");

            //Alors la page ne propose plus de couper. La clé, elle, reste en
            //place : la désactivation ne la touche pas, c'est ce que la page
            //de désactivation annonce elle-même.
            Assert.Equal(HttpStatusCode.Redirect, desactivation.StatusCode);
            string apres = await Client.GetStringAsync("/Identity/Account/Manage/TwoFactorAuthentication");
            Assert.DoesNotContain("Disable2fa", apres);
            Assert.Contains("ResetAuthenticator", apres);
        }

        [Fact]
        public async Task OublierCeNavigateurQuandLaMachineEstMemorisee()
        {
            //Etant donné la page de la double authentification
            //Lorsqu'on demande d'oublier le navigateur
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/TwoFactorAuthentication",
                "/Identity/Account/Manage/TwoFactorAuthentication");

            //Alors la page répond sans erreur
            Assert.True(reponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
        }

        [Fact]
        public async Task UnCodeDUneTrancheTropAncienneEstRefuse()
        {
            //Etant donné un code calculé pour une tranche largement dépassée
            string cle = await CleAsync();

            //Lorsque
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/EnableAuthenticator",
                "/Identity/Account/Manage/EnableAuthenticator",
                ("Input.Code", CodeTotp.Calculer(cle, decalageDeTranche: -50)));

            //Alors la fenêtre de tolérance ne le laisse pas passer
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }
    }
}
