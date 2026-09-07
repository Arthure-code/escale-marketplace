using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Escale.Web.Repositories
{
    public class CommandeRepository : ICommandeRepository
    {
        private readonly ContexteEscale _contexte;

        public CommandeRepository(ContexteEscale contexte)
        {
            _contexte = contexte;
        }

        // Deux périodes se chevauchent dès que l'une commence avant la fin
        // de l'autre et finit après son début. On compte les lignes plutôt
        // que de les distinguer : une annonce à cinq exemplaires supporte
        // cinq réservations simultanées.
        public async Task<Dictionary<int, int>> CompterReservationsAsync(DateTime debut, DateTime fin) =>
            await _contexte.LignesCommande
                .AsNoTracking()
                .Where(l => debut.Date < l.DateFin.Date && fin.Date > l.DateDebut.Date)
                .GroupBy(l => l.AnnonceId)
                .Select(g => new { AnnonceId = g.Key, Nombre = g.Count() })
                .ToDictionaryAsync(x => x.AnnonceId, x => x.Nombre);

        public async Task<int> CompterReservationsAsync(int annonceId, DateTime debut, DateTime fin) =>
            await _contexte.LignesCommande
                .CountAsync(l => l.AnnonceId == annonceId
                    && debut.Date < l.DateFin.Date && fin.Date > l.DateDebut.Date);

        public async Task<bool> AnnonceEstReserveeAsync(int annonceId) =>
            await _contexte.LignesCommande.AnyAsync(l => l.AnnonceId == annonceId);

        public async Task<Commande?> ObtenirAsync(string reference, string utilisateurId) =>
            await _contexte.Commandes
                .AsNoTracking()
                .Include(c => c.Lignes)
                .FirstOrDefaultAsync(c => c.Reference == reference && c.UtilisateurId == utilisateurId);

        public async Task<List<LigneCommande>> ObtenirVentesAsync(string loueurId) =>
            await _contexte.LignesCommande
                .AsNoTracking()
                .Include(l => l.Commande).ThenInclude(c => c!.Utilisateur)
                .Where(l => l.LoueurId == loueurId)
                .OrderByDescending(l => l.DateDebut)
                .ToListAsync();

        public async Task<bool> AjouterSiDisponibleAsync(Commande commande)
        {
            await using IDbContextTransaction transaction =
                await _contexte.Database.BeginTransactionAsync();

            foreach (LigneCommande ligne in commande.Lignes)
            {
                int parc = await _contexte.Annonces
                    .Where(a => a.Id == ligne.AnnonceId)
                    .Select(a => a.Exemplaires)
                    .FirstOrDefaultAsync();

                int retenus = await CompterReservationsAsync(ligne.AnnonceId, ligne.DateDebut, ligne.DateFin);

                if (retenus >= parc)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }

            _contexte.Commandes.Add(commande);
            await _contexte.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
    }
}
