using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    public class ParcoursLoueurTests : SessionConnectee
    {
        protected override string Courriel => FabriqueEscale.Loueuse;

        [Fact]
        public async Task LeTableauDeBordAfficheLesAnnoncesDeLaLoueuse()
        {
            //Etant donné une loueuse connectée
            //Lorsque
            string html = await Client.GetStringAsync("/Tableau/Index");

            //Alors elle voit ses annonces et ses compteurs
            Assert.Contains("Tableau de bord", html);
            Assert.Contains("Mes annonces", html);
        }

        [Fact]
        public async Task PublierUneAnnonceLAjouteAuTableauDeBord()
        {
            //Etant donné le formulaire de publication
            using HttpResponseMessage publication = await PosterAsync(
                "/Tableau/Annonce", "/Tableau/Annonce",
                ("Saisie.Categorie", "Chambre"),
                ("Saisie.Titre", "Chambre d'essai intégrée"),
                ("Saisie.Description", "Une chambre créée par le test d'intégration."),
                ("Saisie.PrixJournalier", "88"),
                ("Saisie.Exemplaires", "2"),
                ("Saisie.Photo", "chambre-1.jpg"),
                ("Saisie.Superficie", "20"),
                ("Saisie.Couchages", "2"),
                ("Saisie.EstDisponible", "true"));

            //Alors la publication renvoie au tableau de bord
            Assert.Equal(HttpStatusCode.Redirect, publication.StatusCode);

            //Et l'annonce y figure
            string tableau = await Client.GetStringAsync("/Tableau/Index");
            Assert.Contains("Chambre d&#x27;essai int", tableau);
        }

        [Fact]
        public async Task UneSaisieIncompleteReaffichLeFormulaireAvecSesErreurs()
        {
            //Etant donné un titre manquant
            using HttpResponseMessage reponse = await PosterAsync(
                "/Tableau/Annonce", "/Tableau/Annonce",
                ("Saisie.Categorie", "Chambre"),
                ("Saisie.Titre", ""),
                ("Saisie.Description", "Description"),
                ("Saisie.PrixJournalier", "88"),
                ("Saisie.Exemplaires", "1"),
                ("Saisie.Photo", "chambre-1.jpg"));

            //Alors rien n'est enregistré et la page revient
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("text-danger", await reponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task RetirerUneAnnonceDuSitePuisLaRemettre()
        {
            //Etant donné une annonce de la loueuse
            using HttpResponseMessage retrait = await PosterAsync(
                "/Tableau/Index?handler=Basculer&id=1", "/Tableau/Index");

            //Alors la bascule renvoie au tableau
            Assert.Equal(HttpStatusCode.Redirect, retrait.StatusCode);

            //Lorsqu'on rebascule
            using HttpResponseMessage retour = await PosterAsync(
                "/Tableau/Index?handler=Basculer&id=1", "/Tableau/Index");

            //Alors l'annonce revient sans avoir été supprimée
            Assert.Equal(HttpStatusCode.Redirect, retour.StatusCode);
            string accueil = await Client.GetStringAsync("/");
            Assert.Contains("offres", accueil);
        }

        [Fact]
        public async Task LeFormulaireDeModificationChargeLAnnonceExistante()
        {
            //Etant donné une annonce de la loueuse
            //Lorsque
            string html = await Client.GetStringAsync("/Tableau/Annonce/1");

            //Alors le formulaire est prérempli
            Assert.Contains("Modifier l", html);
            Assert.Contains("Saisie_Titre", html);
        }

        [Fact]
        public async Task UneAnnonceDUnAutreLoueurResteInaccessible()
        {
            //Etant donné une annonce appartenant à Hugo
            //Lorsque Marie tente de l'ouvrir en modification
            using HttpResponseMessage reponse = await Client.GetAsync("/Tableau/Annonce/6");

            //Alors elle ne la trouve pas
            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }

        [Fact]
        public async Task LAdministrationResteFermeeAUneLoueuse()
        {
            //Etant donné
            //Lorsque
            using HttpResponseMessage reponse = await Client.GetAsync("/Administration/Comptes");

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("AccessDenied", reponse.Headers.Location!.ToString());
        }
    }
}
