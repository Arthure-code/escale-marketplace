using Microsoft.Extensions.Logging.Abstractions;

namespace Escale.Tests.Services
{
    public class CommandeServiceTests
    {
        private const string Utilisateur = "camille";

        private static readonly DateTime Arrivee = new DateTime(2026, 12, 10);
        private static readonly DateTime Depart = new DateTime(2026, 12, 14);
        private static readonly DonneesCarte Carte =
            new DonneesCarte("Camille Roy", PaiementSimuleService.CarteAcceptee, "12/34", "123");

        private readonly Mock<IPanierRepository> _panier = new Mock<IPanierRepository>(MockBehavior.Strict);
        private readonly Mock<ICommandeRepository> _commandes = new Mock<ICommandeRepository>(MockBehavior.Strict);
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly Mock<IPaiementService> _paiement = new Mock<IPaiementService>(MockBehavior.Strict);
        private readonly Mock<IFactureService> _facture = new Mock<IFactureService>(MockBehavior.Strict);
        private readonly CommandeService _service;

        public CommandeServiceTests()
        {
            _service = new CommandeService(_panier.Object, _commandes.Object, _annonces.Object,
                _paiement.Object, _facture.Object, NullLogger<CommandeService>.Instance);
        }

        private static LignePanier Ligne(int annonceId = 1, int prix = 100) => new LignePanier
        {
            Id = annonceId,
            AnnonceId = annonceId,
            UtilisateurId = Utilisateur,
            DateDebut = Arrivee,
            DateFin = Depart,
            Annonce = new Annonce
            {
                Id = annonceId,
                LoueurId = "marie",
                Titre = "Chambre lumineuse",
                Categorie = CategorieAnnonce.Chambre,
                PrixJournalier = prix,
                Description = "Description",
                Photo = "chambre-1.jpg"
            }
        };

        private void PanierContient(params LignePanier[] lignes) =>
            _panier.Setup(p => p.ObtenirAsync(Utilisateur)).ReturnsAsync(lignes.ToList());

        private void ToutEstReservable() =>
            _annonces.Setup(a => a.EstReservableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(true);

        private void PaiementAccepte() =>
            _paiement.Setup(p => p.PayerAsync(It.IsAny<DemandePaiement>()))
                .ReturnsAsync(new ResultatPaiement(true, "Paiement accepté.", "PAY-1", "4242"));

        [Fact]
        public async Task PasserAsync_RefuseUnPanierVide()
        {
            //Etant donné
            PanierContient();

            //Lorsque
            ResultatCommande resultat = await _service.PasserAsync(Utilisateur, Carte);

            //Alors aucune passerelle n'est appelée
            Assert.False(resultat.Reussi);
            Assert.Contains("vide", resultat.Message);
            _paiement.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PasserAsync_NePaiePasSiUneLigneNEstPlusLibre()
        {
            //Etant donné une annonce prise entre-temps
            PanierContient(Ligne());
            _annonces.Setup(a => a.EstReservableAsync(1, Arrivee, Depart)).ReturnsAsync(false);

            //Lorsque
            ResultatCommande resultat = await _service.PasserAsync(Utilisateur, Carte);

            //Alors on s'arrête avant le paiement
            Assert.False(resultat.Reussi);
            Assert.Contains("Chambre lumineuse", resultat.Message);
            _paiement.VerifyNoOtherCalls();
            _commandes.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PasserAsync_NEcritRienQuandLePaiementEstRefuse()
        {
            //Etant donné
            PanierContient(Ligne());
            ToutEstReservable();
            _paiement.Setup(p => p.PayerAsync(It.IsAny<DemandePaiement>()))
                .ReturnsAsync(new ResultatPaiement(false, "Paiement refusé par la banque émettrice.", string.Empty, string.Empty));

            //Lorsque
            ResultatCommande resultat = await _service.PasserAsync(Utilisateur, Carte);

            //Alors ni commande ni vidage du panier
            Assert.False(resultat.Reussi);
            Assert.Contains("refusé", resultat.Message);
            _commandes.Verify(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>()), Times.Never);
            _panier.Verify(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>()), Times.Never);
        }

        [Fact]
        public async Task PasserAsync_FixeLaReferenceAvantDeDemanderLePaiement()
        {
            //Etant donné
            DemandePaiement? demande = null;
            PanierContient(Ligne(prix: 55));
            ToutEstReservable();
            _paiement.Setup(p => p.PayerAsync(It.IsAny<DemandePaiement>()))
                .Callback<DemandePaiement>(d => demande = d)
                .ReturnsAsync(new ResultatPaiement(true, "Paiement accepté.", "PAY-1", "4242"));
            _commandes.Setup(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>())).ReturnsAsync(true);
            _panier.Setup(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>())).Returns(Task.CompletedTask);
            _facture.Setup(f => f.EnvoyerAsync(It.IsAny<Commande>())).Returns(Task.CompletedTask);

            //Lorsque
            ResultatCommande resultat = await _service.PasserAsync(Utilisateur, Carte);

            //Alors la passerelle reçoit une référence qui sert de clé d'idempotence
            Assert.NotNull(demande);
            Assert.StartsWith("ESC-", demande!.Reference);
            Assert.Equal(demande.Reference, resultat.Reference);
            Assert.Equal(220, demande.Montant);
        }

        [Fact]
        public async Task PasserAsync_AbandonneSiLaDerniereUniteEstPriseALEcriture()
        {
            //Etant donné une écriture refusée par le garde transactionnel
            PanierContient(Ligne());
            ToutEstReservable();
            PaiementAccepte();
            _commandes.Setup(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>())).ReturnsAsync(false);

            //Lorsque
            ResultatCommande resultat = await _service.PasserAsync(Utilisateur, Carte);

            //Alors le panier reste intact et le client est prévenu
            Assert.False(resultat.Reussi);
            Assert.Contains("quelqu'un d'autre", resultat.Message);
            _panier.Verify(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>()), Times.Never);
        }

        [Fact]
        public async Task PasserAsync_FigeLesValeursDeLAnnonceDansLaCommande()
        {
            //Etant donné
            Commande? enregistree = null;
            PanierContient(Ligne(prix: 55));
            ToutEstReservable();
            PaiementAccepte();
            _commandes.Setup(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>()))
                .Callback<Commande>(c => enregistree = c)
                .ReturnsAsync(true);
            _panier.Setup(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>())).Returns(Task.CompletedTask);
            _facture.Setup(f => f.EnvoyerAsync(It.IsAny<Commande>())).Returns(Task.CompletedTask);

            //Lorsque
            await _service.PasserAsync(Utilisateur, Carte);

            //Alors titre, catégorie et tarif sont recopiés, pas référencés
            LigneCommande ligne = enregistree!.Lignes.Single();
            Assert.Equal("Chambre lumineuse", ligne.Titre);
            Assert.Equal(CategorieAnnonce.Chambre, ligne.Categorie);
            Assert.Equal(55, ligne.PrixJournalier);
            Assert.Equal(4, ligne.NombreDeJours);
            Assert.Equal(220, ligne.SousTotal);
            Assert.Equal("marie", ligne.LoueurId);
        }

        [Fact]
        public async Task PasserAsync_NeConserveQueLesQuatreDerniersChiffres()
        {
            //Etant donné
            Commande? enregistree = null;
            PanierContient(Ligne());
            ToutEstReservable();
            PaiementAccepte();
            _commandes.Setup(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>()))
                .Callback<Commande>(c => enregistree = c)
                .ReturnsAsync(true);
            _panier.Setup(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>())).Returns(Task.CompletedTask);
            _facture.Setup(f => f.EnvoyerAsync(It.IsAny<Commande>())).Returns(Task.CompletedTask);

            //Lorsque
            await _service.PasserAsync(Utilisateur, Carte);

            //Alors le numéro complet n'entre jamais en base
            Assert.Equal("4242", enregistree!.QuatreDerniers);
            Assert.DoesNotContain(PaiementSimuleService.CarteAcceptee, enregistree.QuatreDerniers);
        }

        [Fact]
        public async Task PasserAsync_VideLePanierUneFoisLaCommandeEcrite()
        {
            //Etant donné
            LignePanier ligne = Ligne();
            PanierContient(ligne);
            ToutEstReservable();
            PaiementAccepte();
            _commandes.Setup(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>())).ReturnsAsync(true);
            _panier.Setup(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>())).Returns(Task.CompletedTask);
            _facture.Setup(f => f.EnvoyerAsync(It.IsAny<Commande>())).Returns(Task.CompletedTask);

            //Lorsque
            await _service.PasserAsync(Utilisateur, Carte);

            //Alors
            _panier.Verify(p => p.SupprimerAsync(It.Is<IEnumerable<LignePanier>>(l => l.Single() == ligne)), Times.Once);
        }

        [Fact]
        public async Task PasserAsync_ReussitMemeSiLEnvoiDeLaFactureEchoue()
        {
            //Etant donné un serveur de courriel en panne
            PanierContient(Ligne());
            ToutEstReservable();
            PaiementAccepte();
            _commandes.Setup(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>())).ReturnsAsync(true);
            _panier.Setup(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>())).Returns(Task.CompletedTask);
            _facture.Setup(f => f.EnvoyerAsync(It.IsAny<Commande>()))
                .ThrowsAsync(new InvalidOperationException("SMTP injoignable"));

            //Lorsque
            ResultatCommande resultat = await _service.PasserAsync(Utilisateur, Carte);

            //Alors une commande déjà payée n'est pas remise en cause
            Assert.True(resultat.Reussi);
            Assert.NotEqual(string.Empty, resultat.Reference);
        }

        [Fact]
        public async Task PasserAsync_AdditionneLesSousTotauxDeToutesLesLignes()
        {
            //Etant donné deux lignes de quatre jours, à 55 et 500
            DemandePaiement? demande = null;
            PanierContient(Ligne(1, 55), Ligne(2, 500));
            ToutEstReservable();
            _paiement.Setup(p => p.PayerAsync(It.IsAny<DemandePaiement>()))
                .Callback<DemandePaiement>(d => demande = d)
                .ReturnsAsync(new ResultatPaiement(true, "Paiement accepté.", "PAY-1", "4242"));
            _commandes.Setup(c => c.AjouterSiDisponibleAsync(It.IsAny<Commande>())).ReturnsAsync(true);
            _panier.Setup(p => p.SupprimerAsync(It.IsAny<IEnumerable<LignePanier>>())).Returns(Task.CompletedTask);
            _facture.Setup(f => f.EnvoyerAsync(It.IsAny<Commande>())).Returns(Task.CompletedTask);

            //Lorsque
            await _service.PasserAsync(Utilisateur, Carte);

            //Alors
            Assert.Equal(2220, demande!.Montant);
        }

        [Fact]
        public async Task ObtenirAsync_DelegueAuDepotAvecLeProprietaire()
        {
            //Etant donné
            Commande commande = new Commande { Reference = "ESC-1" };
            _commandes.Setup(c => c.ObtenirAsync("ESC-1", Utilisateur)).ReturnsAsync(commande);

            //Lorsque
            Commande? trouvee = await _service.ObtenirAsync("ESC-1", Utilisateur);

            //Alors la recherche porte toujours le demandeur, ce qui ferme l'accès aux reçus d'autrui
            Assert.Same(commande, trouvee);
            _commandes.Verify(c => c.ObtenirAsync("ESC-1", Utilisateur), Times.Once);
        }
    }
}
