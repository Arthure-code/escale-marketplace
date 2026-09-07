using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;

namespace Escale.Web.Services
{
    // Une annonce n'est offerte que si trois conditions tiennent ensemble :
    // son loueur l'a rendue visible, son compte est en règle, et aucune
    // commande ne la retient sur la période demandée.
    public class AnnonceService : IAnnonceService
    {
        private readonly IAnnonceRepository _annonces;
        private readonly ICommandeRepository _commandes;
        private readonly IDiffusionService _diffusion;

        public AnnonceService(IAnnonceRepository annonces, ICommandeRepository commandes,
            IDiffusionService diffusion)
        {
            _annonces = annonces;
            _commandes = commandes;
            _diffusion = diffusion;
        }

        public async Task<List<Offre>> RechercherAsync(CategorieAnnonce? categorie, DateTime debut, DateTime fin)
        {
            List<Annonce> visibles = await _annonces.ObtenirVisiblesAsync(categorie);
            Dictionary<int, int> retenus = await _commandes.CompterReservationsAsync(debut, fin);

            return visibles
                .Where(Diffusable)
                .Select(annonce => Offrir(annonce, Retenus(retenus, annonce.Id)))
                .OrderBy(offre => offre.EstComplete)
                .ThenBy(offre => offre.Annonce.PrixJournalier)
                .ToList();
        }

        public async Task<Offre?> ObtenirOffreAsync(int id, DateTime debut, DateTime fin)
        {
            Annonce? annonce = await ObtenirPubliqueAsync(id);

            return annonce is null ? null : await OffrirAsync(annonce, debut, fin);
        }

        public async Task<Offre> OffrirAsync(Annonce annonce, DateTime debut, DateTime fin) =>
            Offrir(annonce, await _commandes.CompterReservationsAsync(annonce.Id, debut, fin));

        public Task<Annonce?> ObtenirAsync(int id) => _annonces.ObtenirAsync(id);

        public async Task<Annonce?> ObtenirPubliqueAsync(int id)
        {
            Annonce? annonce = await _annonces.ObtenirAsync(id);

            return annonce is not null && annonce.EstDisponible && Diffusable(annonce) ? annonce : null;
        }

        public async Task<bool> EstReservableAsync(int annonceId, DateTime debut, DateTime fin)
        {
            Annonce? annonce = await _annonces.ObtenirAsync(annonceId);

            if (annonce is null || !annonce.EstDisponible || !Diffusable(annonce))
            {
                return false;
            }

            return Offrir(annonce, await _commandes.CompterReservationsAsync(annonceId, debut, fin)).Restants > 0;
        }

        public Task<List<Annonce>> ObtenirDuLoueurAsync(string loueurId) =>
            _annonces.ObtenirParLoueurAsync(loueurId);

        public Task<List<Annonce>> ObtenirToutesAsync() => _annonces.ObtenirToutesAsync();

        public Task<Annonce?> ObtenirPourModificationAsync(int id, string loueurId, bool sansRestriction = false) =>
            sansRestriction ? _annonces.ObtenirAsync(id) : _annonces.ObtenirDuLoueurAsync(id, loueurId);

        public async Task PublierAsync(Annonce annonce, string loueurId, IEnumerable<int> equipements)
        {
            annonce.LoueurId = loueurId;
            annonce.DatePublication = DateTime.UtcNow;
            annonce.Equipements = equipements
                .Select(id => new AnnonceEquipement { EquipementId = id })
                .ToList();

            Nettoyer(annonce);

            await _annonces.AjouterAsync(annonce);
        }

        public async Task<bool> ModifierAsync(Annonce saisie, string loueurId, IEnumerable<int> equipements, bool sansRestriction = false)
        {
            Annonce? existante = await ObtenirPourModificationAsync(saisie.Id, loueurId, sansRestriction);

            if (existante is null)
            {
                return false;
            }

            existante.Categorie = saisie.Categorie;
            existante.Titre = saisie.Titre;
            existante.Description = saisie.Description;
            existante.PrixJournalier = saisie.PrixJournalier;
            existante.Photo = saisie.Photo;
            existante.EstDisponible = saisie.EstDisponible;
            existante.Superficie = saisie.Superficie;
            existante.Couchages = saisie.Couchages;
            existante.Marque = saisie.Marque;
            existante.Annee = saisie.Annee;
            existante.Places = saisie.Places;
            existante.Exemplaires = saisie.Exemplaires;

            existante.Equipements.Clear();
            foreach (int equipement in equipements)
            {
                existante.Equipements.Add(new AnnonceEquipement { EquipementId = equipement });
            }

            Nettoyer(existante);

            await _annonces.EnregistrerAsync();
            return true;
        }

        public async Task<bool> BasculerVisibiliteAsync(int id, string loueurId, bool sansRestriction = false)
        {
            Annonce? annonce = await ObtenirPourModificationAsync(id, loueurId, sansRestriction);

            if (annonce is null)
            {
                return false;
            }

            annonce.EstDisponible = !annonce.EstDisponible;
            await _annonces.EnregistrerAsync();

            return true;
        }

        public async Task<bool> SupprimerAsync(int id, string loueurId, bool sansRestriction = false)
        {
            Annonce? annonce = await ObtenirPourModificationAsync(id, loueurId, sansRestriction);

            // Une annonce déjà réservée reste en base : sinon la commande du
            // client perdrait sa référence. On la retire du site à la place.
            if (annonce is null || await _commandes.AnnonceEstReserveeAsync(id))
            {
                return false;
            }

            await _annonces.SupprimerAsync(annonce);
            return true;
        }

        // Ce qui reste sur la période : le parc moins ce qui est déjà retenu.
        // Rien n'est mémorisé, tout se recalcule à chaque appel.
        private static Offre Offrir(Annonce annonce, int retenus) =>
            new Offre(annonce, annonce.Exemplaires, Math.Max(0, annonce.Exemplaires - retenus));

        private static int Retenus(Dictionary<int, int> compte, int annonceId) =>
            compte.TryGetValue(annonceId, out int nombre) ? nombre : 0;

        // Le service ne sait pas ce qui empêche une diffusion : il pose la
        // question aux règles enregistrées.
        private bool Diffusable(Annonce annonce) =>
            _diffusion.EstDiffusable(annonce.Loueur, DateTime.Today);

        // Une chambre n'a ni marque ni places, une voiture n'a ni superficie
        // ni couchages : on efface ce qui ne concerne pas la catégorie.
        private static void Nettoyer(Annonce annonce)
        {
            if (annonce.Categorie == CategorieAnnonce.Chambre)
            {
                annonce.Marque = null;
                annonce.Annee = null;
                annonce.Places = null;
            }
            else
            {
                annonce.Superficie = null;
                annonce.Couchages = null;
            }
        }
    }
}
