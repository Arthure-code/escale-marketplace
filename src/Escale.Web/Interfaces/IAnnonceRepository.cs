using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Seul point d'accès aux annonces stockées. Aucune règle métier ici :
    // le dépôt lit et écrit, il ne décide pas.
    public interface IAnnonceRepository
    {
        Task<List<Annonce>> ObtenirVisiblesAsync(CategorieAnnonce? categorie);

        Task<Annonce?> ObtenirAsync(int id);

        Task<List<Annonce>> ObtenirParLoueurAsync(string loueurId);

        Task<Annonce?> ObtenirDuLoueurAsync(int id, string loueurId);

        Task<List<Annonce>> ObtenirToutesAsync();

        Task AjouterAsync(Annonce annonce);

        Task SupprimerAsync(Annonce annonce);

        Task EnregistrerAsync();
    }
}
