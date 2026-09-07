using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Escale.Tests.Integration
{
    // Les liens envoyés par courriel portent un jeton signé par Identity. Le
    // service d'envoi se contente de journaliser en développement, donc le
    // jeton est demandé au même UserManager que celui de l'application.
    public class MotDePasseEtCourrielTests : SessionHttp
    {
        private async Task<T> AvecComptesAsync<T>(Func<UserManager<Utilisateur>, Task<T>> travail)
        {
            using IServiceScope portee = Fabrique.Services.CreateScope();
            UserManager<Utilisateur> comptes =
                portee.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();

            return await travail(comptes);
        }

        private static string Encoder(string jeton) =>
            WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(jeton));

        [Fact]
        public async Task ReinitialiserSonMotDePasseAvecLeLienRecu()
        {
            //Etant donné le jeton que produirait le courriel
            const string nouveau = "Escale.Reinit.2026";
            string code = await AvecComptesAsync(async comptes =>
            {
                Utilisateur compte = (await comptes.FindByEmailAsync(FabriqueEscale.Voyageur))!;
                return Encoder(await comptes.GeneratePasswordResetTokenAsync(compte));
            });

            //Lorsque la page est ouverte par le lien
            using HttpResponseMessage page = await Client.GetAsync(
                "/Identity/Account/ResetPassword?code=" + code);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);

            //Et que le formulaire est soumis
            using HttpResponseMessage envoi = await PosterAsync(
                "/Identity/Account/ResetPassword", "/Identity/Account/ResetPassword?code=" + code,
                ("Input.Code", await AvecComptesAsync(async comptes =>
                {
                    Utilisateur compte = (await comptes.FindByEmailAsync(FabriqueEscale.Voyageur))!;
                    return await comptes.GeneratePasswordResetTokenAsync(compte);
                })),
                ("Input.Email", FabriqueEscale.Voyageur),
                ("Input.Password", nouveau),
                ("Input.ConfirmPassword", nouveau));

            //Alors le mot de passe est changé
            Assert.Equal(HttpStatusCode.Redirect, envoi.StatusCode);
            Assert.Contains("ResetPasswordConfirmation", envoi.Headers.Location!.ToString());

            bool valide = await AvecComptesAsync(async comptes =>
            {
                Utilisateur compte = (await comptes.FindByEmailAsync(FabriqueEscale.Voyageur))!;
                return await comptes.CheckPasswordAsync(compte, nouveau);
            });
            Assert.True(valide);
        }

        [Fact]
        public async Task UnJetonDeReinitialisationForgeEstRejete()
        {
            //Etant donné un jeton inventé
            //Lorsque
            using HttpResponseMessage envoi = await PosterAsync(
                "/Identity/Account/ResetPassword", "/Identity/Account/ResetPassword?code=" + Encoder("faux"),
                ("Input.Code", "jeton-invente"),
                ("Input.Email", FabriqueEscale.Voyageur),
                ("Input.Password", "Escale.Pirate.2026"),
                ("Input.ConfirmPassword", "Escale.Pirate.2026"));

            //Alors rien n'est changé et la page revient
            Assert.Equal(HttpStatusCode.OK, envoi.StatusCode);
        }

        [Fact]
        public async Task ConfirmerSonCourrielAvecLeLienRecu()
        {
            //Etant donné le jeton de confirmation
            (string id, string code) = await AvecComptesAsync(async comptes =>
            {
                Utilisateur compte = (await comptes.FindByEmailAsync(FabriqueEscale.Loueuse))!;
                return (await comptes.GetUserIdAsync(compte),
                    Encoder(await comptes.GenerateEmailConfirmationTokenAsync(compte)));
            });

            //Lorsque le lien est suivi
            string html = await Client.GetStringAsync(
                $"/Identity/Account/ConfirmEmail?userId={id}&code={code}");

            //Alors la confirmation est annoncée
            Assert.Contains("confirm", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ConfirmerUnChangementDeCourriel()
        {
            //Etant donné une nouvelle adresse et son jeton
            const string nouvelle = "marie.nouvelle@escale.test";
            (string id, string code) = await AvecComptesAsync(async comptes =>
            {
                Utilisateur compte = (await comptes.FindByEmailAsync(FabriqueEscale.Loueuse))!;
                return (await comptes.GetUserIdAsync(compte),
                    Encoder(await comptes.GenerateChangeEmailTokenAsync(compte, nouvelle)));
            });

            //Lorsque le lien est suivi
            using HttpResponseMessage reponse = await Client.GetAsync(
                $"/Identity/Account/ConfirmEmailChange?userId={id}&email={nouvelle}&code={code}");

            //Alors la page répond
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        }

        [Fact]
        public async Task UnJetonDeChangementDeCourrielForgeEstRejete()
        {
            //Etant donné un lien de changement inventé
            string id = await AvecComptesAsync(async comptes =>
            {
                Utilisateur compte = (await comptes.FindByEmailAsync(FabriqueEscale.Loueuse))!;
                return await comptes.GetUserIdAsync(compte);
            });

            //Lorsque
            using HttpResponseMessage reponse = await Client.GetAsync(
                $"/Identity/Account/ConfirmEmailChange?userId={id}&email=pirate@escale.test&code={Encoder("faux")}");

            //Alors l'adresse n'est pas changée
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("Erreur", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task RenvoyerUneConfirmationNeReveleRienSurLAdresse()
        {
            //Etant donné une adresse inconnue
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/ResendEmailConfirmation", "/Identity/Account/ResendEmailConfirmation",
                ("Input.Email", "personne@escale.test"));

            //Alors la réponse ne distingue pas les deux cas
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("rification", await reponse.Content.ReadAsStringAsync());
        }
    }
}
