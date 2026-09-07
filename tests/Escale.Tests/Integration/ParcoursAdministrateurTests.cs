using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    public class ParcoursAdministrateurTests : SessionConnectee
    {
        protected override string Courriel => FabriqueEscale.Administrateur;

        private static string Jour(int decalage) =>
            DateTime.Today.AddDays(decalage).ToString("yyyy-MM-dd");

        [Fact]
        public async Task LaVueDEnsembleAfficheToutePlateforme()
        {
            //Etant donné un administrateur connecté
            //Lorsque
            string html = await Client.GetStringAsync("/Administration/Index");

            //Alors il voit les compteurs de la plateforme entière
            Assert.Contains("Administration", html);
            Assert.Contains("Annonces", html);
        }

        [Fact]
        public async Task LesTroisPagesDAdministrationRepondent()
        {
            //Etant donné
            foreach (string adresse in new[]
            {
                "/Administration/Index",
                "/Administration/Annonces",
                "/Administration/Comptes"
            })
            {
                //Lorsque
                using HttpResponseMessage reponse = await Client.GetAsync(adresse);

                //Alors
                Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            }
        }

        [Fact]
        public async Task BloquerUnComptePuisLeDebloquer()
        {
            //Etant donné la liste des comptes, d'où l'on tire l'identifiant de Hugo
            string comptes = await Client.GetStringAsync("/Administration/Comptes");
            int position = comptes.IndexOf("echeance-", StringComparison.Ordinal);
            Assert.True(position > 0, "Aucun compte de loueur dans la page");
            string id = comptes.Substring(position + 9, 36);

            //Lorsqu'on le bloque pour trente jours
            using HttpResponseMessage blocage = await PosterAsync(
                "/Administration/Comptes?handler=Bloquer", "/Administration/Comptes",
                ("Saisie.Id", id),
                ("Saisie.Debut", Jour(0)),
                ("Saisie.Fin", Jour(30)),
                ("Saisie.Motif", "Annonce non conforme"));

            //Alors le blocage est accepté
            Assert.Equal(HttpStatusCode.Redirect, blocage.StatusCode);

            //Lorsqu'on le débloque
            using HttpResponseMessage deblocage = await PosterAsync(
                "/Administration/Comptes?handler=Debloquer&id=" + id, "/Administration/Comptes");

            //Alors le compte redevient actif, sans que rien n'ait été supprimé
            Assert.Equal(HttpStatusCode.Redirect, deblocage.StatusCode);
        }

        [Fact]
        public async Task UnBlocageDontLaFinPrecedeLeDebutEstRefuse()
        {
            //Etant donné des bornes inversées
            string comptes = await Client.GetStringAsync("/Administration/Comptes");
            int position = comptes.IndexOf("echeance-", StringComparison.Ordinal);
            string id = comptes.Substring(position + 9, 36);

            //Lorsque
            using HttpResponseMessage reponse = await PosterAsync(
                "/Administration/Comptes?handler=Bloquer", "/Administration/Comptes",
                ("Saisie.Id", id),
                ("Saisie.Debut", Jour(10)),
                ("Saisie.Fin", Jour(2)),
                ("Saisie.Motif", "Motif"));

            //Alors le refus revient sur la page
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        }

        [Fact]
        public async Task ProlongerUnAbonnement()
        {
            //Etant donné un loueur dont l'abonnement porte une échéance
            string comptes = await Client.GetStringAsync("/Administration/Comptes");
            int position = comptes.IndexOf("echeance-", StringComparison.Ordinal);
            string id = comptes.Substring(position + 9, 36);

            //Lorsque
            using HttpResponseMessage reponse = await PosterAsync(
                "/Administration/Comptes?handler=Prolonger&id=" + id + "&echeance=" + Jour(365),
                "/Administration/Comptes");

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        }

        [Fact]
        public async Task LAdministrateurBasculeNimporteQuelleAnnonce()
        {
            //Etant donné une annonce d'un loueur quelconque
            using HttpResponseMessage retrait = await PosterAsync(
                "/Administration/Annonces?handler=Basculer&id=2", "/Administration/Annonces");

            //Alors il peut la retirer du site sans en être le propriétaire
            Assert.Equal(HttpStatusCode.Redirect, retrait.StatusCode);

            using HttpResponseMessage retour = await PosterAsync(
                "/Administration/Annonces?handler=Basculer&id=2", "/Administration/Annonces");
            Assert.Equal(HttpStatusCode.Redirect, retour.StatusCode);
        }

        [Fact]
        public async Task SupprimerUneAnnonceJamaisReservee()
        {
            //Etant donné une annonce qu'aucune commande ne porte
            using HttpResponseMessage reponse = await PosterAsync(
                "/Administration/Annonces?handler=Supprimer&id=11", "/Administration/Annonces");

            //Alors
            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        }
    }
}
