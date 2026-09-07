using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Escale.Web.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Escale.Web.Services
{
    // Les photos fournies avec le site restent réutilisables, et chacun peut
    // ajouter les siennes. Un fichier téléversé est renommé et son type est
    // vérifié sur son contenu, pas sur son extension.
    public class PhotoService : IPhotoService
    {
        public const long TailleMaximale = 4 * 1024 * 1024;

        private const string DossierTeleversees = "televersees";

        private static readonly string[] ExtensionsAdmises = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly IWebHostEnvironment _environnement;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(IWebHostEnvironment environnement, ILogger<PhotoService> logger)
        {
            _environnement = environnement;
            _logger = logger;
        }

        public List<string> Catalogue()
        {
            List<string> photos = Fichiers(Images())
                .Where(nom => nom != "hero.jpg")
                .ToList();

            photos.AddRange(Fichiers(Path.Combine(Images(), DossierTeleversees))
                .Select(nom => DossierTeleversees + "/" + nom));

            return photos.OrderBy(nom => nom).ToList();
        }

        public async Task<ResultatPhoto> TeleverserAsync(Stream contenu, string nomOrigine, long taille)
        {
            string extension = Path.GetExtension(nomOrigine ?? string.Empty).ToLowerInvariant();

            if (!ExtensionsAdmises.Contains(extension))
            {
                return Refuser("Format accepté : JPEG, PNG ou WebP.", nomOrigine, taille);
            }

            if (taille <= 0 || taille > TailleMaximale)
            {
                return Refuser("La photo ne doit pas dépasser 4 Mo.", nomOrigine, taille);
            }

            byte[] entete = new byte[12];
            int lus = await contenu.ReadAsync(entete, 0, entete.Length);

            if (lus < entete.Length || !EstUneImage(entete))
            {
                return Refuser("Ce fichier n'est pas une image.", nomOrigine, taille);
            }

            string dossier = Path.Combine(Images(), DossierTeleversees);
            Directory.CreateDirectory(dossier);

            string nom = Guid.NewGuid().ToString("N") + extension;

            using (FileStream sortie = File.Create(Path.Combine(dossier, nom)))
            {
                await sortie.WriteAsync(entete, 0, entete.Length);
                await contenu.CopyToAsync(sortie);
            }

            return new ResultatPhoto(true, "Photo ajoutée.", DossierTeleversees + "/" + nom);
        }

        private ResultatPhoto Refuser(string message, string? nomOrigine, long taille)
        {
            // Le nom d'origine vient de l'utilisateur : il passe en paramètre
            // du gabarit, jamais concaténé dans le message. C'est ce qui ferme
            // l'injection dans les journaux.
            _logger.LogWarning(JournalDeSecurite.TeleversementRefuse,
                "Téléversement refusé : {Motif}, fichier {Nom}, taille {Taille}",
                message, nomOrigine, taille);

            return new ResultatPhoto(false, message, string.Empty);
        }

        // Signatures de fichier : une extension se renomme, un en-tête non.
        private static bool EstUneImage(byte[] entete)
        {
            bool jpeg = entete[0] == 0xFF && entete[1] == 0xD8 && entete[2] == 0xFF;

            bool png = entete[0] == 0x89 && entete[1] == 0x50 && entete[2] == 0x4E && entete[3] == 0x47;

            bool webp = entete[0] == 0x52 && entete[1] == 0x49 && entete[2] == 0x46 && entete[3] == 0x46
                && entete[8] == 0x57 && entete[9] == 0x45 && entete[10] == 0x42 && entete[11] == 0x50;

            return jpeg || png || webp;
        }

        private string Images() => Path.Combine(_environnement.WebRootPath ?? string.Empty, "images");

        private static List<string> Fichiers(string dossier)
        {
            if (!Directory.Exists(dossier))
            {
                return new List<string>();
            }

            return Directory.EnumerateFiles(dossier)
                .Select(Path.GetFileName)
                .Where(nom => nom is not null && ExtensionsAdmises.Contains(Path.GetExtension(nom).ToLowerInvariant()))
                .Select(nom => nom!)
                .ToList();
        }
    }
}
