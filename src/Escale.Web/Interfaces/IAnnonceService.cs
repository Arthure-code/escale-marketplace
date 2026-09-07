using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Règles autour d'une annonce : ce qui est offert au public, ce qu'un
    // loueur a le droit de voir et de changer.
    //
    // `sansRestriction` est le passe de l'administrateur. À faux, chaque appel
    // reste borné au loueur passé en paramètre ; c'est ce qui empêche un loueur
    // de toucher aux annonces d'un autre.
    public interface IAnnonceService
    {
        // Toutes les annonces diffusables, y compris celles qui sont complètes
        // sur la période : elles restent sur le site, signalées comme telles.
        Task<List<Offre>> RechercherAsync(CategorieAnnonce? categorie, DateTime debut, DateTime fin);

        Task<Offre?> ObtenirOffreAsync(int id, DateTime debut, DateTime fin);

        Task<Offre> OffrirAsync(Annonce annonce, DateTime debut, DateTime fin);

        Task<Annonce?> ObtenirAsync(int id);

        Task<Annonce?> ObtenirPubliqueAsync(int id);

        Task<bool> EstReservableAsync(int annonceId, DateTime debut, DateTime fin);

        Task<List<Annonce>> ObtenirDuLoueurAsync(string loueurId);

        Task<List<Annonce>> ObtenirToutesAsync();

        Task<Annonce?> ObtenirPourModificationAsync(int id, string loueurId, bool sansRestriction = false);

        Task PublierAsync(Annonce annonce, string loueurId, IEnumerable<int> equipements);

        Task<bool> ModifierAsync(Annonce saisie, string loueurId, IEnumerable<int> equipements, bool sansRestriction = false);

        Task<bool> BasculerVisibiliteAsync(int id, string loueurId, bool sansRestriction = false);

        Task<bool> SupprimerAsync(int id, string loueurId, bool sansRestriction = false);
    }
}
