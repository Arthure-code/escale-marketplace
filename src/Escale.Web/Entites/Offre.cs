using System;

namespace Escale.Web.Entites
{
    // Ce qu'une annonce propose sur une période précise. Le reste dépend des
    // dates demandées : il se recalcule à chaque recherche et n'est jamais
    // écrit en base.
    public record Offre(Annonce Annonce, int Total, int Restants)
    {
        public bool EstComplete => Restants <= 0;

        public bool EstDernier => Restants == 1 && Total > 1;

        // « chambre » ou « véhicule », accordé au nombre.
        public string Exemplaires(int nombre) =>
            Annonce.NomExemplaire + (nombre > 1 ? "s" : string.Empty);
    }
}
