namespace Escale.Tests.Services
{
    public class PanierServiceTests
    {
        private const string Utilisateur = "camille";

        private static readonly DateTime Arrivee = new DateTime(2026, 12, 10);
        private static readonly DateTime Depart = new DateTime(2026, 12, 14);

        private readonly Mock<IPanierRepository> _depot = new Mock<IPanierRepository>(MockBehavior.Strict);
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly PanierService _service;

        public PanierServiceTests()
        {
            _service = new PanierService(_depot.Object, _annonces.Object);
        }

        [Fact]
        public async Task AjouterAsync_RefuseUnDepartAvantLArrivee()
        {
            //Etant donné des dates inversées
            //Lorsque
            ResultatAjout resultat = await _service.AjouterAsync(Utilisateur, 1, Depart, Arrivee);

            //Alors rien n'est demandé au dépôt : le refus est décidé avant
            Assert.False(resultat.Reussi);
            Assert.Contains("départ", resultat.Message);
            _depot.VerifyNoOtherCalls();
            _annonces.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AjouterAsync_RefuseUnSejourDeDureeNulle()
        {
            //Etant donné une arrivée et un départ le même jour
            //Lorsque
            ResultatAjout resultat = await _service.AjouterAsync(Utilisateur, 1, Arrivee, Arrivee);

            //Alors
            Assert.False(resultat.Reussi);
            _depot.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AjouterAsync_RefuseUneAnnonceIndisponibleSurCesDates()
        {
            //Etant donné une annonce complète sur la période
            _annonces.Setup(a => a.EstReservableAsync(7, Arrivee, Depart)).ReturnsAsync(false);

            //Lorsque
            ResultatAjout resultat = await _service.AjouterAsync(Utilisateur, 7, Arrivee, Depart);

            //Alors on n'écrit rien
            Assert.False(resultat.Reussi);
            Assert.Contains("libre", resultat.Message);
            _depot.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AjouterAsync_RefuseUnDoublonDansLePanier()
        {
            //Etant donné une annonce déjà présente
            _annonces.Setup(a => a.EstReservableAsync(7, Arrivee, Depart)).ReturnsAsync(true);
            _depot.Setup(d => d.ContientAsync(Utilisateur, 7)).ReturnsAsync(true);

            //Lorsque
            ResultatAjout resultat = await _service.AjouterAsync(Utilisateur, 7, Arrivee, Depart);

            //Alors
            Assert.False(resultat.Reussi);
            Assert.Contains("déjà", resultat.Message);
            _depot.Verify(d => d.AjouterAsync(It.IsAny<LignePanier>()), Times.Never);
        }

        [Fact]
        public async Task AjouterAsync_EcritLaLigneQuandTouteLesConditionsSontReunies()
        {
            //Etant donné
            LignePanier? ecrite = null;
            _annonces.Setup(a => a.EstReservableAsync(7, Arrivee, Depart)).ReturnsAsync(true);
            _depot.Setup(d => d.ContientAsync(Utilisateur, 7)).ReturnsAsync(false);
            _depot.Setup(d => d.AjouterAsync(It.IsAny<LignePanier>()))
                .Callback<LignePanier>(ligne => ecrite = ligne)
                .Returns(Task.CompletedTask);

            //Lorsque
            ResultatAjout resultat = await _service.AjouterAsync(Utilisateur, 7, Arrivee, Depart);

            //Alors la ligne porte le propriétaire, l'annonce et les dates
            Assert.True(resultat.Reussi);
            Assert.NotNull(ecrite);
            Assert.Equal(Utilisateur, ecrite!.UtilisateurId);
            Assert.Equal(7, ecrite.AnnonceId);
            Assert.Equal(Arrivee, ecrite.DateDebut);
            Assert.Equal(Depart, ecrite.DateFin);
        }

        [Fact]
        public async Task AjouterAsync_NeGardeQueLaDateEtPasLHeure()
        {
            //Etant donné des dates portant une heure
            LignePanier? ecrite = null;
            DateTime arrivee = Arrivee.AddHours(14);
            DateTime depart = Depart.AddHours(9);
            _annonces.Setup(a => a.EstReservableAsync(7, arrivee, depart)).ReturnsAsync(true);
            _depot.Setup(d => d.ContientAsync(Utilisateur, 7)).ReturnsAsync(false);
            _depot.Setup(d => d.AjouterAsync(It.IsAny<LignePanier>()))
                .Callback<LignePanier>(ligne => ecrite = ligne)
                .Returns(Task.CompletedTask);

            //Lorsque
            await _service.AjouterAsync(Utilisateur, 7, arrivee, depart);

            //Alors l'heure est écartée avant l'écriture
            Assert.Equal(Arrivee, ecrite!.DateDebut);
            Assert.Equal(Depart, ecrite.DateFin);
        }

        [Fact]
        public async Task RetirerAsync_SupprimeLaLigneQuandElleAppartientAuDemandeur()
        {
            //Etant donné
            LignePanier ligne = new LignePanier { Id = 3, UtilisateurId = Utilisateur };
            _depot.Setup(d => d.ObtenirLigneAsync(3, Utilisateur)).ReturnsAsync(ligne);
            _depot.Setup(d => d.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>())).Returns(Task.CompletedTask);

            //Lorsque
            await _service.RetirerAsync(Utilisateur, 3);

            //Alors
            _depot.Verify(d => d.SupprimerAsync(It.Is<IEnumerable<LignePanier>>(l => l.Single() == ligne)), Times.Once);
        }

        [Fact]
        public async Task RetirerAsync_NeSupprimeRienSiLaLigneNEstPasALui()
        {
            //Etant donné une ligne qui n'appartient pas au demandeur, donc introuvable
            _depot.Setup(d => d.ObtenirLigneAsync(3, Utilisateur)).ReturnsAsync((LignePanier?)null);

            //Lorsque
            await _service.RetirerAsync(Utilisateur, 3);

            //Alors
            _depot.Verify(d => d.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>()), Times.Never);
        }

        [Fact]
        public async Task ViderAsync_SupprimeToutesLesLignesDuDemandeur()
        {
            //Etant donné
            List<LignePanier> lignes = new List<LignePanier>
            {
                new LignePanier { Id = 1 },
                new LignePanier { Id = 2 }
            };
            _depot.Setup(d => d.ObtenirAsync(Utilisateur)).ReturnsAsync(lignes);
            _depot.Setup(d => d.SupprimerAsync(lignes)).Returns(Task.CompletedTask);

            //Lorsque
            await _service.ViderAsync(Utilisateur);

            //Alors
            _depot.Verify(d => d.SupprimerAsync(lignes), Times.Once);
        }

        [Fact]
        public async Task ObtenirAsync_EnveloppeLesLignesDansUnPanier()
        {
            //Etant donné
            List<LignePanier> lignes = new List<LignePanier> { new LignePanier { Id = 1 } };
            _depot.Setup(d => d.ObtenirAsync(Utilisateur)).ReturnsAsync(lignes);

            //Lorsque
            Panier panier = await _service.ObtenirAsync(Utilisateur);

            //Alors
            Assert.Same(lignes, panier.Lignes);
        }

        [Fact]
        public async Task CompterAsync_DelegueAuDepot()
        {
            //Etant donné
            _depot.Setup(d => d.CompterAsync(Utilisateur)).ReturnsAsync(4);

            //Lorsque
            int nombre = await _service.CompterAsync(Utilisateur);

            //Alors
            Assert.Equal(4, nombre);
        }
    }
}
