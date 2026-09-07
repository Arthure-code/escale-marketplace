using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // Ce qui touche à la fin de vie d'un compte et aux pages de confirmation
    // d'inscription, sur un compte créé pour l'occasion.
    public class FinDeCompteTests : SessionHttp
    {
        private readonly string _courriel =
            "adieu." + Guid.NewGuid().ToString("N")[..8] + "@escale.test";

        protected override async Task ApresOuvertureAsync()
        {
            using HttpResponseMessage inscription = await PosterAsync(
                "/Identity/Account/Register", "/Identity/Account/Register",
                ("Input.Role", "Voyageur"),
                ("Input.NomComplet", "Compte Jetable"),
                ("Input.Email", _courriel),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.ConfirmPassword", FabriqueEscale.MotDePasse));

            Assert.Equal(HttpStatusCode.Redirect, inscription.StatusCode);
        }

        [Fact]
        public async Task ChangerSonAdresseDeCourriel()
        {
            //Etant donné un compte connecté
            string nouvelle = "change." + Guid.NewGuid().ToString("N")[..8] + "@escale.test";

            //Lorsqu'il demande le changement
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/Email?handler=ChangeEmail",
                "/Identity/Account/Manage/Email",
                ("Input.NewEmail", nouvelle));

            //Alors un lien de confirmation est envoyé, l'adresse n'est pas
            //changée tant qu'il n'est pas suivi
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);

            string page = await Client.GetStringAsync("/Identity/Account/Manage/Email");
            Assert.Contains(_courriel, page);
        }

        [Fact]
        public async Task DemanderLeChangementVersLaMemeAdresseNeChangeRien()
        {
            //Etant donné la même adresse
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/Email?handler=ChangeEmail",
                "/Identity/Account/Manage/Email",
                ("Input.NewEmail", _courriel));

            //Alors la page le dit sans rien envoyer
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        }

        [Fact]
        public async Task UneAdresseMalFormeeEstRefusee()
        {
            //Etant donné une saisie qui n'est pas une adresse
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/Email?handler=ChangeEmail",
                "/Identity/Account/Manage/Email",
                ("Input.NewEmail", "pas-une-adresse"));

            //Alors la page revient avec l'erreur, sans rien envoyer
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task LaPageDeConfirmationDInscriptionRepond()
        {
            //Etant donné l'adresse d'un compte existant
            //Lorsque
            using HttpResponseMessage reponse = await Client.GetAsync(
                "/Identity/Account/RegisterConfirmation?email=" + Uri.EscapeDataString(_courriel));

            //Alors la page s'affiche, avec le lien de secours faute de SMTP
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("confirm", await reponse.Content.ReadAsStringAsync(),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LaConfirmationDInscriptionRefuseUneAdresseInconnue()
        {
            //Etant donné
            using HttpResponseMessage reponse = await Client.GetAsync(
                "/Identity/Account/RegisterConfirmation?email=personne@escale.test");

            //Alors
            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }

        [Fact]
        public async Task SupprimerSonCompteEtSesDonnees()
        {
            //Etant donné le droit à l'effacement
            using HttpResponseMessage suppression = await PosterAsync(
                "/Identity/Account/Manage/DeletePersonalData",
                "/Identity/Account/Manage/DeletePersonalData",
                ("Input.Password", FabriqueEscale.MotDePasse));

            //Alors le compte est effacé et la session close
            Assert.Equal(HttpStatusCode.Redirect, suppression.StatusCode);

            using HttpResponseMessage apres = await Client.GetAsync("/Identity/Account/Manage/Index");
            Assert.Equal(HttpStatusCode.Redirect, apres.StatusCode);
            Assert.Contains("Login", apres.Headers.Location!.ToString());
        }
    }
}
