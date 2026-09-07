namespace Escale.Web.Entites
{
    // Ce qu'un service renvoie à une page : le verdict et, s'il est négatif,
    // la raison à afficher. Aucune exception n'est utilisée pour un refus
    // attendu.
    public record ResultatAjout(bool Reussi, string Message);

    public record ResultatCommande(bool Reussi, string Message, string Reference);

    public record ResultatPaiement(bool Accepte, string Message, string Reference, string QuatreDerniers);

    public record ResultatPhoto(bool Reussi, string Message, string NomFichier);

    public record DonneesCarte(string Titulaire, string Numero, string Expiration, string Cvc);

    // Ce qu'une passerelle reçoit. La référence sert de clé d'idempotence :
    // un vrai prestataire l'utilise pour ne jamais débiter deux fois la même
    // commande. « Carte » sert au mode simulé ; « Jeton » servira à un
    // prestataire qui tokenise la carte dans le navigateur (le serveur ne
    // voit alors jamais le numéro), ce qui est le modèle recommandé.
    public record DemandePaiement(int Montant, string Reference, DonneesCarte? Carte = null, string? Jeton = null);
}
