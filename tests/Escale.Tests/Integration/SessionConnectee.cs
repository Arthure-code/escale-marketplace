using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // La page de connexion est limitée à huit requêtes par minute. Chaque
    // classe de tests ouvre donc sa session une seule fois, dans son propre
    // hôte, plutôt que de se reconnecter à chaque test.
    public abstract class SessionConnectee : SessionHttp
    {
        protected abstract string Courriel { get; }

        protected override async Task ApresOuvertureAsync()
        {
            using HttpResponseMessage reponse = await PosterAsync(
                "/Identity/Account/Login", "/Identity/Account/Login",
                ("Input.Email", Courriel),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.RememberMe", "false"));

            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        }
    }
}
