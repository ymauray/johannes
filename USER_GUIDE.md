# Johannes v2 — Guide d'utilisation (proposition)

> **Statut : implémenté sur la branche `version2` (phases 0 à 8 de `ROADMAP.md`).** Ce guide décrit l'usage de johannes v2 ; la sous-commande `johannes config` (§6) reste à venir (phase 9). Il est écrit à partir de la structure du livre *Anya* (`../Anya`, dont le `Makefile`) et de `TOOLCHAIN.md`. Les décisions prises figurent en fin de document (§12) ; le suivi du développement est dans `ROADMAP.md`.

## 1. Principe

Une seule commande, une seule source de vérité : le `.docx`.

```bash
johannes --docx Anya.docx
```

Elle enchaîne quatre étapes, dans l'ordre :

| Étape | Nom | Ce qui se passe | Outil actuel |
| ----- | --- | --------------- | ------------ |
| 1 | `check` | Vérification typographique du `.docx` | `scrubx` |
| 2 | `generate` | Écriture de `Anya.typ`, `Anya.paige`, `support-functions.typ` | `johannes` v1 |
| 3 | `pdf` | Un PDF par fichier de `Papier/*.typ` | `typst compile` |
| 4 | `epub` | L'EPUB à partir du projet `epub/` | `paige` |

Les `.typ` et `.paige` générés sont des **artefacts**, jamais des sources : ne les modifiez pas à la main, ils sont réécrits à chaque exécution (c'est déjà la règle dans le `Makefile`).

`scrubx` et `paige` deviennent des **bibliothèques internes** de `johannes` : il n'y a plus rien à installer sur le `PATH`, hormis `typst`, qui n'est pas un outil .NET.

## 2. Ce que fait le Makefile aujourd'hui (et ce qu'il laisse de côté)

Constats sur `../Anya/Makefile` :

- La cible `.build/generate.stamp` dépend **uniquement** de `Anya.docx`. Les PDF et l'EPUB dépendent seulement de ce marqueur. Donc modifier `Papier/Assets/common.typ`, un `Papier/*.typ`, `epub/template.paige`, un `.xhtml`, la couverture ou `support-styles.css` **ne relance rien** : il faut faire `make clean`.
- `scrubx` est lancé deux fois (`make check` et dans la recette du marqueur), toujours en `-v`.
- Les trois PDF sont indépendants mais construits l'un après l'autre.
- Le fichier est encodé en Windows-1252 (les accents de `Anya-broché.pdf` s'affichent mal dans d'autres outils) et suppose `make`, `touch` et `mkdir -p`, donc un shell de type POSIX. C'est un frein sous Windows natif.

La v2 reprend la même chaîne et corrige ces quatre points.

## 3. Organisation d'un livre

Aucun changement n'est exigé par rapport à l'existant. Johannes reconnaît par **convention** la structure d'*Anya* :

```
Anya/
├── Anya.docx                 ← la seule source d'écriture
├── support-functions.typ     ← créé s'il manque, jamais écrasé (fonctions Typst à personnaliser)
├── support-styles.css        ← à écrire à la main (styles Paige)
├── Papier/
│   ├── Assets/common.typ     ← mise en page (gabarit `book`)
│   ├── Anya-broché.typ       ← une reliure = un fichier = un PDF
│   ├── Anya-relié.typ
│   └── Anya-jaspé.typ
├── epub/
│   ├── template.paige        ← métadonnées, couverture, pages liminaires, #include du texte
│   └── *.xhtml, cover.jpg, logo.svg
├── .build/                   ← cache de johannes (à ignorer dans git)
├── Anya.typ, Anya.paige      ← générés
└── (sorties) Papier/*.pdf, epub/Anya.epub
```

Tous les chemins sont relatifs au **dossier du `.docx`** : c'est là que sont lus `Papier/` et `epub/`, et là que sont écrits les fichiers générés, quel que soit le dossier depuis lequel `johannes` est lancé.

Règles de découverte (pas de fichier de configuration) :

- **PDF** : chaque `Papier/*.typ` produit le `Papier/*.pdf` de même nom.
- **EPUB** : `epub/` doit contenir **exactement un** fichier `.paige` ; il est compilé en `epub/<nom-du-docx>.epub`.
- Sans `Papier/` (ou sans `epub/`), l'étape correspondante est simplement omise, avec un message.

## 4. Utilisation courante

```bash
# Tout : vérification, génération, PDF, EPUB (ne refait que ce qui est périmé, voir §7)
johannes --docx Anya.docx

# Vérifier seulement
johannes --docx Anya.docx --only check

# Générer les .typ/.paige sans compiler (comportement de la v1)
johannes --docx Anya.docx --until generate

# Reconstruire uniquement l'EPUB
johannes --docx Anya.docx --only epub

# Tout refaire, cache ignoré
johannes --docx Anya.docx --rebuild
```

### Options

| Option | Effet |
| ------ | ----- |
| `--docx`, `-d` | Document source (obligatoire). Le dossier de travail est celui du document. |
| `--only <étapes>` | N'exécute que ces étapes (`check,generate,pdf,epub`). |
| `--until <étape>` | S'arrête après cette étape. |
| `--skip <étapes>` | Saute ces étapes. |
| `--rebuild` | Ignore le cache : reconstruit tout. |
| `--revisions[=<fichier>]`, `--author <nom>` | Écrit la copie annotée (voir §5). |
| `--verbose`, `-v` | Détail des anomalies et des commandes lancées ; signale aussi les étapes non demandées. |
| `--dry-run` | Affiche le plan (étapes à faire / à sauter et pourquoi) sans rien exécuter. |
| `--typst`, `--paige`, `--without-typst`, `--without-paige` | Conservées. Restreignent la chaîne à une branche (voir §8). |

## 5. Vérification typographique : la barrière

L'étape `check` est **bloquante si elle se termine en erreur** : si `scrubx` trouve au moins une erreur, johannes s'arrête avant de générer quoi que ce soit (code de sortie 4, comme `scrubx`). Les avertissements (`VIRGET`) s'affichent mais ne bloquent pas. Un livre qui ne passe pas la vérification n'est donc jamais publié par erreur.

Flux de correction recommandé :

1. `johannes --docx Anya.docx` s'arrête avec la liste des anomalies.
2. `johannes --docx Anya.docx --only check --revisions` écrit `Anya-relu.docx` : chaque correction automatisable y est une révision suivie avec un commentaire (jamais l'original).
3. Ouvrir `Anya-relu.docx` dans Word, accepter ou refuser, corriger à la main ce qui n'a pas de correction automatique (styles, sauts de page, virgule avant « et »).
4. Remplacer `Anya.docx` par la version relue et relancer `johannes --docx Anya.docx`.

**Les règles ne sont pas désactivables** : pas de `-i`/`--ignore`, pas de `-f`, pas de `scrubx.json`. Le jeu de règles est fixe. Un document qui passe `check` est donc garanti conforme, et la génération peut s'appuyer dessus. Seul l'avertissement `VIRGET` (virgule avant « et ») reste à l'appréciation de l'auteur, car il ne bloque pas.

`check` est un **prérequis de `generate`** : même avec `--only generate`, la vérification est rejouée si elle n'a pas déjà réussi pour cette version exacte du `.docx` (empreinte en cache, §7). Il n'y a pas d'option pour la contourner.

## 6. Configuration

**Il n'y en a pas, volontairement** : tout repose sur les conventions du §3. Aucun fichier de réglage n'est lu : le jeu de règles typographiques est fixe.

À terme, les réglages seront gérés comme avec git, par une sous-commande :

```bash
johannes config paper-directory Papier
johannes config --list
```

Cette sous-commande n'est pas encore définie en détail (voir `ROADMAP.md`, phase 9). Priorité prévue, du plus faible au plus fort : valeurs par défaut, `johannes config`, options de ligne de commande.

Aucune donnée éditoriale ne sort de ses fichiers actuels : l'ISBN et la liste des relecteurs restent dans `Papier/*.typ` ; le titre, l'identifiant et la langue restent dans `epub/template.paige`.

## 7. Reconstruction incrémentale

`.build/` remplace `generate.stamp` par une empreinte (SHA-256) de chaque entrée de chaque étape. Une étape est rejouée si une de ses entrées a changé :

| Étape | Entrées surveillées |
| ----- | ------------------- |
| `check` | le `.docx`, la version de johannes (jeu de règles) |
| `generate` | le `.docx`, la version de johannes |
| `pdf` (par fichier) | `Anya.typ`, `support-functions.typ`, le `.typ` de reliure, tout `Papier/Assets/**` |
| `epub` | `Anya.paige`, `support-styles.css`, tout `epub/**` |

Conséquences : modifier une ressource de mise en page ne reconstruit que les PDF ; modifier une page liminaire ne reconstruit que l'EPUB ; modifier le texte reconstruit tout. Les trois PDF sont compilés en parallèle.

`--rebuild` ignore le cache ; `rm -rf .build` aussi.

## 8. Mode « branche unique »

Les options historiques restent utilisables :

| Commande | Étapes exécutées |
| -------- | ---------------- |
| `--typst` | `check`, `generate` (`.typ` seul), `pdf` |
| `--paige` | `check`, `generate` (`.paige` seul), `epub` |
| `--without-typst` | chaîne complète, sans `.typ` ni PDF |
| `--without-paige` | chaîne complète, sans `.paige` ni EPUB |

## 9. Dépendances et contrôles préalables

- **.NET 10** (ou binaire autonome).
- **`typst`** dans le `PATH`.
- Plus de `scrubx` ni de `paige` à installer ; leurs paquets (Homebrew, Scoop, `.deb`) deviennent inutiles à terme.
- Plus de `make`, `touch` ni `mkdir -p` : fonctionne sous PowerShell, cmd et bash. Le `Makefile` peut être conservé comme simple raccourci ou supprimé.

Avant toute étape, johannes vérifie **le plus tôt possible**, pour échouer vite et sans rien écrire :

1. le document existe (code 2) et porte l'extension `.docx` (code 3) ;
2. `typst` est dans le `PATH` (code 8), sauf si l'étape `pdf` n'est pas demandée ;
3. si `epub/` existe, il contient **exactement un** fichier `.paige` (code 7) ; zéro ou plusieurs → erreur qui liste les fichiers trouvés, sauf si l'étape `epub` n'est pas demandée.

## 10. Codes de sortie

| Code | Signification |
| ---- | ------------- |
| 0 | Chaîne terminée (avec ou sans avertissements) |
| 1 | Erreur d'arguments |
| 2 | Document introuvable |
| 3 | Le document n'est pas un `.docx` |
| 4 | Erreurs de typographie détectées (étape `check`) |
| 5 | Échec d'écriture de la copie annotée |
| 6 | Échec de `typst compile` (sa sortie est reprise telle quelle) |
| 7 | Échec de compilation `paige` |
| 8 | Dépendance externe absente (`typst`) |
| 9 | Échec de la génération des `.typ` / `.paige` (document non pris en charge, erreur d'écriture) |

Les codes 1 à 5 sont ceux de `scrubx`, pour garder la compatibilité avec d'éventuels scripts.

## 11. Compatibilité avec la v1

- `johannes --docx X.docx` **change de comportement** : il lançait seulement la génération, il enchaîne désormais toute la chaîne. L'ancien comportement approchant s'obtient avec `--until generate` (la vérification, désormais obligatoire avant la génération, reste jouée).
- Un répertoire sans `Papier/` ni `epub/` se comporte presque comme la v1 (plus la vérification typographique, qui devient bloquante).
- Formats de sortie `.typ`/`.paige` inchangés.

## 12. Décisions

| # | Sujet | Décision |
| - | ----- | -------- |
| D1 | `johannes --docx X.docx` | Chaîne complète : `check`, `generate`, `pdf`, `epub`. |
| D2 | Étape `check` | Bloquante uniquement si elle se termine en erreur. |
| D3 | Configuration | Convention seule d'abord ; ensuite `johannes config <clé> <valeur>`, façon git. |
| D4 | Fichiers de sortie | Dans le même dossier que le `.docx`. |
| D5 | `epub/` | Un seul `.paige`, vérifié dès le démarrage. |
| D6 | `typst` | Présence dans le `PATH` vérifiée dès le démarrage. |
| D7 | Ordre d'implémentation | `Scrubx.Core` et `Paige` d'abord, puis l'orchestrateur, puis le cache. |
| D8 | Historique git de `scrubx` et `paige` | Non conservé : seul le code est repris. |
| D9 | `Scrubx.Web`, `Scrubx.Desktop` | Abandonnés. |
| D10 | `System.CommandLine` | Version stable la plus récente (série 2.0.x). |
| D11 | Code commun | À mutualiser au maximum entre les modules (voir `ROADMAP.md`). |
| D12 | Lecture du `.docx` | Une seule couche (ZIP brut + XML), `DocumentFormat.OpenXml` retiré. |
| D13 | Règles typographiques | Vocabulaire commun d'abord ; exporteurs simplifiés ensuite si la sortie reste identique. |
| D14 | Règles de vérification | Non désactivables : ni `-i`, ni `-f`, ni `scrubx.json`. |
| D15 | `check` et `generate` | `check` est un prérequis de `generate` (confirmé). |

`typst` reste un processus externe : aucun binding .NET connu. Le plan d'implémentation est dans `ROADMAP.md`.
