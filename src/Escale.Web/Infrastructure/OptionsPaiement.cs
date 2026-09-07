namespace Escale.Web.Infrastructure
{
    // Le point d'entrée de configuration du paiement, lié depuis la section
    // « Paiement » par le patron d'options de Microsoft (IOptions<T>).
    //
    // Tant que « Fournisseur » vaut « Simule » (ou reste vide), aucun appel
    // réseau n'a lieu et aucune clé n'est requise. Le jour où un vrai
    // prestataire est choisi, on renseigne son nom et ses clés ici, sans
    // toucher au code des couches supérieures.
    public class OptionsPaiement
    {
        public const string Section = "Paiement";

        // « Simule » par défaut. Une autre valeur désignera un prestataire
        // réel dont l'implémentation aura été ajoutée à Services/.
        public string Fournisseur { get; set; } = "Simule";

        // Renseignées seulement quand un vrai prestataire est branché. La clé
        // publique part au navigateur, la clé secrète et le secret de webhook
        // restent sur le serveur, en configuration jamais dans le code.
        public string? ClePublique { get; set; }

        public string? CleSecrete { get; set; }

        public string? SecretWebhook { get; set; }

        // Devise ISO 4217. Le Canada facture en dollars canadiens.
        public string Devise { get; set; } = "cad";

        public bool EstConfigure =>
            !string.IsNullOrWhiteSpace(Fournisseur)
            && !Fournisseur.Equals("Simule", System.StringComparison.OrdinalIgnoreCase);
    }
}
