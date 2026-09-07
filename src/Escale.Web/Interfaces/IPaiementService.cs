using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Passerelle de paiement. Le reste de l'application ne connaît que cette
    // interface : le jour où un vrai prestataire remplace la simulation, ni
    // la commande, ni les pages, ni les tests ne changent.
    //
    // La méthode est asynchrone parce qu'un vrai appel réseau l'est toujours.
    public interface IPaiementService
    {
        Task<ResultatPaiement> PayerAsync(DemandePaiement demande);
    }
}
