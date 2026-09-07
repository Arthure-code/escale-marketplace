using Escale.Web.Pages.Commandes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Tests.Pages
{
    public class ConfirmationModelTests
    {
        private readonly Mock<ICommandeService> _commandes = new Mock<ICommandeService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);
        private readonly ConfirmationModel _page;

        public ConfirmationModelTests()
        {
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
            _page = new ConfirmationModel(_commandes.Object, _utilisateur.Object);
        }

        [Fact]
        public async Task OnGetAsync_CommandeInconnue_RendNotFound()
        {
            //Etant donné
            _commandes.Setup(s => s.ObtenirAsync("ESC-000", "camille")).ReturnsAsync((Commande?)null);

            //Lorsque
            IActionResult resultat = await _page.OnGetAsync("ESC-000");

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task OnGetAsync_CommandeDUnAutreClient_RendNotFound()
        {
            //Etant donné
            _commandes.Setup(s => s.ObtenirAsync("ESC-123", "camille")).ReturnsAsync((Commande?)null);

            //Lorsque
            IActionResult resultat = await _page.OnGetAsync("ESC-123");

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
            _commandes.Verify(s => s.ObtenirAsync("ESC-123", "camille"), Times.Once);
        }

        [Fact]
        public async Task OnGetAsync_CommandeDuClient_RemplitLePageModel()
        {
            //Etant donné
            Commande commande = new Commande
            {
                Reference = "ESC-123",
                UtilisateurId = "camille",
                QuatreDerniers = "4242",
                Lignes = new List<LigneCommande>
                {
                    new LigneCommande { Titre = "Suite avec balcon", SousTotal = 480 },
                    new LigneCommande { Titre = "Ford Mustang", SousTotal = 1400 }
                }
            };
            _commandes.Setup(s => s.ObtenirAsync("ESC-123", "camille")).ReturnsAsync(commande);

            //Lorsque
            IActionResult resultat = await _page.OnGetAsync("ESC-123");

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.Equal("ESC-123", _page.Commande.Reference);
            Assert.Equal(1880, _page.Commande.Total);
        }
    }
}
