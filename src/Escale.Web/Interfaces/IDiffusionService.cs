using System;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    public interface IDiffusionService
    {
        bool EstDiffusable(Utilisateur? compte, DateTime date);

        EtatCompte Etat(Utilisateur compte, DateTime date);
    }
}
