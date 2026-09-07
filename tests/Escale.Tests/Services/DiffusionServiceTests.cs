using Escale.Web.Services.Regles;

namespace Escale.Tests.Services
{
    public class DiffusionServiceTests
    {
        private static readonly DateTime Aujourdhui = new DateTime(2026, 9, 7);

        private static Utilisateur Compte(Abonnement? abonnement = null, Blocage? blocage = null) =>
            new Utilisateur
            {
                NomComplet = "Marie Tremblay",
                Abonnement = abonnement ?? new Abonnement(),
                Blocage = blocage
            };

        private static DiffusionService Service(params IRegleDeDiffusion[] regles) =>
            new DiffusionService(regles);

        [Fact]
        public void EstDiffusable_AutoriseQuandToutesLesReglesAcceptent()
        {
            //Etant donné deux règles qui acceptent
            Mock<IRegleDeDiffusion> une = new Mock<IRegleDeDiffusion>(MockBehavior.Strict);
            Mock<IRegleDeDiffusion> autre = new Mock<IRegleDeDiffusion>(MockBehavior.Strict);
            Utilisateur compte = Compte();
            une.Setup(r => r.Autorise(compte, Aujourdhui)).Returns(true);
            autre.Setup(r => r.Autorise(compte, Aujourdhui)).Returns(true);

            //Lorsque
            bool diffusable = Service(une.Object, autre.Object).EstDiffusable(compte, Aujourdhui);

            //Alors
            Assert.True(diffusable);
        }

        [Fact]
        public void EstDiffusable_RefuseDesQuUneRegleRefuse()
        {
            //Etant donné une règle qui refuse
            Mock<IRegleDeDiffusion> regle = new Mock<IRegleDeDiffusion>(MockBehavior.Strict);
            Utilisateur compte = Compte();
            regle.Setup(r => r.Autorise(compte, Aujourdhui)).Returns(false);

            //Lorsque
            bool diffusable = Service(regle.Object).EstDiffusable(compte, Aujourdhui);

            //Alors
            Assert.False(diffusable);
        }

        [Fact]
        public void EstDiffusable_AutoriseUnCompteAbsent()
        {
            //Etant donné aucune règle et aucun compte, cas d'une annonce orpheline
            //Lorsque
            bool diffusable = Service().EstDiffusable(null, Aujourdhui);

            //Alors
            Assert.True(diffusable);
        }

        [Fact]
        public void Etat_RendActifQuandAucuneRegleNeRefuse()
        {
            //Etant donné
            Utilisateur compte = Compte();

            //Lorsque
            EtatCompte etat = Service(new CompteNonBloque(), new AbonnementAJour()).Etat(compte, Aujourdhui);

            //Alors
            Assert.Equal(SituationCompte.Actif, etat.Situation);
        }

        [Fact]
        public void Etat_RendLeMotifDeLaPremiereRegleQuiRefuse()
        {
            //Etant donné un compte à la fois bloqué et dont l'abonnement est échu
            Utilisateur compte = Compte(
                abonnement: new Abonnement { Echeance = Aujourdhui.AddDays(-1) },
                blocage: new Blocage { Debut = Aujourdhui.AddDays(-2), Motif = "Annonce non conforme" });

            //Lorsque l'ordre d'enregistrement place le blocage en premier
            EtatCompte etat = Service(new CompteNonBloque(), new AbonnementAJour()).Etat(compte, Aujourdhui);

            //Alors c'est le blocage qui est annoncé
            Assert.Equal(SituationCompte.Bloque, etat.Situation);
            Assert.Equal("Annonce non conforme", etat.Motif);
        }

        [Fact]
        public void Etat_ChangeDeMotifSiLOrdreDEnregistrementChange()
        {
            //Etant donné le même compte, les règles enregistrées dans l'autre sens
            Utilisateur compte = Compte(
                abonnement: new Abonnement { Echeance = Aujourdhui.AddDays(-1) },
                blocage: new Blocage { Debut = Aujourdhui.AddDays(-2), Motif = "Annonce non conforme" });

            //Lorsque
            EtatCompte etat = Service(new AbonnementAJour(), new CompteNonBloque()).Etat(compte, Aujourdhui);

            //Alors c'est l'abonnement qui est annoncé
            Assert.Equal(SituationCompte.AbonnementEchu, etat.Situation);
        }
    }
}
