// Une chambre et une voiture n'ont pas les mêmes caractéristiques ni les
// mêmes équipements : le formulaire ne montre que ce qui concerne la
// catégorie choisie.
(function () {
    // Repéré par son nom plutôt que par un identifiant à nous : l'assistant de
    // balise engendre déjà l'identifiant, et le forcer cassait le lien avec
    // l'étiquette, qui pointait alors dans le vide.
    var categorie = document.querySelector('select[name="Saisie.Categorie"]');

    if (categorie === null) {
        return;
    }

    var chambre = document.getElementById('bloc-chambre');
    var voiture = document.getElementById('bloc-voiture');
    var pastilles = document.querySelectorAll('.equip[data-categorie]');

    function ajuster() {
        var estChambre = categorie.value === 'Chambre';

        chambre.hidden = !estChambre;
        voiture.hidden = estChambre;

        pastilles.forEach(function (pastille) {
            var visible = pastille.dataset.categorie === categorie.value;
            pastille.hidden = !visible;

            if (!visible) {
                pastille.querySelector('input').checked = false;
            }
        });
    }

    categorie.addEventListener('change', ajuster);
    ajuster();
})();
