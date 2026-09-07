// Page générée par le scaffolding Identity, puis adaptée à Escale : choix du
// rôle (voyageur ou loueur), nom complet, abonnement du loueur, et
// journalisation de sécurité. Le moteur (hachage, jetons, confirmation) reste
// celui de Microsoft.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Escale.Web.Areas.Identity.Pages.Account
{
    [EnableRateLimiting("sensible")]
    public class RegisterModel : PageModel
    {
        public const int Mensualite = 29;

        private readonly SignInManager<Utilisateur> _signInManager;
        private readonly UserManager<Utilisateur> _userManager;
        private readonly IUserStore<Utilisateur> _userStore;
        private readonly IUserEmailStore<Utilisateur> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<Utilisateur> userManager,
            IUserStore<Utilisateur> userStore,
            SignInManager<Utilisateur> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Le nom complet est obligatoire.")]
            [Display(Name = "Nom complet")]
            public string NomComplet { get; set; }

            [Required(ErrorMessage = "Le courriel est obligatoire.")]
            [EmailAddress(ErrorMessage = "Ce courriel n'est pas valide.")]
            [Display(Name = "Courriel")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
            [StringLength(64, MinimumLength = 8, ErrorMessage = "Le mot de passe compte au moins 8 caractères.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mot de passe")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmation")]
            [Compare("Password", ErrorMessage = "Les deux mots de passe diffèrent.")]
            public string ConfirmPassword { get; set; }

            // Le rôle choisi décide de tout le reste : un loueur publie des
            // annonces et paie un abonnement, un voyageur réserve.
            [Required(ErrorMessage = "Le type de compte est obligatoire.")]
            [Display(Name = "Type de compte")]
            public string Role { get; set; } = Utilisateur.RoleVoyageur;
        }

        public async Task OnGetAsync(string returnUrl = null, string role = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            Input = new InputModel
            {
                Role = role == Utilisateur.RoleLoueur ? Utilisateur.RoleLoueur : Utilisateur.RoleVoyageur
            };
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            bool loueur = Input.Role == Utilisateur.RoleLoueur;

            Utilisateur user = CreateUser();
            user.NomComplet = Input.NomComplet;

            if (loueur)
            {
                user.Abonnement = new Abonnement
                {
                    Debut = DateTime.UtcNow,
                    Mensualite = Mensualite,
                    Echeance = DateTime.Today.AddMonths(1)
                };
            }

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            IdentityResult result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, loueur ? Utilisateur.RoleLoueur : Utilisateur.RoleVoyageur);

                _logger.LogInformation(JournalDeSecurite.CompteCree,
                    "Compte {UtilisateurId} créé avec le rôle {Role}", user.Id, Input.Role);

                // Le courriel de confirmation ne part que si la confirmation
                // conditionne l'accès. Sans cela on demanderait à quelqu'un de
                // confirmer une chose dont rien ne dépend, et le message serait
                // du bruit. Le réglage seul décide, code et courriel suivent.
                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    string userId = await _userManager.GetUserIdAsync(user);
                    string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    string callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code, returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirmez votre courriel",
                        $"Confirmez votre compte Escale en <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>cliquant ici</a>.");

                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                // Un loueur arrive sur son tableau de bord, un voyageur sur
                // l'accueil, sauf si une page l'attendait déjà.
                if (!Url.IsLocalUrl(returnUrl) || returnUrl == Url.Content("~/"))
                {
                    return RedirectToPage(loueur ? "/Tableau/Index" : "/Index");
                }

                return LocalRedirect(returnUrl);
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, Traduire(error));
            }

            return Page();
        }

        private static string Traduire(IdentityError erreur) => erreur.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Un compte existe déjà avec ce courriel.",
            "PasswordTooShort" => "Le mot de passe compte au moins 8 caractères.",
            "PasswordRequiresDigit" => "Le mot de passe doit contenir un chiffre.",
            "PasswordRequiresUpper" => "Le mot de passe doit contenir une majuscule.",
            "PasswordRequiresLower" => "Le mot de passe doit contenir une minuscule.",
            "PasswordRequiresNonAlphanumeric" => "Le mot de passe doit contenir un caractère spécial.",
            _ => erreur.Description
        };

        private Utilisateur CreateUser()
        {
            try
            {
                return Activator.CreateInstance<Utilisateur>();
            }
            catch
            {
                throw new InvalidOperationException($"Impossible de créer une instance de '{nameof(Utilisateur)}'.");
            }
        }

        private IUserEmailStore<Utilisateur> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("Le magasin d'utilisateurs doit gérer le courriel.");
            }
            return (IUserEmailStore<Utilisateur>)_userStore;
        }
    }
}
