using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Escale.Web.Infrastructure
{
    // En-têtes que le navigateur applique de lui-même. ASP.NET Core fournit
    // HSTS et la redirection HTTPS ; les autres se posent en écrivant un
    // intergiciel en ligne, comme le documente Microsoft.
    //
    // La politique de sécurité du contenu n'autorise aucun script en ligne :
    // c'est la raison pour laquelle les scripts de la fiche et du formulaire
    // vivent dans wwwroot/js. 'unsafe-inline' reste sur les styles à cause
    // des attributs style= du balisage.
    public static class EntetesDeSecurite
    {
        private const string Politique =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data:; " +
            "connect-src 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "object-src 'none'";

        public static IApplicationBuilder UseEntetesDeSecurite(this IApplicationBuilder application) =>
            application.Use(async (contexte, suivant) =>
            {
                IHeaderDictionary entetes = contexte.Response.Headers;

                entetes.ContentSecurityPolicy = Politique;
                entetes.XContentTypeOptions = "nosniff";
                entetes.XFrameOptions = "DENY";
                entetes["Referrer-Policy"] = "strict-origin-when-cross-origin";
                entetes["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
                entetes.Remove("X-Powered-By");

                await suivant();
            });
    }
}
