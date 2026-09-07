using System.Net.Http;
using System.Text.RegularExpressions;

namespace Escale.Tests.Integration
{
    // Ce qu'il faut pour parler à l'application comme un navigateur : un hôte
    // à soi, un client qui garde les témoins, et de quoi lire le jeton
    // antifalsification dans la page servie.
    public abstract partial class SessionHttp : IAsyncLifetime
    {
        [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
        private static partial Regex Jeton();

        protected FabriqueEscale Fabrique { get; } = new FabriqueEscale();

        protected HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            Client = Fabrique.Client();
            await ApresOuvertureAsync();
        }

        public Task DisposeAsync()
        {
            Client.Dispose();
            Fabrique.Dispose();
            return Task.CompletedTask;
        }

        protected virtual Task ApresOuvertureAsync() => Task.CompletedTask;

        // Lire le jeton dans la page servie, c'est aussi vérifier au passage
        // que la protection est bien posée sur le formulaire.
        protected async Task<string> JetonDeAsync(string adresse)
        {
            string html = await Client.GetStringAsync(adresse);
            System.Text.RegularExpressions.Match trouve = Jeton().Match(html);

            Assert.True(trouve.Success, "Aucun jeton antifalsification sur " + adresse);
            return trouve.Groups[1].Value;
        }

        protected async Task<HttpResponseMessage> PosterAsync(string adresse,
            string jetonDe, params (string Cle, string Valeur)[] champs)
        {
            List<KeyValuePair<string, string>> corps = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", await JetonDeAsync(jetonDe))
            };

            foreach ((string cle, string valeur) in champs)
            {
                corps.Add(new KeyValuePair<string, string>(cle, valeur));
            }

            return await Client.PostAsync(adresse, new FormUrlEncodedContent(corps));
        }
    }
}
