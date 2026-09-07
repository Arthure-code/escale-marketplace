using System.Security.Claims;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Escale.Web.Services
{
    public class UtilisateurCourant : IUtilisateurCourant
    {
        private readonly IHttpContextAccessor _accesseur;
        private readonly UserManager<Utilisateur> _comptes;

        public UtilisateurCourant(IHttpContextAccessor accesseur, UserManager<Utilisateur> comptes)
        {
            _accesseur = accesseur;
            _comptes = comptes;
        }

        public bool EstConnecte => Porteur?.Identity?.IsAuthenticated == true;

        public bool EstAdministrateur => Porteur?.IsInRole(Utilisateur.RoleAdministrateur) == true;

        public string Id => Porteur is null ? string.Empty : _comptes.GetUserId(Porteur) ?? string.Empty;

        public Task<Utilisateur?> ObtenirAsync() =>
            Porteur is null ? Task.FromResult<Utilisateur?>(null) : _comptes.GetUserAsync(Porteur);

        private ClaimsPrincipal? Porteur => _accesseur.HttpContext?.User;
    }
}
