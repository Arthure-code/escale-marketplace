using System;
using Escale.Web.Entites;

namespace Escale.Web.Interfaces
{
    // Une condition, et une seule, pour qu'un compte puisse diffuser ses
    // annonces. Ajouter un motif de retrait se fait en écrivant une nouvelle
    // classe et en l'enregistrant, sans toucher aux règles existantes ni au
    // service qui les applique.
    public interface IRegleDeDiffusion
    {
        bool Autorise(Utilisateur compte, DateTime date);

        // Ce que la règle a à dire quand elle refuse.
        EtatCompte Etat(Utilisateur compte);
    }
}
