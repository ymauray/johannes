# Johannes

[![Build .NET](https://github.com/ymauray/johannes/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ymauray/johannes/actions/workflows/dotnet.yml)
[![Licence MIT](https://img.shields.io/badge/Licence-MIT-yellow.svg)](LICENSE)
[![repocheck](https://img.shields.io/badge/repocheck%201.3.0-100%2F100-brightgreen)](https://github.com/ymauray/repocheck)

Johannes est un outil en ligne de commande (CLI) développé en .NET 10 qui fabrique un livre à partir d'un seul fichier Microsoft Word (`.docx`) : vérification typographique, versions [Typst](https://typst.app/) et [Paige](https://github.com/ymauray/paige), PDF et EPUB.

Le nom du projet est un hommage à Johannes Gutenberg, l'inventeur de l'imprimerie à caractères mobiles.

## La chaîne

```bash
johannes --docx MonLivre.docx
```

| Étape | Nom | Ce qui se passe |
| ----- | --- | --------------- |
| 1 | `check` | Vérification typographique du `.docx` (apostrophes, guillemets, espaces insécables, styles, sauts de page…). Bloquante s'il y a des erreurs. |
| 2 | `generate` | Écriture de `MonLivre.typ`, `MonLivre.paige` et `support-functions.typ` à côté du `.docx`. |
| 3 | `pdf` | Un PDF par fichier `Papier/*.typ`, avec `typst compile`, en parallèle. |
| 4 | `epub` | L'EPUB `epub/MonLivre.epub`, compilé depuis l'unique `.paige` du dossier `epub/`, avec le moteur Paige intégré. |

Les `.typ` et `.paige` générés sont des artefacts : ils sont réécrits à chaque exécution. Chaque étape ne refait que ce dont les entrées ont changé (cache dans `.build/`, à côté du `.docx`).

Le guide complet (organisation d'un livre, options, codes de sortie, cache) est dans [USER_GUIDE.md](USER_GUIDE.md).

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download) pour compiler, ou un binaire autonome des [releases](https://github.com/ymauray/johannes/releases).
- [Typst](https://github.com/typst/typst) dans le `PATH`, pour l'étape `pdf` uniquement.

Il n'y a plus rien d'autre à installer : la vérification (ancien `scrubx`) et la compilation EPUB (ancien `paige`) sont intégrées.

## Installation

```bash
git clone https://github.com/ymauray/johannes.git
cd johannes
dotnet build
```

## Utilisation

```bash
# Tout : vérification, génération, PDF, EPUB (ne refait que ce qui est périmé)
johannes --docx MonLivre.docx

# Vérifier seulement
johannes --docx MonLivre.docx --only check

# Écrire une copie annotée (révisions suivies Word) des corrections proposées
johannes --docx MonLivre.docx --only check --revisions

# Générer les .typ et .paige sans compiler
johannes --docx MonLivre.docx --until generate

# Reconstruire uniquement l'EPUB
johannes --docx MonLivre.docx --only epub

# Voir le plan sans rien exécuter, ou tout refaire en ignorant le cache
johannes --docx MonLivre.docx --dry-run
johannes --docx MonLivre.docx --rebuild
```

Avec `dotnet run`, ajoutez `--` avant les options : `dotnet run --project Johannes -- --docx MonLivre.docx`.

| Option | Effet |
| ------ | ----- |
| `--docx`, `-d` | Document source (obligatoire). Le dossier de travail est celui du document. |
| `--only <étapes>`, `--until <étape>`, `--skip <étapes>` | Choisissent les étapes (`check`, `generate`, `pdf`, `epub`). |
| `--rebuild` | Ignore le cache. |
| `--revisions[=<fichier>]`, `--author <nom>` | Écrit la copie annotée (`<nom>-relu.docx` par défaut). |
| `--dry-run`, `--verbose`, `-v` | Plan sans exécution ; détail des anomalies et des commandes. |
| `--typst`, `--paige`, `--without-typst`, `--without-paige` | Restreignent la chaîne à une branche. |

## Formats de sortie

### Typst (`.typ`)

Le fichier commence par `#import "/support-functions.typ" : *`. `support-functions.typ` est créé s'il est absent : Johannes y ajoute les implémentations par défaut de `#ellipsis()` et `#titre()`, ainsi que celles des fonctions requises par les styles Word personnalisés (`#style_<StyleId>()`) et les polices (`#police_<NomDeLaPolice>()`). Les définitions existantes sont préservées pour pouvoir les personnaliser.

### Paige (`.paige`)

Le format Paige est un DSL qui décrit un EPUB. Johannes génère le manifeste et la structure des chapitres à partir des styles Word ; les styles personnalisés deviennent des classes CSS `style_<StyleId>` et les polices des balises `<span class="police_<NomDeLaPolice>">`.

### Conversion

- `Titre1` devient un titre de niveau 1 (Typst) ou un nouveau chapitre (Paige) ; `Titre`, `Ellipse` et les autres styles appellent une fonction `#style_…()` ou une classe CSS.
- Italique, gras et changements de police sont conservés, les segments consécutifs de même formatage sont fusionnés.
- Les espaces insécables du document sont conservées (`~` en Typst, `&#160;` en Paige) ; l'étape `check` garantit leur présence avant la ponctuation double. Les tirets cadratins sont convertis pour chaque format.

## Architecture

Cinq projets dans `johannes.sln` :

- `Johannes.Core` : code commun (typographie, lecture du Word, plan des étapes, contrôles préalables, cache).
- `Johannes.Scrubx` : validateur typographique et écriture des révisions (ancien projet `scrubx`).
- `Johannes.Paige` : lexeur, analyseur et écriture EPUB (ancien projet `paige`).
- `Johannes` : l'exécutable, les étapes `check`, `generate`, `pdf`, `epub`, `DocumentParser` et les exporteurs `TypstExporter` / `PaigeExporter`.
- `Johannes.Tests` : tests xUnit de l'ensemble.

## Tests

```bash
dotnet test
```

Le projet utilise **xUnit**. Une intégration continue est configurée via GitHub Actions.

## Contribution

La branche `main` est protégée. Créez une branche dédiée pour chaque modification et intégrez-la via une pull request. Les workflows de build et de release compilent et exécutent les tests avant de publier les binaires (`win-x64`, `linux-x64`, `osx-arm64`). Voir [CONTRIBUTING.md](CONTRIBUTING.md).

## Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](LICENSE) pour plus de détails.
