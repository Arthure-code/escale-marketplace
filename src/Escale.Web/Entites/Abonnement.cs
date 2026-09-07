using System;

namespace Escale.Web.Entites
{
    // L'abonnement d'un loueur, avec sa propre règle d'échéance. Un voyageur
    // en porte un dont l'échéance reste nulle, donc toujours à jour.
    public class Abonnement
    {
        public DateTime? Debut { get; set; }

        public int Mensualite { get; set; }

        public DateTime? Echeance { get; set; }

        public bool EstAJourLe(DateTime date) =>
            Echeance is null || date.Date <= Echeance.Value.Date;
    }
}
