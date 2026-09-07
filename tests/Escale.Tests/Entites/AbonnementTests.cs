namespace Escale.Tests.Entites
{
    public class AbonnementTests
    {
        private static readonly DateTime Aujourdhui = new DateTime(2026, 9, 7);

        [Fact]
        public void EstAJourLe_VraiQuandAucuneEcheanceNEstPosee()
        {
            //Etant donné un voyageur, qui ne paie pas d'abonnement
            Abonnement abonnement = new Abonnement();

            //Lorsque
            bool ajour = abonnement.EstAJourLe(Aujourdhui);

            //Alors il n'est jamais échu
            Assert.True(ajour);
        }

        [Fact]
        public void EstAJourLe_VraiLeJourMemeDeLEcheance()
        {
            //Etant donné une échéance qui tombe aujourd'hui
            Abonnement abonnement = new Abonnement { Echeance = Aujourdhui };

            //Lorsque
            bool ajour = abonnement.EstAJourLe(Aujourdhui);

            //Alors le jour de l'échéance reste couvert
            Assert.True(ajour);
        }

        [Fact]
        public void EstAJourLe_FauxLeLendemainDeLEcheance()
        {
            //Etant donné
            Abonnement abonnement = new Abonnement { Echeance = Aujourdhui.AddDays(-1) };

            //Lorsque
            bool ajour = abonnement.EstAJourLe(Aujourdhui);

            //Alors
            Assert.False(ajour);
        }

        [Fact]
        public void EstAJourLe_IgnoreLHeureEtNeCompareQueLaDate()
        {
            //Etant donné une échéance en début de journée et un contrôle en soirée
            Abonnement abonnement = new Abonnement { Echeance = Aujourdhui };

            //Lorsque
            bool ajour = abonnement.EstAJourLe(Aujourdhui.AddHours(23));

            //Alors
            Assert.True(ajour);
        }
    }
}
