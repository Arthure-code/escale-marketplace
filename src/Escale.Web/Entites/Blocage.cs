using System;

namespace Escale.Web.Entites
{
    // Une sanction posée par un administrateur, avec sa propre règle de
    // validité. Le compte ne sait pas ce qu'est un blocage, il en porte un.
    public class Blocage
    {
        public DateTime? Debut { get; set; }

        // Nulle, la fin veut dire « jusqu'à nouvel ordre ».
        public DateTime? Fin { get; set; }

        public string? Motif { get; set; }

        public bool EstEnCoursLe(DateTime date) =>
            Debut is not null
            && date.Date >= Debut.Value.Date
            && (Fin is null || date.Date <= Fin.Value.Date);
    }
}
