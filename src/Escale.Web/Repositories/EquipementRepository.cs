using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Escale.Web.Repositories
{
    public class EquipementRepository : IEquipementRepository
    {
        private readonly ContexteEscale _contexte;

        public EquipementRepository(ContexteEscale contexte)
        {
            _contexte = contexte;
        }

        public async Task<List<Equipement>> ObtenirTousAsync() =>
            await _contexte.Equipements.AsNoTracking().OrderBy(e => e.Libelle).ToListAsync();
    }
}
