namespace Escale.Tests.Entites
{
    public class BlocageTests
    {
        private static readonly DateTime Aujourdhui = new DateTime(2026, 9, 7);

        [Fact]
        public void EstEnCoursLe_FauxSansDateDeDebut()
        {
            //Etant donné un blocage jamais posé
            Blocage blocage = new Blocage();

            //Lorsque
            bool encours = blocage.EstEnCoursLe(Aujourdhui);

            //Alors
            Assert.False(encours);
        }

        [Fact]
        public void EstEnCoursLe_FauxAvantLaDateDeDebut()
        {
            //Etant donné un blocage qui commence demain
            Blocage blocage = new Blocage { Debut = Aujourdhui.AddDays(1) };

            //Lorsque
            bool encours = blocage.EstEnCoursLe(Aujourdhui);

            //Alors
            Assert.False(encours);
        }

        [Fact]
        public void EstEnCoursLe_VraiEntreLeDebutEtLaFin()
        {
            //Etant donné
            Blocage blocage = new Blocage
            {
                Debut = Aujourdhui.AddDays(-3),
                Fin = Aujourdhui.AddDays(3)
            };

            //Lorsque
            bool encours = blocage.EstEnCoursLe(Aujourdhui);

            //Alors
            Assert.True(encours);
        }

        [Fact]
        public void EstEnCoursLe_VraiLeJourMemeDuDebutEtDeLaFin()
        {
            //Etant donné un blocage d'une seule journée
            Blocage blocage = new Blocage { Debut = Aujourdhui, Fin = Aujourdhui };

            //Lorsque
            bool encours = blocage.EstEnCoursLe(Aujourdhui);

            //Alors les deux bornes sont incluses
            Assert.True(encours);
        }

        [Fact]
        public void EstEnCoursLe_FauxApresLaFin()
        {
            //Etant donné un blocage terminé hier
            Blocage blocage = new Blocage
            {
                Debut = Aujourdhui.AddDays(-5),
                Fin = Aujourdhui.AddDays(-1)
            };

            //Lorsque
            bool encours = blocage.EstEnCoursLe(Aujourdhui);

            //Alors le compte est de nouveau libre
            Assert.False(encours);
        }

        [Fact]
        public void EstEnCoursLe_VraiIndefinimentQuandLaFinEstNulle()
        {
            //Etant donné un blocage jusqu'à nouvel ordre
            Blocage blocage = new Blocage { Debut = Aujourdhui.AddDays(-1), Fin = null };

            //Lorsque
            bool encours = blocage.EstEnCoursLe(Aujourdhui.AddYears(5));

            //Alors
            Assert.True(encours);
        }
    }
}
