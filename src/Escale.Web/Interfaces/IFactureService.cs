using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Fabrique la facture d'une commande et l'envoie à son client. L'échec de
    // l'envoi ne remet jamais la commande en cause : elle est déjà payée.
    public interface IFactureService
    {
        Task EnvoyerAsync(Commande commande);
    }
}
