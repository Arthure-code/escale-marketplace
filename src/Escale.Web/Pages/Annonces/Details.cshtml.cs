using System;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Annonces
{
    /// <summary>
    /// Fiche publique d'une annonce et ajout au panier.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly IAnnonceService _annonces;
        private readonly IPanierService _panier;
        private readonly IFavorisService _favoris;
        private readonly IUtilisateurCourant _utilisateur;

        public DetailsModel(IAnnonceService annonces, IPanierService panier,
            IFavorisService favoris, IUtilisateurCourant utilisateur)
        {
            _annonces = annonces;
            _panier = panier;
            _favoris = favoris;
            _utilisateur = utilisateur;
        }

        public Offre Offre { get; private set; } = new Offre(new Annonce(), 1, 1);

        public Annonce Annonce => Offre.Annonce;

        [BindProperty(SupportsGet = true)]
        public DateTime? Debut { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Fin { get; set; }

        public string Erreur { get; private set; } = string.Empty;

        public bool EstFavori { get; private set; }

        public int NombreDeJours => Math.Max(1, (Fin!.Value.Date - Debut!.Value.Date).Days);

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!await ChargerAsync(id))
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!await ChargerAsync(id))
            {
                return NotFound();
            }

            if (!_utilisateur.EstConnecte)
            {
                return RedirectToPage("/Identity/Account/Login", new { area = "Identity", returnUrl = $"/Annonces/Details/{id}" });
            }

            ResultatAjout resultat = await _panier.AjouterAsync(
                _utilisateur.Id, id, Debut!.Value, Fin!.Value);

            if (!resultat.Reussi)
            {
                Erreur = resultat.Message;
                return Page();
            }

            return RedirectToPage("/Panier/Index");
        }

        // Mettre en favori demande un compte : le visiteur anonyme est renvoyé
        // vers la connexion, qui lui explique pourquoi.
        public async Task<IActionResult> OnPostFavoriAsync(int id)
        {
            if (!_utilisateur.EstConnecte)
            {
                return RedirectToPage("/Identity/Account/Login",
                    new { area = "Identity", returnUrl = "/Annonces/Details/" + id });
            }

            await _favoris.BasculerAsync(_utilisateur.Id, id);

            return RedirectToPage(new { id });
        }

        private async Task<bool> ChargerAsync(int id)
        {
            Debut ??= DateTime.Today;
            Fin ??= DateTime.Today.AddDays(2);

            if (Fin <= Debut)
            {
                Fin = Debut.Value.AddDays(1);
            }

            Offre? trouvee = await _annonces.ObtenirOffreAsync(id, Debut!.Value, Fin!.Value);

            if (trouvee is null)
            {
                return false;
            }

            Offre = trouvee;

            if (_utilisateur.EstConnecte)
            {
                EstFavori = (await _favoris.ObtenirIdsAsync(_utilisateur.Id)).Contains(id);
            }

            return true;
        }
    }
}
