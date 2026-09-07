using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Escale.Tests.Services
{
    // Le service écrit sur le disque : chaque test lui donne un dossier
    // temporaire à lui, supprimé à la fin, plutôt que le wwwroot du projet.
    public class PhotoServiceTests : IDisposable
    {
        private static readonly byte[] EnteteJpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] EntetePng = { 0x89, 0x50, 0x4E, 0x47, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] EnteteWebp =
            { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 };

        private readonly string _racine;
        private readonly PhotoService _service;

        public PhotoServiceTests()
        {
            _racine = Path.Combine(Path.GetTempPath(), "escale-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_racine, "images"));

            Mock<IWebHostEnvironment> environnement = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
            environnement.SetupGet(e => e.WebRootPath).Returns(_racine);

            _service = new PhotoService(environnement.Object, NullLogger<PhotoService>.Instance);
        }

        public void Dispose()
        {
            if (Directory.Exists(_racine))
            {
                Directory.Delete(_racine, recursive: true);
            }
            GC.SuppressFinalize(this);
        }

        private static MemoryStream Contenu(byte[] entete, int octetsEnPlus = 100)
        {
            MemoryStream flux = new MemoryStream();
            flux.Write(entete, 0, entete.Length);
            flux.Write(new byte[octetsEnPlus], 0, octetsEnPlus);
            flux.Position = 0;
            return flux;
        }

        [Theory]
        [InlineData("photo.gif")]
        [InlineData("photo.svg")]
        [InlineData("photo.exe")]
        [InlineData("photo")]
        public async Task TeleverserAsync_RefuseUneExtensionHorsListeBlanche(string nom)
        {
            //Etant donné
            using MemoryStream flux = Contenu(EnteteJpeg);

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(flux, nom, flux.Length);

            //Alors
            Assert.False(resultat.Reussi);
            Assert.Contains("Format", resultat.Message);
        }

        [Fact]
        public async Task TeleverserAsync_RefuseUnFichierTropLourd()
        {
            //Etant donné une taille annoncée au-dessus de la limite
            using MemoryStream flux = Contenu(EnteteJpeg);

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(
                flux, "photo.jpg", PhotoService.TailleMaximale + 1);

            //Alors
            Assert.False(resultat.Reussi);
            Assert.Contains("4 Mo", resultat.Message);
        }

        [Fact]
        public async Task TeleverserAsync_RefuseUnFichierVide()
        {
            //Etant donné
            using MemoryStream flux = new MemoryStream();

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(flux, "photo.jpg", 0);

            //Alors
            Assert.False(resultat.Reussi);
        }

        [Fact]
        public async Task TeleverserAsync_RefuseUnFichierRenommeEnJpeg()
        {
            //Etant donné un exécutable déguisé : bonne extension, mauvais contenu
            using MemoryStream flux = Contenu(new byte[] { 0x4D, 0x5A, 0x90, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(flux, "charge.jpg", flux.Length);

            //Alors le contrôle porte sur la signature, pas sur le nom
            Assert.False(resultat.Reussi);
            Assert.Contains("pas une image", resultat.Message);
        }

        [Fact]
        public async Task TeleverserAsync_RefuseUnFichierTropCourtPourPorterUneSignature()
        {
            //Etant donné trois octets seulement
            using MemoryStream flux = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(flux, "photo.jpg", 3);

            //Alors
            Assert.False(resultat.Reussi);
            Assert.Contains("pas une image", resultat.Message);
        }

        [Theory]
        [InlineData("photo.jpg")]
        [InlineData("photo.JPEG")]
        [InlineData("photo.png")]
        [InlineData("photo.webp")]
        public async Task TeleverserAsync_AccepteLesFormatsAdmisQuelleQueSoitLaCasse(string nom)
        {
            //Etant donné la signature qui correspond à l'extension
            byte[] entete = nom.ToLowerInvariant().EndsWith(".png") ? EntetePng
                : nom.ToLowerInvariant().EndsWith(".webp") ? EnteteWebp
                : EnteteJpeg;
            using MemoryStream flux = Contenu(entete);

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(flux, nom, flux.Length);

            //Alors
            Assert.True(resultat.Reussi);
        }

        [Fact]
        public async Task TeleverserAsync_RenommeLeFichierEtNeGardeRienDuNomDOrigine()
        {
            //Etant donné un nom qui tente de remonter l'arborescence
            using MemoryStream flux = Contenu(EnteteJpeg);

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(
                flux, "../../../appsettings.jpg", flux.Length);

            //Alors le nom stocké est engendré, et rien ne sort du dossier prévu
            Assert.True(resultat.Reussi);
            Assert.StartsWith("televersees/", resultat.NomFichier);
            Assert.DoesNotContain("..", resultat.NomFichier);
            Assert.DoesNotContain("appsettings", resultat.NomFichier);
            Assert.EndsWith(".jpg", resultat.NomFichier);
        }

        [Fact]
        public async Task TeleverserAsync_EcritLeFichierCompletDansLeDossierDesTeleversees()
        {
            //Etant donné
            using MemoryStream flux = Contenu(EnteteJpeg, octetsEnPlus: 500);

            //Lorsque
            ResultatPhoto resultat = await _service.TeleverserAsync(flux, "photo.jpg", flux.Length);

            //Alors l'en-tête déjà lu est réécrit, donc rien n'est perdu
            string chemin = Path.Combine(_racine, "images", "televersees",
                Path.GetFileName(resultat.NomFichier));
            Assert.True(File.Exists(chemin));
            Assert.Equal(512, new FileInfo(chemin).Length);
        }

        [Fact]
        public void Catalogue_RendLesPhotosDuSiteEtCellesTeleversees()
        {
            //Etant donné une photo livrée, la photo d'accueil et une téléversée
            string images = Path.Combine(_racine, "images");
            File.WriteAllBytes(Path.Combine(images, "chambre-1.jpg"), EnteteJpeg);
            File.WriteAllBytes(Path.Combine(images, "hero.jpg"), EnteteJpeg);
            Directory.CreateDirectory(Path.Combine(images, "televersees"));
            File.WriteAllBytes(Path.Combine(images, "televersees", "abc.png"), EntetePng);

            //Lorsque
            List<string> catalogue = _service.Catalogue();

            //Alors la photo d'accueil est écartée, les téléversées sont préfixées
            Assert.Contains("chambre-1.jpg", catalogue);
            Assert.Contains("televersees/abc.png", catalogue);
            Assert.DoesNotContain("hero.jpg", catalogue);
        }

        [Fact]
        public void Catalogue_IgnoreLesFichiersQuiNeSontPasDesImages()
        {
            //Etant donné un fichier étranger déposé dans le dossier
            string images = Path.Combine(_racine, "images");
            File.WriteAllText(Path.Combine(images, "notes.txt"), "rien");
            File.WriteAllBytes(Path.Combine(images, "chambre-1.jpg"), EnteteJpeg);

            //Lorsque
            List<string> catalogue = _service.Catalogue();

            //Alors
            Assert.Equal(new[] { "chambre-1.jpg" }, catalogue);
        }
    }
}
