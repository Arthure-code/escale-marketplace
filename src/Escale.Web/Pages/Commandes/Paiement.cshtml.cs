using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Commandes
{
    /// <summary>
    /// Paiement simulé. Les contrôles sont réels, la transaction ne l'est pas :
    /// rien n'est envoyé à une banque et aucun numéro complet n'est conservé.
    /// </summary>
    [EnableRateLimiting("sensible")]
    public class PaiementModel : PageModel
    {
        private readonly IPanierService _panier;
        private readonly ICommandeService _commandes;
        private readonly IUtilisateurCourant _utilisateur;

        public PaiementModel(IPanierService panier, ICommandeService commandes, IUtilisateurCourant utilisateur)
        {
            _panier = panier;
            _commandes = commandes;
            _utilisateur = utilisateur;
        }

        public Entites.Panier Contenu { get; private set; } = new Entites.Panier(new());

        public string Refus { get; private set; } = string.Empty;

        [BindProperty]
        public Carte Saisie { get; set; } = new Carte();

        public class Carte
        {
            [Required(ErrorMessage = "Le nom du titulaire est obligatoire.")]
            [Display(Name = "Titulaire de la carte")]
            public string Titulaire { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le numéro de carte est obligatoire.")]
            [Display(Name = "Numéro de carte")]
            public string Numero { get; set; } = string.Empty;

            [Required(ErrorMessage = "L'expiration est obligatoire.")]
            [RegularExpression(@"^\d{2}\s*/\s*\d{2}$", ErrorMessage = "Format attendu : MM/AA.")]
            [Display(Name = "Expiration")]
            public string Expiration { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le code de sécurité est obligatoire.")]
            [RegularExpression(@"^\d{3}$", ErrorMessage = "Le code compte trois chiffres.")]
            [Display(Name = "Code de sécurité")]
            public string Cvc { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await ChargerAsync();

            if (Contenu.EstVide)
            {
                return RedirectToPage("/Panier/Index");
            }

            Saisie.Titulaire = (await _utilisateur.ObtenirAsync())?.NomComplet ?? string.Empty;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await ChargerAsync();

            if (Contenu.EstVide)
            {
                return RedirectToPage("/Panier/Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            ResultatCommande resultat = await _commandes.PasserAsync(
                _utilisateur.Id,
                new DonneesCarte(Saisie.Titulaire, Saisie.Numero, Saisie.Expiration, Saisie.Cvc));

            if (!resultat.Reussi)
            {
                Refus = resultat.Message;
                return Page();
            }

            return RedirectToPage("/Commandes/Confirmation", new { reference = resultat.Reference });
        }

        private async Task ChargerAsync()
        {
            Contenu = await _panier.ObtenirAsync(_utilisateur.Id);
        }
    }
}
