using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Tableau
{
    /// <summary>
    /// Tableau de bord du loueur : ses annonces, leur visibilité et ce qu'elles
    /// ont rapporté. Le service ne rend que ce qui lui appartient.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IAnnonceService _annonces;
        private readonly ICommandeService _commandes;
        private readonly IUtilisateurCourant _utilisateur;
        private readonly IDiffusionService _diffusion;

        public IndexModel(IAnnonceService annonces, ICommandeService commandes,
            IUtilisateurCourant utilisateur, IDiffusionService diffusion)
        {
            _annonces = annonces;
            _commandes = commandes;
            _utilisateur = utilisateur;
            _diffusion = diffusion;
        }

        public List<Annonce> Annonces { get; private set; } = new List<Annonce>();

        public List<LigneCommande> Ventes { get; private set; } = new List<LigneCommande>();

        public Utilisateur? Compte { get; private set; }

        public int Revenu => Ventes.Sum(v => v.SousTotal);

        public int JoursLoues => Ventes.Sum(v => v.NombreDeJours);

        // Le compte peut diffuser, ou non : une annonce marquée visible sur un
        // compte bloqué n'est pas en ligne pour autant.
        public bool CompteDiffuse => Compte is null || _diffusion.EstDiffusable(Compte, DateTime.Today);

        public int EnLigne => CompteDiffuse ? Annonces.Count(a => a.EstDisponible) : 0;

        // La page formule l'alerte : le service dit ce qui se passe, la vue
        // dit comment on l'annonce au loueur.
        public string? Alerte
        {
            get
            {
                if (Compte is null)
                {
                    return null;
                }

                EtatCompte etat = _diffusion.Etat(Compte, DateTime.Today);

                return etat.Situation switch
                {
                    SituationCompte.Bloque => "Votre compte est bloqué. Vos annonces ne sont pas visibles du public. "
                        + (etat.Jusquau is null
                            ? "Le blocage court jusqu'à nouvel ordre."
                            : "Il prend fin le " + etat.Jusquau.Value.ToString("d MMMM yyyy") + "."),

                    SituationCompte.AbonnementEchu => "Votre abonnement est échu depuis le "
                        + etat.Jusquau!.Value.ToString("d MMMM yyyy")
                        + ". Vos annonces restent enregistrées mais ne s'affichent plus sur le site.",

                    _ => null
                };
            }
        }

        public async Task OnGetAsync()
        {
            string loueur = _utilisateur.Id;

            Compte = await _utilisateur.ObtenirAsync();
            Annonces = await _annonces.ObtenirDuLoueurAsync(loueur);
            Ventes = await _commandes.ObtenirVentesAsync(loueur);
        }

        public async Task<IActionResult> OnPostBasculerAsync(int id)
        {
            await _annonces.BasculerVisibiliteAsync(id, _utilisateur.Id);
            return RedirectToPage();
        }
    }
}
