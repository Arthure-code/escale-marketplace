using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Escale.Tests.Integration
{
    // Un compte peut n'avoir aucun mot de passe local, cas prévu par Identity
    // pour les connexions externes. La page de gestion propose alors d'en
    // poser un plutôt que d'en changer.
    public class CompteSansMotDePasseTests : SessionConnectee
    {
        protected override string Courriel => FabriqueEscale.Voyageur;

        protected override async Task ApresOuvertureAsync()
        {
            await base.ApresOuvertureAsync();

            using IServiceScope portee = Fabrique.Services.CreateScope();
            UserManager<Utilisateur> comptes =
                portee.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();

            Utilisateur compte = (await comptes.FindByEmailAsync(Courriel))!;
            IdentityResult retrait = await comptes.RemovePasswordAsync(compte);
            Assert.True(retrait.Succeeded);
        }

        [Fact]
        public async Task ChangerLeMotDePasseRenvoieVersLaPageQuiEnPose()
        {
            //Etant donné un compte sans mot de passe local
            //Lorsqu'on demande à le changer
            using HttpResponseMessage reponse = await Client.GetAsync(
                "/Identity/Account/Manage/ChangePassword");

            //Alors Identity oriente vers la pose plutôt que vers le changement
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("SetPassword", reponse.Headers.Location!.ToString());
        }

        [Fact]
        public async Task PoserUnMotDePasseLocal()
        {
            //Etant donné la page de pose
            const string mot = "Escale.Pose.2026";
            string page = await Client.GetStringAsync("/Identity/Account/Manage/SetPassword");
            Assert.Contains("finissez votre mot de passe", page);

            //Lorsqu'on en pose un
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/SetPassword", "/Identity/Account/Manage/SetPassword",
                ("Input.NewPassword", mot),
                ("Input.ConfirmPassword", mot));

            //Alors il est enregistré
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);

            using IServiceScope portee = Fabrique.Services.CreateScope();
            UserManager<Utilisateur> comptes =
                portee.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
            Utilisateur compte = (await comptes.FindByEmailAsync(Courriel))!;
            Assert.True(await comptes.CheckPasswordAsync(compte, mot));
        }

        [Fact]
        public async Task DeuxMotsDePasseDifferentsSontRefuses()
        {
            //Etant donné une confirmation qui ne correspond pas
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/SetPassword", "/Identity/Account/Manage/SetPassword",
                ("Input.NewPassword", "Escale.Premier.2026"),
                ("Input.ConfirmPassword", "Escale.Second.2026"));

            //Alors rien n'est posé et la page revient avec l'erreur
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UnMotDePasseTropCourtEstRefuse()
        {
            //Etant donné une saisie sous la longueur exigée
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Manage/SetPassword", "/Identity/Account/Manage/SetPassword",
                ("Input.NewPassword", "abc"),
                ("Input.ConfirmPassword", "abc"));

            //Alors
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }
    }
}
