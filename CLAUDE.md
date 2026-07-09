# Hominder

## Conventions de code

### Zéro commentaire

Le code ne contient **aucun commentaire**. Il doit être auto-documenté par des noms
explicites (types, méthodes, variables) et une structure claire.

Cela couvre tous les langages et fichiers du dépôt :

- commentaires de ligne : `//`, `#`
- commentaires de bloc : `/* */`
- commentaires de documentation : `///` (XML doc C#), JSDoc/TSDoc
- commentaires XML de projet : `<!-- -->` dans les `.csproj`, `.slnx`, etc.

Si une intention est réellement non évidente (contrainte externe, contournement),
la rendre explicite via le nommage ou une abstraction — pas via un commentaire.

## Outillage (mise)

La toolchain (`dotnet`, `node`, …) est gérée par **mise**, avec les versions épinglées
dans `mise.toml`. mise est activé dans le shell : **utiliser les outils directement**
(`dotnet build`, `dotnet test`, `npm run …`) — ne pas les préfixer par `mise exec` ni
`mise run`.

## Backend (.NET)

- Clean Architecture + DDD. Solution : `backend/Hominder.slnx`.
- Couches et dépendances : `Domain` (cœur pur, sans dépendance) ← `Application` ←
  `Infrastructure` ; `Api` = composition root (référence Application + Infrastructure).
- Tests : `Hominder.Test.Unit`, `Hominder.Test.Integration`, `Hominder.Test.Architecture`.

### Règles d'architecture (vérifiées par `Hominder.Test.Architecture`)

Ces règles sont appliquées automatiquement par des tests NetArchTest ; toute violation
casse la build de test. Les respecter en écrivant du code, ne pas les contourner.

- `Domain` ne dépend d'aucune autre couche ni d'aucun framework d'infrastructure
  (pas d'EF Core, pas d'ASP.NET Core).
- `Application` ne dépend ni d'`Infrastructure` ni d'`Api`.
- `Infrastructure` ne dépend pas d'`Api`.
- Un domain event (implémentant `IDomainEvent`) est `sealed` ; toute classe nommée
  `*DomainEvent` implémente `IDomainEvent`.

### Building blocks DDD (`Hominder.Domain/Common`)

`Entity<TId>`, `AggregateRoot<TId>` (porte les domain events), `ValueObject`
(égalité structurelle), `IDomainEvent`. Un agrégat hérite de `AggregateRoot<TId>` et est
le seul point d'entrée de son agrégat ; les value objects héritent de `ValueObject`.
