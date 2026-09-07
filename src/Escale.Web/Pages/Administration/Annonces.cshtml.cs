using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Administration
{
    /// <summary>
    /// Toutes les annonces de la plateforme. L'administrateur peut retirer du
    /// site ce qui ne respecte pas les règles, ou le supprimer.
    /// </summary>
    public class AnnoncesModel : PageModel
    {
        private readonly IAnnonceService _annonces;
        private readonly IUtilisateurCourant _utilisateur;
        private readonly IDiffusionService _diffusion;

        public AnnoncesModel(IAnnonceService annonces, IUtilisateurCourant utilisateur,
            IDiffusionService diffusion)
        {
            _annonces = annonces;
            _utilisateur = utilisateur;
            _diffusion = diffusion;
        }

        public List<Annonce> Annonces { get; private set; } = new List<Annonce>();

        [TempData]
        public string? Avis { get; set; }

        public async Task OnGetAsync()
        {
            Annonces = await _annonces.ObtenirToutesAsync();
        }

        public async Task<IActionResult> OnPostBasculerAsync(int id)
        {
            await _annonces.BasculerVisibiliteAsync(id, _utilisateur.Id, sansRestriction: true);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSupprimerAsync(int id)
        {
            Avis = await _annonces.SupprimerAsync(id, _utilisateur.Id, sansRestriction: true)
                ? "Annonce supprimée."
                : "Cette annonce a déjà été réservée, elle ne peut plus être supprimée. Retirez-la du site.";

            return RedirectToPage();
        }

        public bool Diffusable(Annonce annonce) =>
            _diffusion.EstDiffusable(annonce.Loueur, DateTime.Today);

        public EtatCompte Etat(Utilisateur compte) => _diffusion.Etat(compte, DateTime.Today);
    }
}
