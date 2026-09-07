using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Escale.Tests.Integration
{
    // Ouvre une session, active la double authentification avec un vrai code,
    // relève les codes de récupération, puis referme la session. Les tests qui
    // en héritent partent donc d'un compte protégé et déconnecté.
    public abstract partial class SessionAvecDeuxFacteurs : SessionConnectee
    {
        [GeneratedRegex("secret=([A-Z2-7]+)")]
        private static partial Regex Secret();

        [GeneratedRegex("<code class=\"recovery-code\">([^<]+)</code>")]
        private static partial Regex Recuperation();

        protected override string Courriel => FabriqueEscale.Voyageur;

        protected string Cle { get; private set; } = string.Empty;

        protected List<string> Codes { get; } = new List<string>();

        protected override async Task ApresOuvertureAsync()
        {
            await base.ApresOuvertureAsync();

            string page = await Client.GetStringAsync("/Identity/Account/Manage/EnableAuthenticator");
            Cle = Secret().Match(page).Groups[1].Value;
            Assert.NotEqual(string.Empty, Cle);

            using (HttpResponseMessage activation = await PosterAsync(
                "/Identity/Account/Manage/EnableAuthenticator",
                "/Identity/Account/Manage/EnableAuthenticator",
                ("Input.Code", CodeTotp.Calculer(Cle))))
            {
                Assert.Equal(HttpStatusCode.Redirect, activation.StatusCode);

                string codes = await Client.GetStringAsync(activation.Headers.Location!.ToString());
                foreach (System.Text.RegularExpressions.Match trouve in Recuperation().Matches(codes))
                {
                    Codes.Add(trouve.Groups[1].Value.Trim());
                }
            }

            Assert.NotEmpty(Codes);

            using HttpResponseMessage sortie = await PosterAsync(
                "/Identity/Account/Logout?returnUrl=%2F", "/Identity/Account/Manage/Index");
            Assert.Equal(HttpStatusCode.Redirect, sortie.StatusCode);
        }

        // Rend l'adresse de la page qui réclame le second facteur.
        protected async Task<string> DemanderLeSecondFacteurAsync()
        {
            using HttpResponseMessage connexion = await PosterAsync(
                "/Identity/Account/Login", "/Identity/Account/Login",
                ("Input.Email", Courriel),
                ("Input.Password", FabriqueEscale.MotDePasse),
                ("Input.RememberMe", "false"));

            Assert.Equal(HttpStatusCode.Redirect, connexion.StatusCode);
            string adresse = connexion.Headers.Location!.ToString();
            Assert.Contains("LoginWith2fa", adresse);

            return adresse;
        }
    }
}
