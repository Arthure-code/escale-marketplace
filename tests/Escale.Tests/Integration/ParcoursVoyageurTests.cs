using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    public class ParcoursVoyageurTests : SessionConnectee
    {
        protected override string Courriel => FabriqueEscale.Voyageur;

        [Fact]
        public async Task LeParcoursDeReservationVaJusquAuRecu()
        {
            //Etant donné une voyageuse connectée et une annonce libre
            string debut = DateTime.Today.AddDays(20).ToString("yyyy-MM-dd");
            string fin = DateTime.Today.AddDays(24).ToString("yyyy-MM-dd");

            //Lorsqu'elle ajoute une annonce au panier
            using HttpResponseMessage ajout = await PosterAsync(
                "/Annonces/Details/1", "/Annonces/Details/1",
                ("debut", debut), ("fin", fin));

            //Alors elle est renvoyée vers le panier
            Assert.Equal(HttpStatusCode.Redirect, ajout.StatusCode);
            Assert.Contains("/Panier", ajout.Headers.Location!.ToString());

            //Et le panier contient la ligne
            string panier = await Client.GetStringAsync("/Panier/Index");
            Assert.Contains("Récapitulatif", panier);

            //Lorsqu'elle paie avec une carte acceptée
            using HttpResponseMessage paiement = await PosterAsync(
                "/Commandes/Paiement", "/Commandes/Paiement",
                ("Saisie.Titulaire", "Camille Roy"),
                ("Saisie.Numero", PaiementSimuleService.CarteAcceptee),
                ("Saisie.Expiration", "12/34"),
                ("Saisie.Cvc", "123"));

            //Alors la commande est confirmée
            Assert.Equal(HttpStatusCode.Redirect, paiement.StatusCode);
            string confirmation = paiement.Headers.Location!.ToString();
            Assert.Contains("/Commandes/Confirmation", confirmation);

            //Et le reçu s'affiche avec les quatre derniers chiffres, jamais le numéro
            string recu = await Client.GetStringAsync(confirmation);
            Assert.Contains("4242", recu);
            Assert.DoesNotContain(PaiementSimuleService.CarteAcceptee, recu);
        }

        [Fact]
        public async Task UneCarteRefuseeNeCreeAucuneCommande()
        {
            //Etant donné une annonce au panier
            string debut = DateTime.Today.AddDays(40).ToString("yyyy-MM-dd");
            string fin = DateTime.Today.AddDays(43).ToString("yyyy-MM-dd");
            using HttpResponseMessage ajout = await PosterAsync(
                "/Annonces/Details/2", "/Annonces/Details/2",
                ("debut", debut), ("fin", fin));
            Assert.Equal(HttpStatusCode.Redirect, ajout.StatusCode);

            //Lorsque la banque refuse
            using HttpResponseMessage paiement = await PosterAsync(
                "/Commandes/Paiement", "/Commandes/Paiement",
                ("Saisie.Titulaire", "Camille Roy"),
                ("Saisie.Numero", PaiementSimuleService.CarteRefusee),
                ("Saisie.Expiration", "12/34"),
                ("Saisie.Cvc", "123"));

            //Alors la page se réaffiche avec le motif, sans redirection.
            //Le motif vient d'une variable, donc Razor encode ses accents :
            //on vérifie une portion sans accent plutôt que le texte brut.
            Assert.Equal(HttpStatusCode.OK, paiement.StatusCode);
            string page = await paiement.Content.ReadAsStringAsync();
            Assert.Contains("par la banque", page);
            Assert.Contains("avis-erreur", page);
        }

        [Fact]
        public async Task LesFavorisSAjoutentEtSeRetirent()
        {
            //Etant donné la page des favoris
            using HttpResponseMessage ajout = await PosterAsync(
                "/Favoris/Index?handler=Favori&id=3", "/Favoris/Index");

            //Alors la bascule renvoie sur la page
            Assert.Equal(HttpStatusCode.Redirect, ajout.StatusCode);

            //Et l'annonce apparaît dans la liste
            string favoris = await Client.GetStringAsync("/Favoris/Index");
            Assert.DoesNotContain("Aucun favori", favoris);

            //Lorsqu'on rebascule
            using HttpResponseMessage retrait = await PosterAsync(
                "/Favoris/Index?handler=Favori&id=3", "/Favoris/Index");

            //Alors elle en sort
            Assert.Equal(HttpStatusCode.Redirect, retrait.StatusCode);
        }

        [Fact]
        public async Task LesPagesDuCompteRepondent()
        {
            //Etant donné une session ouverte
            foreach (string adresse in new[]
            {
                "/Identity/Account/Manage/Index",
                "/Identity/Account/Manage/Email",
                "/Identity/Account/Manage/ChangePassword",
                "/Identity/Account/Manage/TwoFactorAuthentication",
                "/Identity/Account/Manage/PersonalData",
                "/Identity/Account/Manage/DeletePersonalData",
                "/Identity/Account/Manage/EnableAuthenticator",
                "/Identity/Account/Manage/ResetAuthenticator"
            })
            {
                //Lorsque
                using HttpResponseMessage reponse = await Client.GetAsync(adresse);

                //Alors
                Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            }
        }

        [Fact]
        public async Task LAdministrationResteFermeeAUneVoyageuse()
        {
            //Etant donné une voyageuse connectée
            //Lorsqu'elle demande une page réservée
            using HttpResponseMessage reponse = await Client.GetAsync("/Administration/Index");

            //Alors elle est renvoyée vers l'accès refusé, pas vers la connexion
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("AccessDenied", reponse.Headers.Location!.ToString());
        }

        [Fact]
        public async Task LeTableauDeBordResteFermeAUneVoyageuse()
        {
            //Etant donné
            //Lorsque
            using HttpResponseMessage reponse = await Client.GetAsync("/Tableau/Index");

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("AccessDenied", reponse.Headers.Location!.ToString());
        }
    }
}
