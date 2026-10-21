# ROADMAP — Johannes v2

Suivi du développement d'une session à l'autre. Branche de travail : `version2`. Documents associés : `TOOLCHAIN.md` (état des lieux), `USER_GUIDE.md` (usage visé).

**Reprise de session** : lire la section « Reprise à froid » ci-dessous, puis repérer la première case non cochée et travailler dans l'ordre. Cocher une case seulement quand le code est écrit, `dotnet build` passe et `dotnet test` est vert. Règles de `AGENTS.md` inchangées : pas de commit, de push ni de PR sans demande explicite.

## Reprise à froid — à lire en premier

**État au 2026-10-21** : phases 0 à 9 faites, phase 10 en grande partie (y compris le niveau 2 de la phase 5). `johannes --docx X.docx` fait tout : vérification, génération, PDF, EPUB, avec cache incrémental. Reste la fin de la **phase 10** (voir ci-dessous : tout ce qui reste dépend de l'utilisateur). Branche `version2`, tout est committé (voir `git log`). L'arbre de travail doit être propre ; sinon, ne committer que sur instruction de l'utilisateur (`AGENTS.md` §2).

**Vérifier l'état** : `git status`, `dotnet build` (0 avertissement attendu), `dotnet test` (383 tests attendus).

**Carte du code**

| Projet | Contenu |
| ------ | ------- |
| `Johannes.Core` | `Typography`, `WordXml`, `Stages`, `ExitCodes`, `BookLayout`, `ExecutableLocator`, `Preflight`, `StagePlan`/`StagePlanner`, `Pipeline` (avec `IStage`, `PipelineContext`, `StageOutcome`, `RevisionRequest`), `Fingerprint`, `BuildCache`, `Settings`/`ConfigKeys`/`Configuration`/`ConfigFile` |
| `Johannes.Scrubx` | validateur typographique repris de scrubx (`DocxValidator`, `RuleCatalog`, `DocxRevisionWriter`, `DocxComments`, `DocxPackage`, `ParagraphTextMap`) |
| `Johannes.Paige` | moteur EPUB repris de paige (`Lexer`, `Parser`, `Est`, `Epub`, `Token`), sans CLI |
| `Johannes` | exécutable : `Program.cs` (options, assemblage), `CheckStage`, `PipelineStages.cs` (`GenerateStage`), `PdfStage`, `EpubStage`, `ProcessRunner` (`IProcessRunner`, simulé dans les tests), `ConfigCommand`, `DocumentParser`, `TypstExporter`, `PaigeExporter` |
| `Johannes.Tests` | un seul projet de tests ; `Scrubx/` et `Paige/` importés ; `Fixtures/*.paige` |

**Architecture en deux mots**
- `Program.cs` analyse les options, fabrique un `StagePlan`, lance `Preflight`, puis un `Pipeline` de quatre `IStage` (ordre imposé par `Stages`).
- Chaque étape décide elle-même de son travail : `OmissionReason` (sans objet pour ce livre), `UpToDateReason` (entrées inchangées d'après `BuildCache`), `Run`. Le cache est dans `.build/state.json` à côté du `.docx` ; l'invalidation par version utilise l'identifiant de module des quatre assemblies.
- Codes de sortie : `ExitCodes` (0 à 9), détaillés dans `USER_GUIDE.md` §10.

**Phase 10, points d'entrée**
- Fait : `README.md`, `USER_GUIDE.md` (statut), `nfpm.yaml`, essai de publication autonome ; workflows et `CONTRIBUTING.md` n'ont rien exigé.
- Reste à l'utilisateur ou sur son ordre : `AGENTS.md` §6 à retirer à la fusion, `Makefile` d'*Anya* (autre dépôt), sort de `../scrubx` et `../paige` (archivage, paquets Homebrew et Scoop), fusion dans `main`.

**Environnement de test**
- Livre réel : `C:\Users\MaurayY\perso\Anya` (`Anya.docx`, `Papier/`, `epub/`, `Makefile`). L'utilisateur autorise à s'en servir ; **travailler plutôt sur une copie** dans le dossier temporaire de session, car la génération y réécrit `Anya.typ`, `Anya.paige` et `support-functions.typ`. L'`Anya.docx` de ce dossier a été corrigé et passe `check` (seuls 615 avertissements `VIRGET`).
- Référence de non-régression : les `Anya.typ`/`Anya.paige` du dossier `Anya` sont ceux de la **v1**. La sortie actuelle n'en diffère que par les 809 espaces suivant `?` ou `!` (insécables en v1, ordinaires maintenant, décision actée). Pour comparer : remplacer `?` ou `!` suivi d'une espace par `~` (Typst) ou `&#160;` (Paige) dans la sortie neuve, la différence doit être nulle.
- Les PDF produits ont été relus à l'œil par l'utilisateur (pages toutes présentes) ; l'EPUB est identique à celui du `paige` installé, à l'horodatage près.
- `typst` est installé (`C:\Users\MaurayY\.local\bin`). `scrubx` et `paige` existent en exécutables dans le `PATH` de l'utilisateur (le `scrubx` installé est plus ancien que `../scrubx` : il ignore la règle `ESPAPRES`) ; les dépôts `../scrubx` et `../paige` servent de référence en lecture seule.
- Essai rapide d'un morceau de Core sans exécutable : fichier C# unique `dotnet run essai.cs` avec `#:project <chemin>` en tête.

**Pièges rencontrés**
- `.gitignore` contient `*.paige` : il ignore aussi un dossier nommé `Johannes.Paige` (Windows ne distingue pas la casse). Exceptions en fin de fichier ; vérifier avec `git ls-files` après tout nouveau projet ou dossier.
- Le fichier `.editorconfig` impose tabulations et CRLF pour le C# : après création de fichiers, lancer `dotnet format whitespace johannes.sln --include <fichiers ou dossiers>`. Ne pas le lancer sur toute la solution sans `--include` (il reformate des fichiers existants). Git signale « LF will be replaced by CRLF » : sans gravité (l'index est normalisé en LF).
- Outils : pas de Python dans le shell. Un `cat <<'EOF'` ou un `sed` contenant des apostrophes françaises ou des barres obliques inverses est déformé (par exemple `'\\'` devient `'\'`, `\u00A0` devient `00A0`) : utiliser l'outil d'écriture de fichiers pour tout fichier C# ou Markdown non trivial, puis `head`/`tail` pour assembler si besoin. L'outil d'édition interprète `\u00A0` en vrai caractère dans le texte Markdown : écrire « U+00A0 ».
- La console Windows via git-bash affiche mal les accents sans `Console.OutputEncoding = UTF8` (déjà fait dans `Program.cs`).
- Règle de commit : ne jamais committer sans demande ; messages Conventional Commits en français, avec la ligne `Co-Authored-By` demandée par la session.

## Décisions actées

| # | Sujet | Décision |
| - | ----- | -------- |
| D1 | `johannes --docx X.docx` | Chaîne complète : `check`, `generate`, `pdf`, `epub`. |
| D2 | Étape `check` | Bloquante uniquement si elle se termine en erreur (code 4). Les avertissements ne bloquent pas. |
| D3 | Configuration | Convention seule d'abord. Plus tard, sous-commande façon git : `johannes config <clé> <valeur>` (ex. `paper-directory`). Pas de `johannes.json` écrit à la main dans un premier temps. |
| D4 | Fichiers de sortie | Dans le même dossier que le `.docx`. |
| D5 | `epub/` | Vérifier **au démarrage** qu'il ne contient qu'un seul fichier `.paige`. |
| D6 | `typst` | Vérifier **au démarrage** sa présence dans le `PATH`. |
| D7 | Ordre | Intégrer d'abord `Scrubx.Core` et `Paige`, puis l'orchestrateur, puis le cache. |
| D8 | Historique git de `scrubx` et `paige` | Non conservé : copie du code seul. |
| D9 | `Scrubx.Web`, `Scrubx.Desktop` | Abandonnés (ni repris ni portés). |
| D10 | `System.CommandLine` | Pas de préférence de l'utilisateur ; choix retenu : version stable la plus récente (série 2.0.x, celle de johannes), jamais la préversion 3.0.0-rc de paige. À confirmer à l'implémentation. |
| D11 | Code commun | Mutualiser le maximum entre les modules (voir phase 1). |
| D12 | Lecture du `.docx` | Une seule couche : ZIP brut + `XDocument` (`DocxPackage` de scrubx). `DocumentFormat.OpenXml` est retiré une fois `DocumentParser` porté et la sortie vérifiée identique sur *Anya*. |
| D13 | Règles typographiques | Niveau 1 : vocabulaire commun `Typography` dans `Johannes.Core`. Niveau 2 (phase 5, sous conditions) : exporteurs réduits à la conversion `\u00A0` vers `~` / `&#160;`. |
| D14 | Règles de vérification | Non désactivables : suppression de `-i/--ignore`, `-f/--force` et de `scrubx.json`. Jeu de règles fixe ; seul `VIRGET` reste un avertissement non bloquant. |
| D15 | `check` et `generate` | `check` est un prérequis de `generate` (rejoué si pas déjà réussi pour l'empreinte du `.docx`) ; plus d'option `--no-check` ni `--skip check`. **Confirmé.** |
| D16 | Portées de la configuration | Du plus faible au plus fort : défauts, fichier de l'utilisateur, fichier `.johannes` du livre (dossier du `.docx`), options de la ligne de commande. Clés : `paper-directory`, `epub-directory`, `typst-path`. Décidé en session 2 (l'utilisateur avait laissé le choix). |

Conséquence de D3 : `johannes.json` et `--init` sont supprimés de `USER_GUIDE.md` ; la configuration est traitée en phase 9 (faite, voir D16).

## Questions encore ouvertes

- [ ] **Dossier de travail** : D4 place les sorties à côté du `.docx` ; les `#include` et `--root` de typst, supposent aujourd'hui le dossier courant. **Constaté en phase 1** : la v1 écrit `<nom>.typ`, `<nom>.paige` et `support-functions.typ` dans le dossier **courant** (les exporteurs reçoivent le nom sans répertoire). À corriger en phase 5.
- [ ] **Dépôts d'origine** (`../scrubx`, `../paige`) : archivage ou redirection après regroupement ; paquets Homebrew et Scoop de `paige`.

## Phase 0 — Cadrage

- [x] Lire `../scrubx` et `../paige` (`TOOLCHAIN.md`)
- [x] Créer la branche `version2`
- [x] Étudier le `Makefile` d'*Anya* et rédiger `USER_GUIDE.md`
- [x] Arbitrer les points du §12 du guide (décisions D1 à D7)
- [x] Reporter D1 à D7 dans `USER_GUIDE.md`
- [x] Trancher historique git (D8), périmètre Web/Desktop (D9), `System.CommandLine` (D10)

## Phase 1 — Intégrer `scrubx` et `paige`

Structure retenue : `Johannes.Core` (code commun), `Johannes.Scrubx`, `Johannes.Paige`, `Johannes` (exécutable) et un seul projet de tests `Johannes.Tests` (sous-dossiers `Scrubx/` et `Paige/`, `Fixtures/` pour les `.paige` d'exemple).

- [x] Choisir la structure de la solution
- [x] Créer `Johannes.Core/Typography` (D13, niveau 1) : constantes, espaces, ponctuations doubles
- [x] Remplacer dans `DocxValidator` les `\u00A0`/`\u202F` écrits en dur (46 occurrences → 1 regex littérale conservée)
- [x] Faire consommer `Typography` aux exporteurs : sans objet, les regex d'insertion ont disparu (phase 5)
- [x] Inventorier le code dupliqué : espace de noms `w:` déclaré 5 fois dans scrubx (maintenant `WordXml.W`) ; ouverture du `.docx` (maintenant `WordXml`) ; constantes typographiques (maintenant `Typography`) ; analyse d'arguments et JSON de scrubx (`ArgumentParser`, `RuleConfig`) non importés
- [x] Porter `DocumentParser` sur `XDocument` via `WordXml` (D12), comportement strict conservé (message : le nom de la propriété ou de l'élément est maintenant le nom XML, ex. `rStyle`, au lieu du nom de classe OpenXml)
- [x] Réécrire `DocumentParserTests` sans OpenXml (+ cas : désactivation `w:b`, style par défaut, élément étranger homonyme, éléments ignorés)
- [x] Critère de sortie : `.typ` et `.paige` de `Anya.docx` **et** de `test-document.docx` identiques à ceux de la v1 (comparaison octet à octet)
- [x] Retirer le paquet `DocumentFormat.OpenXml`
- [x] Importer `Scrubx.Core` (`DocxValidator`, `RuleCatalog`, `ParagraphTextMap`, `DocxRevisionWriter`, `DocxComments`, `DocxPackage`) et ses tests (`DocxValidatorTests`, `DocxRevisionWriterTests`, `DocxCommentsTests`, `ParagraphTextMapTests`, `RuleCatalogTests`) ; espace de noms `Johannes.Scrubx`
- [x] Importer le moteur de `Paige` (Lexer, Parser, Est, Epub, Token) en bibliothèque `Johannes.Paige` et ses 62 tests ; le `Program.cs` de paige n'est pas repris (phase 7)
- [x] Une seule version de `System.CommandLine` (2.0.12, D10) : seule celle de johannes subsiste, `Program.cs` de paige et `ArgumentParser` de scrubx n'étant pas repris
- [x] Ne pas reprendre `Scrubx.Web`, `Scrubx.Desktop` ni `deploy/`, `publish.*` (D9)
- [x] `dotnet build` (0 avertissement) et `dotnet test` (243 tests) au vert ; workflow `dotnet.yml` inchangé (il construit la solution entière)
- [ ] Phase 4 : reprendre l'affichage du rapport de `Scrubx.Cli/Program.cs` (316 lignes) ; `CommandLineTests` de scrubx non importés

## Phase 2 — Contrôles préalables

Tout est vérifié **avant** la première étape, pour échouer tôt et sans effet de bord. Code dans `Johannes.Core` : `Preflight`, `BookLayout`, `ExecutableLocator`, `Stages`, `ExitCodes`. Les contrôles ne sont pas encore appelés par l'exécutable (phase 3).

- [x] Document : existe (code 2), extension `.docx` insensible à la casse (code 3)
- [x] `typst` présent dans le `PATH` (D6), code 8 et message explicite ; recherche tenant compte de `PATHEXT` (Windows) et du bit d'exécution (Unix)
- [x] Étape `pdf` omise, avec information, s'il n'y a ni `Papier/` ni `Papier/*.typ` ; `typst` n'est alors pas exigé
- [x] `epub/` : exactement un `.paige` à la racine du dossier (D5), sinon code 7 avec la liste des fichiers trouvés ; confirmé que `paige` ne cherche que dans la racine du dossier et prend le premier fichier (`paigeFiles[0]`), d'où l'intérêt du contrôle
- [x] Étape `epub` omise, avec information, si `epub/` n'existe pas
- [x] Tous les problèmes sont rapportés ; le code de sortie est celui du premier (document, puis `typst`, puis `epub/`)
- [x] Contrôles limités aux étapes demandées (`Stages`)
- [x] Tests unitaires sur dossiers temporaires (`PreflightTests`, `ExecutableLocatorTests`) : 266 tests verts au total
- [x] Essai sur le dossier réel d'*Anya* : trois reliures et `template.paige` découverts, `typst` trouvé

## Phase 3 — Orchestrateur

Code : `Johannes.Core` (`StagePlanner`, `StagePlan`, `Pipeline`, `IStage`, `ExitCodes`), `Johannes/PipelineStages.cs` (`GenerateStage`, `PendingStage`) et `Johannes/Program.cs`.

- [x] Modèle d'étape (`check`, `generate`, `pdf`, `epub`) et enchaînement ordonné, arrêt à la première erreur
- [x] Options `--only`, `--until`, `--skip` (sans `check` si `generate` s'exécute, D15), `--dry-run`, `-v`
- [x] `check` ajouté d'office quand `generate` est demandé (message à l'utilisateur)
- [x] Table des codes de sortie (0 à 8, voir `USER_GUIDE.md` §10) ; ajout du **code 9** (échec de génération), qui manquait au guide
- [x] Conserver `--typst`, `--paige`, `--without-typst`, `--without-paige` avec les règles du §8 du guide (messages d'erreur de la v1 conservés, mais code de sortie 1 au lieu de 0)
- [x] Dossier de travail = dossier du `.docx` (D4) : la génération écrit à côté du `.docx` quel que soit le dossier courant (vérifié depuis un autre dossier, sortie identique à la v1)
- [x] Tests de l'enchaînement avec étapes simulées (`PipelineTests`, `StagePlannerTests`) : 298 tests verts
- [x] Sortie console en UTF-8

## Phase 4 — Étape `check`

- [x] Appeler `DocxValidator` en processus, afficher le rapport (`-v`, avertissements)
- [x] Bloquer si erreurs (D2), continuer si seulement des avertissements
- [x] Supprimer de l'import le mécanisme de règles désactivables (`RuleConfig`, `-i`, `-f`, `--create-config`, `--show-rules` si inutile) : toutes les règles toujours actives (D14)
- [x] `check` rejoué avant `generate` si l'empreinte du `.docx` n'a pas déjà un succès en cache (D15) (une copie annotée demandée par `--revisions` force toujours la vérification)
- [x] `--revisions[=fichier]` et `--author` via `DocxRevisionWriter`
- [x] Tests (`CheckStageTests`, `.docx` en mémoire : valide, erreurs, avertissements seuls, révisions) : 306 tests verts
- **Constat levé** : sur l'*Anya.docx* de l'époque, `ESPAPRES` signalait 1019 erreurs que le `scrubx` installé (plus ancien) ne voyait pas ; le document n'était réellement pas conforme. `check` fonctionne comme prévu.

## Phase 5 — Étape `generate`

- [x] Écrire `<nom>.typ`, `<nom>.paige`, `support-functions.typ` à côté du `.docx` (D4) : fait en phase 3 (`GenerateStage` passe le chemin complet aux exporteurs)
- [x] Les exporteurs ne dépendent pas du dossier courant : `TypstExporter` déduit le dossier de `support-functions.typ` du chemin reçu
- [x] Non-régression : sortie identique à la v1 sur `Anya.docx` (vérifié en phase 3)
- [x] Niveau 2 (D13) : les six regex d'insertion d'espaces sont supprimées (décision de l'utilisateur : l'insécable après `?` et `!` était un reliquat de la v1, les sauts de ligne peuvent changer). Les exporteurs ne font plus que convertir ` ` en `~` (Typst) ou `&#160;` (Paige) ; sur *Anya*, la sortie ne diffère de la v1 que par les 809 espaces suivant `?` ou `!`
- [x] Tests (`TypstExporterTests`, `PaigeExporterTests` : seules les insécables existantes sont converties, les espaces ordinaires restent) : 351 tests verts

## Phase 6 — Étape `pdf`

- [x] Découverte de `Papier/*.typ` (déjà faite en phase 2, `BookLayout`)
- [x] Lancer `typst compile "<f>.typ" --root <dossier>` ; reprendre sa sortie en cas d'échec (code 6)
- [x] Compilation en parallèle des reliures (`PdfStage`)
- [x] Tests avec un exécuteur simulé (`PdfStageTests`) : 311 tests verts ; essai réel sur une copie d'*Anya* : 3 PDF produits en 3,6 s, chaîne complète jusqu'à `pdf`

## Phase 7 — Étape `epub`

- [x] Compiler `epub/*.paige` via le moteur intégré (`EpubStage`), sortie `epub/<nom>.epub` (code 7 en cas d'échec, EPUB tronqué supprimé)
- [x] Contrôle de la règle « un seul `.paige` » déjà garanti par la phase 2 (revérifié dans l'étape)
- [x] Tests (`EpubStageTests`, petit livre en dossier temporaire) : 316 tests verts ; essai réel sur une copie d'*Anya* : EPUB identique à celui du `paige` installé, au seul horodatage `dcterms:modified` près

## Phase 8 — Cache incrémental

- [x] Empreintes SHA-256 par étape et par entrée (`Fingerprint`, `BuildCache` dans `Johannes.Core`), stockées dans `.build/state.json` (avec un `.gitignore` interne) ; une entrée par unité : `check`, `generate`, `pdf:<reliure>`, `epub`
- [x] Surveiller `Papier/Assets/**`, `epub/**` (hors l'EPUB produit), `support-styles.css`, `support-functions.typ`, que le `Makefile` ignore ; un résultat supprimé (PDF, EPUB, `.typ`, `.paige`) est refait
- [x] `--rebuild` ; invalidation à chaque changement de version de johannes (identifiant de module des quatre assemblies)
- [x] Tests (`FingerprintTests`, `BuildCacheTests`, `CachedStagesTests`) : 345 tests verts ; essai réel sur *Anya* : 2,9 s la première fois, 0,4 s quand rien n'a changé, seuls les PDF après modification de `Papier/Assets`, seul l'EPUB après modification du CSS

## Phase 9 — Configuration façon git

Code : `Johannes.Core` (`Settings`, `ConfigKeys`, `Configuration`, `ConfigFile`), `Johannes/ConfigCommand.cs`, branchement dans `Program.cs`, `Preflight` et `BookLayout`.

- [x] Sous-commande `johannes config <clé> [<valeur>]` : lecture (valeur effective, code 1 si inexistante), écriture, `--list` (valeur et origine), `--unset`, `--user`
- [x] Clés : `paper-directory`, `epub-directory`, `typst-path` ; valeurs validées (dossiers relatifs, sans `..` ; clé inconnue refusée en ligne de commande **et** dans les fichiers)
- [x] Portées (D16) : défauts, fichier de l'utilisateur (`%APPDATA%\johannes\config` / `~/.config/johannes/config`), fichier `.johannes` du livre, options `--paper-directory`, `--epub-directory`, `--typst-path`. `johannes config` écrit `.johannes` dans le dossier **courant** ; `johannes --docx` le cherche dans le dossier du `.docx`
- [x] Mettre à jour `USER_GUIDE.md` (§4, §6, §12, statut) et `README.md` ; `johannes.json` et `--init` n'existent plus nulle part
- [x] Tests (`ConfigurationTests`, `ConfigCommandTests`, ajouts à `PreflightTests`) : 383 tests verts ; essai réel sur une copie d'*Anya* dont `Papier/` est renommé `Print/` (chaîne complète, cache compris)
- Correctifs au passage : `johannes --help` plantait (option `--docx` requise lue sans parcours d'aide) ; l'ajout d'une sous-commande exigeait une commande, d'où une action racine vide dans `Program.cs`
- Limite connue : les fichiers écrits à la main (`support-functions.typ`, `Papier/*.typ`) contiennent le chemin `Papier/Assets/…` ; changer `paper-directory` impose de les adapter (signalé dans `USER_GUIDE.md` §6)

## Phase 10 — Distribution et clôture

- [x] Mettre à jour `README.md` (réécrit pour la v2), `USER_GUIDE.md` (statut), `nfpm.yaml` (description). `CONTRIBUTING.md` et les workflows `dotnet.yml` / `release.yml` n'ont rien à changer : ils construisent la solution entière et publient `Johannes/johannes.csproj` en binaire autonome (`win-x64`, `linux-x64`, `osx-arm64`) ; essai local de `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` : un seul `johannes.exe` (74 Mo), chaîne complète et cache fonctionnels sur une copie d'*Anya*
- [ ] `AGENTS.md` : retirer la section 6 (reprise de session du chantier v2) **au moment de la fusion** dans `main`, quand `ROADMAP.md` n'a plus lieu d'être
- [x] Essai réel sur *Anya* : chaîne complète sur une copie, 3 PDF et EPUB ; PDF relus à l'œil par l'utilisateur, EPUB identique à celui de `paige`
- [ ] Retirer ou réduire le `Makefile` d'*Anya* : c'est un autre dépôt, à faire par l'utilisateur (il se réduit à `johannes --docx Anya.docx`)
- [ ] Sort de `../scrubx` et `../paige` (voir questions ouvertes) ; les paquets Homebrew (`ymauray/homebrew-tap`) et Scoop de `paige` sont à retirer ou rediriger
- [ ] Fusion de `version2` dans `main` par pull request, sur instruction explicite

## Journal de session

| Date | Fait | Reste |
| ---- | ---- | ----- |
| 2026-10-21 | Phases 0 à 3 (intégration, contrôles préalables, orchestrateur) | Phases 4 à 10 |
| 2026-10-21 (suite) | Phases 4, 6, 7, 8 et niveau 2 de la phase 5 : `check`, `pdf`, `epub`, cache `.build/`, exporteurs simplifiés ; chaîne complète validée sur *Anya* | Phase 10 (distribution) |
| 2026-10-21 (fin) | Phase 9 : `johannes config`, configuration à quatre niveaux (D16), correctif `--help` | Fin de la phase 10 (fusion, `Makefile` d'*Anya*, dépôts d'origine) |
