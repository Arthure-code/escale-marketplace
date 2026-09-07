using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface IEquipementService
    {
        Task<List<Equipement>> ObtenirTousAsync();
    }
}
