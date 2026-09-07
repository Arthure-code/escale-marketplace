using System.Globalization;
using System.Net;
using System.Net.Http;

namespace Escale.Tests.Integration
{
    // La seule classe qui dépasse volontairement la limitation de débit. Elle
    // a son propre hôte, donc son propre compteur : elle ne prive aucune autre
    // classe de ses requêtes.
    public class DebitDepasseTests : SessionHttp
    {
        [Fact]
        public async Task LaPageDeConnexionFinitParRefuserEtDitQuandRevenir()
        {
            //Etant donné la politique posée sur les pages sensibles
            HttpResponseMessage? refus = null;

            //Lorsqu'on insiste au delà de ce qu'elle autorise
            for (int essai = 0; essai < 20 && refus is null; essai++)
            {
                HttpResponseMessage reponse = await Client.GetAsync("/Identity/Account/Login");

                if (reponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    refus = reponse;
                }
                else
                {
                    reponse.Dispose();
                }
            }

            //Alors la demande est refusée
            Assert.NotNull(refus);

            using (refus)
            {
                //Et le délai d'attente est annoncé
                Assert.True(refus.Headers.TryGetValues("Retry-After", out IEnumerable<string>? delai));
                Assert.True(int.Parse(delai!.Single(), NumberFormatInfo.InvariantInfo) > 0);

                //Et le message porte son jeu de caractères, sans quoi les
                //accents s'afficheraient de travers
                Assert.Equal("utf-8", refus.Content.Headers.ContentType!.CharSet);
                Assert.Contains("Trop de requêtes", await refus.Content.ReadAsStringAsync());
            }
        }
    }
}
