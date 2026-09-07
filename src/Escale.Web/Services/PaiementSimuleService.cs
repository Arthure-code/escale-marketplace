using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using Escale.Web.Entites;
using Escale.Web.Interfaces;

namespace Escale.Web.Services
{
    // Aucun argent ne circule. Le service applique les mêmes contrôles qu'une
    // passerelle réelle, puis accepte ou refuse selon ce qui a été saisi.
    // Numéros d'essai : 4242 4242 4242 4242 passe, 4000 0000 0000 0002 est refusé.
    //
    // C'est le service par défaut, celui qui reste branché tant qu'aucun vrai
    // prestataire n'est configuré. Il tient lieu de « lien de remplacement »
    // et ne fait aucun appel réseau.
    public class PaiementSimuleService : IPaiementService
    {
        public const string CarteAcceptee = "4242424242424242";
        public const string CarteRefusee = "4000000000000002";

        // Rien n'attend ici : on enveloppe simplement le verdict dans une
        // Task pour respecter l'interface asynchrone.
        public Task<ResultatPaiement> PayerAsync(DemandePaiement demande) =>
            Task.FromResult(Verifier(demande.Carte, demande.Montant));

        private static ResultatPaiement Verifier(DonneesCarte? carte, int montant)
        {
            if (carte is null)
            {
                return Refus("Aucune donnée de carte fournie.");
            }

            string chiffres = Chiffres(carte.Numero);

            if (string.IsNullOrWhiteSpace(carte.Titulaire))
            {
                return Refus("Le nom du titulaire est requis.");
            }

            if (chiffres.Length < 13 || chiffres.Length > 19)
            {
                return Refus("Le numéro de carte doit compter entre 13 et 19 chiffres.");
            }

            if (!LuhnEstValide(chiffres))
            {
                return Refus("Ce numéro de carte est invalide. Vérifiez la saisie.");
            }

            if (!ExpirationEstValide(carte.Expiration))
            {
                return Refus("La date d'expiration doit être au format MM/AA et ne pas être dépassée.");
            }

            if (Chiffres(carte.Cvc).Length != 3)
            {
                return Refus("Le code de sécurité compte trois chiffres.");
            }

            if (montant <= 0)
            {
                return Refus("Le montant à payer est nul.");
            }

            if (chiffres == CarteRefusee)
            {
                return Refus("Paiement refusé par la banque émettrice.");
            }

            string reference = "PAY-" + DateTime.UtcNow.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);

            return new ResultatPaiement(true, "Paiement accepté.", reference, chiffres[^4..]);
        }

        private static string Chiffres(string? saisie) =>
            new string((saisie ?? string.Empty).Where(char.IsDigit).ToArray());

        private static ResultatPaiement Refus(string message) =>
            new ResultatPaiement(false, message, string.Empty, string.Empty);

        // Algorithme de Luhn, celui qu'utilisent les vraies passerelles pour
        // écarter une faute de frappe avant même d'appeler la banque.
        private static bool LuhnEstValide(string chiffres)
        {
            int somme = 0;
            bool doubler = false;

            for (int i = chiffres.Length - 1; i >= 0; i--)
            {
                int valeur = chiffres[i] - '0';

                if (doubler)
                {
                    valeur *= 2;
                    if (valeur > 9) { valeur -= 9; }
                }

                somme += valeur;
                doubler = !doubler;
            }

            return somme % 10 == 0;
        }

        private static bool ExpirationEstValide(string expiration)
        {
            string[] parties = (expiration ?? string.Empty).Split('/');

            if (parties.Length != 2
                || !int.TryParse(parties[0].Trim(), out int mois)
                || !int.TryParse(parties[1].Trim(), out int annee)
                || mois < 1 || mois > 12)
            {
                return false;
            }

            int anneeComplete = annee < 100 ? 2000 + annee : annee;
            DateTime finDuMois = new DateTime(anneeComplete, mois, 1).AddMonths(1).AddDays(-1);

            return finDuMois >= DateTime.Today;
        }
    }
}
