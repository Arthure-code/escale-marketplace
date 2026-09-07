using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // Les refus d'inscription : ce que la validation du modèle arrête, et ce
    // qu'Identity arrête ensuite. Classe à part pour disposer de son propre
    // compteur de limitation de débit.
    public class InscriptionRefuseeTests : SessionHttp
    {
        [Fact]
        public async Task UneAdresseDejaInscriteEstRefuseeParIdentity()
        {
            //Etant donné une adresse déjà prise
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Register", "/Identity/Account/Register",
                ("Input.Role", "Voyageur"),
                ("Input.NomComplet", "Doublon"),
                ("Input.Email", FabriqueEscale.Voyageur),
                ("Input.Password", "Escale.Doublon.2026"),
                ("Input.ConfirmPassword", "Escale.Doublon.2026"));

            //Alors le motif d'Identity est remonté dans la page
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("validation-summary-errors", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UneConfirmationQuiNeCorrespondPasEstRefusee()
        {
            //Etant donné deux mots de passe différents
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Register", "/Identity/Account/Register",
                ("Input.Role", "Voyageur"),
                ("Input.NomComplet", "Saisie Fautive"),
                ("Input.Email", "fautive." + Guid.NewGuid().ToString("N")[..8] + "@escale.test"),
                ("Input.Password", "Escale.Premier.2026"),
                ("Input.ConfirmPassword", "Escale.Second.2026"));

            //Alors aucun compte n'est créé
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }
    }
}
