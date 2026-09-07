using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // Parcours anonymes des pages de compte. Elles portent une limitation à
    // huit requêtes par minute, d'où le nombre volontairement restreint
    // d'appels dans cette classe, qui possède son propre hôte.
    public class ComptesIdentityTests : SessionHttp
    {
        [Fact]
        public async Task CreerUnCompteDeVoyageurOuvreLaSessionAussitot()
        {
            //Etant donné un nouveau visiteur
            string courriel = "essai." + Guid.NewGuid().ToString("N")[..8] + "@escale.test";

            //Lorsqu'il s'inscrit
            using HttpResponseMessage inscription = await PosterAsync(
                "/Identity/Account/Register", "/Identity/Account/Register",
                ("Input.Role", "Voyageur"),
                ("Input.NomComplet", "Nouvelle Personne"),
                ("Input.Email", courriel),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.ConfirmPassword", FabriqueEscale.MotDePasse));

            //Alors il est connecté sans avoir à confirmer quoi que ce soit
            Assert.Equal(HttpStatusCode.Redirect, inscription.StatusCode);

            string accueil = await Client.GetStringAsync("/");
            Assert.Contains("Mon compte", accueil);
            Assert.DoesNotContain("Cr&#xE9;er un compte", accueil);
        }

        [Fact]
        public async Task UneInscriptionDeLoueurOuvreUnAbonnement()
        {
            //Etant donné un visiteur qui choisit le rôle de loueur
            string courriel = "loueur." + Guid.NewGuid().ToString("N")[..8] + "@escale.test";

            //Lorsqu'il s'inscrit
            using HttpResponseMessage inscription = await PosterAsync(
                "/Identity/Account/Register", "/Identity/Account/Register",
                ("Input.Role", "Loueur"),
                ("Input.NomComplet", "Nouveau Loueur"),
                ("Input.Email", courriel),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.ConfirmPassword", FabriqueEscale.MotDePasse));

            //Alors il arrive sur son tableau de bord, qui lui est désormais ouvert
            Assert.Equal(HttpStatusCode.Redirect, inscription.StatusCode);
            Assert.Contains("Tableau", inscription.Headers.Location!.ToString());

            using HttpResponseMessage tableau = await Client.GetAsync("/Tableau/Index");
            Assert.Equal(HttpStatusCode.OK, tableau.StatusCode);
        }

        [Fact]
        public async Task UneInscriptionAvecUnCourrielDejaPrisEstRefusee()
        {
            //Etant donné une adresse déjà utilisée par le jeu de démonstration
            //Lorsque
            using HttpResponseMessage inscription = await PosterAsync(
                "/Identity/Account/Register", "/Identity/Account/Register",
                ("Input.Role", "Voyageur"),
                ("Input.NomComplet", "Doublon"),
                ("Input.Email", FabriqueEscale.Voyageur),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.ConfirmPassword", FabriqueEscale.MotDePasse));

            //Alors la page revient avec le motif traduit
            Assert.Equal(HttpStatusCode.OK, inscription.StatusCode);
            Assert.Contains("existe d", await inscription.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UnMotDePasseOublieNeReveleRienSurLExistenceDuCompte()
        {
            //Etant donné une adresse inconnue
            using HttpResponseMessage inconnue = await PosterAsync(
                "/Identity/Account/ForgotPassword", "/Identity/Account/ForgotPassword",
                ("Input.Email", "personne@escale.test"));

            //Alors la réponse est la même que pour une adresse connue
            Assert.Equal(HttpStatusCode.Redirect, inconnue.StatusCode);
            Assert.Contains("ForgotPasswordConfirmation", inconnue.Headers.Location!.ToString());
        }

        [Fact]
        public async Task UnJetonDeConfirmationInvalideEstRejete()
        {
            //Etant donné un lien de confirmation forgé
            //Lorsque
            using HttpResponseMessage reponse = await Client.GetAsync(
                "/Identity/Account/ConfirmEmail?userId=inconnu&code=Zm9v");

            //Alors la page ne confirme rien
            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }

        [Fact]
        public async Task LaConnexionExterneRenvoieVersLaConnexionOrdinaire()
        {
            //Etant donné qu'aucun fournisseur externe n'est configuré
            //Lorsque
            using HttpResponseMessage reponse = await Client.GetAsync("/Identity/Account/ExternalLogin");

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("Login", reponse.Headers.Location!.ToString());
        }
    }
}
