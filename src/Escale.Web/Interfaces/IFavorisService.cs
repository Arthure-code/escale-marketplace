using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Les favoris ne vivent qu'en cache distribué : en mémoire au poste de
    // développement, dans Redis une fois déployé. L'application ne connaît
    // que cette interface, jamais l'implémentation retenue.
    public interface IFavorisService
    {
        Task<List<int>> ObtenirIdsAsync(string utilisateurId);

        Task<List<Offre>> ObtenirAsync(string utilisateurId, DateTime debut, DateTime fin);

        Task<bool> BasculerAsync(string utilisateurId, int annonceId);

        Task<int> CompterAsync(string utilisateurId);
    }
}
