using System.Collections.Generic;

namespace Escale.Web.Entites
{
    // Ce qui est inclus dans une annonce : douche, micro-ondes, boîte
    // automatique... Le code sert à choisir le pictogramme dans la vue.
    public class Equipement
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public CategorieAnnonce Categorie { get; set; }

        public List<AnnonceEquipement> Annonces { get; set; } = new List<AnnonceEquipement>();
    }

    public class AnnonceEquipement
    {
        public int AnnonceId { get; set; }

        public Annonce? Annonce { get; set; }

        public int EquipementId { get; set; }

        public Equipement? Equipement { get; set; }
    }
}
