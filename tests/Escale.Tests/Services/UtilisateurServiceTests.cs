using Microsoft.Extensions.Logging.Abstractions;

namespace Escale.Tests.Services
{
    public class UtilisateurServiceTests
    {
        private static readonly DateTime Aujourdhui = DateTime.Today;

        private readonly Mock<IUtilisateurRepository> _comptes = new Mock<IUtilisateurRepository>(MockBehavior.Strict);
        private readonly UtilisateurService _service;

        public UtilisateurServiceTests()
        {
            _service = new UtilisateurService(_comptes.Object, NullLogger<UtilisateurService>.Instance);
        }

        private static Utilisateur Compte(Blocage? blocage = null) => new Utilisateur
        {
            Id = "marie",
            NomComplet = "Marie Tremblay",
            Blocage = blocage
        };

        [Fact]
        public async Task BloquerAsync_RefuseUneFinAnterieureAuDebut()
        {
            //Etant donné des bornes inversées
            //Lorsque
            ResultatAjout resultat = await _service.BloquerAsync("marie", Aujourdhui, Aujourdhui.AddDays(-1), "Motif");

            //Alors le compte n'est même pas chargé
            Assert.False(resultat.Reussi);
            Assert.Contains("fin du blocage", resultat.Message);
            _comptes.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BloquerAsync_RefuseUnCompteIntrouvable()
        {
            //Etant donné
            _comptes.Setup(c => c.ObtenirAsync("fantome")).ReturnsAsync((Utilisateur?)null);

            //Lorsque
            ResultatAjout resultat = await _service.BloquerAsync("fantome", Aujourdhui, null, "Motif");

            //Alors
            Assert.False(resultat.Reussi);
            Assert.Contains("introuvable", resultat.Message);
            _comptes.Verify(c => c.EnregistrerAsync(), Times.Never);
        }

        [Fact]
        public async Task BloquerAsync_PoseUnBlocageAvecSesDeuxBornes()
        {
            //Etant donné
            Utilisateur compte = Compte();
            DateTime fin = Aujourdhui.AddDays(30);
            _comptes.Setup(c => c.ObtenirAsync("marie")).ReturnsAsync(compte);
            _comptes.Setup(c => c.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            ResultatAjout resultat = await _service.BloquerAsync("marie", Aujourdhui, fin, "Annonce non conforme");

            //Alors
            Assert.True(resultat.Reussi);
            Assert.Equal(Aujourdhui, compte.Blocage!.Debut);
            Assert.Equal(fin, compte.Blocage.Fin);
            Assert.Equal("Annonce non conforme", compte.Blocage.Motif);
        }

        [Fact]
        public async Task BloquerAsync_AccepteUnBlocageSansFin()
        {
            //Etant donné aucune date de fin, cas du « jusqu'à nouvel ordre »
            Utilisateur compte = Compte();
            _comptes.Setup(c => c.ObtenirAsync("marie")).ReturnsAsync(compte);
            _comptes.Setup(c => c.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            ResultatAjout resultat = await _service.BloquerAsync("marie", Aujourdhui, null, "Motif");

            //Alors
            Assert.True(resultat.Reussi);
            Assert.Null(compte.Blocage!.Fin);
            Assert.Contains("nouvel ordre", resultat.Message);
        }

        [Fact]
        public async Task BloquerAsync_RamenneUnMotifVideAUnMotifAbsent()
        {
            //Etant donné un motif fait d'espaces
            Utilisateur compte = Compte();
            _comptes.Setup(c => c.ObtenirAsync("marie")).ReturnsAsync(compte);
            _comptes.Setup(c => c.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            await _service.BloquerAsync("marie", Aujourdhui, null, "   ");

            //Alors on n'écrit pas une chaîne blanche en base
            Assert.Null(compte.Blocage!.Motif);
        }

        [Fact]
        public async Task DebloquerAsync_RetireLeBlocageSansRienSupprimerDAutre()
        {
            //Etant donné un compte bloqué
            Utilisateur compte = Compte(new Blocage { Debut = Aujourdhui.AddDays(-1) });
            _comptes.Setup(c => c.ObtenirAsync("marie")).ReturnsAsync(compte);
            _comptes.Setup(c => c.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            bool debloque = await _service.DebloquerAsync("marie");

            //Alors
            Assert.True(debloque);
            Assert.Null(compte.Blocage);
            Assert.Equal("Marie Tremblay", compte.NomComplet);
        }

        [Fact]
        public async Task DebloquerAsync_FauxSurUnCompteIntrouvable()
        {
            //Etant donné
            _comptes.Setup(c => c.ObtenirAsync("fantome")).ReturnsAsync((Utilisateur?)null);

            //Lorsque
            bool debloque = await _service.DebloquerAsync("fantome");

            //Alors
            Assert.False(debloque);
            _comptes.Verify(c => c.EnregistrerAsync(), Times.Never);
        }

        [Fact]
        public async Task ProlongerAbonnementAsync_PorteLEcheanceALaDateDemandee()
        {
            //Etant donné
            Utilisateur compte = Compte();
            DateTime echeance = Aujourdhui.AddMonths(1);
            _comptes.Setup(c => c.ObtenirAsync("marie")).ReturnsAsync(compte);
            _comptes.Setup(c => c.EnregistrerAsync()).Returns(Task.CompletedTask);

            //Lorsque
            bool prolonge = await _service.ProlongerAbonnementAsync("marie", echeance);

            //Alors
            Assert.True(prolonge);
            Assert.Equal(echeance, compte.Abonnement.Echeance);
        }

        [Fact]
        public async Task PeutSeConnecterAsync_RefuseUnCompteBloqueAujourdHui()
        {
            //Etant donné
            _comptes.Setup(c => c.ObtenirParCourrielAsync("marie@escale.test"))
                .ReturnsAsync(Compte(new Blocage { Debut = Aujourdhui.AddDays(-1) }));

            //Lorsque
            bool peut = await _service.PeutSeConnecterAsync("marie@escale.test");

            //Alors
            Assert.False(peut);
        }

        [Fact]
        public async Task PeutSeConnecterAsync_AutoriseUnBlocageDejaTermine()
        {
            //Etant donné
            _comptes.Setup(c => c.ObtenirParCourrielAsync("marie@escale.test"))
                .ReturnsAsync(Compte(new Blocage
                {
                    Debut = Aujourdhui.AddDays(-10),
                    Fin = Aujourdhui.AddDays(-1)
                }));

            //Lorsque
            bool peut = await _service.PeutSeConnecterAsync("marie@escale.test");

            //Alors
            Assert.True(peut);
        }

        [Fact]
        public async Task PeutSeConnecterAsync_NeRevelePasQuUnCompteEstInconnu()
        {
            //Etant donné une adresse qui n'existe pas
            _comptes.Setup(c => c.ObtenirParCourrielAsync("inconnu@escale.test"))
                .ReturnsAsync((Utilisateur?)null);

            //Lorsque
            bool peut = await _service.PeutSeConnecterAsync("inconnu@escale.test");

            //Alors la réponse est la même que pour un compte valide : c'est
            //l'authentification qui refusera, sans distinguer les deux cas
            Assert.True(peut);
        }
    }
}
