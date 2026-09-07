using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface IUtilisateurRepository
    {
        Task<List<Utilisateur>> ObtenirTousAsync();

        Task<Utilisateur?> ObtenirAsync(string id);

        Task<Utilisateur?> ObtenirParCourrielAsync(string courriel);

        Task<Dictionary<string, string>> ObtenirRolesAsync();

        Task EnregistrerAsync();
    }
}
