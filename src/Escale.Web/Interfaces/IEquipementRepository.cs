using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface IEquipementRepository
    {
        Task<List<Equipement>> ObtenirTousAsync();
    }
}
