using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Escale.Web.Repositories
{
    // Jeu de démonstration. Appelé uniquement en développement par Amorcage :
    // ces comptes n'existent jamais en production, et leur mot de passe vient
    // de la configuration de développement, pas du code.
    public static class Semences
    {
        // Les codes d'équipement se répètent d'une annonce à l'autre : les
        // nommer une fois évite qu'une faute de frappe passe inaperçue jusqu'à
        // l'exception de clé absente au démarrage.
        private const string Douche = "douche";
        private const string MicroOndes = "micro-ondes";
        private const string Climatisation = "climatisation";
        private const string Ascenseur = "ascenseur";
        private const string Automatique = "automatique";
        private const string SiegesChauffants = "sieges-chauffants";
        private const string Bluetooth = "bluetooth";
        private const string Coffre = "coffre";

        // Ce que toute annonce porte, quelle que soit sa catégorie. Regrouper
        // ces six valeurs garde les deux fabriques lisibles.
        private sealed record Fiche(string Loueur, string Titre, string Description,
            int Prix, string Photo, int Exemplaires);
        public static async Task RemplirAsync(IServiceProvider services, IConfiguration configuration)
        {
            ContexteEscale contexte = services.GetRequiredService<ContexteEscale>();
            UserManager<Utilisateur> comptes = services.GetRequiredService<UserManager<Utilisateur>>();

            string motDePasse = configuration["Semences:MotDePasse"]
                ?? throw new InvalidOperationException(
                    "Semences:MotDePasse doit être défini pour remplir le jeu de démonstration.");

            Utilisateur marie = await CompteAsync(comptes, motDePasse, "marie@escale.test", "Marie Tremblay", Utilisateur.RoleLoueur);
            Utilisateur hugo = await CompteAsync(comptes, motDePasse, "hugo@escale.test", "Hugo Bélanger", Utilisateur.RoleLoueur);
            await CompteAsync(comptes, motDePasse, "camille@escale.test", "Camille Roy", Utilisateur.RoleVoyageur);

            if (!await contexte.Equipements.AnyAsync())
            {
                contexte.Equipements.AddRange(
                    Equip(Douche, "Douche privée", CategorieAnnonce.Chambre),
                    Equip("wifi", "Wifi inclus", CategorieAnnonce.Chambre),
                    Equip("cuisine", "Coin cuisine", CategorieAnnonce.Chambre),
                    Equip(MicroOndes, "Micro-ondes", CategorieAnnonce.Chambre),
                    Equip("secheuse", "Laveuse et sécheuse", CategorieAnnonce.Chambre),
                    Equip("cour", "Cour ou balcon", CategorieAnnonce.Chambre),
                    Equip(Climatisation, "Climatisation", CategorieAnnonce.Chambre),
                    Equip(Ascenseur, "Ascenseur", CategorieAnnonce.Chambre),
                    Equip(Automatique, "Boîte automatique", CategorieAnnonce.Voiture),
                    Equip("gps", "Navigation intégrée", CategorieAnnonce.Voiture),
                    Equip(SiegesChauffants, "Sièges chauffants", CategorieAnnonce.Voiture),
                    Equip(Bluetooth, "Bluetooth", CategorieAnnonce.Voiture),
                    Equip("hybride", "Motorisation hybride", CategorieAnnonce.Voiture),
                    Equip("quatre-roues", "Quatre roues motrices", CategorieAnnonce.Voiture),
                    Equip(Coffre, "Grand coffre", CategorieAnnonce.Voiture));

                await contexte.SaveChangesAsync();
            }

            if (await contexte.Annonces.AnyAsync())
            {
                return;
            }

            Dictionary<string, int> codes = await contexte.Equipements.ToDictionaryAsync(e => e.Code, e => e.Id);

            Chambre(contexte, new Fiche(marie.Id, "Chambre très lumineuse",
                "Plein sud, fenêtres du sol au plafond, vue dégagée sur la baie. Lit double et coin lecture.",
                55, "chambre-1.jpg", 3), 22, 2, codes, Douche, "wifi", MicroOndes, Ascenseur);

            Chambre(contexte, new Fiche(marie.Id, "Suite avec balcon",
                "Coin salon séparé, balcon privé donnant sur le port, machine à café. Idéale pour un séjour de plusieurs nuits.",
                120, "chambre-2.jpg", 1), 38, 3, codes, Douche, "wifi", "cuisine", MicroOndes, "cour", Climatisation, Ascenseur);

            Chambre(contexte, new Fiche(marie.Id, "Chambre simple",
                "Sur cour intérieure, au calme, pour une personne. Bureau et rangement mural.",
                75, "chambre-3.jpg", 6), 16, 1, codes, Douche, "wifi");

            Chambre(contexte, new Fiche(marie.Id, "Loft sous les toits",
                "Mezzanine sous poutres apparentes, lucarne plein ciel, cuisine complète. Cinquième étage sans ascenseur.",
                165, "chambre-4.jpg", 1), 46, 2, codes, Douche, "wifi", "cuisine", MicroOndes, "secheuse", Climatisation);

            Chambre(contexte, new Fiche(marie.Id, "Chambre classique",
                "Mobilier de bois, deux fauteuils, salle de bain complète. Petit déjeuner servi en salle.",
                95, "chambre-5.jpg", 4), 26, 2, codes, Douche, "wifi", Climatisation, Ascenseur);

            Voiture(contexte, new Fiche(hugo.Id, "Kia Sportage",
                "VUS compact, quatre roues motrices, très bon volume de coffre pour les bagages d'une famille.",
                300, "voiture-1.jpg", 4), "Kia", 2021, 5, codes, Automatique, "quatre-roues", Coffre, Bluetooth);

            Voiture(contexte, new Fiche(hugo.Id, "Ford Mustang",
                "Coupé sportif à boîte manuelle. Deux vraies places à l'arrière, coffre réduit.",
                350, "voiture-2.jpg", 1), "Ford", 2020, 4, codes, Bluetooth, SiegesChauffants);

            Voiture(contexte, new Fiche(hugo.Id, "Mazda CX-5",
                "VUS intermédiaire, tenue de route soignée, grand coffre modulable.",
                400, "voiture-3.jpg", 2), "Mazda", 2022, 5, codes, Automatique, "gps", Coffre, Bluetooth, SiegesChauffants);

            Voiture(contexte, new Fiche(hugo.Id, "Toyota Corolla",
                "Berline compacte hybride, très sobre en ville comme sur la route.",
                396, "voiture-4.jpg", 5), "Toyota", 2023, 5, codes, Automatique, "hybride", "gps", Bluetooth);

            Voiture(contexte, new Fiche(hugo.Id, "Hyundai Tucson",
                "VUS polyvalent hybride, le plus grand coffre du parc.",
                500, "voiture-5.jpg", 2), "Hyundai", 2025, 5, codes, Automatique, "hybride", "quatre-roues", Coffre, "gps");

            Voiture(contexte, new Fiche(hugo.Id, "Honda Civic",
                "Berline sportive récente, sièges chauffants et conduite souple.",
                475, "voiture-6.jpg", 3), "Honda", 2024, 5, codes, Automatique, SiegesChauffants, Bluetooth);

            await contexte.SaveChangesAsync();
        }

        private static Equipement Equip(string code, string libelle, CategorieAnnonce categorie) =>
            new Equipement { Code = code, Libelle = libelle, Categorie = categorie };

        private static void Chambre(ContexteEscale c, Fiche fiche, int superficie, int couchages,
            Dictionary<string, int> codes, params string[] equipements)
        {
            c.Annonces.Add(new Annonce
            {
                LoueurId = fiche.Loueur,
                Categorie = CategorieAnnonce.Chambre,
                Titre = fiche.Titre,
                Description = fiche.Description,
                PrixJournalier = fiche.Prix,
                Photo = fiche.Photo,
                Superficie = superficie,
                Couchages = couchages,
                Exemplaires = fiche.Exemplaires,
                Equipements = equipements.Select(code => new AnnonceEquipement { EquipementId = codes[code] }).ToList()
            });
        }

        private static void Voiture(ContexteEscale c, Fiche fiche, string marque, int annee, int places,
            Dictionary<string, int> codes, params string[] equipements)
        {
            c.Annonces.Add(new Annonce
            {
                LoueurId = fiche.Loueur,
                Categorie = CategorieAnnonce.Voiture,
                Titre = fiche.Titre,
                Description = fiche.Description,
                PrixJournalier = fiche.Prix,
                Photo = fiche.Photo,
                Marque = marque,
                Annee = annee,
                Places = places,
                Exemplaires = fiche.Exemplaires,
                Equipements = equipements.Select(code => new AnnonceEquipement { EquipementId = codes[code] }).ToList()
            });
        }

        private static async Task<Utilisateur> CompteAsync(UserManager<Utilisateur> comptes,
            string motDePasse, string courriel, string nom, string role)
        {
            Utilisateur? existant = await comptes.FindByEmailAsync(courriel);
            if (existant is not null)
            {
                return existant;
            }

            Utilisateur utilisateur = new Utilisateur
            {
                UserName = courriel,
                Email = courriel,
                EmailConfirmed = true,
                NomComplet = nom
            };

            if (role == Utilisateur.RoleLoueur)
            {
                utilisateur.Abonnement = new Abonnement
                {
                    Debut = DateTime.UtcNow,
                    Mensualite = 29,
                    Echeance = DateTime.Today.AddMonths(1)
                };
            }

            await comptes.CreateAsync(utilisateur, motDePasse);
            await comptes.AddToRoleAsync(utilisateur, role);

            return utilisateur;
        }
    }
}
