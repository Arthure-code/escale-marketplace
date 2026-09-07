using System.Collections.Generic;
using System.Linq;

namespace Escale.Web.Entites
{
    // Le panier d'un utilisateur, déjà trié par catégorie. La page affiche,
    // elle ne regroupe pas.
    public class Panier
    {
        public Panier(List<LignePanier> lignes)
        {
            Lignes = lignes;
        }

        public List<LignePanier> Lignes { get; }

        public List<LignePanier> Chambres =>
            Lignes.Where(l => l.Annonce!.Categorie == CategorieAnnonce.Chambre).ToList();

        public List<LignePanier> Voitures =>
            Lignes.Where(l => l.Annonce!.Categorie == CategorieAnnonce.Voiture).ToList();

        public int Total => Lignes.Sum(l => l.SousTotal);

        public bool EstVide => Lignes.Count == 0;
    }
}
