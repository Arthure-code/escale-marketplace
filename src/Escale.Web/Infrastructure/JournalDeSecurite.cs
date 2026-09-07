using Microsoft.Extensions.Logging;

namespace Escale.Web.Infrastructure
{
    // Événements de sécurité et d'audit, avec un identifiant stable par
    // événement. OWASP A09 demande de tracer les échecs d'authentification,
    // les échecs de contrôle d'accès et les échecs de validation.
    //
    // Les gabarits de message ne concatènent jamais l'entrée utilisateur :
    // c'est ce qui rend le journal interrogeable et ce qui ferme l'injection
    // dans les journaux. Aucun mot de passe, aucun numéro de carte, aucun
    // jeton et aucun courriel n'y figure : on journalise l'identifiant du
    // compte, jamais son adresse.
    public static class JournalDeSecurite
    {
        public static readonly EventId ConnexionReussie = new EventId(1001, "ConnexionReussie");
        public static readonly EventId ConnexionRefusee = new EventId(1002, "ConnexionRefusee");
        public static readonly EventId ConnexionCompteBloque = new EventId(1003, "ConnexionCompteBloque");
        public static readonly EventId Deconnexion = new EventId(1004, "Deconnexion");
        public static readonly EventId CompteCree = new EventId(1005, "CompteCree");

        public static readonly EventId CompteBloque = new EventId(2001, "CompteBloque");
        public static readonly EventId CompteDebloque = new EventId(2002, "CompteDebloque");
        public static readonly EventId AbonnementModifie = new EventId(2003, "AbonnementModifie");
        public static readonly EventId AnnonceSupprimee = new EventId(2004, "AnnonceSupprimee");
        public static readonly EventId VisibiliteBasculee = new EventId(2005, "VisibiliteBasculee");

        public static readonly EventId CommandePassee = new EventId(3001, "CommandePassee");
        public static readonly EventId PaiementRefuse = new EventId(3002, "PaiementRefuse");
        public static readonly EventId ReservationImpossible = new EventId(3003, "ReservationImpossible");

        public static readonly EventId TeleversementRefuse = new EventId(4001, "TeleversementRefuse");
        public static readonly EventId DebitDepasse = new EventId(4002, "DebitDepasse");
    }
}
