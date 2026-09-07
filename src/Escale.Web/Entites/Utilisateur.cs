using System;
using Microsoft.AspNetCore.Identity;

namespace Escale.Web.Entites
{
    // Un compte : qui il est, depuis quand, et les deux états qu'un
    // administrateur peut lui poser. Il porte ses données, il ne décide pas
    // de ce qu'elles autorisent : cette question appartient aux règles de
    // diffusion, et sa formulation appartient aux vues.
    public class Utilisateur : IdentityUser
    {
        public const string RoleVoyageur = "Voyageur";
        public const string RoleLoueur = "Loueur";
        public const string RoleAdministrateur = "Administrateur";

        [PersonalData]
        public string NomComplet { get; set; } = string.Empty;

        public DateTime DateInscription { get; set; } = DateTime.UtcNow;

        public Abonnement Abonnement { get; set; } = new Abonnement();

        // Nul tant qu'aucun blocage n'a été posé.
        public Blocage? Blocage { get; set; }
    }
}
