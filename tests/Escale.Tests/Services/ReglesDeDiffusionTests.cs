using Escale.Web.Services.Regles;

namespace Escale.Tests.Services
{
    public class ReglesDeDiffusionTests
    {
        private static readonly DateTime Aujourdhui = new DateTime(2026, 9, 7);

        private static Utilisateur Compte(Abonnement? abonnement = null, Blocage? blocage = null) =>
            new Utilisateur
            {
                NomComplet = "Hugo Bélanger",
                Abonnement = abonnement ?? new Abonnement(),
                Blocage = blocage
            };

        [Fact]
        public void AbonnementAJour_AutoriseUnAbonnementCourant()
        {
            //Etant donné
            Utilisateur compte = Compte(new Abonnement { Echeance = Aujourdhui.AddDays(10) });

            //Lorsque
            bool autorise = new AbonnementAJour().Autorise(compte, Aujourdhui);

            //Alors
            Assert.True(autorise);
        }

        [Fact]
        public void AbonnementAJour_RefuseUnAbonnementEchu()
        {
            //Etant donné
            Utilisateur compte = Compte(new Abonnement { Echeance = Aujourdhui.AddDays(-1) });

            //Lorsque
            bool autorise = new AbonnementAJour().Autorise(compte, Aujourdhui);

            //Alors
            Assert.False(autorise);
        }

        [Fact]
        public void AbonnementAJour_AnnonceLEcheanceDansSonEtat()
        {
            //Etant donné
            DateTime echeance = Aujourdhui.AddDays(-1);
            Utilisateur compte = Compte(new Abonnement { Echeance = echeance });

            //Lorsque
            EtatCompte etat = new AbonnementAJour().Etat(compte);

            //Alors la vue reçoit la date, pas une phrase toute faite
            Assert.Equal(SituationCompte.AbonnementEchu, etat.Situation);
            Assert.Equal(echeance, etat.Jusquau);
            Assert.Null(etat.Motif);
        }

        [Fact]
        public void CompteNonBloque_AutoriseUnCompteSansBlocage()
        {
            //Etant donné
            Utilisateur compte = Compte();

            //Lorsque
            bool autorise = new CompteNonBloque().Autorise(compte, Aujourdhui);

            //Alors
            Assert.True(autorise);
        }

        [Fact]
        public void CompteNonBloque_RefuseUnBlocageEnCours()
        {
            //Etant donné
            Utilisateur compte = Compte(blocage: new Blocage { Debut = Aujourdhui.AddDays(-1) });

            //Lorsque
            bool autorise = new CompteNonBloque().Autorise(compte, Aujourdhui);

            //Alors
            Assert.False(autorise);
        }

        [Fact]
        public void CompteNonBloque_AutoriseDeNouveauUnBlocageTermine()
        {
            //Etant donné un blocage arrivé à son terme
            Utilisateur compte = Compte(blocage: new Blocage
            {
                Debut = Aujourdhui.AddDays(-10),
                Fin = Aujourdhui.AddDays(-1)
            });

            //Lorsque
            bool autorise = new CompteNonBloque().Autorise(compte, Aujourdhui);

            //Alors les annonces reviennent d'elles-mêmes
            Assert.True(autorise);
        }

        [Fact]
        public void CompteNonBloque_AnnonceLaFinEtLeMotifDansSonEtat()
        {
            //Etant donné
            DateTime fin = Aujourdhui.AddDays(3);
            Utilisateur compte = Compte(blocage: new Blocage
            {
                Debut = Aujourdhui.AddDays(-1),
                Fin = fin,
                Motif = "Annonce non conforme"
            });

            //Lorsque
            EtatCompte etat = new CompteNonBloque().Etat(compte);

            //Alors
            Assert.Equal(SituationCompte.Bloque, etat.Situation);
            Assert.Equal(fin, etat.Jusquau);
            Assert.Equal("Annonce non conforme", etat.Motif);
        }
    }
}
