using System.Globalization;
using System.Threading.RateLimiting;
using Escale.Web.Entites;
using Escale.Web.Infrastructure;
using Escale.Web.Interfaces;
using Escale.Web.Repositories;
using Escale.Web.Services;
using Escale.Web.Services.Regles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Kestrel annonce « Server: Kestrel » par défaut. On le retire : un en-tête de
// moins qui renseigne un attaquant sur la pile en place.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

// ---------------------------------------------------------------- journal
// En production, la sortie est du JSON sur la console : c'est le format
// qu'Azure App Service, Application Insights et n'importe quel collecteur
// savent lire. En développement, la console lisible suffit.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        options.UseUtcTimestamp = true;
    });
}

// ---------------------------------------------------------------- données
builder.Services.AddDbContext<ContexteEscale>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Escale")));


// ------------------------------------------------------------- identité
builder.Services
    .AddIdentity<Utilisateur, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        // On n'impose pas la confirmation de l'adresse pour ouvrir un compte :
        // personne ne s'inscrit pour attendre un courriel avant de pouvoir
        // regarder les annonces. Le moteur de confirmation reste en place,
        // jetons et pages compris, prêt si la règle change un jour.
        options.SignIn.RequireConfirmedAccount = false;

        // Verrouillage après échecs répétés : sans lui, un mot de passe
        // s'essaie indéfiniment.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ContexteEscale>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Le cookie antifalsification garde son nom propre, HttpOnly et SameSite Strict.
// On ne force pas Secure : l'antiforgery refuse de servir en HTTP si on l'exige,
// et la redirection HTTPS plus HSTS le rendent déjà Secure en production.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "Escale.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ------------------------------------------------------- couches internes
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUtilisateurCourant, UtilisateurCourant>();

builder.Services.AddScoped<IAnnonceRepository, AnnonceRepository>();
builder.Services.AddScoped<IUtilisateurRepository, UtilisateurRepository>();
builder.Services.AddScoped<IEquipementRepository, EquipementRepository>();
builder.Services.AddScoped<IPanierRepository, PanierRepository>();
builder.Services.AddScoped<ICommandeRepository, CommandeRepository>();

builder.Services.AddScoped<IAnnonceService, AnnonceService>();
builder.Services.AddScoped<IEquipementService, EquipementService>();
builder.Services.AddScoped<IPanierService, PanierService>();
builder.Services.AddScoped<ICommandeService, CommandeService>();
// Passerelle de paiement, choisie par configuration comme le cache. Tant
// que « Paiement:Fournisseur » n'est pas renseigné (ou vaut « Simule »), le
// service simulé reste en place : aucun appel réseau, aucune clé requise, et
// donc aucun risque tant qu'aucun vrai paiement ne circule.
//
// Pour brancher un prestataire réel : ajouter sa classe dans Services/ (une
// classe qui implémente IPaiementService), la sélectionner dans le switch
// ci-dessous, et poser ses clés dans la section « Paiement » de la config.
builder.Services.Configure<OptionsPaiement>(
    builder.Configuration.GetSection(OptionsPaiement.Section));

string fournisseur = builder.Configuration[$"{OptionsPaiement.Section}:Fournisseur"] ?? "Simule";

// Brancher un vrai prestataire revient à écrire une classe qui implémente
// IPaiementService, puis à ajouter ici la branche qui porte son nom. Rien
// d'autre dans l'application ne change.
switch (fournisseur.ToLowerInvariant())
{
    default:
        builder.Services.AddScoped<IPaiementService, PaiementSimuleService>();
        break;
}

// Courriel, choisi par configuration comme le reste. Hôte SMTP présent →
// envoi réel (Gmail aujourd'hui, service externe demain, même code). Hôte
// absent → le courriel est journalisé, l'application tourne sans compte de
// messagerie. Patron IEmailSender documenté par Microsoft.
builder.Services.Configure<OptionsCourriel>(
    builder.Configuration.GetSection(OptionsCourriel.Section));

string? hoteCourriel = builder.Configuration[$"{OptionsCourriel.Section}:Hote"];

if (string.IsNullOrWhiteSpace(hoteCourriel))
{
    builder.Services.AddScoped<ICourrielService, CourrielJournalService>();
}
else
{
    builder.Services.AddScoped<ICourrielService, CourrielSmtpService>();
}

builder.Services.AddScoped<IFactureService, FactureService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, PontCourrielIdentity>();
builder.Services.AddScoped<IUtilisateurService, UtilisateurService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IFavorisService, FavorisService>();

builder.Services.AddScoped<IRegleDeDiffusion, CompteNonBloque>();
builder.Services.AddScoped<IRegleDeDiffusion, AbonnementAJour>();
builder.Services.AddScoped<IDiffusionService, DiffusionService>();

// ------------------------------------------------------------------ cache
// Une seule interface devant toutes les implémentations. Sans chaîne de
// connexion, l'implémentation en mémoire du framework ; avec, Redis.
string? redis = builder.Configuration.GetConnectionString("Redis");

if (string.IsNullOrWhiteSpace(redis))
{
    builder.Services.AddDistributedMemoryCache(options =>
    {
        // Une borne explicite : sans elle, le cache en mémoire grossit tant
        // que le processus vit.
        options.SizeLimit = 32 * 1024 * 1024;
    });
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redis;
        options.InstanceName = "Escale:";
    });
}

// --------------------------------------------------------- débit entrant
// Un limiteur global pour tout le site, plus une politique nommée que les
// pages sensibles réclament par attribut. Le global continue de s'appliquer
// en plus de la politique nommée.
//
// Appeler RequireRateLimiting sur MapRazorPages écraserait l'attribut des
// pages : c'est pour cela que le plafond général passe par GlobalLimiter.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // La partition suit l'identité quand elle existe, l'adresse sinon.
    // Partitionner sur une adresse reste sensible à l'usurpation de source :
    // en production, un pare-feu applicatif doit venir en amont.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexte =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: contexte.User.Identity?.Name
                ?? contexte.Connection.RemoteIpAddress?.ToString()
                ?? "inconnu",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));

    // AddFixedWindowLimiter poserait un seul seau partagé par tout le site :
    // huit inscriptions par minute pour l'ensemble des visiteurs. AddPolicy
    // permet de partitionner, donc de compter par demandeur.
    options.AddPolicy("sensible", contexte =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexte.User.Identity?.Name
                ?? contexte.Connection.RemoteIpAddress?.ToString()
                ?? "inconnu",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (contexte, annulation) =>
    {
        if (contexte.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan delai))
        {
            contexte.HttpContext.Response.Headers.RetryAfter =
                ((int)delai.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        ILogger journal = contexte.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Escale.Debit");

        journal.LogWarning(JournalDeSecurite.DebitDepasse,
            "Débit dépassé sur {Chemin}", contexte.HttpContext.Request.Path);

        // Sans jeu de caractères déclaré, le navigateur retombe sur son encodage
        // par défaut et affiche « requÃªtes » à la place des accents.
        contexte.HttpContext.Response.ContentType = "text/plain; charset=utf-8";

        await contexte.HttpContext.Response.WriteAsync(
            "Trop de requêtes. Réessayez dans un instant.", annulation);
    };
});

// ------------------------------------------------------------------ pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Panier");
    options.Conventions.AuthorizeFolder("/Favoris");
    options.Conventions.AuthorizeFolder("/Commandes");
    options.Conventions.AuthorizeFolder("/Tableau", "EstLoueur");
    options.Conventions.AuthorizeFolder("/Administration", "EstAdministrateur");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EstLoueur", regle => regle.RequireRole(Utilisateur.RoleLoueur, Utilisateur.RoleAdministrateur));
    options.AddPolicy("EstAdministrateur", regle => regle.RequireRole(Utilisateur.RoleAdministrateur));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// ------------------------------------------------------------- démarrage
// Le schéma s'applique par migration en développement seulement. En
// production, Microsoft recommande de générer un script idempotent et de le
// faire relire, plutôt que de laisser l'application modifier sa base au
// démarrage.
await using (AsyncServiceScope portee = app.Services.CreateAsyncScope())
{
    await Amorcage.PreparerAsync(portee.ServiceProvider, app.Environment, app.Configuration);
}

// -------------------------------------------------------------- pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Rien de plus en développement : la page d'erreur détaillée reste utile.
}

app.UseEntetesDeSecurite();
app.UseStatusCodePagesWithReExecute("/CodeStatut", "?code={0}");

CultureInfo culture = new CultureInfo("fr-CA");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});

app.UseHttpsRedirection();

// Les fichiers de wwwroot ne changent qu'au déploiement et portent déjà une
// empreinte par asp-append-version : les mettre en cache un an supprime
// autant d'allers-retours vers le serveur.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = contexte =>
    {
        contexte.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=31536000,immutable";
    }
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

// Rendu visible pour les tests d'intégration éventuels. Le constructeur protégé
// dit que cette classe n'est pas faite pour être instanciée : elle n'existe que
// comme point d'entrée et comme repère de type.
public partial class Program
{
    protected Program()
    {
    }
}
