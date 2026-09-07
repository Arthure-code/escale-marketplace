using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface IPanierRepository
    {
        Task<List<LignePanier>> ObtenirAsync(string utilisateurId);

        Task<int> CompterAsync(string utilisateurId);

        Task<bool> ContientAsync(string utilisateurId, int annonceId);

        Task<LignePanier?> ObtenirLigneAsync(int ligneId, string utilisateurId);

        Task AjouterAsync(LignePanier ligne);

        Task SupprimerAsync(IEnumerable<LignePanier> lignes);
    }
}
