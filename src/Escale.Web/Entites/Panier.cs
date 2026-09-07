using System.Collections.Generic;
using System.Linq;

namespace Escale.Web.Entites
{
    // Le panier d'un utilisateur, déjà trié par catégorie. La page affiche,
    // elle ne regroupe pas.
    public class Panier
    {
        // Les deux groupes sont calculés une fois, à la construction. En
        // propriété, chaque lecture refaisait le tri et rendait une nouvelle
        // liste, ce qu'une propriété ne doit pas faire : la page les lit
        // plusieurs fois pour l'affichage et pour les sous-totaux.
        public Panier(List<LignePanier> lignes)
        {
            Lignes = lignes;
            Chambres = lignes.Where(l => Categorie(l) == CategorieAnnonce.Chambre).ToList();
            Voitures = lignes.Where(l => Categorie(l) == CategorieAnnonce.Voiture).ToList();
        }

        public List<LignePanier> Lignes { get; }

        public List<LignePanier> Chambres { get; }

        public List<LignePanier> Voitures { get; }

        private static CategorieAnnonce? Categorie(LignePanier ligne) => ligne.Annonce?.Categorie;

        public int Total => Lignes.Sum(l => l.SousTotal);

        public bool EstVide => Lignes.Count == 0;
    }
}
