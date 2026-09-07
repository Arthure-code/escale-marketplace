using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface IUtilisateurService
    {
        Task<List<Utilisateur>> ObtenirTousAsync();

        Task<Utilisateur?> ObtenirAsync(string id);

        Task<Dictionary<string, string>> ObtenirRolesAsync();

        Task<ResultatAjout> BloquerAsync(string id, DateTime debut, DateTime? fin, string motif);

        Task<bool> DebloquerAsync(string id);

        Task<bool> ProlongerAbonnementAsync(string id, DateTime? echeance);

        Task<bool> PeutSeConnecterAsync(string courriel);
    }
}
