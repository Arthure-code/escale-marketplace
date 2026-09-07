using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface ICommandeService
    {
        Task<ResultatCommande> PasserAsync(string utilisateurId, DonneesCarte carte);

        Task<Commande?> ObtenirAsync(string reference, string utilisateurId);

        Task<List<LigneCommande>> ObtenirVentesAsync(string loueurId);
    }
}
