using System.Net.Mail;
using Escale.Web.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Escale.Tests.Services
{
    public class CourrielSmtpServiceTests
    {
        private static CourrielSmtpService Service(string hote) =>
            new CourrielSmtpService(
                Options.Create(new OptionsCourriel
                {
                    Hote = hote,
                    Port = 587,
                    Utilisateur = "envoi@escale.test",
                    MotDePasse = "mot-de-passe-d-application",
                    ExpediteurAdresse = "no-reply@escale.test",
                    ExpediteurNom = "Escale"
                }),
                NullLogger<CourrielSmtpService>.Instance);

        [Fact]
        public async Task EnvoyerAsync_RemonteLEchecPlutotQueDeLeTaire()
        {
            //Etant donné un hôte SMTP qui n'existe pas
            CourrielSmtpService service = Service("smtp.hote.inexistant.escale");

            //Lorsque
            Task envoi = service.EnvoyerAsync("camille@escale.test", "Sujet", "<p>Corps</p>");

            //Alors l'appelant est prévenu : c'est ce qui permet à la commande
            //de journaliser l'échec sans annuler un paiement déjà encaissé
            await Assert.ThrowsAnyAsync<Exception>(() => envoi);
        }

        [Fact]
        public void LOptionEstConfigureeDesQueLHoteEstRenseigne()
        {
            //Etant donné
            OptionsCourriel vide = new OptionsCourriel();
            OptionsCourriel remplie = new OptionsCourriel { Hote = "smtp.gmail.com" };

            //Alors c'est cette bascule qui choisit entre envoi réel et journal
            Assert.False(vide.EstConfigure);
            Assert.True(remplie.EstConfigure);
        }

        [Fact]
        public void LeSchemaDePaiementParDefautEstLeServiceSimule()
        {
            //Etant donné les options de paiement telles qu'elles sortent de la
            //configuration vide
            OptionsPaiement options = new OptionsPaiement();

            //Alors aucun prestataire réel n'est branché par défaut
            Assert.Equal("Simule", options.Fournisseur);
        }
    }
}
