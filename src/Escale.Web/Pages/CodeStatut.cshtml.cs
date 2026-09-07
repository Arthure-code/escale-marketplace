using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Escale.Web.Pages
{
    /// <summary>
    /// Page unique pour les codes d'état renvoyés sans contenu, réexécutée
    /// par le pipeline à la place de la réponse vide.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class CodeStatutModel : PageModel
    {
        public int Code { get; private set; }

        public string Titre { get; private set; } = string.Empty;

        public string Explication { get; private set; } = string.Empty;

        public string? CheminDemande { get; private set; }

        public void OnGet(int code)
        {
            Code = code;
            CheminDemande = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;

            (Titre, Explication) = code switch
            {
                404 => ("Cette page n'existe pas", "L'adresse est peut-être mal écrite, ou l'annonce a été retirée."),
                403 => ("Accès refusé", "Votre compte n'a pas les droits nécessaires pour cette page."),
                401 => ("Connexion requise", "Connectez-vous pour accéder à cette page."),
                _ => ("Requête impossible", "Le serveur n'a pas pu traiter cette demande.")
            };
        }
    }
}
