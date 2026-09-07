using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Escale.Web.Repositories
{
    public class UtilisateurRepository : IUtilisateurRepository
    {
        private readonly ContexteEscale _contexte;

        public UtilisateurRepository(ContexteEscale contexte)
        {
            _contexte = contexte;
        }

        public async Task<List<Utilisateur>> ObtenirTousAsync() =>
            await _contexte.Users.AsNoTracking().OrderBy(u => u.NomComplet).ToListAsync();

        public async Task<Utilisateur?> ObtenirAsync(string id) =>
            await _contexte.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<Utilisateur?> ObtenirParCourrielAsync(string courriel)
        {
            // La normalisation se fait avant la requête et non dedans : une
            // comparaison portant un StringComparison ne se traduit pas en SQL,
            // et Entity Framework la refuserait à l'exécution.
            string normalise = courriel.ToUpperInvariant();

            return await _contexte.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalise);
        }

        // Un rôle par compte dans cette application : la jointure rend donc
        // un dictionnaire, pas une liste.
        public async Task<Dictionary<string, string>> ObtenirRolesAsync() =>
            await (from lien in _contexte.UserRoles
                   join role in _contexte.Roles on lien.RoleId equals role.Id
                   select new { lien.UserId, role.Name })
                .ToDictionaryAsync(x => x.UserId, x => x.Name ?? string.Empty);

        public async Task EnregistrerAsync() => await _contexte.SaveChangesAsync();
    }
}
