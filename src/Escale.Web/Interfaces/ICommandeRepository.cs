using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface ICommandeRepository
    {
        // Combien d'exemplaires de chaque annonce sont retenus sur la période.
        Task<Dictionary<int, int>> CompterReservationsAsync(DateTime debut, DateTime fin);

        Task<int> CompterReservationsAsync(int annonceId, DateTime debut, DateTime fin);

        Task<bool> AnnonceEstReserveeAsync(int annonceId);

        Task<Commande?> ObtenirAsync(string reference, string utilisateurId);

        Task<List<LigneCommande>> ObtenirVentesAsync(string loueurId);

        // Enregistre la commande seulement si chaque annonce a encore un
        // exemplaire libre au moment d'écrire. Le contrôle et l'écriture
        // tiennent dans une transaction : sans elle, deux paiements
        // simultanés sur la dernière unité passeraient tous les deux.
        Task<bool> AjouterSiDisponibleAsync(Commande commande);
    }
}
