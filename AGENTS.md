# Standards de Codage et d'Interaction (Johannes)

Ce document définit les règles de développement et d'interaction pour le projet Johannes. Ces règles s'appliquent en priorité à toute intelligence artificielle intervenant sur le codebase.

## 1. Messages de Commit
- Tous les messages de commit doivent suivre la norme **Conventional Commits**.
- Les messages doivent être rédigés **exclusivement en français**.
- Format : `<type>(<scope>): <description>` (ex: `feat(parser): ajout du support des tableaux`).

## 2. Gestion des Commits par l'IA
- **Invitation uniquement** : L'IA ne doit JAMAIS effectuer de commit de sa propre initiative. Elle doit attendre une instruction explicite (Directive) de l'utilisateur.
- **Discrétion** : L'IA ne doit PAS demander à l'utilisateur s'il souhaite committer après chaque modification. C'est à l'utilisateur de décider du moment opportun pour consolider les changements.
- **Co-auteur** : L'agent IA doit obligatoirement s'identifier dans les commits comme co-auteur (via la mention `Co-authored-by: <Nom> <email>` dans le message de commit).

## 3. Gestion des Branches et Pull Requests
- La branche `main` est protégée. Toute modification doit être développée sur une branche dédiée et intégrée via une pull request.
- L'IA ne doit pousser une branche ou créer une pull request que sur instruction explicite de l'utilisateur.

## 4. Cycle de Développement (Plan-Act-Validate)
- **Tests Unitaires** : Après chaque modification de code, si un test unitaire peut être ajouté pour valider le changement, il doit être implémenté immédiatement dans le projet `Johannes.Tests`.
- **Validation Systématique** : Le projet doit être recompilé (`dotnet build`) après chaque modification pour garantir l'absence de régressions de compilation.
- **Exécution des Tests** : Les tests unitaires doivent être lancés (`dotnet test`) après chaque modification significative.

## 5. Standards Techniques
- **C# / .NET 10** : Utilisation des fonctionnalités modernes du langage.
- **Indentation** : Tabulations pour le C#, espaces pour le Markdown/YAML/Typst/Paige (configuré via `.editorconfig`).
- **Typographie** : Respect scrupuleux des règles typographiques françaises lors de l'export (espaces insécables, etc.).

## 6. Reprise de session
- Lire d'abord `README.md` (la chaîne `check`, `generate`, `pdf`, `epub`, les options, l'architecture) puis `USER_GUIDE.md` (usage détaillé : organisation d'un livre, cache, configuration, codes de sortie).
- `ROADMAP.md` garde l'historique du chantier v2 : décisions D1 à D16, carte du code, pièges rencontrés, environnement de test (section « Reprise à froid »). `TOOLCHAIN.md` est l'état des lieux de `scrubx` et `paige` avant leur intégration, à titre de mémoire.
- Structure de la solution : `Johannes.Core` (code commun : typographie, plan des étapes, contrôles préalables, cache, configuration), `Johannes.Scrubx` (vérification typographique), `Johannes.Paige` (moteur EPUB), `Johannes` (exécutable, étapes et exporteurs), `Johannes.Tests` (un seul projet de tests).
- Après création de fichiers C#, lancer `dotnet format whitespace johannes.sln --include <fichiers ou dossiers>` (tabulations et CRLF imposés par `.editorconfig`) ; ne pas le lancer sur toute la solution sans `--include`.
- `.gitignore` contient `*.paige` : vérifier avec `git ls-files` que tout nouveau projet ou fixture de ce nom est bien suivi.
- Tout essai sur un livre réel se fait sur une copie : la génération réécrit `.typ`, `.paige` et `support-functions.typ` à côté du `.docx`.
