using System.Threading.Tasks;
using Escale.Web.Interfaces;
using Microsoft.Extensions.Logging;

namespace Escale.Web.Services
{
    // Repli quand aucun SMTP n'est configuré : le courriel n'est pas envoyé,
    // il est écrit dans le journal. L'application tourne ainsi sans compte de
    // messagerie, et on voit ce qui serait parti. C'est le mode par défaut en
    // développement, celui que documente Microsoft pour tester.
    public class CourrielJournalService : ICourrielService
    {
        private readonly ILogger<CourrielJournalService> _logger;

        public CourrielJournalService(ILogger<CourrielJournalService> logger)
        {
            _logger = logger;
        }

        public Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml)
        {
            _logger.LogInformation(
                "Courriel non envoyé (aucun SMTP configuré). Destinataire {Destinataire}, sujet {Sujet}",
                destinataire, sujet);

            return Task.CompletedTask;
        }
    }
}
