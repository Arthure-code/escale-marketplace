using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Escale.Tests.Services
{
    public class FavorisServiceTests
    {
        private const string Utilisateur = "camille";
        private const string Cle = "escale:favoris:camille";

        private static readonly DateTime Arrivee = new DateTime(2026, 12, 10);
        private static readonly DateTime Depart = new DateTime(2026, 12, 14);

        private readonly Mock<IDistributedCache> _cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly FavorisService _service;

        // Champs plutôt que littéraux dans l'appel : un tableau constant
        // en argument est réalloué à chaque exécution du test.
        private static readonly int[] UnSeul = { 7 };
        private static readonly int[] LAutre = { 2 };
        private static readonly int[] DernierEnTete = { 9, 2, 5 };
        private static readonly int[] OrdreDuCache = { 9, 3 };

        public FavorisServiceTests()
        {
            _service = new FavorisService(_cache.Object, _annonces.Object);
        }

        // GetStringAsync et SetStringAsync sont des extensions : c'est la
        // méthode d'instance sous-jacente qu'il faut doubler.
        private void CacheContient(params int[] ids) =>
            _cache.Setup(c => c.GetAsync(Cle, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ids.ToList())));

        private void CacheVide() =>
            _cache.Setup(c => c.GetAsync(Cle, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

        private List<int> InterceptEcriture()
        {
            List<int> ecrit = new List<int>();
            _cache.Setup(c => c.SetAsync(Cle, It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (_, valeur, _, _) =>
                    {
                        ecrit.Clear();
                        ecrit.AddRange(JsonSerializer.Deserialize<List<int>>(Encoding.UTF8.GetString(valeur))!);
                    })
                .Returns(Task.CompletedTask);
            return ecrit;
        }

        [Fact]
        public async Task ObtenirIdsAsync_RendUneListeVideQuandLEntreeAExpire()
        {
            //Etant donné une entrée absente du cache
            CacheVide();

            //Lorsque
            List<int> ids = await _service.ObtenirIdsAsync(Utilisateur);

            //Alors perdre ses favoris est le comportement voulu, pas une erreur
            Assert.Empty(ids);
        }

        [Fact]
        public async Task BasculerAsync_AjouteUneAnnonceAbsente()
        {
            //Etant donné
            CacheVide();
            List<int> ecrit = InterceptEcriture();

            //Lorsque
            bool ajoute = await _service.BasculerAsync(Utilisateur, 7);

            //Alors
            Assert.True(ajoute);
            Assert.Equal(UnSeul, ecrit);
        }

        [Fact]
        public async Task BasculerAsync_RetireUneAnnonceDejaPresente()
        {
            //Etant donné
            CacheContient(2, 7);
            List<int> ecrit = InterceptEcriture();

            //Lorsque
            bool ajoute = await _service.BasculerAsync(Utilisateur, 7);

            //Alors
            Assert.False(ajoute);
            Assert.Equal(LAutre, ecrit);
        }

        [Fact]
        public async Task BasculerAsync_PlaceLeDernierAjouteEnTete()
        {
            //Etant donné
            CacheContient(2, 5);
            List<int> ecrit = InterceptEcriture();

            //Lorsque
            await _service.BasculerAsync(Utilisateur, 9);

            //Alors
            Assert.Equal(DernierEnTete, ecrit);
        }

        [Fact]
        public async Task BasculerAsync_EcritUneExpirationDeCinqJoursDepuisLaModification()
        {
            //Etant donné
            CacheVide();
            DistributedCacheEntryOptions? options = null;
            _cache.Setup(c => c.SetAsync(Cle, It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                    (_, _, o, _) => options = o)
                .Returns(Task.CompletedTask);

            //Lorsque
            await _service.BasculerAsync(Utilisateur, 1);

            //Alors le délai court depuis l'écriture, pas depuis la création
            Assert.Equal(TimeSpan.FromDays(5), options!.AbsoluteExpirationRelativeToNow);
            Assert.Equal(FavorisService.Duree, options.AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public async Task CompterAsync_RendLeNombreDIdentifiantsEnCache()
        {
            //Etant donné
            CacheContient(1, 2, 3);

            //Lorsque
            int nombre = await _service.CompterAsync(Utilisateur);

            //Alors
            Assert.Equal(3, nombre);
        }

        [Fact]
        public async Task ObtenirAsync_EcarteUneAnnonceQuiNEstPlusPublique()
        {
            //Etant donné deux favoris dont un dont le loueur est bloqué
            CacheContient(1, 2);
            Annonce annonce = new Annonce { Id = 2, Titre = "Chambre", Description = "d", Photo = "p.jpg" };
            _annonces.Setup(a => a.ObtenirOffreAsync(1, Arrivee, Depart)).ReturnsAsync((Offre?)null);
            _annonces.Setup(a => a.ObtenirOffreAsync(2, Arrivee, Depart))
                .ReturnsAsync(new Offre(annonce, 1, 1));

            //Lorsque
            List<Offre> favoris = await _service.ObtenirAsync(Utilisateur, Arrivee, Depart);

            //Alors elle reste en cache mais ne s'affiche pas
            Assert.Single(favoris);
            Assert.Equal(2, favoris[0].Annonce.Id);
        }

        [Fact]
        public async Task ObtenirAsync_ConserveLOrdreDuCache()
        {
            //Etant donné
            CacheContient(9, 3);
            _annonces.Setup(a => a.ObtenirOffreAsync(It.IsAny<int>(), Arrivee, Depart))
                .ReturnsAsync((int id, DateTime _, DateTime _) =>
                    new Offre(new Annonce { Id = id, Titre = "t", Description = "d", Photo = "p.jpg" }, 1, 1));

            //Lorsque
            List<Offre> favoris = await _service.ObtenirAsync(Utilisateur, Arrivee, Depart);

            //Alors
            Assert.Equal(OrdreDuCache, favoris.Select(f => f.Annonce.Id));
        }
    }
}
