using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Qui consulte la page. Les PageModel passent par là plutôt que par
    // UserManager : ils n'ont pas à connaître le fournisseur d'identité, et
    // un test fournit un identifiant en une ligne.
    public interface IUtilisateurCourant
    {
        bool EstConnecte { get; }

        bool EstAdministrateur { get; }

        string Id { get; }

        Task<Utilisateur?> ObtenirAsync();
    }
}
