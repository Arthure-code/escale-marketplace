using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Administration
{
    /// <summary>
    /// Les comptes de la plateforme. Bloquer un compte retire son contenu du
    /// site pour la durée choisie, sans rien supprimer.
    /// </summary>
    public class ComptesModel : PageModel
    {
        private readonly IUtilisateurService _comptes;
        private readonly IDiffusionService _diffusion;

        public ComptesModel(IUtilisateurService comptes, IDiffusionService diffusion)
        {
            _comptes = comptes;
            _diffusion = diffusion;
        }

        public List<Utilisateur> Comptes { get; private set; } = new List<Utilisateur>();

        public Dictionary<string, string> Roles { get; private set; } = new Dictionary<string, string>();

        [TempData]
        public string? Avis { get; set; }

        [BindProperty]
        public Blocage Saisie { get; set; } = new Blocage();

        public class Blocage
        {
            public string Id { get; set; } = string.Empty;

            [Required(ErrorMessage = "La date de début est obligatoire.")]
            [DataType(DataType.Date)]
            [Display(Name = "Début du blocage")]
            public DateTime? Debut { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Fin du blocage")]
            public DateTime? Fin { get; set; }

            [MaxLength(200, ErrorMessage = "Le motif ne peut pas dépasser 200 caractères.")]
            public string? Motif { get; set; }

            // Coché, la fin est ignorée : le blocage court jusqu'à ce qu'un
            // administrateur le lève.
            [Display(Name = "Jusqu'à nouvel ordre")]
            public bool Indetermine { get; set; }
        }

        public async Task OnGetAsync()
        {
            await ChargerAsync();
        }

        public async Task<IActionResult> OnPostBloquerAsync()
        {
            if (!ModelState.IsValid)
            {
                await ChargerAsync();
                return Page();
            }

            ResultatAjout resultat = await _comptes.BloquerAsync(
                Saisie.Id,
                Saisie.Debut!.Value,
                Saisie.Indetermine ? null : Saisie.Fin,
                Saisie.Motif ?? string.Empty);

            Avis = resultat.Message;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDebloquerAsync(string id)
        {
            Avis = await _comptes.DebloquerAsync(id)
                ? "Compte débloqué. Son contenu revient sur le site."
                : "Ce compte est introuvable.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostProlongerAsync(string id, DateTime? echeance)
        {
            Avis = await _comptes.ProlongerAbonnementAsync(id, echeance)
                ? "Abonnement mis à jour."
                : "Ce compte est introuvable.";

            return RedirectToPage();
        }

        private async Task ChargerAsync()
        {
            Comptes = await _comptes.ObtenirTousAsync();
            Roles = await _comptes.ObtenirRolesAsync();
        }

        public EtatCompte Etat(Utilisateur compte) => _diffusion.Etat(compte, DateTime.Today);

        public string Role(string id) => Roles.TryGetValue(id, out string? role) ? role : "Voyageur";
    }
}
