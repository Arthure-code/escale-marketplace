using System.Linq;
using Escale.Web.Pages;
using Microsoft.AspNetCore.Mvc;

namespace Escale.Tests.Pages
{
    public class IndexModelTests
    {
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly Mock<IFavorisService> _favoris = new Mock<IFavorisService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);

        public IndexModelTests()
        {
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(false);
        }

        // Le tableau attendu est un champ et non un littéral posé dans l'appel :
        // un tableau constant en argument est réalloué à chaque exécution.
        private static readonly int[] FavorisAttendus = { 2, 7 };

        private static readonly DateTime Debut = new DateTime(2026, 11, 3);
        private static readonly DateTime Fin = new DateTime(2026, 11, 5);

        private static Offre Offrir(int id, string titre, CategorieAnnonce categorie, int total = 1, int restants = 1) =>
            new Offre(new Annonce { Id = id, Titre = titre, Categorie = categorie, Exemplaires = total }, total, restants);

        private static List<Offre> DeuxAnnonces() => new List<Offre>
        {
            Offrir(1, "Chambre simple", CategorieAnnonce.Chambre),
            Offrir(2, "Ford Mustang", CategorieAnnonce.Voiture)
        };

        [Fact]
        public async Task OnGetAsync_RemplitLePageModelAvecLesAnnoncesDisponibles()
        {
            //Etant donné
            List<Offre> attendues = DeuxAnnonces();
            _annonces.Setup(s => s.RechercherAsync(null, Debut, Fin)).ReturnsAsync(attendues);
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Fin };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            List<Offre> obtenues = Assert.IsAssignableFrom<List<Offre>>(page.Resultats);
            Assert.Equal(attendues, obtenues);
        }

        [Fact]
        public async Task OnGetAsync_TransmetLaCategorieDemandee()
        {
            //Etant donné
            _annonces.Setup(s => s.RechercherAsync(CategorieAnnonce.Voiture, Debut, Fin))
                .ReturnsAsync(new List<Offre>());
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object)
            {
                Categorie = CategorieAnnonce.Voiture,
                Debut = Debut,
                Fin = Fin
            };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            _annonces.Verify(s => s.RechercherAsync(CategorieAnnonce.Voiture, Debut, Fin), Times.Once);
        }

        [Fact]
        public async Task OnGetAsync_SansDates_ChercheSurLesDeuxProchainsJours()
        {
            //Etant donné
            _annonces.Setup(s => s.RechercherAsync(null, DateTime.Today, DateTime.Today.AddDays(2)))
                .ReturnsAsync(new List<Offre>());
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object);

            //Lorsque
            await page.OnGetAsync();

            //Alors
            Assert.Equal(DateTime.Today, page.Debut);
            Assert.Equal(DateTime.Today.AddDays(2), page.Fin);
        }

        [Fact]
        public async Task OnGetAsync_DatesInversees_CorrigeLaFinEtPrevientLeVisiteur()
        {
            //Etant donné
            _annonces.Setup(s => s.RechercherAsync(null, Debut, Debut.AddDays(1)))
                .ReturnsAsync(new List<Offre>());
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Debut.AddDays(-2) };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            Assert.Equal(Debut.AddDays(1), page.Fin);
            Assert.NotEmpty(page.Message);
        }

        [Fact]
        public async Task OnGetAsync_RechercheValide_NAffichePasDeMessage()
        {
            //Etant donné
            _annonces.Setup(s => s.RechercherAsync(null, Debut, Fin)).ReturnsAsync(DeuxAnnonces());
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Fin };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            Assert.Empty(page.Message);
        }

        [Fact]
        public async Task OnGetAsync_UtilisateurConnecte_ChargeSesFavorisPourLesCoches()
        {
            //Etant donné
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(true);
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _favoris.Setup(s => s.ObtenirIdsAsync("camille")).ReturnsAsync(new List<int> { 2, 7 });
            _annonces.Setup(s => s.RechercherAsync(null, Debut, Fin)).ReturnsAsync(DeuxAnnonces());
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Fin };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            Assert.Equal(FavorisAttendus, page.Favoris.OrderBy(i => i));
        }

        [Fact]
        public async Task OnGetAsync_VisiteurAnonyme_NInterrogePasLeCache()
        {
            //Etant donné
            _annonces.Setup(s => s.RechercherAsync(null, Debut, Fin)).ReturnsAsync(DeuxAnnonces());
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Fin };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            Assert.Empty(page.Favoris);
            _favoris.Verify(s => s.ObtenirIdsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OnPostFavoriAsync_VisiteurAnonyme_RedirigeVersLaConnexionAvecLaRaison()
        {
            //Etant donné
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object);

            //Lorsque
            IActionResult resultat = await page.OnPostFavoriAsync(2);

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Identity/Account/Login", redirection.PageName);
            Assert.Equal("/", redirection.RouteValues!["returnUrl"]);
            _favoris.Verify(s => s.BasculerAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task OnPostFavoriAsync_UtilisateurConnecte_BasculeLeFavori()
        {
            //Etant donné
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(true);
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _favoris.Setup(s => s.BasculerAsync("camille", 2)).ReturnsAsync(true);
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Fin };

            //Lorsque
            IActionResult resultat = await page.OnPostFavoriAsync(2);

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Null(redirection.PageName);
            _favoris.Verify(s => s.BasculerAsync("camille", 2), Times.Once);
        }

        [Fact]
        public async Task Completes_CompteLesAnnoncesSansExemplaireRestant()
        {
            //Etant donné
            _annonces.Setup(s => s.RechercherAsync(null, Debut, Fin)).ReturnsAsync(new List<Offre>
            {
                Offrir(1, "Kia Sportage", CategorieAnnonce.Voiture, total: 3, restants: 2),
                Offrir(2, "Ford Mustang", CategorieAnnonce.Voiture, total: 1, restants: 0),
                Offrir(3, "Mazda CX-5", CategorieAnnonce.Voiture, total: 2, restants: 0)
            });
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Fin };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            Assert.Equal(3, page.Resultats.Count);
            Assert.Equal(2, page.Completes);
        }

        [Fact]
        public async Task OnGetAsync_AnnonceComplete_ResteDansLesResultats()
        {
            //Etant donné
            _annonces.Setup(s => s.RechercherAsync(null, Debut, Fin)).ReturnsAsync(new List<Offre>
            {
                Offrir(2, "Ford Mustang", CategorieAnnonce.Voiture, total: 1, restants: 0)
            });
            IndexModel page = new IndexModel(_annonces.Object, _favoris.Object, _utilisateur.Object) { Debut = Debut, Fin = Fin };

            //Lorsque
            await page.OnGetAsync();

            //Alors
            Offre offre = Assert.Single(page.Resultats);
            Assert.True(offre.EstComplete);
            Assert.Equal("Ford Mustang", offre.Annonce.Titre);
        }
    }
}
