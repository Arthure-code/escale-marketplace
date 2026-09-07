using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Escale.Web.Services
{
    // Une entrée de cache par utilisateur, contenant la liste ordonnée des
    // identifiants mis en favori. Cinq jours après la dernière modification,
    // l'entrée expire d'elle-même.
    public class FavorisService : IFavorisService
    {
        public static readonly TimeSpan Duree = TimeSpan.FromDays(5);

        private const string Prefixe = "escale:favoris:";

        private readonly IDistributedCache _cache;
        private readonly IAnnonceService _annonces;

        public FavorisService(IDistributedCache cache, IAnnonceService annonces)
        {
            _cache = cache;
            _annonces = annonces;
        }

        public async Task<List<int>> ObtenirIdsAsync(string utilisateurId)
        {
            string? contenu = await _cache.GetStringAsync(Cle(utilisateurId));

            if (string.IsNullOrEmpty(contenu))
            {
                return new List<int>();
            }

            return JsonSerializer.Deserialize<List<int>>(contenu) ?? new List<int>();
        }

        // Une annonce retirée du site ou dont le loueur est bloqué reste dans
        // l'entrée de cache, mais ne s'affiche plus dans les favoris.
        public async Task<List<Offre>> ObtenirAsync(string utilisateurId, DateTime debut, DateTime fin)
        {
            List<Offre> favoris = new List<Offre>();

            foreach (int id in await ObtenirIdsAsync(utilisateurId))
            {
                Offre? offre = await _annonces.ObtenirOffreAsync(id, debut, fin);

                if (offre is not null)
                {
                    favoris.Add(offre);
                }
            }

            return favoris;
        }

        public async Task<bool> BasculerAsync(string utilisateurId, int annonceId)
        {
            List<int> ids = await ObtenirIdsAsync(utilisateurId);

            bool ajoute = !ids.Remove(annonceId);

            if (ajoute)
            {
                ids.Insert(0, annonceId);
            }

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Duree
            };

            await _cache.SetStringAsync(Cle(utilisateurId), JsonSerializer.Serialize(ids), options);

            return ajoute;
        }

        public async Task<int> CompterAsync(string utilisateurId) =>
            (await ObtenirIdsAsync(utilisateurId)).Count;

        private static string Cle(string utilisateurId) => Prefixe + utilisateurId;
    }
}
