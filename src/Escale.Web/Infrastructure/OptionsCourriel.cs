namespace Escale.Web.Infrastructure
{
    // Réglages de l'envoi de courriel, liés depuis la section « Courriel » par
    // le patron d'options de Microsoft. Aucune valeur en dur : l'hôte, le
    // compte et le mot de passe viennent tous de la configuration.
    //
    // Aujourd'hui : SMTP Gmail (smtp.gmail.com, 587, mot de passe d'application).
    // Demain : n'importe quel SMTP externe (SendGrid, Mailgun, Amazon SES...) se
    // branche en changeant ces mêmes valeurs, sans toucher au code.
    public class OptionsCourriel
    {
        public const string Section = "Courriel";

        public string? Hote { get; set; }

        public int Port { get; set; } = 587;

        public bool ActiverSsl { get; set; } = true;

        public string? Utilisateur { get; set; }

        public string? MotDePasse { get; set; }

        public string ExpediteurAdresse { get; set; } = "no-reply@escale.example.com";

        public string ExpediteurNom { get; set; } = "Escale";

        // Tant que l'hôte n'est pas renseigné, on n'envoie rien : on journalise
        // le courriel. C'est le mode de développement par défaut.
        public bool EstConfigure => !string.IsNullOrWhiteSpace(Hote);
    }
}
