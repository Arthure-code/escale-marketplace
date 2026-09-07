using System;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface IPanierService
    {
        Task<Panier> ObtenirAsync(string utilisateurId);

        Task<int> CompterAsync(string utilisateurId);

        Task<ResultatAjout> AjouterAsync(string utilisateurId, int annonceId, DateTime debut, DateTime fin);

        Task RetirerAsync(string utilisateurId, int ligneId);

        Task ViderAsync(string utilisateurId);
    }
}
