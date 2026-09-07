using System.Net;
using System.Text;
using System.Threading.Tasks;
using Escale.Web.Entites;
using Escale.Web.Interfaces;
using Microsoft.Extensions.Logging;

namespace Escale.Web.Services
{
    // La facture reprend le reçu déjà figé dans la commande. Aucune donnée de
    // carte n'y figure au-delà des quatre derniers chiffres, comme le veut la
    // norme PCI. Toute valeur qui vient d'un utilisateur est encodée avant
    // d'entrer dans le HTML, ce qui écarte l'injection dans le courriel.
    public class FactureService : IFactureService
    {
        private readonly IUtilisateurRepository _comptes;
        private readonly ICourrielService _courriel;
        private readonly ILogger<FactureService> _journal;

        public FactureService(IUtilisateurRepository comptes, ICourrielService courriel,
            ILogger<FactureService> journal)
        {
            _comptes = comptes;
            _courriel = courriel;
            _journal = journal;
        }

        public async Task EnvoyerAsync(Commande commande)
        {
            Utilisateur? client = await _comptes.ObtenirAsync(commande.UtilisateurId);

            if (client?.Email is null)
            {
                _journal.LogWarning("Facture non envoyée : courriel introuvable pour la commande {Reference}",
                    commande.Reference);
                return;
            }

            string sujet = $"Votre réservation Escale, commande {commande.Reference}";
            string corps = Rendre(commande, client.NomComplet);

            await _courriel.EnvoyerAsync(client.Email, sujet, corps);
        }

        private static string Rendre(Commande commande, string nomClient)
        {
            static string E(string valeur) => WebUtility.HtmlEncode(valeur);

            StringBuilder html = new StringBuilder();
            html.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:auto;color:#08202D\">");
            html.Append("<h1 style=\"font-size:22px;margin:0 0 4px\">Escale</h1>");
            html.Append("<p style=\"color:#4C6675;margin:0 0 20px\">Facture</p>");

            html.Append($"<p>Bonjour {E(nomClient)},</p>");
            html.Append("<p>Merci pour votre réservation. Voici le détail de votre commande.</p>");

            html.Append("<table style=\"width:100%;border-collapse:collapse;font-size:14px;margin:18px 0\">");
            html.Append("<tr style=\"text-align:left;color:#4C6675;border-bottom:1px solid #E6EEF2\">"
                + "<th style=\"padding:8px 0\">Réservation</th><th>Période</th><th style=\"text-align:right\">Montant</th></tr>");

            foreach (LigneCommande ligne in commande.Lignes)
            {
                string categorie = ligne.Categorie == CategorieAnnonce.Chambre ? "Chambre" : "Voiture";
                html.Append("<tr style=\"border-bottom:1px solid #E6EEF2\">");
                html.Append($"<td style=\"padding:10px 0\"><b>{E(ligne.Titre)}</b><br>"
                    + $"<span style=\"color:#4C6675\">{categorie}</span></td>");
                html.Append($"<td style=\"color:#4C6675\">{ligne.DateDebut:d MMM yyyy} au {ligne.DateFin:d MMM yyyy}<br>"
                    + $"{ligne.NombreDeJours} jour(s) à {ligne.PrixJournalier} $</td>");
                html.Append($"<td style=\"text-align:right\"><b>{ligne.SousTotal} $</b></td>");
                html.Append("</tr>");
            }

            html.Append("</table>");

            html.Append($"<p style=\"text-align:right;font-size:18px\">Total réglé : <b>{commande.Total} $</b></p>");
            html.Append($"<p style=\"color:#4C6675;font-size:13px\">Commande {E(commande.Reference)}, "
                + $"réglée le {commande.DateCommande.ToLocalTime():d MMMM yyyy}, "
                + $"carte se terminant par {E(commande.QuatreDerniers)}.</p>");

            html.Append("<p style=\"color:#5D7A88;font-size:12px;margin-top:24px\">"
                + "Escale, location de chambres et de voitures entre particuliers. Ce courriel est une confirmation, aucune action n'est requise.</p>");
            html.Append("</div>");

            return html.ToString();
        }
    }
}
