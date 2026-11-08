# Boutique Diayma - TD Visual Studio Débogage

TD C# et Technologies .NET - ESP / UCAD - Département Génie Informatique - 2026/2027

Étudiant : **NOM Prénom** - Classe : **...**

Dépôt : https://github.com/DA19s/BoutiqueDiayma2025

---

## 1. Récupération et exécution du code

J'ai fait un fork du dépôt du prof (https://github.com/elhadjiousmane/BoutiqueDiayma2025) sur mon compte GitHub, puis je l'ai cloné dans Visual Studio avec **Cloner un dépôt**.

À l'ouverture de `Diayma.sln`, Visual Studio affiche l'avertissement *"Cette solution contient des packages présentant des vulnérabilités"*. Je l'ai fermé.

Pour lancer l'application : choisir le profil **P2FixAnAppDotNetCode** à côté du bouton ▶ puis **F5**. Le site s'ouvre sur http://localhost:62929 et affiche la liste des produits.

## 2. Projets de la solution

La solution `Diayma.sln` contient **un seul projet** :

- **Diayma** (`P2FixAnAppDotNetCode/Diayma.csproj`) : une application web **ASP.NET Core MVC**. Le namespace utilisé dans le code est `P2FixAnAppDotNetCode` (le projet a été renommé en Diayma mais pas le namespace).

Organisation du projet :

| Dossier | Contenu |
|---|---|
| Controllers | ProductController, CartController, OrderController, LanguageController |
| Components | CartSummaryViewComponent (résumé du panier), LanguageSelectorViewComponent (choix de la langue) |
| Models | Product, Cart, Order + Repositories (ProductRepository, OrderRepository) + Services (ProductService, OrderService, LanguageService) |
| Views | les vues Razor (.cshtml) |
| Resources | les traductions (.resx) |
| wwwroot | CSS, JS, bootstrap, jquery |

## 3. Version du SDK .NET

Dans `Diayma.csproj` on a :

```xml
<TargetFramework>netcoreapp2.0</TargetFramework>
<PackageReference Include="Microsoft.AspNetCore.All" Version="2.0.6" />
```

Le projet utilise donc **.NET Core 2.0** (ASP.NET Core 2.0). Le SDK qui correspond est le **SDK .NET Core 2.1.202** (dernier SDK de la version 2.0, avec le runtime 2.0.9).

Cette version date de 2018 et n'est plus supportée par Microsoft. C'est pour ça qu'on a l'avertissement sur les packages vulnérables. À la compilation on voit aussi ces avertissements :

- `NETSDK1138` : la version cible netcoreapp2.0 n'est plus prise en charge
- `NU1902` / `NU1903` : vulnérabilités connues dans `Microsoft.AspNetCore.All 2.0.6` et `Microsoft.NETCore.App 2.0.0`

## 4. Installation du SDK

Le projet compile avec le SDK .NET 10, mais pour l'exécuter et le déboguer il faut le runtime .NET Core 2.0.

J'ai installé le **SDK .NET Core 2.1.202 (Windows x64)** depuis https://dotnet.microsoft.com/download/dotnet/2.0

Vérification dans un terminal :

```
dotnet --list-sdks
dotnet --list-runtimes
```

On doit voir `2.1.202` dans les SDK et `Microsoft.NETCore.App 2.0.9` dans les runtimes.

## 5. Mon dépôt GitHub

https://github.com/DA19s/BoutiqueDiayma2025

## 6. Bugs trouvés

### Bug 1 : le total du panier ne tient pas compte de la quantité

**Pour le reproduire :** ajouter le produit "XIAOMI Redmi 13C" deux fois au panier.

- Le panier affiche Quantité = 2 et Sous-total = 176 000
- Mais le **Total = 88 000** (au lieu de 176 000)
- Le résumé du panier en haut de la page affiche aussi "2 item(s) 88 000"
- La moyenne est fausse aussi

**Cause :** dans `Models/Cart.cs`, la méthode `GetTotalValue()` fait seulement la somme des prix sans multiplier par la quantité :

```csharp
return GetCartLineList().Sum(x => x.Product.Price);
```

**Correction :**

```csharp
return GetCartLineList().Sum(x => x.Product.Price * x.Quantity);
```

et pour `GetAverageValue()` j'ai fait total / nombre d'articles.

### Bug 2 : le stock ne diminue jamais après une commande

**Pour le reproduire :** le Redmi a un stock de 10. On l'ajoute 2 fois au panier, on clique sur "Passer la commande", on remplit le formulaire et on valide. Quand on revient sur la liste des produits, le stock est **toujours 10** (au lieu de 8).

**Cause :** dans `Startup.cs` (ligne 32), `ProductRepository` est enregistré en `AddTransient`. Donc un nouvel objet est créé à chaque requête et son constructeur recrée la liste des produits avec les stocks de départ (`ProductRepository.cs` lignes 15-16). La mise à jour du stock est perdue.

**Correction :** j'ai remplacé `AddTransient` par `AddSingleton` pour `IProductRepository`. Après la correction le stock passe bien de 10 à 8.

### Autres problèmes remarqués (pas corrigés)

- Quand on valide le formulaire de commande vide, les messages d'erreur affichent `ErrorMissingName`, `ErrorMissingAddress`, `CartEmpty`... au lieu du texte traduit. Les fichiers .resx du contrôleur et du modèle Order ne sont pas trouvés (le projet s'appelle Diayma mais le namespace est P2FixAnAppDotNetCode, et `Order.resx` est dans le mauvais dossier).
- Le choix "Espagnol" ne change pas la langue : il n'y a pas de culture `es` dans `Startup.cs` ni de fichiers `.es.resx`.
- Les prix sont en FCFA mais ils s'affichent en € (ou en $ en anglais).
- Le panier est en `AddSingleton` donc il est partagé entre tous les utilisateurs du site.

## 7. Points d'arrêt

| | Fichier | Ligne | Code | Quand on s'arrête |
|---|---|---|---|---|
| a | Components/CartSummaryViewComponent.cs | 12 | `_cart = cart as Cart;` | à chaque page (le résumé du panier est dans le layout) |
| b | Controllers/ProductController.cs | 15 | `_productService = productService;` | à l'affichage de la page d'accueil |
| c | Controllers/OrderController.cs | 17 | `_cart = pCart;` | quand on clique sur "Passer la commande" |
| d | Controllers/CartController.cs | 15 | `_cart = pCart;` | quand on ajoute un produit ou qu'on ouvre le panier |
| e | Startup.cs | 20 | `Configuration = configuration;` | une seule fois au démarrage |

Les 5 points d'arrêt sont dans des **constructeurs**. Au chargement de la page d'accueil on passe seulement par **e**, **b** et **a**. Les points **c** et **d** ne sont pas atteints.

## 8. Parcours du débogueur jusqu'à l'affichage des produits

Les modes utilisés :

- **Pas à pas détaillé (F11)** : pour entrer dans une méthode de notre code (ex : `_productService.GetAllProducts()`).
- **Pas à pas principal (F10)** : pour exécuter une ligne sans entrer dedans, surtout pour le code du framework (ex : les `services.AddXxx()` dans `ConfigureServices`).
- **Pas à pas sortant (Maj+F11)** : pour sortir d'une méthode et revenir à celle qui l'a appelée (ex : sortir de `GenerateProductData()` après avoir vu un produit).
- **F5** : pour aller directement au point d'arrêt suivant.

J'ai lancé le débogage avec **F11** au lieu de F5 pour que le débogueur s'arrête dès la première ligne de `Main`. La fenêtre **Pile des appels** (Ctrl+Alt+C) permet de voir à chaque fois le namespace, la classe et la méthode.

### Étape 1 : démarrage de l'application

| # | Namespace | Classe | Méthode | Mode |
|---|---|---|---|---|
| 1 | P2FixAnAppDotNetCode | Program | `Main(string[] args)` | F11 (démarrage) |
| 2 | P2FixAnAppDotNetCode | Program | `BuildWebHost(string[] args)` | F11 |
| 3 | P2FixAnAppDotNetCode | Startup | `Startup(IConfiguration)` constructeur, **point e** | F11 |
| 4 | P2FixAnAppDotNetCode | Startup | `ConfigureServices(IServiceCollection)` : enregistrement des services | F10 |
| 5 | P2FixAnAppDotNetCode | Startup | `Configure(IApplicationBuilder, IHostingEnvironment)` | F10 |
| 5a | P2FixAnAppDotNetCode | Startup | lambda des lignes 43 à 60 (liste des cultures), appelée par `options.Value` ligne 68 | F11 |
| 5b | P2FixAnAppDotNetCode | Startup | lambda des lignes 70 à 75 : route par défaut `{controller=Product}/{action=Index}/{id?}` | F11 |

Ensuite `Run()` démarre le serveur et le navigateur demande la page `/`. Avec la route par défaut, c'est `ProductController.Index()` qui est appelé.

### Étape 2 : création du contrôleur et récupération des produits

ASP.NET crée `ProductController` par injection de dépendances. Il crée d'abord les objets demandés par son constructeur :

| # | Namespace | Classe | Méthode | Mode |
|---|---|---|---|---|
| 6 | P2FixAnAppDotNetCode.Models.Repositories | ProductRepository | `ProductRepository()` constructeur puis `GenerateProductData()` | F11 |
| 7 | P2FixAnAppDotNetCode.Models | Product | `Product(int, int, double, string, string)` appelé 9 fois | Maj+F11 après le 1er |
| 8 | P2FixAnAppDotNetCode.Models.Repositories | OrderRepository | `OrderRepository()` constructeur | F11 |
| 9 | P2FixAnAppDotNetCode.Models.Services | ProductService | `ProductService(IProductRepository, IOrderRepository)` constructeur | F11 |
| 10 | P2FixAnAppDotNetCode.Models.Services | LanguageService | constructeur par défaut (pas de code, le débogueur ne s'arrête pas) | - |
| 11 | P2FixAnAppDotNetCode.Controllers | ProductController | `ProductController(IProductService, ILanguageService)` constructeur, **point b** | F5 |
| 12 | P2FixAnAppDotNetCode.Controllers | ProductController | `Index()` | F10 |
| 13 | P2FixAnAppDotNetCode.Models.Services | ProductService | `GetAllProducts()` | F11 |
| 14 | P2FixAnAppDotNetCode.Models.Repositories | ProductRepository | `GetAllProducts()` : garde les produits avec Stock > 0 et les trie par nom | F11 puis Maj+F11 |

Remarque : avec F5, le débogueur va directement du point e au point b et ne s'arrête pas dans les étapes 6 à 9. Pour les voir, j'ai ajouté un point d'arrêt temporaire dans `ProductRepository.cs` ligne 15.

### Étape 3 : affichage (vues Razor)

`Index()` fait `return View(products);` et MVC exécute les vues :

| # | Fichier / Classe | Ce qui se passe | Mode |
|---|---|---|---|
| 15 | Views/_ViewStart.cshtml | définit le layout `_Layout` | F10 |
| 16 | Views/Product/Index.cshtml | boucle `foreach` (ligne 27) sur les produits : Name, Description, Price, Stock | F10 |
| 17 | Views/Shared/_Layout.cshtml | appelle `Component.InvokeAsync("CartSummary")` (ligne 22) | F10 |
| 18 | P2FixAnAppDotNetCode.Models.Cart | le panier est créé (singleton, une seule fois) | - |
| 19 | P2FixAnAppDotNetCode.Components.CartSummaryViewComponent | constructeur **point a** puis `Invoke()` | F5 puis F11 |
| 20 | Views/Shared/Components/CartSummary/Default.cshtml | affiche le résumé du panier (`Cart.Lines`) | F10 |
| 21 | P2FixAnAppDotNetCode.Components.LanguageSelectorViewComponent | `Invoke(ILanguageService)` appelé par le layout (ligne 33) | F11 |
| 22 | Views/Shared/Components/LanguageSelector/Default.cshtml | affiche la liste des langues | F10 |

Après ça la page HTML est envoyée au navigateur et la liste des produits s'affiche.

**Résumé des namespaces visités :** `P2FixAnAppDotNetCode`, `P2FixAnAppDotNetCode.Models.Repositories`, `P2FixAnAppDotNetCode.Models`, `P2FixAnAppDotNetCode.Models.Services`, `P2FixAnAppDotNetCode.Controllers`, `P2FixAnAppDotNetCode.Components`.

## 9. Déploiement en exécutable Windows

J'ai publié l'application en mode **autonome** (self-contained) pour Windows 64 bits. Comme ça l'exécutable marche même sur un PC qui n'a pas .NET installé.

En ligne de commande, à la racine du dépôt :

```
dotnet publish P2FixAnAppDotNetCode/Diayma.csproj -c Release -r win-x64 --self-contained true -o ../publish/Diayma
```

Ou dans Visual Studio : clic droit sur le projet **Diayma** > **Publier** > profil **FolderProfile** (j'ai changé le dossier de sortie qui était `F:\Diayma` en `bin\Release\netcoreapp2.0\win-x64\publish\`).

Pour lancer : double-cliquer sur **Diayma.exe** dans le dossier publié. Une console s'ouvre avec `Now listening on: http://localhost:5000`, il faut ouvrir cette adresse dans le navigateur.

## 10. Lien vers l'exécutable

Lien Google Drive (dossier compressé en .zip) : **(lien à ajouter)**

Décompresser le zip, puis lancer `Diayma.exe` et ouvrir http://localhost:5000

## 11. Optionnel

### a) Langue wolof

J'ai ajouté le wolof comme langue d'affichage :

- `Startup.cs` : ajout de `new CultureInfo("wo")` dans la liste des cultures
- `LanguageService.cs` : ajout de `case "Wolof": culture = "wo";`
- `LanguageSelector/Default.cshtml` : ajout de l'option Wolof dans la liste
- Ajout des fichiers de traduction `.wo.resx` dans le dossier `Resources` (copiés depuis les fichiers `.fr.resx` puis traduits)

Pour garder les options de culture du français, quand on choisit le wolof le cookie est créé avec `new RequestCulture("fr", "wo")` : les textes sont en wolof mais les nombres, les prix et les dates restent au format français.

Exemples : "Liste des produits" → "Limu marsandiis yi", "Ajouter au panier" → "Yokk ci pañe bi", "Votre panier" → "Sa pañe", "Merci !" → "Jërëjëf !"

### b) Commits

- Correction du total du panier et de la mise à jour du stock
- Ajout de la langue wolof
- Réponses du TD dans le README + profil de publication

### c) Lien du dépôt

Lien déposé sur Google Classroom : https://github.com/DA19s/BoutiqueDiayma2025
