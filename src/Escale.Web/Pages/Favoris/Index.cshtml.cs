using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Favoris
{
    /// <summary>
    /// Les annonces mises de côté par l'utilisateur connecté. Elles vivent en
    /// cache et disparaissent d'elles-mêmes au bout de cinq jours.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IFavorisService _favoris;
        private readonly IUtilisateurCourant _utilisateur;

        public IndexModel(IFavorisService favoris, IUtilisateurCourant utilisateur)
        {
            _favoris = favoris;
            _utilisateur = utilisateur;
        }

        public List<Offre> Offres { get; private set; } = new List<Offre>();

        // Les favoris n'ont pas de sélecteur de dates : on montre ce qui est
        // disponible sur la période proposée par défaut sur l'accueil.
        public DateTime Debut { get; } = DateTime.Today;

        public DateTime Fin { get; } = DateTime.Today.AddDays(2);

        public HashSet<int> Marques { get; private set; } = new HashSet<int>();

        public int Retirees { get; private set; }

        public async Task OnGetAsync()
        {
            await ChargerAsync();
        }

        public async Task<IActionResult> OnPostFavoriAsync(int id)
        {
            await _favoris.BasculerAsync(_utilisateur.Id, id);
            return RedirectToPage();
        }

        private async Task ChargerAsync()
        {
            string utilisateur = _utilisateur.Id;

            Offres = await _favoris.ObtenirAsync(utilisateur, Debut, Fin);
            Marques = (await _favoris.ObtenirIdsAsync(utilisateur)).ToHashSet();
            Retirees = Marques.Count - Offres.Count;
        }
    }
}
