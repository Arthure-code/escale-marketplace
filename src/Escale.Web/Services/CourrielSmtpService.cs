using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Escale.Web.Infrastructure;
using Escale.Web.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Escale.Web.Services
{
    // Envoi par SMTP, avec le client intégré à .NET : aucune dépendance
    // tierce. Convient à Gmail (smtp.gmail.com, 587, mot de passe
    // d'application) comme à tout SMTP externe, la différence étant seulement
    // dans la configuration.
    public class CourrielSmtpService : ICourrielService
    {
        private readonly OptionsCourriel _options;
        private readonly ILogger<CourrielSmtpService> _journal;

        public CourrielSmtpService(IOptions<OptionsCourriel> options, ILogger<CourrielSmtpService> journal)
        {
            _options = options.Value;
            _journal = journal;
        }

        public async Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml)
        {
            // Le chiffrement du transport n'est pas une option de configuration :
            // sans lui, l'identifiant et le mot de passe du compte d'envoi
            // circulent en clair. Une valeur mal remplie ne doit pas pouvoir
            // dégrader cela, donc la constante est écrite ici.
            using SmtpClient client = new SmtpClient(_options.Hote, _options.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_options.Utilisateur, _options.MotDePasse)
            };

            using MailMessage message = new MailMessage
            {
                From = new MailAddress(_options.ExpediteurAdresse, _options.ExpediteurNom),
                Subject = sujet,
                Body = corpsHtml,
                IsBodyHtml = true
            };
            message.To.Add(destinataire);

            await client.SendMailAsync(message);

            _journal.LogInformation("Courriel envoyé à {Destinataire}, sujet {Sujet}", destinataire, sujet);
        }
    }
}
