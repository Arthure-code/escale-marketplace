using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Tableau
{
    /// <summary>
    /// Publication et modification d'une annonce. Un loueur ne voit que les
    /// siennes ; un administrateur passe partout.
    /// </summary>
    public class AnnonceModel : PageModel
    {
        private readonly IAnnonceService _annonces;
        private readonly IEquipementService _equipements;
        private readonly IPhotoService _photos;
        private readonly IUtilisateurCourant _utilisateur;

        public AnnonceModel(IAnnonceService annonces, IEquipementService equipements,
            IPhotoService photos, IUtilisateurCourant utilisateur)
        {
            _annonces = annonces;
            _equipements = equipements;
            _photos = photos;
            _utilisateur = utilisateur;
        }

        [BindProperty]
        public Entites.Annonce Saisie { get; set; } = new Entites.Annonce();

        [BindProperty]
        public List<int> EquipementsChoisis { get; set; } = new List<int>();

        [BindProperty]
        public IFormFile? Fichier { get; set; }

        public List<Equipement> Equipements { get; private set; } = new List<Equipement>();

        public List<string> Photos { get; private set; } = new List<string>();

        public bool Modification => Saisie.Id != 0;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id is not null)
            {
                Entites.Annonce? existante = await _annonces.ObtenirPourModificationAsync(
                    id.Value, _utilisateur.Id, _utilisateur.EstAdministrateur);

                if (existante is null)
                {
                    return NotFound();
                }

                Saisie = existante;
                EquipementsChoisis = existante.Equipements.Select(e => e.EquipementId).ToList();
            }

            await PreparerAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Saisie.LoueurId");
            ModelState.Remove("Fichier");

            if (Fichier is not null && Fichier.Length > 0)
            {
                using Stream contenu = Fichier.OpenReadStream();
                ResultatPhoto photo = await _photos.TeleverserAsync(contenu, Fichier.FileName, Fichier.Length);

                if (photo.Reussi)
                {
                    Saisie.Photo = photo.NomFichier;
                    ModelState.Remove("Saisie.Photo");
                }
                else
                {
                    ModelState.AddModelError("Fichier", photo.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                await PreparerAsync();
                return Page();
            }

            if (Saisie.Id == 0)
            {
                await _annonces.PublierAsync(Saisie, _utilisateur.Id, EquipementsChoisis);
            }
            else if (!await _annonces.ModifierAsync(
                Saisie, _utilisateur.Id, EquipementsChoisis, _utilisateur.EstAdministrateur))
            {
                return NotFound();
            }

            return RedirectToPage(_utilisateur.EstAdministrateur ? "/Administration/Annonces" : "/Tableau/Index");
        }

        private async Task PreparerAsync()
        {
            Equipements = await _equipements.ObtenirTousAsync();
            Photos = _photos.Catalogue();
        }
    }
}
