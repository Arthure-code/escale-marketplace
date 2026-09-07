using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages.Commandes
{
    /// <summary>
    /// Reçu de la commande. Un utilisateur ne peut consulter que les siennes.
    /// </summary>
    public class ConfirmationModel : PageModel
    {
        private readonly ICommandeService _commandes;
        private readonly IUtilisateurCourant _utilisateur;

        public ConfirmationModel(ICommandeService commandes, IUtilisateurCourant utilisateur)
        {
            _commandes = commandes;
            _utilisateur = utilisateur;
        }

        public Commande Commande { get; private set; } = new Commande();

        public async Task<IActionResult> OnGetAsync(string reference)
        {
            Commande? trouvee = await _commandes.ObtenirAsync(reference, _utilisateur.Id);

            if (trouvee is null)
            {
                return NotFound();
            }

            Commande = trouvee;
            return Page();
        }
    }
}
