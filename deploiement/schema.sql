CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "AspNetRoles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "NomComplet" TEXT NOT NULL,
    "DateInscription" TEXT NOT NULL,
    "Abonnement_Debut" TEXT NULL,
    "Abonnement_Mensualite" INTEGER NOT NULL,
    "Abonnement_Echeance" TEXT NULL,
    "Blocage_Debut" TEXT NULL,
    "Blocage_Fin" TEXT NULL,
    "Blocage_Motif" TEXT NULL,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE "Equipements" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Equipements" PRIMARY KEY AUTOINCREMENT,
    "Code" TEXT NOT NULL,
    "Libelle" TEXT NOT NULL,
    "Categorie" INTEGER NOT NULL
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Annonces" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Annonces" PRIMARY KEY AUTOINCREMENT,
    "LoueurId" TEXT NOT NULL,
    "Categorie" INTEGER NOT NULL,
    "Titre" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "PrixJournalier" INTEGER NOT NULL,
    "Photo" TEXT NOT NULL,
    "EstDisponible" INTEGER NOT NULL,
    "DatePublication" TEXT NOT NULL,
    "Superficie" INTEGER NULL,
    "Couchages" INTEGER NULL,
    "Marque" TEXT NULL,
    "Annee" INTEGER NULL,
    "Places" INTEGER NULL,
    "Exemplaires" INTEGER NOT NULL,
    CONSTRAINT "FK_Annonces_AspNetUsers_LoueurId" FOREIGN KEY ("LoueurId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Commandes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Commandes" PRIMARY KEY AUTOINCREMENT,
    "Reference" TEXT NOT NULL,
    "UtilisateurId" TEXT NOT NULL,
    "DateCommande" TEXT NOT NULL,
    "ReferencePaiement" TEXT NOT NULL,
    "QuatreDerniers" TEXT NOT NULL,
    CONSTRAINT "FK_Commandes_AspNetUsers_UtilisateurId" FOREIGN KEY ("UtilisateurId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AnnonceEquipements" (
    "AnnonceId" INTEGER NOT NULL,
    "EquipementId" INTEGER NOT NULL,
    CONSTRAINT "PK_AnnonceEquipements" PRIMARY KEY ("AnnonceId", "EquipementId"),
    CONSTRAINT "FK_AnnonceEquipements_Annonces_AnnonceId" FOREIGN KEY ("AnnonceId") REFERENCES "Annonces" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AnnonceEquipements_Equipements_EquipementId" FOREIGN KEY ("EquipementId") REFERENCES "Equipements" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LignesPanier" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LignesPanier" PRIMARY KEY AUTOINCREMENT,
    "UtilisateurId" TEXT NOT NULL,
    "AnnonceId" INTEGER NOT NULL,
    "DateDebut" TEXT NOT NULL,
    "DateFin" TEXT NOT NULL,
    CONSTRAINT "FK_LignesPanier_Annonces_AnnonceId" FOREIGN KEY ("AnnonceId") REFERENCES "Annonces" ("Id") ON DELETE CASCADE
);

CREATE TABLE "LignesCommande" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LignesCommande" PRIMARY KEY AUTOINCREMENT,
    "CommandeId" INTEGER NOT NULL,
    "AnnonceId" INTEGER NOT NULL,
    "LoueurId" TEXT NOT NULL,
    "Titre" TEXT NOT NULL,
    "Categorie" INTEGER NOT NULL,
    "PrixJournalier" INTEGER NOT NULL,
    "DateDebut" TEXT NOT NULL,
    "DateFin" TEXT NOT NULL,
    "NombreDeJours" INTEGER NOT NULL,
    "SousTotal" INTEGER NOT NULL,
    CONSTRAINT "FK_LignesCommande_Annonces_AnnonceId" FOREIGN KEY ("AnnonceId") REFERENCES "Annonces" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LignesCommande_Commandes_CommandeId" FOREIGN KEY ("CommandeId") REFERENCES "Commandes" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AnnonceEquipements_EquipementId" ON "AnnonceEquipements" ("EquipementId");

CREATE INDEX "IX_Annonces_LoueurId" ON "Annonces" ("LoueurId");

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE UNIQUE INDEX "IX_Commandes_Reference" ON "Commandes" ("Reference");

CREATE INDEX "IX_Commandes_UtilisateurId" ON "Commandes" ("UtilisateurId");

CREATE UNIQUE INDEX "IX_Equipements_Code" ON "Equipements" ("Code");

CREATE INDEX "IX_LignesCommande_AnnonceId" ON "LignesCommande" ("AnnonceId");

CREATE INDEX "IX_LignesCommande_CommandeId" ON "LignesCommande" ("CommandeId");

CREATE INDEX "IX_LignesPanier_AnnonceId" ON "LignesPanier" ("AnnonceId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260904175121_SchemaInitial', '8.0.30');

COMMIT;

