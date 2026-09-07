namespace Escale.Tests.Services
{
    public class AnnonceServiceTests
    {
        private static readonly DateTime Arrivee = new DateTime(2026, 12, 10);
        private static readonly DateTime Depart = new DateTime(2026, 12, 14);

        private readonly Mock<IAnnonceRepository> _annonces = new Mock<IAnnonceRepository>(MockBehavior.Strict);
        private readonly Mock<ICommandeRepository> _commandes = new Mock<ICommandeRepository>(MockBehavior.Strict);
        private readonly Mock<IDiffusionService> _diffusion = new Mock<IDiffusionService>(MockBehavior.Strict);
        private readonly AnnonceService _service;

        public AnnonceServiceTests()
        {
            _service = new AnnonceService(_annonces.Object, _commandes.Object, _diffusion.Object);
            _diffusion.Setup(d => d.EstDiffusable(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(true);
        }

        private static Annonce Annonce(int id = 1, int exemplaires = 1, int prix = 100,
            bool disponible = true, CategorieAnnonce categorie = CategorieAnnonce.Chambre) =>
            new Annonce
            {
                Id = id,
                Titre = "Chambre",
                Description = "Description",
                Photo = "chambre-1.jpg",
                PrixJournalier = prix,
                Exemplaires = exemplaires,
                EstDisponible = disponible,
                Categorie = categorie,
                Loueur = new Utilisateur { NomComplet = "Marie" }
            };

        [Fact]
        public async Task RechercherAsync_RetrancheLesReservationsQuiChevauchentLesDates()
        {
            //Etant donné un parc de quatre, dont trois déjà retenus
            _annonces.Setup(a => a.ObtenirVisiblesAsync(null))
                .ReturnsAsync(new List<Annonce> { Annonce(exemplaires: 4) });
            _commandes.Setup(c => c.CompterReservationsAsync(Arrivee, Depart))
                .ReturnsAsync(new Dictionary<int, int> { [1] = 3 });

            //Lorsque
            List<Offre> offres = await _service.RechercherAsync(null, Arrivee, Depart);

            //Alors
            Assert.Equal(4, offres[0].Total);
            Assert.Equal(1, offres[0].Restants);
        }

        [Fact]
        public async Task RechercherAsync_NeDescendJamaisSousZeroRestant()
        {
            //Etant donné plus de réservations que d'exemplaires, cas limite
            _annonces.Setup(a => a.ObtenirVisiblesAsync(null))
                .ReturnsAsync(new List<Annonce> { Annonce(exemplaires: 2) });
            _commandes.Setup(c => c.CompterReservationsAsync(Arrivee, Depart))
                .ReturnsAsync(new Dictionary<int, int> { [1] = 5 });

            //Lorsque
            List<Offre> offres = await _service.RechercherAsync(null, Arrivee, Depart);

            //Alors
            Assert.Equal(0, offres[0].Restants);
            Assert.True(offres[0].EstComplete);
        }

        [Fact]
        public async Task RechercherAsync_GardeUneAnnonceCompleteDansLesResultats()
        {
            //Etant donné une annonce entièrement retenue
            _annonces.Setup(a => a.ObtenirVisiblesAsync(null))
                .ReturnsAsync(new List<Annonce> { Annonce(exemplaires: 1) });
            _commandes.Setup(c => c.CompterReservationsAsync(Arrivee, Depart))
                .ReturnsAsync(new Dictionary<int, int> { [1] = 1 });

            //Lorsque
            List<Offre> offres = await _service.RechercherAsync(null, Arrivee, Depart);

            //Alors elle reste visible, marquée complète, plutôt que de disparaître
            Assert.Single(offres);
            Assert.True(offres[0].EstComplete);
        }

        [Fact]
        public async Task RechercherAsync_ClasseLesCompletesEnDernierPuisParPrix()
        {
            //Etant donné une annonce chère et libre, une bon marché et complète
            _annonces.Setup(a => a.ObtenirVisiblesAsync(null)).ReturnsAsync(new List<Annonce>
            {
                Annonce(id: 1, exemplaires: 1, prix: 50),
                Annonce(id: 2, exemplaires: 1, prix: 300),
                Annonce(id: 3, exemplaires: 1, prix: 100)
            });
            _commandes.Setup(c => c.CompterReservationsAsync(Arrivee, Depart))
                .ReturnsAsync(new Dictionary<int, int> { [1] = 1 });

            //Lorsque
            List<Offre> offres = await _service.RechercherAsync(null, Arrivee, Depart);

            //Alors les libres viennent d'abord, du moins cher au plus cher
            Assert.Equal(new[] { 3, 2, 1 }, offres.Select(o => o.Annonce.Id));
        }

        [Fact]
        public async Task RechercherAsync_EcarteLesAnnoncesDUnCompteNonDiffusable()
        {
            //Etant donné un loueur bloqué
            _annonces.Setup(a => a.ObtenirVisiblesAsync(null))
                .ReturnsAsync(new List<Annonce> { Annonce() });
            _commandes.Setup(c => c.CompterReservationsAsync(Arrivee, Depart))
                .ReturnsAsync(new Dictionary<int, int>());
            _diffusion.Setup(d => d.EstDiffusable(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(false);

            //Lorsque
            List<Offre> offres = await _service.RechercherAsync(null, Arrivee, Depart);

            //Alors ses annonces quittent le site sans être supprimées
            Assert.Empty(offres);
        }

        [Fact]
        public async Task RechercherAsync_ConsidereQuUneAnnonceSansReservationEstEntiereementLibre()
        {
            //Etant donné aucune réservation sur la période
            _annonces.Setup(a => a.ObtenirVisiblesAsync(null))
                .ReturnsAsync(new List<Annonce> { Annonce(exemplaires: 6) });
            _commandes.Setup(c => c.CompterReservationsAsync(Arrivee, Depart))
                .ReturnsAsync(new Dictionary<int, int>());

            //Lorsque
            List<Offre> offres = await _service.RechercherAsync(null, Arrivee, Depart);

            //Alors
            Assert.Equal(6, offres[0].Restants);
        }

        [Fact]
        public async Task ObtenirPubliqueAsync_RefuseUneAnnonceRetireeDuSite()
        {
            //Etant donné une annonce masquée par son loueur
            _annonces.Setup(a => a.ObtenirAsync(1)).ReturnsAsync(Annonce(disponible: false));

            //Lorsque
            Annonce? annonce = await _service.ObtenirPubliqueAsync(1);

            //Alors
            Assert.Null(annonce);
        }

        [Fact]
        public async Task ObtenirPubliqueAsync_RefuseUneAnnonceDUnCompteNonDiffusable()
        {
            //Etant donné
            _annonces.Setup(a => a.ObtenirAsync(1)).ReturnsAsync(Annonce());
            _diffusion.Setup(d => d.EstDiffusable(It.IsAny<Utilisateur>(), It.IsAny<DateTime>())).Returns(false);

            //Lorsque
            Annonce? annonce = await _service.ObtenirPubliqueAsync(1);

            //Alors
            Assert.Null(annonce);
        }

        [Fact]
        public async Task ObtenirOffreAsync_RendNulQuandLAnnonceNEstPasPublique()
        {
            //Etant donné une annonce inexistante
            _annonces.Setup(a => a.ObtenirAsync(9)).ReturnsAsync((Annonce?)null);

            //Lorsque
            Offre? offre = await _service.ObtenirOffreAsync(9, Arrivee, Depart);

            //Alors
            Assert.Null(offre);
        }

        [Fact]
        public async Task EstReservableAsync_FauxQuandTousLesExemplairesSontRetenus()
        {
            //Etant donné
            _annonces.Setup(a => a.ObtenirAsync(1)).ReturnsAsync(Annonce(exemplaires: 2));
            _commandes.Setup(c => c.CompterReservationsAsync(1, Arrivee, Depart)).ReturnsAsync(2);

            //Lorsque
            bool reservable = await _service.EstReservableAsync(1, Arrivee, Depart);

            //Alors
            Assert.False(reservable);
        }

        [Fact]
        public async Task EstReservableAsync_VraiTantQuIlResteUnExemplaire()
        {
            //Etant donné
            _annonces.Setup(a => a.ObtenirAsync(1)).ReturnsAsync(Annonce(exemplaires: 2));
            _commandes.Setup(c => c.CompterReservationsAsync(1, Arrivee, Depart)).ReturnsAsync(1);

            //Lorsque
            bool reservable = await _service.EstReservableAsync(1, Arrivee, Depart);

            //Alors
            Assert.True(reservable);
        }

        [Fact]
        public async Task EstReservableAsync_FauxPourUneAnnonceInconnue()
        {
            //Etant donné
            _annonces.Setup(a => a.ObtenirAsync(9)).ReturnsAsync((Annonce?)null);

            //Lorsque
            bool reservable = await _service.EstReservableAsync(9, Arrivee, Depart);

            //Alors on n'interroge même pas les commandes
            Assert.False(reservable);
            _commandes.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PublierAsync_PoseLeLoueurAppelantEtNonCeluiQuiEstSaisi()
        {
            //Etant donné une annonce qui prétend appartenir à quelqu'un d'autre
            Annonce annonce = Annonce();
            annonce.LoueurId = "voleur";
            _annonces.Setup(a => a.AjouterAsync(annonce)).Returns(Task.CompletedTask);

            //Lorsque
            await _service.PublierAsync(annonce, "marie", new[] { 1, 2 });

            //Alors l'identité vient de l'appelant
            Assert.Equal("marie", annonce.LoueurId);
            Assert.Equal(2, annonce.Equipements.Count);
        }

        [Fact]
        public async Task PublierAsync_EffaceLesChampsQuiNeConcernentPasLaCategorie()
        {
            //Etant donné une chambre à laquelle on a rempli des champs de voiture
            Annonce annonce = Annonce(categorie: CategorieAnnonce.Chambre);
            annonce.Marque = "Kia";
            annonce.Annee = 2021;
            annonce.Places = 5;
            annonce.Superficie = 22;
            _annonces.Setup(a => a.AjouterAsync(annonce)).Returns(Task.CompletedTask);

            //Lorsque
            await _service.PublierAsync(annonce, "marie", Array.Empty<int>());

            //Alors
            Assert.Null(annonce.Marque);
            Assert.Null(annonce.Annee);
            Assert.Null(annonce.Places);
            Assert.Equal(22, annonce.Superficie);
        }

        [Fact]
        public async Task PublierAsync_EffaceLesChampsDeChambreSurUneVoiture()
        {
            //Etant donné
            Annonce annonce = Annonce(categorie: CategorieAnnonce.Voiture);
            annonce.Superficie = 22;
            annonce.Couchages = 2;
            annonce.Marque = "Kia";
            _annonces.Setup(a => a.AjouterAsync(annonce)).Returns(Task.CompletedTask);

            //Lorsque
            await _service.PublierAsync(annonce, "hugo", Array.Empty<int>());

            //Alors
            Assert.Null(annonce.Superficie);
            Assert.Null(annonce.Couchages);
            Assert.Equal("Kia", annonce.Marque);
        }

        [Fact]
        public async Task ModifierAsync_RefuseQuandLAnnonceNAppartientPasAuDemandeur()
        {
            //Etant donné une annonce d'un autre loueur, donc introuvable pour lui
            _annonces.Setup(a => a.ObtenirDuLoueurAsync(1, "hugo")).ReturnsAsync((Annonce?)null);

            //Lorsque
            bool modifiee = await _service.ModifierAsync(Annonce(), "hugo", Array.Empty<int>());

            //Alors rien n'est enregistré
            Assert.False(modifiee);
            _annonces.Verify(a => a.EnregistrerAsync(), Times.Never);
        }

        [Fact]
        public async Task ModifierAsync_RemplaceLesChampsEtLesEquipements()
        {
            //Etant donné une annonce existante et une saisie différente
            Annonce existante = Annonce(prix: 100);
            existante.Equipements.Add(new AnnonceEquipement { EquipementId = 9 });
            Annonce saisie = Annonce(prix: 250);
            saisie.Titre = "Nouveau titre";

            _annonces.Setup(a => a.ObtenirDuLoueurAsync(1, "marie")).ReturnsAsync(existante);
            _annonces.Setup(a => a.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            bool modifiee = await _service.ModifierAsync(saisie, "marie", new[] { 1, 2, 3 });

            //Alors
            Assert.True(modifiee);
            Assert.Equal(250, existante.PrixJournalier);
            Assert.Equal("Nouveau titre", existante.Titre);
            Assert.Equal(new[] { 1, 2, 3 }, existante.Equipements.Select(e => e.EquipementId));
        }

        [Fact]
        public async Task ModifierAsync_PasseParLaVoieSansRestrictionPourUnAdministrateur()
        {
            //Etant donné un administrateur, qui n'est pas le loueur
            Annonce existante = Annonce();
            _annonces.Setup(a => a.ObtenirAsync(1)).ReturnsAsync(existante);
            _annonces.Setup(a => a.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            bool modifiee = await _service.ModifierAsync(Annonce(), "admin", Array.Empty<int>(), sansRestriction: true);

            //Alors la recherche ne filtre pas sur le loueur
            Assert.True(modifiee);
            _annonces.Verify(a => a.ObtenirDuLoueurAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task BasculerVisibiliteAsync_InverseLEtatEtEnregistre()
        {
            //Etant donné une annonce en ligne
            Annonce annonce = Annonce(disponible: true);
            _annonces.Setup(a => a.ObtenirDuLoueurAsync(1, "marie")).ReturnsAsync(annonce);
            _annonces.Setup(a => a.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            bool bascule = await _service.BasculerVisibiliteAsync(1, "marie");

            //Alors
            Assert.True(bascule);
            Assert.False(annonce.EstDisponible);
        }

        [Fact]
        public async Task BasculerVisibiliteAsync_RefuseSurUneAnnonceQuiNEstPasAuDemandeur()
        {
            //Etant donné
            _annonces.Setup(a => a.ObtenirDuLoueurAsync(1, "hugo")).ReturnsAsync((Annonce?)null);

            //Lorsque
            bool bascule = await _service.BasculerVisibiliteAsync(1, "hugo");

            //Alors
            Assert.False(bascule);
            _annonces.Verify(a => a.EnregistrerAsync(), Times.Never);
        }

        [Fact]
        public async Task SupprimerAsync_RefuseUneAnnonceDejaReservee()
        {
            //Etant donné une annonce portée par une commande
            _annonces.Setup(a => a.ObtenirDuLoueurAsync(1, "marie")).ReturnsAsync(Annonce());
            _commandes.Setup(c => c.AnnonceEstReserveeAsync(1)).ReturnsAsync(true);

            //Lorsque
            bool supprimee = await _service.SupprimerAsync(1, "marie");

            //Alors elle reste en base pour que la commande garde sa référence
            Assert.False(supprimee);
            _annonces.Verify(a => a.SupprimerAsync(It.IsAny<Annonce>()), Times.Never);
        }

        [Fact]
        public async Task SupprimerAsync_SupprimeUneAnnonceJamaisReservee()
        {
            //Etant donné
            Annonce annonce = Annonce();
            _annonces.Setup(a => a.ObtenirDuLoueurAsync(1, "marie")).ReturnsAsync(annonce);
            _commandes.Setup(c => c.AnnonceEstReserveeAsync(1)).ReturnsAsync(false);
            _annonces.Setup(a => a.SupprimerAsync(annonce)).Returns(Task.CompletedTask);

            //Lorsque
            bool supprimee = await _service.SupprimerAsync(1, "marie");

            //Alors
            Assert.True(supprimee);
            _annonces.Verify(a => a.SupprimerAsync(annonce), Times.Once);
        }
    }
}
