using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;

namespace Escale.Web.Services
{
    public class PanierService : IPanierService
    {
        private readonly IPanierRepository _panier;
        private readonly IAnnonceService _annonces;

        public PanierService(IPanierRepository panier, IAnnonceService annonces)
        {
            _panier = panier;
            _annonces = annonces;
        }

        public async Task<Panier> ObtenirAsync(string utilisateurId) =>
            new Panier(await _panier.ObtenirAsync(utilisateurId));

        public Task<int> CompterAsync(string utilisateurId) => _panier.CompterAsync(utilisateurId);

        public async Task<ResultatAjout> AjouterAsync(string utilisateurId, int annonceId, DateTime debut, DateTime fin)
        {
            if (fin.Date <= debut.Date)
            {
                return new ResultatAjout(false, "La date de départ doit suivre la date d'arrivée.");
            }

            if (!await _annonces.EstReservableAsync(annonceId, debut, fin))
            {
                return new ResultatAjout(false, "Cette annonce n'est pas libre sur ces dates.");
            }

            if (await _panier.ContientAsync(utilisateurId, annonceId))
            {
                return new ResultatAjout(false, "Cette annonce est déjà dans votre panier.");
            }

            await _panier.AjouterAsync(new LignePanier
            {
                UtilisateurId = utilisateurId,
                AnnonceId = annonceId,
                DateDebut = debut.Date,
                DateFin = fin.Date
            });

            return new ResultatAjout(true, "Annonce ajoutée au panier.");
        }

        public async Task RetirerAsync(string utilisateurId, int ligneId)
        {
            LignePanier? ligne = await _panier.ObtenirLigneAsync(ligneId, utilisateurId);

            if (ligne is not null)
            {
                await _panier.SupprimerAsync(new[] { ligne });
            }
        }

        public async Task ViderAsync(string utilisateurId)
        {
            List<LignePanier> lignes = await _panier.ObtenirAsync(utilisateurId);
            await _panier.SupprimerAsync(lignes);
        }
    }
}
