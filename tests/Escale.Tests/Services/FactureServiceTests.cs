using Microsoft.Extensions.Logging.Abstractions;

namespace Escale.Tests.Services
{
    public class FactureServiceTests
    {
        private readonly Mock<IUtilisateurRepository> _comptes = new Mock<IUtilisateurRepository>(MockBehavior.Strict);
        private readonly Mock<ICourrielService> _courriel = new Mock<ICourrielService>(MockBehavior.Strict);
        private readonly FactureService _service;

        private string _destinataire = string.Empty;
        private string _sujet = string.Empty;
        private string _corps = string.Empty;

        public FactureServiceTests()
        {
            _service = new FactureService(_comptes.Object, _courriel.Object,
                NullLogger<FactureService>.Instance);

            _courriel.Setup(c => c.EnvoyerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((d, s, c) =>
                {
                    _destinataire = d;
                    _sujet = s;
                    _corps = c;
                })
                .Returns(Task.CompletedTask);
        }

        private static Commande Commande(string titre = "Chambre lumineuse",
            string reference = "ESC-260907-101010", string quatreDerniers = "4242") => new Commande
            {
                Reference = reference,
                UtilisateurId = "camille",
                QuatreDerniers = quatreDerniers,
                DateCommande = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc),
                Lignes = new List<LigneCommande>
                {
                    new LigneCommande
                    {
                        Titre = titre,
                        Categorie = CategorieAnnonce.Chambre,
                        PrixJournalier = 55,
                        DateDebut = new DateTime(2026, 12, 10),
                        DateFin = new DateTime(2026, 12, 14),
                        NombreDeJours = 4,
                        SousTotal = 220
                    }
                }
            };

        private void ClientEst(string? courriel, string nom = "Camille Roy") =>
            _comptes.Setup(c => c.ObtenirAsync("camille")).ReturnsAsync(
                courriel is null ? null : new Utilisateur { Email = courriel, NomComplet = nom });

        [Fact]
        public async Task EnvoyerAsync_NEnvoieRienQuandLeClientEstIntrouvable()
        {
            //Etant donné
            ClientEst(null);

            //Lorsque
            await _service.EnvoyerAsync(Commande());

            //Alors
            _courriel.Verify(c => c.EnvoyerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EnvoyerAsync_AdresseLaFactureAuClientEtNommeLaCommandeDansLeSujet()
        {
            //Etant donné
            ClientEst("camille@escale.test");

            //Lorsque
            await _service.EnvoyerAsync(Commande());

            //Alors
            Assert.Equal("camille@escale.test", _destinataire);
            Assert.Contains("ESC-260907-101010", _sujet);
        }

        [Fact]
        public async Task EnvoyerAsync_EncodeLeNomDuClientAvantDeLInsererDansLeHtml()
        {
            //Etant donné un nom porteur de balises
            ClientEst("camille@escale.test", "<script>alert(1)</script>");

            //Lorsque
            await _service.EnvoyerAsync(Commande());

            //Alors la balise n'entre pas telle quelle dans le courriel
            Assert.DoesNotContain("<script>", _corps);
            Assert.Contains("&lt;script&gt;", _corps);
        }

        [Fact]
        public async Task EnvoyerAsync_EncodeLeTitreDeLAnnonce()
        {
            //Etant donné un titre choisi par un loueur
            ClientEst("camille@escale.test");

            //Lorsque
            await _service.EnvoyerAsync(Commande(titre: "<img src=x onerror=alert(1)>"));

            //Alors
            Assert.DoesNotContain("<img src=x", _corps);
            Assert.Contains("&lt;img", _corps);
        }

        [Fact]
        public async Task EnvoyerAsync_NeFaitFigurerQueLesQuatreDerniersChiffres()
        {
            //Etant donné
            ClientEst("camille@escale.test");

            //Lorsque
            await _service.EnvoyerAsync(Commande());

            //Alors aucun numéro complet, conformément à la norme PCI
            Assert.Contains("4242", _corps);
            Assert.DoesNotContain(PaiementSimuleService.CarteAcceptee, _corps);
            Assert.DoesNotContain("Expiration", _corps);
            Assert.DoesNotContain("cvc", _corps, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EnvoyerAsync_ReprendLeDetailFigeDansLaCommande()
        {
            //Etant donné
            ClientEst("camille@escale.test");

            //Lorsque
            await _service.EnvoyerAsync(Commande());

            //Alors la facture rend compte de ce qui a été payé
            Assert.Contains("Chambre lumineuse", _corps);
            Assert.Contains("220 $", _corps);
            Assert.Contains("4 jour(s) à 55 $", _corps);
            Assert.Contains("ESC-260907-101010", _corps);
        }
    }
}
