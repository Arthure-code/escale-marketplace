using Escale.Web.Pages.Favoris;
using Microsoft.AspNetCore.Mvc;

namespace Escale.Tests.Pages
{
    public class FavorisIndexModelTests
    {
        private const string Utilisateur = "camille";

        private readonly Mock<IFavorisService> _favoris = new Mock<IFavorisService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);
        private readonly IndexModel _page;

        private static readonly int[] MarquesAttendues = { 1, 2 };

        public FavorisIndexModelTests()
        {
            _utilisateur.SetupGet(u => u.Id).Returns(Utilisateur);
            _page = new IndexModel(_favoris.Object, _utilisateur.Object);
        }

        private static Offre Offre(int id) =>
            new Offre(new Annonce { Id = id, Titre = "t", Description = "d", Photo = "p.jpg" }, 1, 1);

        [Fact]
        public async Task OnGetAsync_ChargeLesFavorisDeLUtilisateurConnecte()
        {
            //Etant donné
            List<Offre> offres = new List<Offre> { Offre(1), Offre(2) };
            _favoris.Setup(f => f.ObtenirAsync(Utilisateur, _page.Debut, _page.Fin)).ReturnsAsync(offres);
            _favoris.Setup(f => f.ObtenirIdsAsync(Utilisateur)).ReturnsAsync(new List<int> { 1, 2 });

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(2, _page.Offres.Count);
            Assert.Equal(MarquesAttendues, _page.Marques.OrderBy(i => i));
        }

        [Fact]
        public async Task OnGetAsync_CompteCommeRetireCeQuiEstEnCacheMaisPlusAffichable()
        {
            //Etant donné trois favoris dont un seul reste public
            _favoris.Setup(f => f.ObtenirAsync(Utilisateur, _page.Debut, _page.Fin))
                .ReturnsAsync(new List<Offre> { Offre(1) });
            _favoris.Setup(f => f.ObtenirIdsAsync(Utilisateur)).ReturnsAsync(new List<int> { 1, 2, 3 });

            //Lorsque
            await _page.OnGetAsync();

            //Alors la page peut annoncer que deux ne sont plus disponibles
            Assert.Equal(2, _page.Retirees);
        }

        [Fact]
        public async Task OnGetAsync_NAnnonceAucunRetraitQuandToutEstAffichable()
        {
            //Etant donné
            _favoris.Setup(f => f.ObtenirAsync(Utilisateur, _page.Debut, _page.Fin))
                .ReturnsAsync(new List<Offre> { Offre(1) });
            _favoris.Setup(f => f.ObtenirIdsAsync(Utilisateur)).ReturnsAsync(new List<int> { 1 });

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(0, _page.Retirees);
        }

        [Fact]
        public async Task OnPostFavoriAsync_BasculeLeFavoriDuDemandeurEtRevientSurLaPage()
        {
            //Etant donné
            _favoris.Setup(f => f.BasculerAsync(Utilisateur, 7)).ReturnsAsync(false);

            //Lorsque
            IActionResult resultat = await _page.OnPostFavoriAsync(7);

            //Alors le service reçoit l'identité de l'appelant, jamais celle du formulaire
            _favoris.Verify(f => f.BasculerAsync(Utilisateur, 7), Times.Once);
            Assert.IsType<RedirectToPageResult>(resultat);
        }
    }
}
