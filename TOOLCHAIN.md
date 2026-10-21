# TOOLCHAIN — état des lieux avant regroupement

Constats issus de la lecture de `../scrubx` et `../paige` (README, SPECIFICATION/SPECS, AGENTS, `.csproj`, `.slnx`, workflows). Le code source n'a pas été lu en détail. Les documents d'origine font foi.

## Chaîne de production visée

```
document.docx ──► scrubx (validation typographique) ──► johannes (conversion) ──► .typ  ──► typst (PDF)
                                                                              └─► .paige ──► paige (EPUB 3)
```

| Outil | Rôle | Entrée | Sortie |
| ----- | ---- | ------ | ------ |
| `scrubx` | Détecte (et propose en révisions suivies) les défauts typographiques | `.docx` | rapport console, `*-relu.docx` |
| `johannes` | Convertit le Word vers Typst et Paige | `.docx` | `.typ`, `.paige` |
| `paige` | Compile le DSL en livre numérique | `.paige` | `.epub` |

Les trois outils : .NET 10, MIT, Conventional Commits en français, `main` protégée, `AGENTS.md` + `CLAUDE.md`, badge repocheck, Dependabot.

## johannes (ce dépôt)

- Projets : `Johannes` (exe), `Johannes.Tests` ; `johannes.sln`.
- Paquets : `DocumentFormat.OpenXml` 3.5.1, `System.CommandLine` 2.0.12.
- Classes : `DocumentParser`, `IExporter`, `TypstExporter`, `PaigeExporter`.
- Options : `--docx/-d`, `--typst`, `--paige`, `--without-typst`, `--without-paige`.
- Distribution : `nfpm.yaml` (.deb/.rpm), workflows `dotnet.yml` et `release.yml`.
- Écrit `support-functions.typ` et `.paige` à côté du document.

## scrubx (`../scrubx`)

- Rôle : valider la typographie d'un `.docx` (`SPECIFICATION.md`).
- Solution `Scrubx.slnx`, cinq projets :
  - `Scrubx.Core` : `DocxValidator` (738 lignes), `RuleCatalog`, `ParagraphTextMap`, `DocxRevisionWriter`, `DocxComments`, `DocxPackage`. Pas de dépendance hors BCL : le `.docx` est lu comme un ZIP OOXML via `System.Xml.Linq`, **sans** `DocumentFormat.OpenXml`.
  - `Scrubx.Cli` : `Program`, `ArgumentParser` (maison, pas `System.CommandLine`), `RuleConfig`.
  - `Scrubx.Web` : ASP.NET Core minimal API et front statique (`wwwroot/`, HTML/CSS/JS sans build). Prévu derrière nginx (`deploy/`, unité systemd).
  - `Scrubx.Desktop` : WPF + WebView2, `net10.0-windows`, héberge `Scrubx.Web` en processus.
  - `Scrubx.Tests` : xUnit, `.docx` construits en mémoire ; référence `Scrubx.Cli` et `Scrubx.Web` (`Microsoft.AspNetCore.TestHost`).
- 14 règles à code court (`APOS`, `GDROIT`, `TIRET`, `EIMANQ`, `EIPONC`, `EGUIL`, `ESPAPRES`, `EFINPAR`, `DESPACE`, `STYLEINV`, `TITRE1`, `SAUTPAGE`, `VIRGET`, plus erreurs de lecture non désactivables).
- Config : `scrubx.json` (code → booléen) dans le répertoire courant ; `-i/--ignore`, `-f/--force`.
- Codes de sortie : 0 ok, 1 arguments/config, 2 introuvable, 3 extension, 4 erreurs de validation, 5 échec `--revisions`.
- Distribution : `publish.sh`/`publish.ps1` (Cli seul), `release.yml` (Cli `osx-arm64`/`win-x64`/`linux-x64`, Web `linux-x64`, Desktop `win-x64`), `tests.yml`.
- Styles Word autorisés par la règle `STYLEINV` : `Normal`, `Titre1`, `Ellipse`. Ce sont aussi ceux que johannes traite nativement.
- Portage macOS (`Scrubx.Mac`) envisagé, non démarré (`SPECIFICATION.md` §8).
- Dans le dossier : `Anya.docx`, `Scrubx.Cli.exe`, `Scrubx.Cli-win-x64.exe` (binaires non suivis par git pour le second ; `.claude/` non suivi).

## paige (`../paige`)

- Rôle : compiler des `.paige` en EPUB 3.0.1 (`SPECS.md`).
- Solution `Paige.slnx` : `Paige` (exe, `AssemblyName` `paige`) et `Paige.Tests`.
- Pipeline : `Lexer` → `Parser` → EST (`EpubDocument`, `EpubMetadata`, `ManifestItem`) → `Epub.Write()`. Environ 540 lignes de code source pour 700+ lignes de tests (62 tests annoncés : 19 + 25 + 18).
- Directives : `#metadata(identifier, title, language)`, `#manifest.add(...)[contenu]`, `#include "chemin"`.
- Génère `mimetype`, `container.xml`, `content.opf`, `nav.xhtml`, `cover.xhtml`.
- CLI : `--project-root` (défaut `.`), `--output` (défaut `mybook.epub`), `--help`.
- Paquet : `System.CommandLine` 3.0.0-rc.1 (**préversion**, alors que johannes utilise 2.0.12).
- Distribution : Homebrew (`ymauray/tap`), Scoop, `.deb`/`.rpm` via `nfpm.yaml`.
- Méthodologie : TDD strict.

## Points de contact existants

- johannes **produit** du `.paige` ; paige le **consomme**. Le contrat est le DSL (`#metadata`, `#manifest.add`, `#include`) ; il n'y a aucune dépendance de code entre les deux.
- scrubx et johannes lisent chacun le `.docx` à leur manière (JSON/ZIP brut vs `DocumentFormat.OpenXml`).
- Les règles typographiques françaises (espaces insécables avant `? ! : ;`) existent des deux côtés : scrubx les **vérifie**, johannes les **applique** à l'export.

## Écarts à arbitrer pour le regroupement

1. **Lecture du `.docx`** : `Scrubx.Core` (ZIP + XML brut, nécessaire à l'écriture de révisions fidèles à l'octet) contre `DocumentFormat.OpenXml` côté johannes.
2. **`System.CommandLine`** : 2.0.12 (johannes) contre 3.0.0-rc.1 (paige) contre analyseur maison (scrubx).
3. **Forme de la CLI** : outils séparés (`scrubx`, `johannes`, `paige`) ou un seul exécutable à sous-commandes. Aujourd'hui johannes n'a pas de sous-commande (`--docx` obligatoire).
4. **Périmètre de scrubx** : `Scrubx.Web` et `Scrubx.Desktop` (WPF/WebView2, Windows seulement) suivent-ils, ou seule la partie Core/Cli est reprise ? `Desktop` impose un build `windows-latest` et un TFM différent.
5. **Historique git** : conserver l'historique des deux dépôts (fusion de sous-arbres) ou repartir du code seul.
6. **Packaging et releases** : trois `release.yml` et trois `nfpm.yaml`/scripts de publication à unifier ; paige est aussi distribué via Homebrew et Scoop.
7. **Conventions divergentes** : `AGENTS.md` de johannes interdit les commits spontanés ; la règle du co-auteur n'est pas confirmée dans ceux de scrubx/paige (non lus en entier). SPECS de paige indique `identifier: <int>` alors que le README donne une chaîne ISBN.
8. **Noms** : `Johannes.Tests` contre `Scrubx.Tests`/`Paige.Tests`, avec un `UnitTest1.cs` résiduel dans scrubx.
9. **Dépôts d'origine** : sort de `../scrubx` et `../paige` (archivage, redirection, badges) après regroupement.

## Non lu

Code source des trois projets, workflows, `deploy/`, `CONTRIBUTING.md`, `SECURITY.md`, `SPECIFICATION.md` de scrubx au-delà du §6, `AGENTS.md` de paige.
