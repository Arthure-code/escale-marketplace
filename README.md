# escale-marketplace

A rental marketplace where rooms and cars are booked side by side. Owners
publish their own listings and decide what the public sees; travellers search by
date, fill a cart and pay through a simulated gateway.

ASP.NET Core 8 Razor Pages, Entity Framework Core and SQLite. One stylesheet, no
CSS framework, no package from outside the Microsoft stack.

> The user interface is in French, as is the code vocabulary. This README and
> the repository metadata are in English.

## Screenshots

### Home and search

![The home page on a wide screen: a dark header, a hero photograph of a lake below mountains carrying the headline Une chambre, une voiture, une seule escale, a white search panel with a Tout, Chambres, Voitures selector and arrival and departure date fields, then the count 11 offres above a grid of listing photographs each tagged CHAMBRE or VOITURE](docs/preview-accueil.png)

### A listing

![The Loft sous les toits page: a wide photograph of the room on the left, and on the right a booking panel showing 165 $ par nuit, two date fields, the multiplication of the nightly rate by the number of days, a total and an Ajouter au panier button. Underneath, a Chambre chip, the title, the owner's name, the description and a row of amenity chips under Ce qui est inclus](docs/preview-detail.png)

### The cart

![The cart page for a signed in traveller: two lines, a room at 220 $ and a car at 2000 $, each with its photograph, its dates, its amenity chips and a Retirer button. On the right a summary splits the total between a CHAMBRES group and a VOITURES group, shows 2220 $ and offers Passer au paiement. The header carries a Panier link with a badge showing 2](docs/preview-panier.png)

### Owner dashboard

![The owner dashboard: four counters for listings, bookings, revenue and days rented, then a table of the owner's five rooms. Each row shows the photograph, title, description, category, the number of units, the nightly rate, an En ligne badge, and buttons to take the listing off the site or edit it](docs/preview-tableau.png)

### Signing in

![The sign in page: a centred white card on the page background, with a Connexion heading, a Courriel field, a Mot de passe field, a Rester connecté checkbox, an orange Se connecter button, and two links underneath for the forgotten password and for creating an account](docs/preview-connexion.png)

### Mobile, 390 px

![The home page on a phone: the header wraps to two rows, the brand above the four navigation links, the hero headline and paragraph wrap without being cut, the search panel becomes a single column with the two date fields stacked and a full width Rechercher button, and the listing cards run one per row. No horizontal scroll](docs/preview-mobile.png)

## How it works

**Rooms and cars in the same search.** One date range, one result list, two
categories side by side. Filter to one or keep both.

**A listing is a fleet, not a single unit.** An owner says how many identical
units they rent under one listing: one loft, six identical rooms, four of the
same car. Search subtracts the bookings that overlap the requested dates and
shows what is left, "3 véhicules disponibles" or "Complet sur ces dates" when
every unit is taken. A full listing stays visible, greyed and labelled, with its
booking button disabled. Change the dates and the count is recomputed on the
spot.

**Three kinds of account.** A traveller registers for free, an owner pays a
monthly subscription, and an administrator sits above both. The role decides
what the account can reach: the cart needs any account, the dashboard needs an
owner, the administration section needs an administrator.

**Owners run their own listings.** Each owner sees only their own listings and
the bookings placed on them, uploads their own photographs, and can take a
listing off the public site with one click and put it back the same way, without
deleting anything.

**Two conditions gate a listing.** It appears publicly when its owner marked it
visible and the owner's account is in good standing. Block an account or let its
subscription lapse and its listings leave the site at once; nothing is deleted,
and they come back when the block ends or the subscription is renewed.

**A cart that survives a sign-out.** Cart lines are stored, not held in the
session. The summary keeps rooms and cars in separate groups with their own
subtotals.

**A simulated payment.** The card is validated the way a real gateway would,
then the result is an acceptance or a refusal. No money moves, and only the last
four digits are kept.

**Favourites that expire.** A signed-in traveller can set a listing aside and
find it again for five days from the last change. Losing them afterwards is the
intended behaviour, not a defect.

**Amenities as pictograms.** Each listing carries what is included, drawn as
small icons: shower, wifi, kitchen, air conditioning and lift for rooms;
automatic gearbox, navigation, bluetooth, hybrid and four-wheel drive for cars.

## Running it

```bash
cd src/Escale.Web
dotnet user-secrets set "Semences:MotDePasse" "Your.Password1"
dotnet user-secrets set "Administrateur:MotDePasse" "Your.Password1"
dotnet run
```

No password is stored in the repository, so the two secrets above are what
create the demonstration accounts and the administrator on first start. Startup
stops with a clear message if the first one is missing. The database file is
created on first start and filled with fifteen amenities and eleven listings.
Delete `escale.db` to start over.

## Stack

ASP.NET Core 8 Razor Pages, Entity Framework Core 8, SQLite, ASP.NET Core
Identity. Repositories, services and interfaces in separate folders, so a page
never knows which database is behind it. Page model tests with xUnit and Moq.

## Résumé

Place de marché où l'on loue une chambre et une voiture au même endroit, en
ASP.NET Core 8 Razor Pages avec une base SQLite. Une annonce représente un parc
et non un exemplaire unique : la recherche retranche les réservations qui
chevauchent les dates demandées et affiche ce qu'il reste, sans jamais stocker
ce nombre. Trois rôles, voyageur, loueur et administrateur, décident de ce que
chacun atteint. Une annonce ne paraît publiquement que si son loueur l'a rendue
visible et que son compte est en règle : un blocage ou un abonnement échu la
retire du site et son retour la fait réapparaître, sans rien supprimer. Le
panier est persisté, le paiement est simulé et ne conserve que les quatre
derniers chiffres, les favoris vivent cinq jours dans un cache. Le découpage
sépare les entités, les interfaces, les dépôts et les services : seul un dépôt
connaît la base, de sorte qu'aucune page ne sait ce qu'il y a derrière. Les
comptes reposent sur ASP.NET Core Identity, avec ses pages générées et son
moteur d'origine. Interface et vocabulaire du code en français.

## Licence

MIT. See [LICENSE](LICENSE).
