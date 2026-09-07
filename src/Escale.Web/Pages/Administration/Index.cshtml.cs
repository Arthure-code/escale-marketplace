using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Administration
{
    /// <summary>
    /// Vue d'ensemble de la plateforme : ce qui est publié, par qui, et ce que
    /// cela a rapporté. Rien n'est filtré par propriétaire ici.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IAnnonceService _annonces;
        private readonly IUtilisateurService _comptes;
        private readonly ICommandeService _commandes;

        private readonly IDiffusionService _diffusion;

        public IndexModel(IAnnonceService annonces, IUtilisateurService comptes,
            ICommandeService commandes, IDiffusionService diffusion)
        {
            _annonces = annonces;
            _comptes = comptes;
            _commandes = commandes;
            _diffusion = diffusion;
        }

        public List<Annonce> Annonces { get; private set; } = new List<Annonce>();

        public List<Utilisateur> Comptes { get; private set; } = new List<Utilisateur>();

        public Dictionary<string, string> Roles { get; private set; } = new Dictionary<string, string>();

        public List<LigneCommande> Reservations { get; private set; } = new List<LigneCommande>();

        public int EnLigne => Annonces.Count(a => a.EstDisponible && Diffusable(a));

        public int Retirees => Annonces.Count - EnLigne;

        public int Loueurs => Roles.Count(r => r.Value == Utilisateur.RoleLoueur);

        public int Bloques => Comptes.Count(c => !_diffusion.EstDiffusable(c, DateTime.Today));

        public int Volume => Reservations.Sum(r => r.SousTotal);

        public async Task OnGetAsync()
        {
            Annonces = await _annonces.ObtenirToutesAsync();
            Comptes = await _comptes.ObtenirTousAsync();
            Roles = await _comptes.ObtenirRolesAsync();

            Reservations = new List<LigneCommande>();
            foreach (Utilisateur compte in Comptes)
            {
                Reservations.AddRange(await _commandes.ObtenirVentesAsync(compte.Id));
            }

            Reservations = Reservations.OrderByDescending(r => r.DateDebut).ToList();
        }

        private bool Diffusable(Annonce annonce) =>
            _diffusion.EstDiffusable(annonce.Loueur, DateTime.Today);
    }
}
