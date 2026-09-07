using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // Les pages de gestion de compte ne portent pas de limitation de débit :
    // on peut les exercer largement dans une seule session.
    public class GestionDuCompteTests : SessionConnectee
    {
        protected override string Courriel => FabriqueEscale.Loueuse;

        [Fact]
        public async Task ChangerLeMotDePassePuisLeRemettre()
        {
            //Etant donné un mot de passe courant
            const string nouveau = "Escale.Test.2027";

            //Lorsqu'on le change
            using HttpResponseMessage changement = await PosterAsync(
                "/Identity/Account/Manage/ChangePassword", "/Identity/Account/Manage/ChangePassword",
                ("Input.OldPassword", FabriqueEscale.MotDePasse),
                ("Input.NewPassword", nouveau),
                ("Input.ConfirmPassword", nouveau));

            //Alors la page revient avec son message d'état
            Assert.Equal(HttpStatusCode.Redirect, changement.StatusCode);

            //Lorsqu'on le remet
            using HttpResponseMessage retour = await PosterAsync(
                "/Identity/Account/Manage/ChangePassword", "/Identity/Account/Manage/ChangePassword",
                ("Input.OldPassword", nouveau),
                ("Input.NewPassword", FabriqueEscale.MotDePasse),
                ("Input.ConfirmPassword", FabriqueEscale.MotDePasse));

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, retour.StatusCode);
        }

        [Fact]
        public async Task UnMauvaisMotDePasseCourantEstRefuse()
        {
            //Etant donné une saisie erronée
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/ChangePassword", "/Identity/Account/Manage/ChangePassword",
                ("Input.OldPassword", "Mauvais.Mot.2026"),
                ("Input.NewPassword", "Autre.Mot.2026"),
                ("Input.ConfirmPassword", "Autre.Mot.2026"));

            //Alors la page revient sans avoir rien changé
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ModifierLeNumeroDeTelephoneDuProfil()
        {
            //Etant donné
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/Index", "/Identity/Account/Manage/Index",
                ("Input.PhoneNumber", "418-555-0142"));

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);

            string profil = await Client.GetStringAsync("/Identity/Account/Manage/Index");
            Assert.Contains("418-555-0142", profil);
        }

        [Fact]
        public async Task DemanderUnCourrielDeVerification()
        {
            //Etant donné une adresse non confirmée
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/Email?handler=SendVerificationEmail",
                "/Identity/Account/Manage/Email",
                ("Input.NewEmail", FabriqueEscale.Loueuse));

            //Alors la page revient avec son message d'état
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        }

        [Fact]
        public async Task TelechargerSesDonneesPersonnellesRendUnFichierJson()
        {
            //Etant donné le droit d'accès prévu par la loi 25
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/DownloadPersonalData",
                "/Identity/Account/Manage/PersonalData");

            //Alors le fichier est servi en pièce jointe
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Equal("application/json", reponse.Content.Headers.ContentType!.MediaType);
            Assert.Contains("NomComplet", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task LaPageDuVerificateurAffichLaCleEtSonAdresseOtpauth()
        {
            //Etant donné
            //Lorsque
            string html = await Client.GetStringAsync("/Identity/Account/Manage/EnableAuthenticator");

            //Alors la clé et l'adresse de configuration sont proposées
            Assert.Contains("otpauth://totp/Escale", html);
            Assert.Contains("qrCodeData", html);
        }

        [Fact]
        public async Task UnCodeDeVerificationFauxEstRefuse()
        {
            //Etant donné un code inventé
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/EnableAuthenticator",
                "/Identity/Account/Manage/EnableAuthenticator",
                ("Input.Code", "000000"));

            //Alors la double authentification n'est pas activée
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ReinitialiserLaCleDuVerificateur()
        {
            //Etant donné
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/ResetAuthenticator",
                "/Identity/Account/Manage/ResetAuthenticator");

            //Alors on repart sur la page de configuration
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("EnableAuthenticator", reponse.Headers.Location!.ToString());
        }

        [Fact]
        public async Task LesConnexionsExternesSAffichentSansFournisseur()
        {
            //Etant donné qu'aucun fournisseur n'est configuré
            //Lorsque
            using HttpResponseMessage reponse = await Client.GetAsync(
                "/Identity/Account/Manage/ExternalLogins");

            //Alors la page répond sans proposer de service
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        }

        [Fact]
        public async Task SeDeconnecterFermeLaSession()
        {
            //Etant donné une session ouverte
            using HttpResponseMessage deconnexion = await PosterAsync(
                "/Identity/Account/Logout?returnUrl=%2F", "/Identity/Account/Manage/Index");

            //Alors on est renvoyé et la session est close
            Assert.Equal(HttpStatusCode.Redirect, deconnexion.StatusCode);

            using HttpResponseMessage apres = await Client.GetAsync("/Tableau/Index");
            Assert.Equal(HttpStatusCode.Redirect, apres.StatusCode);
            Assert.Contains("Login", apres.Headers.Location!.ToString());
        }
    }
}
