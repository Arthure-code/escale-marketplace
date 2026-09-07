using System;
using System.Collections.Generic;
using System.Linq;
using Escale.Web.Entites;
using Escale.Web.Interfaces;

namespace Escale.Web.Services
{
    // Applique toutes les règles enregistrées. Le service ne les connaît pas :
    // il reçoit la collection et les interroge dans l'ordre d'enregistrement,
    // qui décide aussi quel motif est annoncé quand plusieurs échouent.
    public class DiffusionService : IDiffusionService
    {
        private readonly IReadOnlyList<IRegleDeDiffusion> _regles;

        public DiffusionService(IEnumerable<IRegleDeDiffusion> regles)
        {
            _regles = regles.ToList();
        }

        public bool EstDiffusable(Utilisateur? compte, DateTime date) =>
            compte is null || _regles.All(regle => regle.Autorise(compte, date));

        public EtatCompte Etat(Utilisateur compte, DateTime date)
        {
            IRegleDeDiffusion? refusee = _regles.FirstOrDefault(regle => !regle.Autorise(compte, date));

            return refusee is null ? EtatCompte.Actif : refusee.Etat(compte);
        }
    }
}
