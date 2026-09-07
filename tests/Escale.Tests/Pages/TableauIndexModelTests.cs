using Escale.Web.Pages.Tableau;
using Microsoft.AspNetCore.Mvc;

namespace Escale.Tests.Pages
{
    public class TableauIndexModelTests
    {
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly Mock<ICommandeService> _commandes = new Mock<ICommandeService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);
        private readonly Mock<IDiffusionService> _diffusion = new Mock<IDiffusionService>(MockBehavior.Strict);
        private readonly IndexModel _page;

        public TableauIndexModelTests()
        {
            _utilisateur.SetupGet(u => u.Id).Returns("marie");
            _utilisateur.Setup(u => u.ObtenirAsync()).ReturnsAsync(Loueur());
            EnRegle();
            _page = new IndexModel(_annonces.Object, _commandes.Object, _utilisateur.Object, _diffusion.Object);
        }

        // Le tableau de bord ne calcule plus la règle : il la lit.
        private void EnRegle()
        {
            _diffusion.Setup(s => s.EstDiffusable(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(true);
            _diffusion.Setup(s => s.Etat(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(EtatCompte.Actif);
        }

        private void EnDefaut(EtatCompte etat)
        {
            _diffusion.Setup(s => s.EstDiffusable(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(false);
            _diffusion.Setup(s => s.Etat(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(etat);
        }

        private static Utilisateur Loueur() => new Utilisateur
        {
            Id = "marie",
            NomComplet = "Marie Tremblay",
            Abonnement = new Abonnement { Echeance = DateTime.Today.AddMonths(1) }
        };

        private static Annonce Annonce(int id, bool visible) => new Annonce
        {
            Id = id,
            Titre = "Chambre " + id,
            LoueurId = "marie",
            EstDisponible = visible
        };

        private static LigneCommande Vente(int sousTotal, int jours) => new LigneCommande
        {
            LoueurId = "marie",
            Titre = "Suite avec balcon",
            SousTotal = sousTotal,
            NombreDeJours = jours
        };

        private void Donnees(List<Annonce> annonces, List<LigneCommande> ventes)
        {
            _annonces.Setup(s => s.ObtenirDuLoueurAsync("marie")).ReturnsAsync(annonces);
            _commandes.Setup(s => s.ObtenirVentesAsync("marie")).ReturnsAsync(ventes);
        }

        [Fact]
        public async Task OnGetAsync_NeDemandeQueLesDonneesDuLoueurConnecte()
        {
            //Etant donné
            Donnees(new List<Annonce>(), new List<LigneCommande>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            _annonces.Verify(s => s.ObtenirDuLoueurAsync("marie"), Times.Once);
            _commandes.Verify(s => s.ObtenirVentesAsync("marie"), Times.Once);
        }

        [Fact]
        public async Task EnLigne_NeCompteQueLesAnnoncesVisibles()
        {
            //Etant donné
            Donnees(new List<Annonce>
            {
                Annonce(1, visible: true),
                Annonce(2, visible: false),
                Annonce(3, visible: true)
            }, new List<LigneCommande>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(3, _page.Annonces.Count);
            Assert.Equal(2, _page.EnLigne);
        }

        [Fact]
        public async Task EnLigne_CompteBloque_EstNulEtLAlerteEstAffichee()
        {
            //Etant donné
            EnDefaut(new EtatCompte(SituationCompte.Bloque, DateTime.Today.AddDays(10), "Annonce non conforme"));
            Donnees(new List<Annonce> { Annonce(1, visible: true) }, new List<LigneCommande>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(0, _page.EnLigne);
            Assert.Contains("bloqué", _page.Alerte);
        }

        [Fact]
        public async Task EnLigne_AbonnementEchu_EstNulEtLAlerteEstAffichee()
        {
            //Etant donné
            EnDefaut(new EtatCompte(SituationCompte.AbonnementEchu, DateTime.Today.AddDays(-3), null));
            Donnees(new List<Annonce> { Annonce(1, visible: true) }, new List<LigneCommande>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(0, _page.EnLigne);
            Assert.Contains("abonnement est échu", _page.Alerte);
        }

        [Fact]
        public async Task CompteEnRegle_NAffichePasDAlerte()
        {
            //Etant donné
            Donnees(new List<Annonce>(), new List<LigneCommande>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Null(_page.Alerte);
        }

        [Fact]
        public async Task Revenu_AdditionneLesSousTotauxDesVentes()
        {
            //Etant donné
            Donnees(new List<Annonce>(), new List<LigneCommande> { Vente(480, 4), Vente(300, 4) });

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(780, _page.Revenu);
            Assert.Equal(8, _page.JoursLoues);
        }

        [Fact]
        public async Task Revenu_SansVente_EstNul()
        {
            //Etant donné
            Donnees(new List<Annonce>(), new List<LigneCommande>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(0, _page.Revenu);
            Assert.Equal(0, _page.JoursLoues);
        }

        [Fact]
        public async Task OnPostBasculerAsync_BasculeAuNomDuLoueurConnecteSansPasseAdministrateur()
        {
            //Etant donné
            _annonces.Setup(s => s.BasculerVisibiliteAsync(7, "marie", false)).ReturnsAsync(true);

            //Lorsque
            IActionResult resultat = await _page.OnPostBasculerAsync(7);

            //Alors
            Assert.IsType<RedirectToPageResult>(resultat);
            _annonces.Verify(s => s.BasculerVisibiliteAsync(7, "marie", false), Times.Once);
        }
    }
}
