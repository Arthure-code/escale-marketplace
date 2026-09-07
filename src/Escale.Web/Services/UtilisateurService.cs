using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Infrastructure;
using Escale.Web.Interfaces;
using Microsoft.Extensions.Logging;

namespace Escale.Web.Services
{
    // Ce que l'administrateur fait aux comptes. Bloquer ne supprime rien :
    // le contenu quitte le site le temps du blocage et revient ensuite.
    public class UtilisateurService : IUtilisateurService
    {
        private readonly IUtilisateurRepository _comptes;
        private readonly ILogger<UtilisateurService> _journal;

        public UtilisateurService(IUtilisateurRepository comptes, ILogger<UtilisateurService> journal)
        {
            _comptes = comptes;
            _journal = journal;
        }

        public Task<List<Utilisateur>> ObtenirTousAsync() => _comptes.ObtenirTousAsync();

        public Task<Utilisateur?> ObtenirAsync(string id) => _comptes.ObtenirAsync(id);

        public Task<Dictionary<string, string>> ObtenirRolesAsync() => _comptes.ObtenirRolesAsync();

        public async Task<ResultatAjout> BloquerAsync(string id, DateTime debut, DateTime? fin, string motif)
        {
            if (fin is not null && fin.Value.Date < debut.Date)
            {
                return new ResultatAjout(false, "La fin du blocage doit suivre son début.");
            }

            Utilisateur? compte = await _comptes.ObtenirAsync(id);

            if (compte is null)
            {
                return new ResultatAjout(false, "Ce compte est introuvable.");
            }

            compte.Blocage = new Blocage
            {
                Debut = debut.Date,
                Fin = fin?.Date,
                Motif = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim()
            };

            await _comptes.EnregistrerAsync();

            _journal.LogWarning(JournalDeSecurite.CompteBloque,
                "Compte {UtilisateurId} bloqué du {Debut} au {Fin}", id, debut.Date, fin?.Date);

            return new ResultatAjout(true, fin is null
                ? "Compte bloqué jusqu'à nouvel ordre."
                : "Compte bloqué jusqu'au " + fin.Value.ToString("d MMMM yyyy") + ".");
        }

        public async Task<bool> DebloquerAsync(string id)
        {
            Utilisateur? compte = await _comptes.ObtenirAsync(id);

            if (compte is null)
            {
                return false;
            }

            compte.Blocage = null;

            await _comptes.EnregistrerAsync();

            _journal.LogWarning(JournalDeSecurite.CompteDebloque, "Compte {UtilisateurId} débloqué", id);
            return true;
        }

        public async Task<bool> ProlongerAbonnementAsync(string id, DateTime? echeance)
        {
            Utilisateur? compte = await _comptes.ObtenirAsync(id);

            if (compte is null)
            {
                return false;
            }

            compte.Abonnement.Echeance = echeance?.Date;

            await _comptes.EnregistrerAsync();

            _journal.LogInformation(JournalDeSecurite.AbonnementModifie,
                "Abonnement du compte {UtilisateurId} porté au {Echeance}", id, echeance?.Date);
            return true;
        }

        public async Task<bool> PeutSeConnecterAsync(string courriel)
        {
            Utilisateur? compte = await _comptes.ObtenirParCourrielAsync(courriel);

            return compte is null || compte.Blocage is null || !compte.Blocage.EstEnCoursLe(DateTime.Today);
        }
    }
}
