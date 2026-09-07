using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Panier
{
    /// <summary>
    /// Panier de l'utilisateur connecté, séparé en chambres et en voitures.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IPanierService _panier;
        private readonly IUtilisateurCourant _utilisateur;

        public IndexModel(IPanierService panier, IUtilisateurCourant utilisateur)
        {
            _panier = panier;
            _utilisateur = utilisateur;
        }

        public Entites.Panier Contenu { get; private set; } = new Entites.Panier(new());

        public async Task OnGetAsync()
        {
            Contenu = await _panier.ObtenirAsync(_utilisateur.Id);
        }

        public async Task<IActionResult> OnPostRetirerAsync(int id)
        {
            await _panier.RetirerAsync(_utilisateur.Id, id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostViderAsync()
        {
            await _panier.ViderAsync(_utilisateur.Id);
            return RedirectToPage();
        }
    }
}
