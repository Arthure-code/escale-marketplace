using Escale.Web.Pages.Tableau;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Escale.Tests.Pages
{
    public class TableauAnnonceModelTests
    {
        private readonly Mock<IAnnonceService> _annonces = new Mock<IAnnonceService>(MockBehavior.Strict);
        private readonly Mock<IEquipementService> _equipements = new Mock<IEquipementService>(MockBehavior.Strict);
        private readonly Mock<IPhotoService> _photos = new Mock<IPhotoService>(MockBehavior.Strict);
        private readonly Mock<IUtilisateurCourant> _utilisateur = new Mock<IUtilisateurCourant>(MockBehavior.Strict);

        public TableauAnnonceModelTests()
        {
            _utilisateur.SetupGet(u => u.Id).Returns("marie");
            _utilisateur.SetupGet(u => u.EstAdministrateur).Returns(false);
            _photos.Setup(s => s.Catalogue()).Returns(new List<string> { "chambre-1.jpg", "voiture-1.jpg" });
            _equipements.Setup(s => s.ObtenirTousAsync()).ReturnsAsync(new List<Equipement>
            {
                new Equipement { Id = 1, Code = "douche", Libelle = "Douche privée", Categorie = CategorieAnnonce.Chambre },
                new Equipement { Id = 2, Code = "wifi", Libelle = "Wifi inclus", Categorie = CategorieAnnonce.Chambre }
            });
        }

        private AnnonceModel Page()
        {
            ModelStateDictionary etat = new ModelStateDictionary();
            ActionContext contexte = new ActionContext(
                new DefaultHttpContext(), new RouteData(), new PageActionDescriptor(), etat);

            return new AnnonceModel(_annonces.Object, _equipements.Object, _photos.Object, _utilisateur.Object)
            {
                PageContext = new PageContext(contexte)
                {
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), etat)
                }
            };
        }

        private static Annonce Chambre() => new Annonce
        {
            Id = 7,
            LoueurId = "marie",
            Categorie = CategorieAnnonce.Chambre,
            Titre = "Suite avec balcon",
            Description = "Balcon privé sur le port.",
            PrixJournalier = 120,
            Equipements = new List<AnnonceEquipement>
            {
                new AnnonceEquipement { AnnonceId = 7, EquipementId = 1 }
            }
        };

        [Fact]
        public async Task OnGetAsync_SansIdentifiant_OuvreUnFormulaireDeCreation()
        {
            //Etant donné
            AnnonceModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnGetAsync(null);

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.False(page.Modification);
            Assert.Equal(2, page.Equipements.Count);
            Assert.Equal(2, page.Photos.Count);
        }

        [Fact]
        public async Task OnGetAsync_AnnonceDUnAutreLoueur_RendNotFound()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirPourModificationAsync(7, "marie", false)).ReturnsAsync((Annonce?)null);
            AnnonceModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnGetAsync(7);

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task OnGetAsync_SonAnnonce_RemplitLaSaisieEtLesEquipementsCoches()
        {
            //Etant donné
            _annonces.Setup(s => s.ObtenirPourModificationAsync(7, "marie", false)).ReturnsAsync(Chambre());
            AnnonceModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnGetAsync(7);

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.True(page.Modification);
            Assert.Equal("Suite avec balcon", page.Saisie.Titre);
            Assert.Equal(new[] { 1 }, page.EquipementsChoisis);
        }

        [Fact]
        public async Task OnGetAsync_Administrateur_AtteintLAnnonceDeNimporteQuiSansRestriction()
        {
            //Etant donné
            _utilisateur.SetupGet(u => u.EstAdministrateur).Returns(true);
            _annonces.Setup(s => s.ObtenirPourModificationAsync(7, "marie", true)).ReturnsAsync(Chambre());
            AnnonceModel page = Page();

            //Lorsque
            IActionResult resultat = await page.OnGetAsync(7);

            //Alors
            Assert.IsType<PageResult>(resultat);
            _annonces.Verify(s => s.ObtenirPourModificationAsync(7, "marie", true), Times.Once);
        }

        [Fact]
        public async Task OnPostAsync_ModelStateInvalide_RendLaPageSansRienPublier()
        {
            //Etant donné
            AnnonceModel page = Page();
            page.ModelState.AddModelError("Saisie.Titre", "Le titre est obligatoire.");

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            Assert.IsType<PageResult>(resultat);
            _annonces.Verify(s => s.PublierAsync(It.IsAny<Annonce>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        }

        [Fact]
        public async Task OnPostAsync_NouvelleAnnonce_PublieAuNomDuLoueurConnecte()
        {
            //Etant donné
            _annonces.Setup(s => s.PublierAsync(It.IsAny<Annonce>(), "marie", It.IsAny<IEnumerable<int>>()))
                .Returns(Task.CompletedTask);
            AnnonceModel page = Page();
            page.Saisie = new Annonce { Id = 0, Categorie = CategorieAnnonce.Chambre, Titre = "Studio du quai", Description = "Court séjour.", PrixJournalier = 88 };
            page.EquipementsChoisis = new List<int> { 1, 2 };

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Tableau/Index", redirection.PageName);
            _annonces.Verify(s => s.PublierAsync(page.Saisie, "marie", page.EquipementsChoisis), Times.Once);
        }

        [Fact]
        public async Task OnPostAsync_ModificationRefusee_RendNotFound()
        {
            //Etant donné
            _annonces.Setup(s => s.ModifierAsync(It.IsAny<Annonce>(), "marie", It.IsAny<IEnumerable<int>>(), false))
                .ReturnsAsync(false);
            AnnonceModel page = Page();
            page.Saisie = new Annonce { Id = 7, Categorie = CategorieAnnonce.Chambre, Titre = "Volée", Description = "x", PrixJournalier = 10 };

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task OnPostAsync_ModificationAcceptee_RetourneAuTableauDeBord()
        {
            //Etant donné
            _annonces.Setup(s => s.ModifierAsync(It.IsAny<Annonce>(), "marie", It.IsAny<IEnumerable<int>>(), false))
                .ReturnsAsync(true);
            AnnonceModel page = Page();
            page.Saisie = new Annonce { Id = 7, Categorie = CategorieAnnonce.Chambre, Titre = "Suite rénovée", Description = "x", PrixJournalier = 130 };

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            RedirectToPageResult redirection = Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("/Tableau/Index", redirection.PageName);
        }

        [Fact]
        public async Task OnPostAsync_PhotoRefusee_RendLaPageAvecLeMotif()
        {
            //Etant donné
            _photos.Setup(s => s.TeleverserAsync(It.IsAny<Stream>(), "virus.exe", It.IsAny<long>()))
                .ReturnsAsync(new ResultatPhoto(false, "Format accepté : JPEG, PNG ou WebP.", string.Empty));
            AnnonceModel page = Page();
            page.Saisie = new Annonce { Id = 0, Categorie = CategorieAnnonce.Chambre, Titre = "Studio", Description = "x", PrixJournalier = 80 };
            page.Fichier = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "Fichier", "virus.exe");

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            Assert.IsType<PageResult>(resultat);
            Assert.Equal("Format accepté : JPEG, PNG ou WebP.", page.ModelState["Fichier"]!.Errors[0].ErrorMessage);
            _annonces.Verify(s => s.PublierAsync(It.IsAny<Annonce>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        }

        [Fact]
        public async Task OnPostAsync_PhotoAcceptee_RemplaceLaPhotoDeLAnnonce()
        {
            //Etant donné
            _photos.Setup(s => s.TeleverserAsync(It.IsAny<Stream>(), "chalet.jpg", It.IsAny<long>()))
                .ReturnsAsync(new ResultatPhoto(true, "Photo ajoutée.", "televersees/abc.jpg"));
            _annonces.Setup(s => s.PublierAsync(It.IsAny<Annonce>(), "marie", It.IsAny<IEnumerable<int>>()))
                .Returns(Task.CompletedTask);
            AnnonceModel page = Page();
            page.Saisie = new Annonce { Id = 0, Categorie = CategorieAnnonce.Chambre, Titre = "Studio", Description = "x", PrixJournalier = 80, Photo = "defaut.jpg" };
            page.Fichier = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "Fichier", "chalet.jpg");

            //Lorsque
            IActionResult resultat = await page.OnPostAsync();

            //Alors
            Assert.IsType<RedirectToPageResult>(resultat);
            Assert.Equal("televersees/abc.jpg", page.Saisie.Photo);
        }
    }
}
