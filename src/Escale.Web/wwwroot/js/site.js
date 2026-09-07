// Photo manquante : on retombe sur l'image par défaut. Le gestionnaire est
// posé une seule fois en phase de capture, parce que l'événement « error »
// d'une image ne remonte pas. Cela remplace les attributs onerror en ligne,
// que la politique de sécurité du contenu interdit.
document.addEventListener('error', function (evenement) {
    const cible = evenement.target;

    if (cible instanceof HTMLImageElement && !cible.dataset.replie) {
        cible.dataset.replie = 'oui';
        cible.src = '/images/defaut.jpg';
    }
}, true);
