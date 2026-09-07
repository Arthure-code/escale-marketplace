using System.Threading.Tasks;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Escale.Web.Services
{
    // Les pages d'Identity envoient leurs courriels (mot de passe oublié,
    // confirmation) via l'IEmailSender de Microsoft. On le relie à notre
    // ICourrielService : un seul chemin d'envoi pour tout le site.
    public class PontCourrielIdentity : IEmailSender
    {
        private readonly ICourrielService _courriel;

        public PontCourrielIdentity(ICourrielService courriel)
        {
            _courriel = courriel;
        }

        // Les noms de paramètres sont ceux de l'interface de Microsoft, pas les
        // nôtres : un appelant qui nomme ses arguments doit retrouver le
        // contrat qu'il a lu dans la documentation.
        public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
            _courriel.EnvoyerAsync(email, subject, htmlMessage);
    }
}
