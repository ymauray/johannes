# Standards de Codage et d'Interaction (Johannes)

Ce document définit les règles de développement et d'interaction pour le projet Johannes. Ces règles s'appliquent en priorité à toute intelligence artificielle intervenant sur le codebase.

## 1. Messages de Commit
- Tous les messages de commit doivent suivre la norme **Conventional Commits**.
- Les messages doivent être rédigés **exclusivement en français**.
- Format : `<type>(<scope>): <description>` (ex: `feat(parser): ajout du support des tableaux`).

## 2. Gestion des Commits par l'IA
- **Invitation uniquement** : L'IA ne doit JAMAIS effectuer de commit de sa propre initiative. Elle doit attendre une instruction explicite (Directive) de l'utilisateur.
- **Discrétion** : L'IA ne doit PAS demander à l'utilisateur s'il souhaite committer après chaque modification. C'est à l'utilisateur de décider du moment opportun pour consolider les changements.

## 3. Cycle de Développement (Plan-Act-Validate)
- **Tests Unitaires** : Après chaque modification de code, si un test unitaire peut être ajouté pour valider le changement, il doit être implémenté immédiatement dans le projet `Johannes.Tests`.
- **Validation Systématique** : Le projet doit être recompilé (`dotnet build`) après chaque modification pour garantir l'absence de régressions de compilation.
- **Exécution des Tests** : Les tests unitaires doivent être lancés (`dotnet test`) après chaque modification significative.

## 4. Standards Techniques
- **C# / .NET 10** : Utilisation des fonctionnalités modernes du langage.
- **Indentation** : Tabulations pour le C#, espaces pour le Markdown/YAML/Typst (configuré via `.editorconfig`).
- **Typographie** : Respect scrupuleux des règles typographiques françaises lors de l'export (espaces insécables, etc.).
