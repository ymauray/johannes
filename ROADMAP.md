# ROADMAP — Johannes v2

Suivi du développement d'une session à l'autre. Branche de travail : `version2`. Documents associés : `TOOLCHAIN.md` (état des lieux), `USER_GUIDE.md` (usage visé).

**Reprise de session** : lire ce fichier, repérer la première case non cochée, travailler dans l'ordre. Cocher une case seulement quand le code est écrit, `dotnet build` passe et `dotnet test` est vert. Règles de `AGENTS.md` inchangées : pas de commit, de push ni de PR sans demande explicite.

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
| D13 | Règles typographiques | Niveau 1 : vocabulaire commun `Typography` dans `Johannes.Core`. Niveau 2 (phase 5, sous conditions) : exporteurs réduits à la conversion ` ` vers `~` / `&#160;`. |
| D14 | Règles de vérification | Non désactivables : suppression de `-i/--ignore`, `-f/--force` et de `scrubx.json`. Jeu de règles fixe ; seul `VIRGET` reste un avertissement non bloquant. |
| D15 | `check` et `generate` | `check` est un prérequis de `generate` (rejoué si pas déjà réussi pour l'empreinte du `.docx`) ; plus d'option `--no-check` ni `--skip check`. **Confirmé.** |

Conséquence de D3 : `johannes.json` et `--init` sont supprimés de `USER_GUIDE.md` ; la configuration est traitée en phase 9.

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
- [x] Remplacer dans `DocxValidator` les ` `/` ` écrits en dur (46 occurrences → 1 regex littérale conservée)
- [ ] Faire consommer `Typography` aux exporteurs : **reporté volontairement à la phase 5**, car leurs regex d'insertion d'espaces (`AbstractExporter`) sont censées disparaître (D13, niveau 2)
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

- [ ] Modèle d'étape (`check`, `generate`, `pdf`, `epub`) et enchaînement ordonné
- [ ] Options `--only`, `--until`, `--skip` (sans `check`, voir D15), `--dry-run`, `-v`
- [ ] Table des codes de sortie (0 à 8, voir `USER_GUIDE.md` §10)
- [ ] Conserver `--typst`, `--paige`, `--without-typst`, `--without-paige` avec les règles du §8 du guide
- [ ] Dossier de travail = dossier du `.docx` (D4)
- [ ] Tests de l'enchaînement avec des étapes simulées

## Phase 4 — Étape `check`

- [ ] Appeler `DocxValidator` en processus, afficher le rapport (`-v`, avertissements)
- [ ] Bloquer si erreurs (D2), continuer si seulement des avertissements
- [ ] Supprimer de l'import le mécanisme de règles désactivables (`RuleConfig`, `-i`, `-f`, `--create-config`, `--show-rules` si inutile) : toutes les règles toujours actives (D14)
- [ ] `check` rejoué avant `generate` si l'empreinte du `.docx` n'a pas déjà un succès en cache (D15)
- [ ] `--revisions[=fichier]` et `--author` via `DocxRevisionWriter`
- [ ] Tests sur `Anya.docx`-like en mémoire (valide, erreurs, avertissements seuls)

## Phase 5 — Étape `generate`

- [ ] Écrire `<nom>.typ`, `<nom>.paige`, `support-functions.typ` à côté du `.docx` (D4)
- [ ] Vérifier que les exports existants (`TypstExporter`, `PaigeExporter`) ne dépendent pas du dossier courant
- [ ] Non-régression : sortie identique à la v1 sur `test-document.docx`
- [ ] Niveau 2 (D13) : comparer sur *Anya* les `.typ`/`.paige` obtenus avec et sans les regex d'insertion d'espaces (`SpaceBeforeQuestionMarkRegex` et voisines, `ColonRegex`, `SemiColonRegex`) ; si identiques, les supprimer et ne garder que ` ` vers `~` (Typst) / `&#160;` (Paige) ; attention aux cas `12:30`, `https://…`
- [ ] Tests

## Phase 6 — Étape `pdf`

- [ ] Découverte de `Papier/*.typ`
- [ ] Lancer `typst compile "<f>.typ" --root <dossier>` ; reprendre sa sortie en cas d'échec (code 6)
- [ ] Compilation en parallèle des reliures
- [ ] Tests avec un faux exécutable `typst`

## Phase 7 — Étape `epub`

- [ ] Compiler `epub/*.paige` via le moteur intégré, sortie `epub/<nom>.epub` (code 7 en cas d'échec)
- [ ] Contrôle de la règle « un seul `.paige` » déjà garanti par la phase 2
- [ ] Test de bout en bout sur un petit livre d'exemple

## Phase 8 — Cache incrémental

- [ ] Empreintes SHA-256 par étape et par entrée (tableau du §7 du guide), stockées dans `.build/`
- [ ] Surveiller `Papier/Assets/**`, `epub/**`, `support-styles.css`, `support-functions.typ`, que le `Makefile` ignore
- [ ] `--rebuild` ; invalidation à chaque changement de version de johannes
- [ ] Tests : modifier une entrée ne rejoue que l'étape concernée

## Phase 9 — Configuration façon git

- [ ] Sous-commande `johannes config <clé> [<valeur>]` (lecture, écriture, liste, suppression)
- [ ] Premières clés : `paper-directory`, `epub-directory`, `typst-path`
- [ ] Portées possibles (dépôt / utilisateur) à décider
- [ ] Mettre à jour `USER_GUIDE.md` (retirer `--init` et `johannes.json` ou les remplacer)

## Phase 10 — Distribution et clôture

- [ ] Mettre à jour `README.md`, `AGENTS.md`, `CONTRIBUTING.md`, `nfpm.yaml`, workflows de release (binaires `win-x64`, `linux-x64`, `osx-arm64`)
- [ ] Essai réel sur *Anya* : résultats comparés à ceux du `Makefile` (PDF, EPUB)
- [ ] Retirer ou réduire le `Makefile` d'*Anya*
- [ ] Sort de `../scrubx` et `../paige` (voir questions ouvertes)
- [ ] Fusion de `version2` dans `main` par pull request, sur instruction explicite

## Journal de session

| Date | Fait | Reste |
| ---- | ---- | ----- |
