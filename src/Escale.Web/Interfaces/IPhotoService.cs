using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Le catalogue de photos et l'ajout des siennes. Le PageModel ne touche
    // pas au disque, il passe par là.
    public interface IPhotoService
    {
        List<string> Catalogue();

        Task<ResultatPhoto> TeleverserAsync(Stream contenu, string nomOrigine, long taille);
    }
}
