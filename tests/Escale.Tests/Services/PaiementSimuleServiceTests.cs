namespace Escale.Tests.Services
{
    public class PaiementSimuleServiceTests
    {
        private readonly PaiementSimuleService _service = new PaiementSimuleService();

        private static DonneesCarte Carte(string numero = PaiementSimuleService.CarteAcceptee,
            string titulaire = "Camille Roy", string expiration = "12/34", string cvc = "123") =>
            new DonneesCarte(titulaire, numero, expiration, cvc);

        [Fact]
        public async Task PayerAsync_AccepteUneCarteValide()
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(2220, "CMD-1", Carte());

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.True(resultat.Accepte);
            Assert.Equal("4242", resultat.QuatreDerniers);
            Assert.StartsWith("PAY-", resultat.Reference);
        }

        [Fact]
        public async Task PayerAsync_NeConserveQueLesQuatreDerniersChiffres()
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(100, "CMD-2", Carte());

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors le numéro complet ne doit apparaître nulle part
            Assert.Equal(4, resultat.QuatreDerniers.Length);
            Assert.DoesNotContain(PaiementSimuleService.CarteAcceptee, resultat.Reference);
            Assert.DoesNotContain(PaiementSimuleService.CarteAcceptee, resultat.Message);
        }

        [Fact]
        public async Task PayerAsync_RefuseLaCarteDeTestDeLEmetteur()
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(100, "CMD-3", Carte(PaiementSimuleService.CarteRefusee));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("émettrice", resultat.Message);
            Assert.Equal(string.Empty, resultat.QuatreDerniers);
        }

        [Fact]
        public async Task PayerAsync_RefuseQuandAucuneCarteNEstFournie()
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(100, "CMD-4");

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("Aucune donnée", resultat.Message);
        }

        [Fact]
        public async Task PayerAsync_RefuseUnTitulaireVide()
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(100, "CMD-5", Carte(titulaire: "  "));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("titulaire", resultat.Message);
        }

        [Theory]
        [InlineData("4242424242")]
        [InlineData("42424242424242424242")]
        public async Task PayerAsync_RefuseUneLongueurHorsBornes(string numero)
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(100, "CMD-6", Carte(numero));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("13 et 19", resultat.Message);
        }

        [Fact]
        public async Task PayerAsync_RefuseUnNumeroQuiEchoueAuControleDeLuhn()
        {
            //Etant donné un numéro de bonne longueur mais dont la clé est fausse
            DemandePaiement demande = new DemandePaiement(100, "CMD-7", Carte("4242424242424243"));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("invalide", resultat.Message);
        }

        [Fact]
        public async Task PayerAsync_AccepteUnNumeroEspaceOuTirete()
        {
            //Etant donné la même carte, saisie comme elle est imprimée
            DemandePaiement demande = new DemandePaiement(100, "CMD-8", Carte("4242 4242-4242 4242"));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors les séparateurs ne doivent pas faire échouer la saisie
            Assert.True(resultat.Accepte);
        }

        [Theory]
        [InlineData("13/34")]
        [InlineData("00/34")]
        [InlineData("12-34")]
        [InlineData("")]
        public async Task PayerAsync_RefuseUneExpirationMalFormee(string expiration)
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(100, "CMD-9", Carte(expiration: expiration));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("expiration", resultat.Message);
        }

        [Fact]
        public async Task PayerAsync_RefuseUneCarteExpiree()
        {
            //Etant donné une échéance largement passée
            DemandePaiement demande = new DemandePaiement(100, "CMD-10", Carte(expiration: "01/20"));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("expiration", resultat.Message);
        }

        [Theory]
        [InlineData("12")]
        [InlineData("12345")]
        public async Task PayerAsync_RefuseUnCodeDeSecuriteQuiNaPasTroisChiffres(string cvc)
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(100, "CMD-11", Carte(cvc: cvc));

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("sécurité", resultat.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task PayerAsync_RefuseUnMontantNulOuNegatif(int montant)
        {
            //Etant donné
            DemandePaiement demande = new DemandePaiement(montant, "CMD-12", Carte());

            //Lorsque
            ResultatPaiement resultat = await _service.PayerAsync(demande);

            //Alors
            Assert.False(resultat.Accepte);
            Assert.Contains("montant", resultat.Message);
        }
    }
}
