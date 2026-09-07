using System;

namespace Escale.Web.Entites
{
    // Le panier vit en base, pas en session : il survit à la déconnexion.
    public class LignePanier
    {
        public int Id { get; set; }

        public string UtilisateurId { get; set; } = string.Empty;

        public int AnnonceId { get; set; }

        public Annonce? Annonce { get; set; }

        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }

        public int NombreDeJours => Math.Max(1, (DateFin.Date - DateDebut.Date).Days);

        public int SousTotal => Annonce is null ? 0 : NombreDeJours * Annonce.PrixJournalier;
    }
}
