# Johannes

[![Build .NET](https://github.com/ymauray/johannes/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ymauray/johannes/actions/workflows/dotnet.yml)
[![Licence MIT](https://img.shields.io/badge/Licence-MIT-yellow.svg)](LICENSE)

Johannes est un outil en ligne de commande (CLI) développé en .NET 10 permettant de convertir des fichiers Microsoft Word (`.docx`) vers les formats [Typst](https://typst.app/) et [Paige](https://github.com/ymauray/paige).

Le nom du projet est un hommage à Johannes Gutenberg, l'inventeur de l'imprimerie à caractères mobiles.

## Fonctionnalités

- **Conversion multi-format** : Convertit un fichier `.docx` vers les formats `.typ` (Typst) et `.paige` (Paige).
- **Support des styles Word** :
  - `Titre 1` -> Titre de niveau 1 Typst (`= ...`) ou nouveau chapitre Paige.
  - `Titre` -> Appel à la fonction `#titre()` (Typst) ou paragraphe de classe `style_Titre` (Paige).
  - `Normal` -> Texte brut.
  - `Ellipse` -> Appel à une fonction `#ellipsis()` (Typst) ou séparateur visuel (Paige).
  - Autres styles -> Appel à une fonction `#style_<StyleId>()` (Typst) ou paragraphe de classe `style_<StyleId>` (Paige).
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

### Modes d'exportation exclusifs

Si vous souhaitez forcer un format unique :

```bash
dotnet run -- --docx "MonDocument.docx" --typst
# ou
dotnet run -- --docx "MonDocument.docx" --paige
```

*Note : Ces options sont mutuellement exclusives et interdisent les options `--without-*`.*

### Désactiver un format d'exportation

Par défaut, Johannes génère les deux formats. Vous pouvez en désactiver un avec :

```bash
dotnet run -- --docx "MonDocument.docx" --without-typst
# ou
dotnet run -- --docx "MonDocument.docx" --without-paige
```

### Convertir un fichier Word

L'option `--docx` ou `-d` est obligatoire :

```bash
dotnet run -- --docx "MonDocument.docx"
# ou
dotnet run -- -d "MonDocument.docx"
```

## Tests

Pour lancer les tests unitaires :

```bash
dotnet test
```

Le projet utilise **xUnit** pour valider la logique de transformation du texte (italique, ponctuation française, tirets cadratins, etc.). Une intégration continue (CI) est configurée via GitHub Actions.

## Contribution

La branche `main` est protégée. Créez une branche dédiée pour chaque modification et intégrez-la via une pull request. Les workflows de build et de release compilent et exécutent les tests avant de publier les artefacts.

## Structure de sortie

### Typst (.typ)

Le programme ajoute automatiquement un import au début du fichier :

```typst
#import "/support-functions.typ" : *
```

Le fichier `support-functions.typ` est créé automatiquement s'il est absent. Johannes y ajoute les implémentations par défaut de `#ellipsis()` et `#titre()`, ainsi que celles des fonctions requises par les styles Word personnalisés. Chaque fonction ajoutée est signalée par un commentaire et les définitions existantes sont préservées afin de pouvoir les personnaliser.

### Paige (.paige)

Le format Paige est un DSL (Domain Specific Language) qui permet de générer des fichiers EPUB. Johannes génère automatiquement le manifeste et la structure des chapitres à partir des styles Word. Les styles Word personnalisés sont exportés comme classes CSS `style_<StyleId>` ; ils restent sans effet tant qu'aucune règle CSS correspondante n'est définie.

## Architecture

Le projet est conçu de manière modulaire :
- `DocumentParser` : Analyse la structure OpenXML du fichier Word. En cas d'erreur de conversion ou d'élément non supporté (par exemple une propriété de run ou un élément inline inconnu), il génère une exception détaillée incluant le style et le texte brut du paragraphe concerné pour faciliter le diagnostic.
- `IExporter` : Interface pour définir différents formats de sortie.
- `TypstExporter` : Implémentation concrète pour le format Typst.
- `PaigeExporter` : Implémentation concrète pour le format Paige.

## Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.
