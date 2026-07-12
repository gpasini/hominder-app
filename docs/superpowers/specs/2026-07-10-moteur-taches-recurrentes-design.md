# Moteur de tâches d'entretien récurrentes — Design

Date : 2026-07-10
Statut : validé, prêt pour la planification

## Contexte

Hominder vise à gérer les tâches d'entretien récurrentes d'une maison (tailler
l'olivier au printemps, contrôle technique tous les 2 ans, saturateur sur la
terrasse tous les 2 ans, etc.), avec un bouton « c'est fait » qui recalcule la
prochaine échéance.

Le projet complet se décompose en trois briques indépendantes :

1. **Moteur de tâches récurrentes** (cette spec) — le socle métier + une UI web de gestion.
2. **Affichage kiosque** — petit écran à l'entrée, en lecture seule (spec ultérieure).
3. **Domotique / surcouche Home Assistant** — dashboard maison (spec ultérieure).

Cette spec ne couvre que la **brique 1**. Les briques 2 et 3 se brancheront sur le
même backend et feront chacune l'objet de leur propre spec.

## Décisions produit

- **Foyer unique, membres nommés** : pas d'authentification ni de mot de passe.
  Une tâche peut être assignée à un membre ; l'historique conserve qui a fait quoi et quand.
- **Quatre modèles de récurrence** (voir Domaine).
- **Pas de notifications ni de rappels** en v1 : le cœur calcule et expose l'état
  (`À venir` / `Dû` / `En retard` / `Fait`). L'affichage est du ressort de l'UI et,
  plus tard, du kiosque. Un point d'extension (domain event de complétion) est prévu.
- **Livrable v1 utilisable de bout en bout** : domaine + API + UI web de gestion.

## Décisions techniques

- Backend .NET 10, Clean Architecture + DDD existante (`Domain` ← `Application` ←
  `Infrastructure`, `Api` = composition root). MediatR + Autofac déjà en place.
- Persistance : **PostgreSQL** via EF Core + Npgsql, conteneur géré par colima/Docker.
- Frontend : React 19 + Vite + TS existant, **TanStack Query** + client typé généré
  depuis l'OpenAPI (`openapi-typescript`).
- Le template `WeatherForecast` de l'Api est supprimé.

## Domaine (`Hominder.Domain`)

Le `Domain` reste pur (aucune dépendance framework, vérifié par `Test.Architecture`).

### Agrégats

**`MaintenanceTask : AggregateRoot<MaintenanceTaskId>`** — seul point d'entrée de son agrégat.

- `Title`, `Notes` (optionnel)
- `RecurrencePolicy` (VO polymorphe)
- `AssigneeId : HouseholdMemberId?` (optionnel)
- `Completions` : collection interne de complétions (`CompletedOn : DateOnly`,
  `CompletedBy : HouseholdMemberId`)
- Comportement :
  - `MarkDone(DateOnly date, HouseholdMemberId by, DateOnly? nextDueOverride)` →
    ajoute une complétion, applique `nextDueOverride` puis émet
    `MaintenanceTaskCompletedDomainEvent`. `nextDueOverride` est **obligatoire pour
    `FixedDatePolicy`** (la date suivante est saisie) et **interdit pour les trois
    autres politiques** (échéance recalculée automatiquement, ou terminale pour
    `OneOffPolicy`).
  - Les invariants (titre non vide, date de complétion cohérente, override requis
    ou interdit selon la politique) sont gardés dans l'agrégat et lèvent des
    exceptions de domaine.

**`HouseholdMember : AggregateRoot<HouseholdMemberId>`**

- `Name`. Rien d'autre (pas de mot de passe).

### Value Object `RecurrencePolicy` et variantes

`RecurrencePolicy` est un `ValueObject` abstrait. Chaque variante calcule une
**fenêtre d'échéance** `DueWindow(OpenDate, DueDate)` à partir de l'historique de
complétions et d'une date de référence :

- **`IntervalPolicy(int amount, RecurrenceUnit unit)`**
  `DueDate = (dernière complétion, ou date de départ fournie à la création) + intervalle`.
  `OpenDate = DueDate` (pas de fenêtre).
- **`MonthWindowPolicy(Month startMonth, Month endMonth)`**
  Chaque année : `OpenDate = 1er jour du mois de début`, `DueDate = dernier jour du
  mois de fin`. Gère le passage d'année (ex. novembre→février).
- **`FixedDatePolicy(DateOnly dueDate)`**
  `OpenDate = DueDate = date saisie`. À la complétion, `nextDueOverride` est
  **obligatoire** et remplace la date.
- **`OneOffPolicy(DateOnly dueDate)`**
  `OpenDate = DueDate = échéance unique`. Une fois complétée, la tâche est en état
  terminal `Done` ; `nextDueOverride` est interdit.

`RecurrenceUnit` : `Days`, `Weeks`, `Months`, `Years`.

### Calcul du statut (à la lecture, jamais stocké)

À partir de `DueWindow` et de `today` :

- `Upcoming` : `today < OpenDate`
- `Due` : `OpenDate <= today <= DueDate`
- `Overdue` : `today > DueDate`
- `Done` : `OneOffPolicy` complétée.

Pour les politiques à date unique, la fenêtre se réduit au jour même : `Upcoming`
avant, `Overdue` après. Le nombre de jours de retard `= today - DueDate` est exposé
pour l'affichage. Un « délai d'anticipation » configurable pourra être ajouté plus
tard (logique de rappel, hors v1).

### Domain events

- `MaintenanceTaskCompletedDomainEvent` (sealed, implémente `IDomainEvent`) — émis
  par `MarkDone`. Point d'extension pour le kiosque / les notifications futures.

## Application (`Hominder.Application`)

### Abstractions CQRS custom

- `ICommand<TResponse> : IRequest<TResponse>` et `ICommand : IRequest` (non générique).
- `IQuery<TResponse> : IRequest<TResponse>`.

Elles servent de cible aux behaviors MediatR :

- `LoggingBehavior<TRequest, TResponse>` : sur toutes les requêtes (commands + queries),
  log entrée / sortie / durée.
- `TransactionBehavior<TRequest, TResponse>` : **contraint aux `ICommand`**. Ouvre une
  transaction SQL via `IUnitOfWork`, exécute le handler, commit si succès / rollback sur
  exception. Les queries n'y passent pas.

Ordre d'enregistrement dans `AddApplication` : `LoggingBehavior` englobe `TransactionBehavior`.

**Pas de FluentValidation** : la validation vit dans les handlers de command et les
invariants du domaine.

### Commands

- `CreateMaintenanceTaskCommand` (titre, notes, spec de politique, assigné)
- `UpdateMaintenanceTaskCommand`
- `DeleteMaintenanceTaskCommand`
- `MarkMaintenanceTaskDoneCommand` (taskId, date, membre, `nextDueOverride`)
- `CreateHouseholdMemberCommand`
- `DeleteHouseholdMemberCommand`

### Queries

- `GetMaintenanceTasksQuery` → liste enrichie : statut calculé, prochaine échéance,
  jours de retard, assigné ; triée par urgence (En retard → Dû → À venir → Done).
- `GetHouseholdMembersQuery`

### Ports (implémentés dans Infrastructure)

- `IMaintenanceTaskRepository`, `IHouseholdMemberRepository`, `IUnitOfWork`.
- `TimeProvider` (natif .NET) injectée pour fournir `today` au calcul de statut ;
  permet de figer le temps dans les tests.

## Infrastructure (`Hominder.Infrastructure`)

- `HominderDbContext` (EF Core + Npgsql).
- Mapping via `IEntityTypeConfiguration<>` (le `Domain` reste sans attribut EF).
- **`RecurrencePolicy` persistée en colonne `jsonb`** unique (schéma compact et
  évolutif : ajouter une variante ne nécessite pas de migration de colonne).
- `Completions` : table enfant liée à l'agrégat `MaintenanceTask`.
- Strongly-typed IDs (`MaintenanceTaskId`, `HouseholdMemberId`) mappés via value converters.
- `IUnitOfWork` implémenté sur le `DbContext` ; repositories concrets ici.
- `AddInfrastructure()` câblée depuis l'Api (composition root).
- Migrations EF Core versionnées. Service `docker-compose` `db` (Postgres) ajouté.

## API (`Hominder.Api`)

Suppression du template `WeatherForecast`. Minimal APIs mappant les commands/queries :

- `GET /api/tasks`, `POST /api/tasks`
- `PUT /api/tasks/{id}`, `DELETE /api/tasks/{id}`
- `POST /api/tasks/{id}/completions` (marquer fait)
- `GET /api/members`, `POST /api/members`, `DELETE /api/members/{id}`

OpenAPI déjà activé → sert de source au client typé du frontend.

## Frontend (`frontend`)

- Page principale : tâches groupées par statut (En retard → Dû → À venir → Done),
  carte par tâche (titre, prochaine échéance, assigné, bouton **« C'est fait »**).
  Le bouton ouvre une mini-saisie (date + membre, et nouvelle date pour `FixedDatePolicy`).
- Formulaire créer / éditer une tâche (choix de la politique + ses paramètres).
- Écran de gestion des membres.
- **TanStack Query** sur `fetch` ; types générés depuis l'OpenAPI via `openapi-typescript`.
- CSS simple, pas de design system en v1 (le travail visuel viendra avec le kiosque).

## Tests

- **`Test.Unit` (domaine)** — cœur de la valeur : chaque variante de `RecurrencePolicy`
  → fenêtre & statut selon `today` figé ; `MarkDone` (historique + événement + règles
  d'override).
- **`Test.Unit` (handlers)** — commands/queries avec repositories en double.
- **`Test.Integration`** — API + **PostgreSQL réel via Testcontainers** : persistance
  jsonb, mapping des IDs, endpoints de bout en bout.
- **`Test.Architecture`** — règles existantes respectées par la nouvelle structure.
- **Front** — Vitest léger sur le formatage de statut (optionnel v1).

## Hors périmètre (v1)

- Notifications, rappels, emails, jobs planifiés.
- Authentification / multi-foyers.
- Kiosque et intégration Home Assistant (briques 2 et 3).
- Délai d'anticipation configurable sur les échéances.
