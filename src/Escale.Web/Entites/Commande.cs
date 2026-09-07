using System;
using System.Collections.Generic;
using System.Linq;

namespace Escale.Web.Entites
{
    public class Commande
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public string UtilisateurId { get; set; } = string.Empty;

        public Utilisateur? Utilisateur { get; set; }

        public DateTime DateCommande { get; set; } = DateTime.UtcNow;

        // Trace du paiement simulé : jamais un numéro de carte complet.
        public string ReferencePaiement { get; set; } = string.Empty;

        public string QuatreDerniers { get; set; } = string.Empty;

        public List<LigneCommande> Lignes { get; set; } = new List<LigneCommande>();

        public int Total => Lignes.Sum(ligne => ligne.SousTotal);
    }

    // Les valeurs sont recopiées au moment de la commande : si le loueur
    // change son tarif demain, la commande d'hier ne bouge pas.
    public class LigneCommande
    {
        public int Id { get; set; }

        public int CommandeId { get; set; }

        public Commande? Commande { get; set; }

        public int AnnonceId { get; set; }

        public Annonce? Annonce { get; set; }

        public string LoueurId { get; set; } = string.Empty;

        public string Titre { get; set; } = string.Empty;

        public CategorieAnnonce Categorie { get; set; }

        public int PrixJournalier { get; set; }

        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }

        public int NombreDeJours { get; set; }

        public int SousTotal { get; set; }
    }
}
