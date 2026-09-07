using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Escale.Web.Repositories
{
    public class AnnonceRepository : IAnnonceRepository
    {
        private readonly ContexteEscale _contexte;

        public AnnonceRepository(ContexteEscale contexte)
        {
            _contexte = contexte;
        }

        public async Task<List<Annonce>> ObtenirVisiblesAsync(CategorieAnnonce? categorie)
        {
            IQueryable<Annonce> requete = Completes().Where(a => a.EstDisponible);

            if (categorie is not null)
            {
                requete = requete.Where(a => a.Categorie == categorie);
            }

            return await requete.OrderBy(a => a.PrixJournalier).ToListAsync();
        }

        // Suivie par le contexte : c'est l'annonce que l'administrateur
        // modifie, bascule ou supprime. Une entité détachée verrait ses
        // changements ignorés au moment d'enregistrer.
        public async Task<Annonce?> ObtenirAsync(int id) =>
            await Suivies().FirstOrDefaultAsync(a => a.Id == id);

        public async Task<List<Annonce>> ObtenirParLoueurAsync(string loueurId) =>
            await Completes()
                .Where(a => a.LoueurId == loueurId)
                .OrderByDescending(a => a.DatePublication)
                .ToListAsync();

        public async Task<Annonce?> ObtenirDuLoueurAsync(int id, string loueurId) =>
            await _contexte.Annonces
                .Include(a => a.Equipements)
                .FirstOrDefaultAsync(a => a.Id == id && a.LoueurId == loueurId);

        public async Task<List<Annonce>> ObtenirToutesAsync() =>
            await Completes().OrderByDescending(a => a.DatePublication).ToListAsync();

        public async Task SupprimerAsync(Annonce annonce)
        {
            _contexte.Annonces.Remove(annonce);
            await _contexte.SaveChangesAsync();
        }

        public async Task AjouterAsync(Annonce annonce)
        {
            _contexte.Annonces.Add(annonce);
            await _contexte.SaveChangesAsync();
        }

        public async Task EnregistrerAsync() => await _contexte.SaveChangesAsync();

        // Les listes ne sont que lues : sans suivi, Entity Framework
        // n'instancie pas d'instantané par entité, ce qui coupe autant de
        // travail et autant de mémoire retenue le temps de la requête.
        private IQueryable<Annonce> Completes() => Suivies().AsNoTracking();

        private IQueryable<Annonce> Suivies() =>
            _contexte.Annonces
                .Include(a => a.Loueur)
                .Include(a => a.Equipements).ThenInclude(ae => ae.Equipement);
    }
}
