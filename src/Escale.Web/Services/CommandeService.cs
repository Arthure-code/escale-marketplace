using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Infrastructure;
using Escale.Web.Interfaces;
using Microsoft.Extensions.Logging;

namespace Escale.Web.Services
{
    // Passage de commande : on revérifie chaque ligne, on demande le paiement,
    // puis on transforme le panier en commande. Rien n'est écrit si le
    // paiement est refusé.
    public class CommandeService : ICommandeService
    {
        private readonly IPanierRepository _panier;
        private readonly ICommandeRepository _commandes;
        private readonly IAnnonceService _annonces;
        private readonly IPaiementService _paiement;
        private readonly IFactureService _facture;
        private readonly ILogger<CommandeService> _journal;

        public CommandeService(IPanierRepository panier, ICommandeRepository commandes,
            IAnnonceService annonces, IPaiementService paiement, IFactureService facture,
            ILogger<CommandeService> journal)
        {
            _panier = panier;
            _commandes = commandes;
            _annonces = annonces;
            _paiement = paiement;
            _facture = facture;
            _journal = journal;
        }

        public async Task<ResultatCommande> PasserAsync(string utilisateurId, DonneesCarte carte)
        {
            List<LignePanier> lignes = await _panier.ObtenirAsync(utilisateurId);

            if (lignes.Count == 0)
            {
                return new ResultatCommande(false, "Votre panier est vide.", string.Empty);
            }

            foreach (LignePanier ligne in lignes)
            {
                if (!await _annonces.EstReservableAsync(ligne.AnnonceId, ligne.DateDebut, ligne.DateFin))
                {
                    _journal.LogInformation(JournalDeSecurite.ReservationImpossible,
                        "Commande interrompue, annonce {AnnonceId} indisponible pour {UtilisateurId}",
                        ligne.AnnonceId, utilisateurId);

                    return new ResultatCommande(false,
                        $"« {ligne.Annonce!.Titre} » n'est plus libre sur ces dates. Retirez cette ligne du panier.",
                        string.Empty);
                }
            }

            int total = lignes.Sum(l => l.SousTotal);

            // La référence est fixée avant le paiement : elle sert de clé
            // d'idempotence à la passerelle, pour qu'un même passage de
            // commande ne débite jamais deux fois.
            string reference = "ESC-" + DateTime.UtcNow.ToString("yyMMdd-HHmmss", CultureInfo.InvariantCulture);

            ResultatPaiement paiement = await _paiement.PayerAsync(
                new DemandePaiement(total, reference, carte));

            if (!paiement.Accepte)
            {
                // Le motif du refus, jamais la carte : ni numéro, ni date, ni
                // code de sécurité ne doivent apparaître dans un journal.
                _journal.LogWarning(JournalDeSecurite.PaiementRefuse,
                    "Paiement refusé pour {UtilisateurId}, montant {Montant}, motif {Motif}",
                    utilisateurId, total, paiement.Message);

                return new ResultatCommande(false, paiement.Message, string.Empty);
            }

            Commande commande = new Commande
            {
                Reference = reference,
                UtilisateurId = utilisateurId,
                ReferencePaiement = paiement.Reference,
                QuatreDerniers = paiement.QuatreDerniers,
                Lignes = lignes.Select(Recopier).ToList()
            };

            // Dernier rempart : entre la vérification plus haut et cette
            // écriture, un autre client a pu prendre la dernière unité.
            if (!await _commandes.AjouterSiDisponibleAsync(commande))
            {
                _journal.LogWarning(JournalDeSecurite.ReservationImpossible,
                    "Commande abandonnée à l'écriture, plus d'exemplaire libre pour {UtilisateurId}",
                    utilisateurId);

                return new ResultatCommande(false,
                    "Une des annonces vient d'être réservée par quelqu'un d'autre. Rien n'a été prélevé.",
                    string.Empty);
            }

            await _panier.SupprimerAsync(lignes);

            _journal.LogInformation(JournalDeSecurite.CommandePassee,
                "Commande {Reference} enregistrée pour {UtilisateurId}, {Lignes} ligne(s), montant {Montant}",
                commande.Reference, utilisateurId, commande.Lignes.Count, total);

            // La facture part après coup. Un échec d'envoi ne remet pas en
            // cause une commande déjà payée : on le journalise, c'est tout.
            try
            {
                await _facture.EnvoyerAsync(commande);
            }
            catch (System.Exception ex)
            {
                _journal.LogError(ex, "Facture non envoyée pour la commande {Reference}", commande.Reference);
            }

            return new ResultatCommande(true, paiement.Message, commande.Reference);
        }

        public Task<Commande?> ObtenirAsync(string reference, string utilisateurId) =>
            _commandes.ObtenirAsync(reference, utilisateurId);

        public Task<List<LigneCommande>> ObtenirVentesAsync(string loueurId) =>
            _commandes.ObtenirVentesAsync(loueurId);

        // Les valeurs sont figées ici : un changement de tarif demain ne
        // réécrit pas la commande d'aujourd'hui.
        private static LigneCommande Recopier(LignePanier ligne) => new LigneCommande
        {
            AnnonceId = ligne.AnnonceId,
            LoueurId = ligne.Annonce!.LoueurId,
            Titre = ligne.Annonce.Titre,
            Categorie = ligne.Annonce.Categorie,
            PrixJournalier = ligne.Annonce.PrixJournalier,
            DateDebut = ligne.DateDebut,
            DateFin = ligne.DateFin,
            NombreDeJours = ligne.NombreDeJours,
            SousTotal = ligne.SousTotal
        };
    }
}
