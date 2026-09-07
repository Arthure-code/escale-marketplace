using System;

namespace Escale.Web.Entites
{
    public enum SituationCompte
    {
        Actif,
        Bloque,
        AbonnementEchu
    }

    // Ce qu'une vue a besoin de savoir pour décrire un compte, sans phrase
    // toute faite : la situation, la date qui la borne, et le motif s'il y en
    // a un. La formulation appartient à la vue, pas à l'entité.
    public record EtatCompte(SituationCompte Situation, DateTime? Jusquau, string? Motif)
    {
        public static readonly EtatCompte Actif =
            new EtatCompte(SituationCompte.Actif, null, null);

        public bool EstActif => Situation == SituationCompte.Actif;
    }
}
