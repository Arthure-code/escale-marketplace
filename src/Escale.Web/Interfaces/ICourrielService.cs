using System.Threading.Tasks;

namespace Escale.Web.Interfaces
{
    // Même contrat que l'IEmailSender documenté par Microsoft : un destinataire,
    // un sujet, un corps HTML. Le reste de l'application ne connaît que cette
    // interface ; l'implémentation, SMTP ou service externe, se choisit par
    // configuration.
    public interface ICourrielService
    {
        Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml);
    }
}
