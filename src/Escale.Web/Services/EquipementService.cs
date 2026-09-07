using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;

namespace Escale.Web.Services
{
    public class EquipementService : IEquipementService
    {
        private readonly IEquipementRepository _equipements;

        public EquipementService(IEquipementRepository equipements)
        {
            _equipements = equipements;
        }

        public Task<List<Equipement>> ObtenirTousAsync() => _equipements.ObtenirTousAsync();
    }
}
