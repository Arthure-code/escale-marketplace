using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // Identity rend ses motifs par des codes en anglais. La page les traduit
    // avant de les afficher, et c'est cette traduction qui est vérifiée ici.
    // Classe à part : la page d'inscription porte la limitation de débit.
    public class MotifsDeRefusDInscriptionTests : SessionHttp
    {
        private Task<HttpResponseMessage> SInscrireAsync(string motDePasse, string returnUrl = "") =>
            PosterAsync(
                "/Identity/Account/Register" + returnUrl, "/Identity/Account/Register",
                ("Input.Role", "Voyageur"),
                ("Input.NomComplet", "Candidat"),
                ("Input.Email", "candidat." + Guid.NewGuid().ToString("N")[..8] + "@escale.test"),
                ("Input.Password", motDePasse),
                ("Input.ConfirmPassword", motDePasse));

        [Fact]
        public async Task UnMotDePasseSansChiffreNiMajusculeNiSymboleEstRefuseEnFrancais()
        {
            //Etant donné une suite de lettres minuscules
            using HttpResponseMessage reponse = await SInscrireAsync("abcdefghij");

            //Alors chaque règle non tenue est dite en français
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

            string page = await reponse.Content.ReadAsStringAsync();
            Assert.Contains("doit contenir un chiffre", page);
            Assert.Contains("doit contenir une majuscule", page);
            Assert.Contains("sp&#xE9;cial", page);
        }

        [Fact]
        public async Task UneInscriptionVenueDUnePageParticuliereYRamene()
        {
            //Etant donné une page qui attendait la personne
            using HttpResponseMessage reponse = await SInscrireAsync(
                "Escale.Retour.2026", "?returnUrl=%2FFavoris%2FIndex");

            //Alors elle y est ramenée plutôt que sur l'accueil
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Equal("/Favoris/Index", reponse.Headers.Location!.ToString());
        }
    }
}
