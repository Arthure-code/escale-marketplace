using System;
using Escale.Web.Entites;
using Escale.Web.Interfaces;

namespace Escale.Web.Services.Regles
{
    public class AbonnementAJour : IRegleDeDiffusion
    {
        public bool Autorise(Utilisateur compte, DateTime date) =>
            compte.Abonnement.EstAJourLe(date);

        public EtatCompte Etat(Utilisateur compte) =>
            new EtatCompte(SituationCompte.AbonnementEchu, compte.Abonnement.Echeance, null);
    }
}
