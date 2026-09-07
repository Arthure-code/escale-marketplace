using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Escale.Tests.Integration
{
    // Le témoin d'authentification reste valide un moment après la disparition
    // du compte qu'il désigne : Identity ne revalide l'empreinte de sécurité
    // que toutes les trente minutes. Toutes les pages de gestion doivent donc
    // se défendre elles-mêmes, et c'est ce que vérifie cette classe.
    public class CompteDisparuTests : SessionHttp
    {
        private readonly string _courriel =
            "disparu." + Guid.NewGuid().ToString("N")[..8] + "@escale.test";

        private string _jeton = string.Empty;

        protected override async Task ApresOuvertureAsync()
        {
            // L'inscription ouvre la session dans la foulée.
            using HttpResponseMessage inscription = await PosterAsync(
                "/Identity/Account/Register", "/Identity/Account/Register",
                ("Input.Role", "Voyageur"),
                ("Input.NomComplet", "Compte Disparu"),
                ("Input.Email", _courriel),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.ConfirmPassword", FabriqueEscale.MotDePasse));

            Assert.Equal(HttpStatusCode.Redirect, inscription.StatusCode);

            // Le jeton antifalsification est pris tant que les formulaires
            // répondent encore : après l'effacement, plus aucune page n'en
            // sert. Il reste valable, il est lié au nom porté par le témoin.
            _jeton = await JetonDeAsync("/Identity/Account/Manage/Index");

            using IServiceScope portee = Fabrique.Services.CreateScope();
            UserManager<Utilisateur> comptes =
                portee.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();

            Utilisateur compte = (await comptes.FindByEmailAsync(_courriel))!;
            Assert.True((await comptes.DeleteAsync(compte)).Succeeded);
        }

        private Task<HttpResponseMessage> PosterAvecLeJetonAsync(string adresse,
            params (string Cle, string Valeur)[] champs)
        {
            List<KeyValuePair<string, string>> corps = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", _jeton)
            };

            foreach ((string cle, string valeur) in champs)
            {
                corps.Add(new KeyValuePair<string, string>(cle, valeur));
            }

            return Client.PostAsync(adresse, new FormUrlEncodedContent(corps));
        }

        [Theory]
        [InlineData("/Identity/Account/Manage/Index")]
        [InlineData("/Identity/Account/Manage/Email")]
        [InlineData("/Identity/Account/Manage/ChangePassword")]
        [InlineData("/Identity/Account/Manage/SetPassword")]
        [InlineData("/Identity/Account/Manage/PersonalData")]
        [InlineData("/Identity/Account/Manage/DeletePersonalData")]
        [InlineData("/Identity/Account/Manage/TwoFactorAuthentication")]
        [InlineData("/Identity/Account/Manage/EnableAuthenticator")]
        [InlineData("/Identity/Account/Manage/GenerateRecoveryCodes")]
        [InlineData("/Identity/Account/Manage/ExternalLogins")]
        public async Task AucunePageDeGestionNeSAfficheSansSonCompte(string adresse)
        {
            //Etant donné un témoin qui désigne un compte effacé
            //Lorsque la page est demandée
            using HttpResponseMessage reponse = await Client.GetAsync(adresse);

            //Alors elle refuse de se construire plutôt que de tomber
            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }

        [Theory]
        [InlineData("/Identity/Account/Manage/Index")]
        [InlineData("/Identity/Account/Manage/Email?handler=ChangeEmail")]
        [InlineData("/Identity/Account/Manage/DeletePersonalData")]
        [InlineData("/Identity/Account/Manage/DownloadPersonalData")]
        [InlineData("/Identity/Account/Manage/GenerateRecoveryCodes")]
        [InlineData("/Identity/Account/Manage/ResetAuthenticator")]
        public async Task AucunFormulaireDeGestionNAboutitSansSonCompte(string adresse)
        {
            //Etant donné le même témoin et un jeton pris avant l'effacement
            //Lorsque le formulaire est soumis
            using HttpResponseMessage reponse = await PosterAvecLeJetonAsync(adresse,
                ("Input.PhoneNumber", "418-555-0199"),
                ("Input.NewEmail", "ailleurs@escale.test"),
                ("Input.Password", FabriqueEscale.MotDePasse));

            //Alors rien n'est fait
            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }
    }
}
