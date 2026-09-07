using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Escale.Web.Entites
{
    public class Annonce
    {
        public int Id { get; set; }

        // Propriétaire de l'annonce. Un loueur ne voit et ne modifie que les siennes.
        public string LoueurId { get; set; } = string.Empty;

        public Utilisateur? Loueur { get; set; }

        [Display(Name = "Catégorie")]
        public CategorieAnnonce Categorie { get; set; }

        [Required(ErrorMessage = "Le titre est obligatoire.")]
        [MaxLength(120, ErrorMessage = "Le titre ne peut pas dépasser 120 caractères.")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description est obligatoire.")]
        [MaxLength(600, ErrorMessage = "La description ne peut pas dépasser 600 caractères.")]
        public string Description { get; set; } = string.Empty;

        [Range(1, 5000, ErrorMessage = "Le prix journalier doit se situer entre 1 et 5000 $.")]
        [Display(Name = "Prix journalier")]
        public int PrixJournalier { get; set; }

        [MaxLength(80)]
        public string Photo { get; set; } = "defaut.jpg";

        // Le loueur décide. À faux, l'annonce disparaît du site public
        // mais reste dans son tableau de bord.
        [Display(Name = "Visible sur le site")]
        public bool EstDisponible { get; set; } = true;

        public DateTime DatePublication { get; set; } = DateTime.UtcNow;

        [Display(Name = "Superficie en m²")]
        [Range(0, 2000, ErrorMessage = "La superficie doit se situer entre 0 et 2000 m².")]
        public int? Superficie { get; set; }

        [Display(Name = "Nombre de personnes")]
        [Range(0, 20, ErrorMessage = "Le nombre de personnes doit se situer entre 0 et 20.")]
        public int? Couchages { get; set; }

        [MaxLength(60)]
        public string? Marque { get; set; }

        [Display(Name = "Année")]
        [Range(1950, 2100, ErrorMessage = "L'année doit se situer entre 1950 et 2100.")]
        public int? Annee { get; set; }

        [Display(Name = "Nombre de places")]
        [Range(0, 12, ErrorMessage = "Le nombre de places doit se situer entre 0 et 12.")]
        public int? Places { get; set; }

        // Combien d'unités identiques le loueur met en location sous cette
        // annonce. Une chambre d'hôtes vaut 1, un parc de véhicules vaut le
        // nombre de véhicules.
        [Range(1, 200, ErrorMessage = "Le nombre d'exemplaires doit se situer entre 1 et 200.")]
        [Display(Name = "Exemplaires en location")]
        public int Exemplaires { get; set; } = 1;

        public List<AnnonceEquipement> Equipements { get; set; } = new List<AnnonceEquipement>();

        public string Resume
        {
            get
            {
                if (string.IsNullOrEmpty(Description))
                {
                    return string.Empty;
                }

                if (Description.Length <= 92)
                {
                    return Description;
                }

                return Description.Substring(0, 90).TrimEnd() + "…";
            }
        }

        public string Unite => Categorie == CategorieAnnonce.Chambre ? "par nuit" : "par jour";

        public string NomExemplaire => Categorie == CategorieAnnonce.Chambre ? "chambre" : "véhicule";
    }
}
