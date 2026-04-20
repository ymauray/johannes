# Johannes

Johannes est un outil en ligne de commande (CLI) développé en .NET 10 permettant de convertir des fichiers Microsoft Word (`.docx`) vers le format [Typst](https://typst.app/).

Le nom du projet est un hommage à Johannes Gutenberg, l'inventeur de l'imprimerie à caractères mobiles.

## Fonctionnalités

- **Conversion automatique** : Scanne le répertoire courant pour trouver des fichiers `.docx` et les convertir.
- **Support des styles Word** :
  - `Titre 1` -> Titre de niveau 1 Typst (`= ...`)
  - `Titre` -> Appel à une fonction personnalisée `#titre()`
  - `Normal` -> Texte brut
  - `Ellipse` -> Appel à une fonction `#ellipsis()`
- **Formatage** : Support de l'italique.
- **Typographie française** : Gestion automatique des espaces insécables avant la ponctuation double (`?`, `!`, `:`, `;`).
- **Nettoyage** : Conversion des tirets cadratins et espaces insécables Word vers la syntaxe Typst.

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Typst](https://github.com/typst/typst) (pour compiler les fichiers `.typ` générés)

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

### Convertir un fichier spécifique

Utilisez l'option `--docx` ou `-d` :

```bash
dotnet run -- --docx "MonDocument.docx"
```

## Structure de sortie

Le programme génère un fichier `.typ` pour chaque document traité. Il ajoute automatiquement un import au début du fichier :

```typst
#import "/support-functions.typ" : *
```

Assurez-vous d'avoir un fichier `support-functions.typ` disponible dans votre projet Typst pour définir les fonctions `#titre()` et `#ellipsis()`.

## Architecture

Le projet est conçu de manière modulaire :
- `DocumentParser` : Analyse la structure OpenXML du fichier Word.
- `IExporter` : Interface pour définir différents formats de sortie.
- `TypstExporter` : Implémentation concrète pour le format Typst.

## Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.
