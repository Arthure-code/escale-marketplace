using Escale.Web.Pages.Administration;

namespace Escale.Tests.Pages
{
    public class AdministrationIndexModelTests
    {
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurService> _comptes = new Mock<IUtilisateurService>(MockBehavior.Strict);
        private readonly Mock<ICommandeService> _commandes = new Mock<ICommandeService>(MockBehavior.Strict);
        private readonly Mock<IDiffusionService> _diffusion = new Mock<IDiffusionService>(MockBehavior.Strict);
        private readonly IndexModel _page;

        public AdministrationIndexModelTests()
        {
            _page = new IndexModel(_annonces.Object, _comptes.Object, _commandes.Object, _diffusion.Object);
            _diffusion.Setup(d => d.EstDiffusable(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(true);
        }

        private static Annonce Annonce(int id, bool disponible = true) => new Annonce
        {
            Id = id,
            Titre = "Annonce",
            Description = "Description",
            Photo = "p.jpg",
            EstDisponible = disponible,
            Loueur = new Utilisateur { NomComplet = "Marie" }
        };

        private static Utilisateur Compte(string id) => new Utilisateur { Id = id, NomComplet = id };

        private void Plateforme(List<Annonce> annonces, List<Utilisateur> comptes,
            Dictionary<string, string> roles, params LigneCommande[] ventes)
        {
            _annonces.Setup(a => a.ObtenirToutesAsync()).ReturnsAsync(annonces);
            _comptes.Setup(c => c.ObtenirTousAsync()).ReturnsAsync(comptes);
            _comptes.Setup(c => c.ObtenirRolesAsync()).ReturnsAsync(roles);
            foreach (Utilisateur compte in comptes)
            {
                _commandes.Setup(c => c.ObtenirVentesAsync(compte.Id))
                    .ReturnsAsync(ventes.Where(v => v.LoueurId == compte.Id).ToList());
            }
        }

        [Fact]
        public async Task OnGetAsync_ChargeToutesLesAnnoncesSansFiltrerParProprietaire()
        {
            //Etant donné deux loueurs différents
            Plateforme(
                new List<Annonce> { Annonce(1), Annonce(2) },
                new List<Utilisateur> { Compte("marie"), Compte("hugo") },
                new Dictionary<string, string>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors l'administrateur voit la plateforme entière
            Assert.Equal(2, _page.Annonces.Count);
            Assert.Equal(2, _page.Comptes.Count);
        }

        [Fact]
        public async Task EnLigne_NeCompteQueCeQuiEstVisibleEtDiffusable()
        {
            //Etant donné une annonce en ligne et une retirée par son loueur
            Plateforme(
                new List<Annonce> { Annonce(1, disponible: true), Annonce(2, disponible: false) },
                new List<Utilisateur>(),
                new Dictionary<string, string>());

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(1, _page.EnLigne);
            Assert.Equal(1, _page.Retirees);
        }

        [Fact]
        public async Task EnLigne_EcarteLesAnnoncesDUnLoueurNonDiffusable()
        {
            //Etant donné un loueur bloqué
            Plateforme(
                new List<Annonce> { Annonce(1) },
                new List<Utilisateur>(),
                new Dictionary<string, string>());
            _diffusion.Setup(d => d.EstDiffusable(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(false);

            //Lorsque
            await _page.OnGetAsync();

            //Alors l'annonce compte comme retirée, sans avoir été supprimée
            Assert.Equal(0, _page.EnLigne);
            Assert.Equal(1, _page.Retirees);
        }

        [Fact]
        public async Task Loueurs_CompteLesComptesPortantLeRoleLoueur()
        {
            //Etant donné
            Plateforme(
                new List<Annonce>(),
                new List<Utilisateur>(),
                new Dictionary<string, string>
                {
                    ["marie"] = Utilisateur.RoleLoueur,
                    ["hugo"] = Utilisateur.RoleLoueur,
                    ["camille"] = Utilisateur.RoleVoyageur
                });

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(2, _page.Loueurs);
        }

        [Fact]
        public async Task Bloques_CompteLesComptesQueLesReglesEcartent()
        {
            //Etant donné deux comptes dont un seul est diffusable
            Utilisateur marie = Compte("marie");
            Utilisateur hugo = Compte("hugo");
            Plateforme(new List<Annonce>(), new List<Utilisateur> { marie, hugo },
                new Dictionary<string, string>());
            _diffusion.Setup(d => d.EstDiffusable(marie, It.IsAny<DateTime>())).Returns(true);
            _diffusion.Setup(d => d.EstDiffusable(hugo, It.IsAny<DateTime>())).Returns(false);

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Equal(1, _page.Bloques);
        }

        [Fact]
        public async Task OnGetAsync_RassembleLesVentesDeTousLesLoueurs()
        {
            //Etant donné une vente chez chacun
            LigneCommande vente1 = new LigneCommande
            {
                LoueurId = "marie", SousTotal = 220, DateDebut = new DateTime(2026, 12, 10)
            };
            LigneCommande vente2 = new LigneCommande
            {
                LoueurId = "hugo", SousTotal = 2000, DateDebut = new DateTime(2026, 12, 20)
            };
            Plateforme(new List<Annonce>(),
                new List<Utilisateur> { Compte("marie"), Compte("hugo") },
                new Dictionary<string, string>(), vente1, vente2);

            //Lorsque
            await _page.OnGetAsync();

            //Alors les deux sont réunies et le volume est additionné
            Assert.Equal(2, _page.Reservations.Count);
            Assert.Equal(2220, _page.Volume);
        }

        [Fact]
        public async Task OnGetAsync_ClasseLesReservationsDeLaPlusRecenteALaPlusAncienne()
        {
            //Etant donné deux réservations à des dates différentes
            LigneCommande ancienne = new LigneCommande { LoueurId = "marie", DateDebut = new DateTime(2026, 1, 5) };
            LigneCommande recente = new LigneCommande { LoueurId = "marie", DateDebut = new DateTime(2026, 12, 20) };
            Plateforme(new List<Annonce>(), new List<Utilisateur> { Compte("marie") },
                new Dictionary<string, string>(), ancienne, recente);

            //Lorsque
            await _page.OnGetAsync();

            //Alors
            Assert.Same(recente, _page.Reservations[0]);
            Assert.Same(ancienne, _page.Reservations[1]);
        }
    }
}
