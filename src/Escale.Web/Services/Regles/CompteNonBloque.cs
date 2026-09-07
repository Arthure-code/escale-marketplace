using System;
using Escale.Web.Entites;
using Escale.Web.Interfaces;

namespace Escale.Web.Services.Regles
{
    public class CompteNonBloque : IRegleDeDiffusion
    {
        public bool Autorise(Utilisateur compte, DateTime date) =>
            compte.Blocage is null || !compte.Blocage.EstEnCoursLe(date);

        public EtatCompte Etat(Utilisateur compte) =>
            new EtatCompte(SituationCompte.Bloque, compte.Blocage?.Fin, compte.Blocage?.Motif);
    }
}
