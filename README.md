# Johannes

Johannes est un outil en ligne de commande (CLI) développé en .NET 10 permettant de convertir des fichiers Microsoft Word (`.docx`) vers les formats [Typst](https://typst.app/) et [Paige](https://github.com/ymauray/paige).

Le nom du projet est un hommage à Johannes Gutenberg, l'inventeur de l'imprimerie à caractères mobiles.

## Fonctionnalités

- **Conversion multi-format** : Scanne le répertoire courant pour trouver des fichiers `.docx` et les convertir simultanément en `.typ` (Typst) et `.paige` (Paige).
- **Support des styles Word** :
  - `Titre 1` -> Titre de niveau 1 Typst (`= ...`) ou nouveau chapitre Paige.
  - `Titre` -> Appel à une fonction personnalisée `#titre()` (Typst) ou titre centré (Paige).
  - `Normal` -> Texte brut.
  - `Ellipse` -> Appel à une fonction `#ellipsis()` (Typst) ou séparateur visuel (Paige).
- **Formatage** : Support de l'italique.
- **Typographie française** : Gestion automatique des espaces insécables avant la ponctuation double (`?`, `!`, `:`, `;`).
- **Nettoyage** : Conversion des tirets cadratins et espaces insécables Word vers la syntaxe appropriée pour chaque format.

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Typst](https://github.com/typst/typst) (pour compiler les fichiers `.typ` générés)
- [Paige](https://github.com/ymauray/paige) (pour générer des fichiers EPUB à partir des fichiers `.paige`)

## Installation

Clonez le dépôt et compilez le projet :

```bash
git clone https://github.com/ymauray/johannes.git
cd johannes
dotnet build
```

## Utilisation

### Convertir tous les fichiers du dossier courant

Lancez simplement l'exécutable sans arguments :

```bash
dotnet run
```

Par défaut, Johannes générera des fichiers `.typ` et `.paige` pour chaque document Word trouvé.

### Modes d'exportation exclusifs

Si vous souhaitez forcer un format unique pour tous les fichiers du répertoire courant sans aucune autre configuration :

```bash
dotnet run -- --typst
# ou
dotnet run -- --paige
```

*Note : Ces options sont mutuellement exclusives. Elles autorisent l'utilisation de `--docx`, mais interdisent les options `--without-*`.*

### Désactiver un format d'exportation

Par défaut, Johannes génère les deux formats. Vous pouvez en désactiver un avec :

```bash
dotnet run -- --without-typst
# ou
dotnet run -- --without-paige
```

*Note : Contrairement aux modes exclusifs, ces options peuvent être combinées avec `--docx`.*

### Convertir un fichier spécifique

Utilisez l'option `--docx` ou `-d` :

```bash
dotnet run -- --docx "MonDocument.docx"
```

## Tests

Pour lancer les tests unitaires :

```bash
dotnet test
```

Le projet utilise **xUnit** pour valider la logique de transformation du texte (italique, ponctuation française, tirets cadratins, etc.). Une intégration continue (CI) est configurée via GitHub Actions.

## Structure de sortie

### Typst (.typ)

Le programme ajoute automatiquement un import au début du fichier :

```typst
#import "/support-functions.typ" : *
```

Assurez-vous d'avoir un fichier `support-functions.typ` disponible dans votre projet Typst pour définir les fonctions `#titre()` et `#ellipsis()`.

### Paige (.paige)

Le format Paige est un DSL (Domain Specific Language) qui permet de générer des fichiers EPUB. Johannes génère automatiquement le manifeste et la structure des chapitres à partir des styles Word.

## Architecture

Le projet est conçu de manière modulaire :
- `DocumentParser` : Analyse la structure OpenXML du fichier Word. En cas d'erreur de conversion ou d'élément non supporté (par exemple une propriété de run ou un élément inline inconnu), il génère une exception détaillée incluant le style et le texte brut du paragraphe concerné pour faciliter le diagnostic.
- `IExporter` : Interface pour définir différents formats de sortie.
- `TypstExporter` : Implémentation concrète pour le format Typst.
- `PaigeExporter` : Implémentation concrète pour le format Paige.

## Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.
