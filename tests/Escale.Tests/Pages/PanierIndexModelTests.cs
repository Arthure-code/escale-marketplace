using Escale.Web.Pages.Panier;
using Microsoft.AspNetCore.Mvc;

namespace Escale.Tests.Pages
{
    public class PanierIndexModelTests
    {
        private readonly Mock<IPanierService> _panier = new Mock<IPanierService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);
        private readonly IndexModel _page;

        public PanierIndexModelTests()
        {
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _page = new IndexModel(_panier.Object, _utilisateur.Object);
        }

        [Fact]
        public async Task OnGetAsync_ChargeLePanierDeLUtilisateurConnecte()
        {
            //Etant donné
            Panier contenu = new Panier(new List<LignePanier>());
            _panier.Setup(s => s.ObtenirAsync("camille")).ReturnsAsync(contenu);

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Same(contenu, _page.Contenu);
            _panier.Verify(s => s.ObtenirAsync("camille"), Times.Once);
        }

        [Fact]
        public async Task OnPostRetirerAsync_RetireLaLigneEtRecharge()
        {
            //Etant donné
            _panier.Setup(s => s.RetirerAsync("camille", 5)).Returns(Task.CompletedTask);

            //Lorsque
            IActionResult resultat = await _page.OnPostRetirerAsync(5);

            //Alors
            Assert.IsType<RedirectToPageResult>(resultat);
            _panier.Verify(s => s.RetirerAsync("camille", 5), Times.Once);
        }

        [Fact]
        public async Task OnPostViderAsync_VideLePanierDeLUtilisateurConnecte()
        {
            //Etant donné
            _panier.Setup(s => s.ViderAsync("camille")).Returns(Task.CompletedTask);

            //Lorsque
            IActionResult resultat = await _page.OnPostViderAsync();

            //Alors
            Assert.IsType<RedirectToPageResult>(resultat);
            _panier.Verify(s => s.ViderAsync("camille"), Times.Once);
        }
    }
}
