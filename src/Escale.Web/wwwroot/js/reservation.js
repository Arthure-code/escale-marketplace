// Le total n'est jamais stocké : il se recalcule à partir du tarif et des
// deux dates à chaque changement, et le serveur refait le même calcul au
// moment de l'ajout au panier.
(function () {
    var formulaire = document.getElementById('reservation');

    if (formulaire === null) {
        return;
    }

    var debut = document.getElementById('debut');
    var fin = document.getElementById('fin');
    var ligne = document.getElementById('detail-calcul');
    var montant = document.getElementById('detail-montant');
    var total = document.getElementById('detail-total');

    var prix = Number.parseInt(formulaire.dataset.prix, 10);
    var unite = formulaire.dataset.unite;
    var jourEnMs = 24 * 60 * 60 * 1000;

    // Tout le calcul se fait en UTC : passer par l'heure locale décalerait le
    // lendemain d'un jour au changement d'heure.
    function enUtc(valeur) {
        var p = valeur.split('-');
        return Date.UTC(+p[0], +p[1] - 1, +p[2]);
    }

    function texte(instant) {
        return new Date(instant).toISOString().slice(0, 10);
    }

    function recalculer() {
        if (!debut.value || !fin.value) {
            return;
        }

        fin.min = texte(enUtc(debut.value) + jourEnMs);

        if (fin.value < fin.min) {
            fin.value = fin.min;
        }

        var jours = Math.max(1, (enUtc(fin.value) - enUtc(debut.value)) / jourEnMs);
        var somme = prix * jours;

        ligne.textContent = prix + ' $ x ' + jours + ' ' + unite + (jours > 1 ? 's' : '');
        montant.textContent = somme + ' $';
        total.textContent = somme + ' $';
    }

    debut.addEventListener('change', recalculer);
    fin.addEventListener('change', recalculer);
    recalculer();
})();
