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
                    Equip("douche", "Douche privée", CategorieAnnonce.Chambre),
                    Equip("wifi", "Wifi inclus", CategorieAnnonce.Chambre),
                    Equip("cuisine", "Coin cuisine", CategorieAnnonce.Chambre),
                    Equip("micro-ondes", "Micro-ondes", CategorieAnnonce.Chambre),
                    Equip("secheuse", "Laveuse et sécheuse", CategorieAnnonce.Chambre),
                    Equip("cour", "Cour ou balcon", CategorieAnnonce.Chambre),
                    Equip("climatisation", "Climatisation", CategorieAnnonce.Chambre),
                    Equip("ascenseur", "Ascenseur", CategorieAnnonce.Chambre),
                    Equip("automatique", "Boîte automatique", CategorieAnnonce.Voiture),
                    Equip("gps", "Navigation intégrée", CategorieAnnonce.Voiture),
                    Equip("sieges-chauffants", "Sièges chauffants", CategorieAnnonce.Voiture),
                    Equip("bluetooth", "Bluetooth", CategorieAnnonce.Voiture),
                    Equip("hybride", "Motorisation hybride", CategorieAnnonce.Voiture),
                    Equip("quatre-roues", "Quatre roues motrices", CategorieAnnonce.Voiture),
                    Equip("coffre", "Grand coffre", CategorieAnnonce.Voiture));

                await contexte.SaveChangesAsync();
            }

            if (await contexte.Annonces.AnyAsync())
            {
                return;
            }

            Dictionary<string, int> codes = await contexte.Equipements.ToDictionaryAsync(e => e.Code, e => e.Id);

            Chambre(contexte, marie.Id, "Chambre très lumineuse",
                "Plein sud, fenêtres du sol au plafond, vue dégagée sur la baie. Lit double et coin lecture.",
                55, "chambre-1.jpg", 22, 2, 3, codes, "douche", "wifi", "micro-ondes", "ascenseur");

            Chambre(contexte, marie.Id, "Suite avec balcon",
                "Coin salon séparé, balcon privé donnant sur le port, machine à café. Idéale pour un séjour de plusieurs nuits.",
                120, "chambre-2.jpg", 38, 3, 1, codes, "douche", "wifi", "cuisine", "micro-ondes", "cour", "climatisation", "ascenseur");

            Chambre(contexte, marie.Id, "Chambre simple",
                "Sur cour intérieure, au calme, pour une personne. Bureau et rangement mural.",
                75, "chambre-3.jpg", 16, 1, 6, codes, "douche", "wifi");

            Chambre(contexte, marie.Id, "Loft sous les toits",
                "Mezzanine sous poutres apparentes, lucarne plein ciel, cuisine complète. Cinquième étage sans ascenseur.",
                165, "chambre-4.jpg", 46, 2, 1, codes, "douche", "wifi", "cuisine", "micro-ondes", "secheuse", "climatisation");

            Chambre(contexte, marie.Id, "Chambre classique",
                "Mobilier de bois, deux fauteuils, salle de bain complète. Petit déjeuner servi en salle.",
                95, "chambre-5.jpg", 26, 2, 4, codes, "douche", "wifi", "climatisation", "ascenseur");

            Voiture(contexte, hugo.Id, "Kia Sportage",
                "VUS compact, quatre roues motrices, très bon volume de coffre pour les bagages d'une famille.",
                300, "voiture-1.jpg", "Kia", 2021, 5, 4, codes, "automatique", "quatre-roues", "coffre", "bluetooth");

            Voiture(contexte, hugo.Id, "Ford Mustang",
                "Coupé sportif à boîte manuelle. Deux vraies places à l'arrière, coffre réduit.",
                350, "voiture-2.jpg", "Ford", 2020, 4, 1, codes, "bluetooth", "sieges-chauffants");

            Voiture(contexte, hugo.Id, "Mazda CX-5",
                "VUS intermédiaire, tenue de route soignée, grand coffre modulable.",
                400, "voiture-3.jpg", "Mazda", 2022, 5, 2, codes, "automatique", "gps", "coffre", "bluetooth", "sieges-chauffants");

            Voiture(contexte, hugo.Id, "Toyota Corolla",
                "Berline compacte hybride, très sobre en ville comme sur la route.",
                396, "voiture-4.jpg", "Toyota", 2023, 5, 5, codes, "automatique", "hybride", "gps", "bluetooth");

            Voiture(contexte, hugo.Id, "Hyundai Tucson",
                "VUS polyvalent hybride, le plus grand coffre du parc.",
                500, "voiture-5.jpg", "Hyundai", 2025, 5, 2, codes, "automatique", "hybride", "quatre-roues", "coffre", "gps");

            Voiture(contexte, hugo.Id, "Honda Civic",
                "Berline sportive récente, sièges chauffants et conduite souple.",
                475, "voiture-6.jpg", "Honda", 2024, 5, 3, codes, "automatique", "sieges-chauffants", "bluetooth");

            await contexte.SaveChangesAsync();
        }

        private static Equipement Equip(string code, string libelle, CategorieAnnonce categorie) =>
            new Equipement { Code = code, Libelle = libelle, Categorie = categorie };

        private static void Chambre(ContexteEscale c, string loueur, string titre, string desc, int prix,
            string photo, int superficie, int couchages, int exemplaires, Dictionary<string, int> codes, params string[] equipements)
        {
            c.Annonces.Add(new Annonce
            {
                LoueurId = loueur,
                Categorie = CategorieAnnonce.Chambre,
                Titre = titre,
                Description = desc,
                PrixJournalier = prix,
                Photo = photo,
                Superficie = superficie,
                Couchages = couchages,
                Exemplaires = exemplaires,
                Equipements = equipements.Select(code => new AnnonceEquipement { EquipementId = codes[code] }).ToList()
            });
        }

        private static void Voiture(ContexteEscale c, string loueur, string titre, string desc, int prix,
            string photo, string marque, int annee, int places, int exemplaires, Dictionary<string, int> codes, params string[] equipements)
        {
            c.Annonces.Add(new Annonce
            {
                LoueurId = loueur,
                Categorie = CategorieAnnonce.Voiture,
                Titre = titre,
                Description = desc,
                PrixJournalier = prix,
                Photo = photo,
                Marque = marque,
                Annee = annee,
                Places = places,
                Exemplaires = exemplaires,
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
