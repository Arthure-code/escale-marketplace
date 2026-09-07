using Escale.Web.Pages.Commandes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Escale.Tests.Pages
{
    public class PaiementModelTests
    {
        private readonly Mock<IPanierService> _panier = new Mock<IPanierService>(MockBehavior.Strict);
        private readonly Mock<ICommandeService> _commandes = new Mock<ICommandeService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);

        private static readonly DateTime Debut = new DateTime(2026, 10, 2);
        private static readonly DateTime Fin = new DateTime(2026, 10, 6);

        public PaiementModelTests()
        {
            _utilisateur.SetupGet(u => u.Id).Returns("camille");
        }

        private PaiementModel Page()
        {
            ModelStateDictionary etat = new ModelStateDictionary();
            ActionContext contexte = new ActionContext(
                new DefaultHttpContext(), new RouteData(), new PageActionDescriptor(), etat);

            return new PaiementModel(_panier.Object, _commandes.Object, _utilisateur.Object)
            {
                PageContext = new PageContext(contexte)
                {
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), etat)
                },
                Saisie = new PaiementModel.Carte
                {
                    Titulaire = "Camille Roy",
                    Numero = "4242424242424242",
                    Expiration = "12/30",
                    Cvc = "123"
                }
            };
        }

        private void PanierContient(params int[] prix)
        {
            List<LignePanier> lignes = prix.Select((p, i) => new LignePanier
            {
                Id = i + 1,
                AnnonceId = i + 1,
                DateDebut = Debut,
                DateFin = Fin,
                Annonce = new Annonce { Id = i + 1, Titre = "Annonce " + (i + 1), PrixJournalier = p }
            }).ToList();

            _panier.Setup(s => s.ObtenirAsync("camille")).ReturnsAsync(new Panier(lignes));
        }

        [Fact]
        public async Task OnGetAsync_PanierVide_RedirigeVersLePanier()
        {
            //Etant donné
            PanierContient();
            PaiementModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnGetAsync();

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Panier/Index", redirection.PageName);
        }

        [Fact]
        public async Task OnGetAsync_PanierGarni_PreRemplitLeTitulaire()
        {
            //Etant donné
            PanierContient(120);
            _utilisateur.Setup(u => u.ObtenirAsync())
                .ReturnsAsync(new Utilisateur { NomComplet = "Camille Roy" });
            PaiementModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnGetAsync();

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.Equal("Camille Roy", page.Saisie.Titulaire);
            Assert.Equal(480, page.Contenu.Total);
        }

        [Fact]
        public async Task OnPostAsync_ModelStateInvalide_RendLaPageSansPasserCommande()
        {
            //Etant donné
            PanierContient(120);
            PaiementModel page = Page();
            page.ModelState.AddModelError("Saisie.Numero", "Le numéro de carte est obligatoire.");

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            Assert.IsType<PageResult>(resultat);
            _commandes.Verify(s => s.PasserAsync(It.IsAny<string>(), It.IsAny<DonneesCarte>()), Times.Never);
        }

        [Fact]
        public async Task OnPostAsync_PanierVide_RedirigeVersLePanier()
        {
            //Etant donné
            PanierContient();
            PaiementModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Panier/Index", redirection.PageName);
            _commandes.Verify(s => s.PasserAsync(It.IsAny<string>(), It.IsAny<DonneesCarte>()), Times.Never);
        }

        [Fact]
        public async Task OnPostAsync_CommandeRefusee_RendLaPageAvecLeMotif()
        {
            //Etant donné
            PanierContient(120);
            _commandes.Setup(s => s.PasserAsync("camille", It.IsAny<DonneesCarte>()))
                .ReturnsAsync(new ResultatCommande(false, "Paiement refusé par la banque émettrice.", string.Empty));
            PaiementModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.Equal("Paiement refusé par la banque émettrice.", page.Refus);
        }

        [Fact]
        public async Task OnPostAsync_CommandeAcceptee_RedirigeVersLaConfirmation()
        {
            //Etant donné
            PanierContient(120);
            _commandes.Setup(s => s.PasserAsync("camille", It.IsAny<DonneesCarte>()))
                .ReturnsAsync(new ResultatCommande(true, "Paiement accepté.", "ESC-260904-035206"));
            PaiementModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Commandes/Confirmation", redirection.PageName);
            Assert.Equal("ESC-260904-035206", redirection.RouteValues!["reference"]);
        }

        [Fact]
        public async Task OnPostAsync_TransmetLaCarteSaisieAuService()
        {
            //Etant donné
            PanierContient(120);
            DonneesCarte? envoyee = null;
            _commandes.Setup(s => s.PasserAsync("camille", It.IsAny<DonneesCarte>()))
                .Callback<string, DonneesCarte>((_, carte) => envoyee = carte)
                .ReturnsAsync(new ResultatCommande(true, "Paiement accepté.", "ESC-1"));
            PaiementModel page = Page();

            //Lorsque
            await page.OnPostAsync();

            //Alors
            Assert.Equal("4242424242424242", envoyee!.Numero);
            Assert.Equal("12/30", envoyee.Expiration);
        }
    }
}
