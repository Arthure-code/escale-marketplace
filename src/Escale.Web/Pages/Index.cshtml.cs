using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages
{
    /// <summary>
    /// Vitrine publique. Elle ne montre que les annonces rendues visibles par
    /// leur loueur et libres sur la période demandée.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IAnnonceService _annonces;
        private readonly IFavorisService _favoris;
        private readonly IUtilisateurCourant _utilisateur;

        public IndexModel(IAnnonceService annonces, IFavorisService favoris, IUtilisateurCourant utilisateur)
        {
            _annonces = annonces;
            _favoris = favoris;
            _utilisateur = utilisateur;
        }

        [BindProperty(SupportsGet = true)]
        public CategorieAnnonce? Categorie { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? Debut { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? Fin { get; set; }

        public List<Offre> Resultats { get; private set; } = new List<Offre>();

        public int Completes => Resultats.Count(o => o.EstComplete);

        public HashSet<int> Favoris { get; private set; } = new HashSet<int>();

        public string Message { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Debut ??= DateTime.Today;
            Fin ??= DateTime.Today.AddDays(2);

            if (Fin <= Debut)
            {
                Fin = Debut.Value.AddDays(1);
                Message = "La date de fin doit suivre la date de début. Elle a été replacée au lendemain.";
            }

            Resultats = await _annonces.RechercherAsync(Categorie, Debut.Value, Fin.Value);

            if (_utilisateur.EstConnecte)
            {
                Favoris = (await _favoris.ObtenirIdsAsync(_utilisateur.Id)).ToHashSet();
            }
        }

        // Mettre en favori demande un compte : le visiteur anonyme est renvoyé
        // vers la connexion, qui lui explique pourquoi.
        public async Task<IActionResult> OnPostFavoriAsync(int id)
        {
            if (!_utilisateur.EstConnecte)
            {
                return RedirectToPage("/Identity/Account/Login", new { area = "Identity", returnUrl = "/" });
            }

            await _favoris.BasculerAsync(_utilisateur.Id, id);

            return RedirectToPage(new { categorie = Categorie, debut = Debut, fin = Fin });
        }
    }
}
