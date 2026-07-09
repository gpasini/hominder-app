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

## Backend (.NET)

- Clean Architecture + DDD. Solution : `backend/Hominder.slnx`.
- `dotnet` est géré par mise et n'est pas sur le PATH : préfixer chaque commande par
  `mise exec -- dotnet …`.
- Couches et dépendances : `Domain` (cœur pur, sans dépendance) ← `Application` ←
  `Infrastructure` ; `Api` = composition root (référence Application + Infrastructure).
- Tests : `Hominder.Test.Unit`, `Hominder.Test.Integration`.
