using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Escale.Web.Repositories
{
    public class PanierRepository : IPanierRepository
    {
        private readonly ContexteEscale _contexte;

        public PanierRepository(ContexteEscale contexte)
        {
            _contexte = contexte;
        }

        public async Task<List<LignePanier>> ObtenirAsync(string utilisateurId) =>
            await _contexte.LignesPanier
                .Include(l => l.Annonce).ThenInclude(a => a!.Equipements).ThenInclude(ae => ae.Equipement)
                .Where(l => l.UtilisateurId == utilisateurId)
                .OrderBy(l => l.DateDebut)
                .ToListAsync();

        public async Task<int> CompterAsync(string utilisateurId) =>
            await _contexte.LignesPanier.CountAsync(l => l.UtilisateurId == utilisateurId);

        public async Task<bool> ContientAsync(string utilisateurId, int annonceId) =>
            await _contexte.LignesPanier.AnyAsync(l => l.UtilisateurId == utilisateurId && l.AnnonceId == annonceId);

        public async Task<LignePanier?> ObtenirLigneAsync(int ligneId, string utilisateurId) =>
            await _contexte.LignesPanier
                .FirstOrDefaultAsync(l => l.Id == ligneId && l.UtilisateurId == utilisateurId);

        public async Task AjouterAsync(LignePanier ligne)
        {
            _contexte.LignesPanier.Add(ligne);
            await _contexte.SaveChangesAsync();
        }

        public async Task SupprimerAsync(IEnumerable<LignePanier> lignes)
        {
            _contexte.LignesPanier.RemoveRange(lignes);
            await _contexte.SaveChangesAsync();
        }
    }
}
