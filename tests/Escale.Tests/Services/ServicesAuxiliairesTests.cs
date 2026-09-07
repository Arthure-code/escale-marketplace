using Microsoft.Extensions.Logging.Abstractions;

namespace Escale.Tests.Services
{
    // Les services de délégation : peu de code, mais ce sont eux qui tiennent
    // les coutures entre les couches et le repli quand rien n'est configuré.
    public class ServicesAuxiliairesTests
    {
        [Fact]
        public async Task EquipementService_DelegueAuDepot()
        {
            //Etant donné
            List<Equipement> equipements = new List<Equipement>
            {
                new Equipement { Code = "wifi", Libelle = "Wifi inclus" }
            };
            Mock<IEquipementRepository> depot = new Mock<IEquipementRepository>(MockBehavior.Strict);
            depot.Setup(d => d.ObtenirTousAsync()).ReturnsAsync(equipements);

            //Lorsque
            List<Equipement> obtenus = await new EquipementService(depot.Object).ObtenirTousAsync();

            //Alors
            Assert.Same(equipements, obtenus);
        }

        [Fact]
        public async Task CourrielJournalService_NEnvoieRienEtNeLevePas()
        {
            //Etant donné le repli utilisé quand aucun SMTP n'est configuré
            CourrielJournalService service =
                new CourrielJournalService(NullLogger<CourrielJournalService>.Instance);

            //Lorsque
            await service.EnvoyerAsync("camille@escale.test", "Sujet", "<p>Corps</p>");

            //Alors l'application tourne sans compte de messagerie
        }

        [Fact]
        public async Task PontCourrielIdentity_TransmetLesTroisArgumentsDansLOrdre()
        {
            //Etant donné le pont entre l'IEmailSender de Microsoft et le nôtre
            Mock<ICourrielService> courriel = new Mock<ICourrielService>(MockBehavior.Strict);
            courriel.Setup(c => c.EnvoyerAsync("camille@escale.test", "Sujet", "<p>Corps</p>"))
                .Returns(Task.CompletedTask);

            //Lorsque
            await new PontCourrielIdentity(courriel.Object)
                .SendEmailAsync("camille@escale.test", "Sujet", "<p>Corps</p>");

            //Alors un seul chemin d'envoi sert à tout le site
            courriel.Verify(c => c.EnvoyerAsync("camille@escale.test", "Sujet", "<p>Corps</p>"), Times.Once);
        }
    }
}
