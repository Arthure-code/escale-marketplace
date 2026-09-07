using Escale.Web.Pages.Annonces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Tests.Pages
{
    public class DetailsModelTests
    {
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly Mock<IPanierService> _panier = new Mock<IPanierService>(MockBehavior.Strict);
        private readonly Mock<IFavorisService> _favoris = new Mock<IFavorisService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);
        private readonly DetailsModel _page;

        private static readonly DateTime Debut = new DateTime(2026, 10, 2);
        private static readonly DateTime Fin = new DateTime(2026, 10, 6);

        public DetailsModelTests()
        {
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(false);
            _page = new DetailsModel(_annonces.Object, _panier.Object, _favoris.Object, _utilisateur.Object)
            {
                Debut = Debut,
                Fin = Fin
            };
        }

        private static Offre Offre(int id, int total = 1, int restants = 1) => new Offre(
            new Annonce
            {
                Id = id,
                Titre = "Suite avec balcon",
                Categorie = CategorieAnnonce.Chambre,
                PrixJournalier = 120,
                LoueurId = "marie",
                Exemplaires = total
            },
            total, restants);

        [Fact]
        public async Task OnGetAsync_AnnonceInconnue_RendNotFound()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(404, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync((Offre?)null);

            //Lorsque
            IActionResult resultat = await _page.OnGetAsync(404);

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task OnGetAsync_AnnonceConnue_ExposeLAnnonce()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));

            //Lorsque
            IActionResult resultat = await _page.OnGetAsync(2);

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.Equal("Suite avec balcon", _page.Annonce.Titre);
        }

        [Fact]
        public async Task OnGetAsync_SansDates_ProposeAujourdhuiEtDeuxJours()
        {
            //Etant donné
            DetailsModel page = new DetailsModel(_annonces.Object, _panier.Object, _favoris.Object, _utilisateur.Object);
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));

            //Lorsque
            await page.OnGetAsync(2);

            //Alors
            Assert.Equal(DateTime.Today, page.Debut);
            Assert.Equal(DateTime.Today.AddDays(2), page.Fin);
        }

        [Fact]
        public async Task OnGetAsync_DatesInversees_ReplaceLeDepartAuLendemain()
        {
            //Etant donné
            DetailsModel page = new DetailsModel(_annonces.Object, _panier.Object, _favoris.Object, _utilisateur.Object)
            {
                Debut = Debut,
                Fin = Debut.AddDays(-3)
            };
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));

            //Lorsque
            await page.OnGetAsync(2);

            //Alors
            Assert.Equal(Debut.AddDays(1), page.Fin);
        }

        [Fact]
        public async Task OnPostAsync_VisiteurNonConnecte_RedirigeVersLaConnexion()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(false);

            //Lorsque
            IActionResult resultat = await _page.OnPostAsync(2);

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Identity/Account/Login", redirection.PageName);
            _panier.Verify(s => s.AjouterAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task OnPostAsync_AjoutAccepte_RedirigeVersLePanier()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(true);
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _favoris.Setup(s => s.ObtenirIdsAsync("camille")).ReturnsAsync(new List<int>());
            _panier.Setup(s => s.AjouterAsync("camille", 2, Debut, Fin))
                .ReturnsAsync(new ResultatAjout(true, "Annonce ajoutée au panier."));

            //Lorsque
            IActionResult resultat = await _page.OnPostAsync(2);

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Panier/Index", redirection.PageName);
        }

        [Fact]
        public async Task OnPostAsync_AjoutRefuse_ResteSurLaPageAvecLeMessage()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(true);
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _favoris.Setup(s => s.ObtenirIdsAsync("camille")).ReturnsAsync(new List<int>());
            _panier.Setup(s => s.AjouterAsync("camille", 2, Debut, Fin))
                .ReturnsAsync(new ResultatAjout(false, "Cette annonce est déjà dans votre panier."));

            //Lorsque
            IActionResult resultat = await _page.OnPostAsync(2);

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.Equal("Cette annonce est déjà dans votre panier.", _page.Erreur);
        }

        [Fact]
        public async Task NombreDeJours_CompteLesNuitsEntreLesDeuxDates()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));

            //Lorsque
            await _page.OnGetAsync(2);

            //Alors
            Assert.Equal(4, _page.NombreDeJours);
        }

        [Fact]
        public async Task OnPostFavoriAsync_VisiteurAnonyme_RedirigeVersLaConnexionAvecLeRetour()
        {
            //Etant donné rien de plus que le visiteur non connecté

            //Lorsque
            IActionResult resultat = await _page.OnPostFavoriAsync(2);

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Identity/Account/Login", redirection.PageName);
            Assert.Equal("/Annonces/Details/2", redirection.RouteValues!["returnUrl"]);
            _favoris.Verify(s => s.BasculerAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task OnPostFavoriAsync_UtilisateurConnecte_BasculeEtRevientSurLaFiche()
        {
            //Etant donné
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(true);
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _favoris.Setup(s => s.BasculerAsync("camille", 2)).ReturnsAsync(true);

            //Lorsque
            IActionResult resultat = await _page.OnPostFavoriAsync(2);

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal(2, redirection.RouteValues!["id"]);
            _favoris.Verify(s => s.BasculerAsync("camille", 2), Times.Once);
        }

        [Fact]
        public async Task OnGetAsync_UtilisateurConnecte_IndiqueSiLAnnonceEstDejaEnFavori()
        {
            //Etant donné
            _utilisateur.SetupGet(u => u.EstConnecte).Returns(true);
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _favoris.Setup(s => s.ObtenirIdsAsync("camille")).ReturnsAsync(new List<int> { 2 });
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Offre(2));

            //Lorsque
            await _page.OnGetAsync(2);

            //Alors
            Assert.True(_page.EstFavori);
        }

        [Fact]
        public async Task OnGetAsync_TousLesExemplairesReserves_ExposeUneOffreComplete()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Offre(2, total: 3, restants: 0));

            //Lorsque
            IActionResult resultat = await _page.OnGetAsync(2);

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.True(_page.Offre.EstComplete);
            Assert.Equal(0, _page.Offre.Restants);
        }

        [Fact]
        public async Task OnGetAsync_UnSeulExemplaireRestant_LeSignale()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirOffreAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Offre(2, total: 4, restants: 1));

            //Lorsque
            await _page.OnGetAsync(2);

            //Alors
            Assert.True(_page.Offre.EstDernier);
            Assert.False(_page.Offre.EstComplete);
        }
    }
}
