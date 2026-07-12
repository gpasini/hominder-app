# Moteur de tâches d'entretien récurrentes — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construire le moteur de tâches d'entretien récurrentes de Hominder (domaine + API + UI web de gestion), avec calcul de statut à la lecture et bouton « c'est fait ».

**Architecture:** Clean Architecture / DDD existante (`Domain` ← `Application` ← `Infrastructure`, `Api` = composition root). CQRS via MediatR avec behaviors logging + transaction. Persistance PostgreSQL (EF Core + Npgsql), politique de récurrence sérialisée en `jsonb`. Frontend React + TanStack Query + client typé généré depuis l'OpenAPI.

**Tech Stack:** .NET 10, MediatR 12.5, Autofac, EF Core + Npgsql, PostgreSQL, xUnit, Testcontainers, React 19, Vite, TypeScript, TanStack Query, openapi-typescript + openapi-fetch.

## Global Constraints

- **Zéro commentaire** dans tout le code produit (aucun `//`, `/* */`, `///`, JSDoc/TSDoc, ni `<!-- -->`). Le code s'auto-documente par le nommage. Copié de `CLAUDE.md`.
- **Outils via mise, sans préfixe** : `dotnet build`, `dotnet test`, `npm run …` directement.
- **`Domain` reste pur** : aucune dépendance sur `Application`, `Infrastructure`, `Api`, ni sur `Microsoft.EntityFrameworkCore` / `Microsoft.AspNetCore` (vérifié par `Hominder.Test.Architecture`).
- **`Application` ne dépend ni d'`Infrastructure` ni d'`Api`. `Infrastructure` ne dépend pas d'`Api`.**
- **Tout `*DomainEvent` est `sealed` et implémente `IDomainEvent`** (vérifié par `Hominder.Test.Architecture`).
- Backend : `TargetFramework=net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `ImplicitUsings=enable`. Le code doit compiler sans warning.
- Un agrégat hérite de `AggregateRoot<TId>` et est le seul point d'entrée de son agrégat ; les value objects héritent de `ValueObject`.
- API en dev : `http://localhost:5191` (profil `http` de `launchSettings.json`). Frontend dev : `http://localhost:5173` (Vite).

## Building blocks existants (à consommer, ne pas recréer)

- `Hominder.Domain.Common.Entity<TId>` : `Id` (`protected init`), égalité par type + Id.
- `Hominder.Domain.Common.AggregateRoot<TId>` : `RaiseDomainEvent(IDomainEvent)`, `DomainEvents`, `ClearDomainEvents()`.
- `Hominder.Domain.Common.ValueObject` : override `IEnumerable<object?> GetEqualityComponents()`.
- `Hominder.Domain.Common.IDomainEvent` : `DateTime OccurredOnUtc { get; }`.

## Structure des fichiers

**Domain (`backend/Hominder.Domain`)**
- `Common/DomainException.cs` — exception métier.
- `Household/HouseholdMemberId.cs`, `Household/HouseholdMember.cs`.
- `Maintenance/MaintenanceTaskId.cs`, `Maintenance/RecurrenceUnit.cs`, `Maintenance/MaintenanceStatus.cs`, `Maintenance/DueWindow.cs`, `Maintenance/MaintenanceEvaluation.cs`, `Maintenance/Completion.cs`, `Maintenance/MaintenanceTaskCompletedDomainEvent.cs`.
- `Maintenance/Policies/RecurrencePolicy.cs` (+ `IntervalPolicy.cs`, `MonthWindowPolicy.cs`, `FixedDatePolicy.cs`, `OneOffPolicy.cs`).
- `Maintenance/MaintenanceTask.cs`.

**Application (`backend/Hominder.Application`)**
- `Common/Messaging/{IBaseCommand,ICommand,IQuery}.cs`.
- `Common/Persistence/{IUnitOfWork,IMaintenanceTaskRepository,IHouseholdMemberRepository}.cs`.
- `Common/Behaviors/{LoggingBehavior,TransactionBehavior}.cs`.
- `Common/Exceptions/NotFoundException.cs`.
- `Maintenance/RecurrencePolicyInput.cs`, `Maintenance/RecurrencePolicyFactory.cs`.
- `Maintenance/Commands/*` (Create/Update/Delete/MarkDone + handlers).
- `Maintenance/Queries/*` (GetMaintenanceTasks + views + handler).
- `Household/Commands/*`, `Household/Queries/*`.
- `DependencyInjection.cs` (modifié).

**Infrastructure (`backend/Hominder.Infrastructure`)**
- `Persistence/HominderDbContext.cs`.
- `Persistence/Configurations/{MaintenanceTaskConfiguration,HouseholdMemberConfiguration}.cs`.
- `Persistence/Serialization/RecurrencePolicyJsonConverter.cs`.
- `Persistence/Repositories/{MaintenanceTaskRepository,HouseholdMemberRepository}.cs`.
- `Persistence/Migrations/*` (généré).
- `DependencyInjection.cs` (nouveau).

**Api (`backend/Hominder.Api`)**
- `Program.cs` (réécrit, sans WeatherForecast).
- `Endpoints/{MaintenanceTaskEndpoints,HouseholdMemberEndpoints}.cs`.
- `ExceptionHandling/DomainExceptionHandler.cs`.
- `appsettings.json` / `appsettings.Development.json` (chaîne de connexion).

**Racine** — `docker-compose.yml` (service `db` Postgres).

**Frontend (`frontend`)**
- `src/api/client.ts`, `src/api/schema.d.ts` (généré).
- `src/query/queryClient.ts`.
- `src/features/members/{useMembers.ts,MembersScreen.tsx}`.
- `src/features/tasks/{useTasks.ts,taskStatus.ts,TaskList.tsx,TaskCard.tsx,TaskForm.tsx,MarkDoneDialog.tsx}`.
- `src/App.tsx` (réécrit).

---

## Couche Domaine

### Task 1: Identifiants fortement typés + DomainException

**Files:**
- Create: `backend/Hominder.Domain/Common/DomainException.cs`
- Create: `backend/Hominder.Domain/Maintenance/MaintenanceTaskId.cs`
- Create: `backend/Hominder.Domain/Household/HouseholdMemberId.cs`
- Test: `backend/Hominder.Test.Unit/Domain/StronglyTypedIdTests.cs`

**Interfaces:**
- Produces: `MaintenanceTaskId(Guid Value)` + `MaintenanceTaskId.New()`; `HouseholdMemberId(Guid Value)` + `HouseholdMemberId.New()`; `DomainException(string message)`.

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Domain;

public class StronglyTypedIdTests
{
    [Fact]
    public void New_ProducesDistinctValues()
    {
        var first = MaintenanceTaskId.New();
        var second = MaintenanceTaskId.New();

        Assert.NotEqual(first, second);
        Assert.NotEqual(Guid.Empty, first.Value);
    }

    [Fact]
    public void SameValue_AreEqual()
    {
        var value = Guid.NewGuid();

        Assert.Equal(new MaintenanceTaskId(value), new MaintenanceTaskId(value));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter StronglyTypedIdTests`
Expected: FAIL (compilation) — `MaintenanceTaskId` introuvable.

- [ ] **Step 3: Write minimal implementation**

`Common/DomainException.cs`:

```csharp
namespace Hominder.Domain.Common;

public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
```

`Maintenance/MaintenanceTaskId.cs`:

```csharp
namespace Hominder.Domain.Maintenance;

public readonly record struct MaintenanceTaskId(Guid Value)
{
    public static MaintenanceTaskId New() => new(Guid.NewGuid());
}
```

`Household/HouseholdMemberId.cs`:

```csharp
namespace Hominder.Domain.Household;

public readonly record struct HouseholdMemberId(Guid Value)
{
    public static HouseholdMemberId New() => new(Guid.NewGuid());
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter StronglyTypedIdTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add strongly-typed ids and domain exception"
```

---

### Task 2: Enums, DueWindow, MaintenanceEvaluation, RecurrencePolicy abstrait

**Files:**
- Create: `backend/Hominder.Domain/Maintenance/RecurrenceUnit.cs`
- Create: `backend/Hominder.Domain/Maintenance/MaintenanceStatus.cs`
- Create: `backend/Hominder.Domain/Maintenance/DueWindow.cs`
- Create: `backend/Hominder.Domain/Maintenance/MaintenanceEvaluation.cs`
- Create: `backend/Hominder.Domain/Maintenance/Policies/RecurrencePolicy.cs`
- Test: `backend/Hominder.Test.Unit/Domain/DueWindowTests.cs`

**Interfaces:**
- Produces:
  - `enum RecurrenceUnit { Days, Weeks, Months, Years }`
  - `enum MaintenanceStatus { Upcoming, Due, Overdue, Done }`
  - `sealed class DueWindow : ValueObject { DateOnly OpenDate; DateOnly DueDate; ctor(open, due) }`
  - `sealed record MaintenanceEvaluation(MaintenanceStatus Status, DueWindow Window, int DaysOverdue)`
  - `abstract class RecurrencePolicy : ValueObject` with:
    - `abstract DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions)`
    - `abstract bool IsTerminal(IReadOnlyList<DateOnly> completions)`
    - `abstract bool RequiresNextDueOverride { get; }`
    - `abstract RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride)`

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Domain;

public class DueWindowTests
{
    [Fact]
    public void Construct_WithOpenAfterDue_Throws()
    {
        var open = new DateOnly(2026, 5, 10);
        var due = new DateOnly(2026, 5, 1);

        Assert.Throws<DomainException>(() => new DueWindow(open, due));
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new DueWindow(new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 31));
        var b = new DueWindow(new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 31));

        Assert.Equal(a, b);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter DueWindowTests`
Expected: FAIL (compilation) — `DueWindow` introuvable.

- [ ] **Step 3: Write minimal implementation**

`Maintenance/RecurrenceUnit.cs`:

```csharp
namespace Hominder.Domain.Maintenance;

public enum RecurrenceUnit
{
    Days,
    Weeks,
    Months,
    Years,
}
```

`Maintenance/MaintenanceStatus.cs`:

```csharp
namespace Hominder.Domain.Maintenance;

public enum MaintenanceStatus
{
    Upcoming,
    Due,
    Overdue,
    Done,
}
```

`Maintenance/DueWindow.cs`:

```csharp
using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance;

public sealed class DueWindow : ValueObject
{
    public DueWindow(DateOnly openDate, DateOnly dueDate)
    {
        if (openDate > dueDate)
        {
            throw new DomainException("La date d'ouverture ne peut pas être postérieure à l'échéance.");
        }

        OpenDate = openDate;
        DueDate = dueDate;
    }

    public DateOnly OpenDate { get; }

    public DateOnly DueDate { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return OpenDate;
        yield return DueDate;
    }
}
```

`Maintenance/MaintenanceEvaluation.cs`:

```csharp
namespace Hominder.Domain.Maintenance;

public sealed record MaintenanceEvaluation(MaintenanceStatus Status, DueWindow Window, int DaysOverdue);
```

`Maintenance/Policies/RecurrencePolicy.cs`:

```csharp
using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public abstract class RecurrencePolicy : ValueObject
{
    public abstract bool RequiresNextDueOverride { get; }

    public abstract DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions);

    public abstract bool IsTerminal(IReadOnlyList<DateOnly> completions);

    public abstract RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter DueWindowTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add recurrence enums, due window, policy base"
```

---

### Task 3: IntervalPolicy

**Files:**
- Create: `backend/Hominder.Domain/Maintenance/Policies/IntervalPolicy.cs`
- Test: `backend/Hominder.Test.Unit/Domain/IntervalPolicyTests.cs`

**Interfaces:**
- Consumes: `RecurrencePolicy`, `RecurrenceUnit`, `DueWindow`, `DomainException`.
- Produces: `sealed class IntervalPolicy(int amount, RecurrenceUnit unit, DateOnly startReference) : RecurrencePolicy` with public `Amount`, `Unit`, `StartReference`.

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class IntervalPolicyTests
{
    private static readonly DateOnly Start = new(2024, 3, 1);

    [Fact]
    public void NextDueWindow_BeforeAnyCompletion_UsesStartReferencePlusInterval()
    {
        var policy = new IntervalPolicy(2, RecurrenceUnit.Years, Start);

        var window = policy.NextDueWindow(new DateOnly(2025, 1, 1), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.DueDate);
        Assert.Equal(window.OpenDate, window.DueDate);
    }

    [Fact]
    public void NextDueWindow_UsesLatestCompletionPlusInterval()
    {
        var policy = new IntervalPolicy(6, RecurrenceUnit.Months, Start);

        var window = policy.NextDueWindow(
            new DateOnly(2026, 1, 1),
            [new DateOnly(2025, 4, 10), new DateOnly(2025, 10, 5)]);

        Assert.Equal(new DateOnly(2026, 4, 5), window.DueDate);
    }

    [Fact]
    public void RequiresNextDueOverride_IsFalse_AndIsNotTerminal()
    {
        var policy = new IntervalPolicy(1, RecurrenceUnit.Weeks, Start);

        Assert.False(policy.RequiresNextDueOverride);
        Assert.False(policy.IsTerminal([new DateOnly(2025, 1, 1)]));
    }

    [Fact]
    public void Construct_WithNonPositiveAmount_Throws()
    {
        Assert.Throws<DomainException>(() => new IntervalPolicy(0, RecurrenceUnit.Days, Start));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter IntervalPolicyTests`
Expected: FAIL (compilation) — `IntervalPolicy` introuvable.

- [ ] **Step 3: Write minimal implementation**

```csharp
using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public sealed class IntervalPolicy : RecurrencePolicy
{
    public IntervalPolicy(int amount, RecurrenceUnit unit, DateOnly startReference)
    {
        if (amount <= 0)
        {
            throw new DomainException("L'intervalle doit être strictement positif.");
        }

        Amount = amount;
        Unit = unit;
        StartReference = startReference;
    }

    public int Amount { get; }

    public RecurrenceUnit Unit { get; }

    public DateOnly StartReference { get; }

    public override bool RequiresNextDueOverride => false;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions)
    {
        var reference = completions.Count > 0 ? completions.Max() : StartReference;
        var due = AddInterval(reference);
        return new DueWindow(due, due);
    }

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => false;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride) => this;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Unit;
        yield return StartReference;
    }

    private DateOnly AddInterval(DateOnly reference) => Unit switch
    {
        RecurrenceUnit.Days => reference.AddDays(Amount),
        RecurrenceUnit.Weeks => reference.AddDays(7 * Amount),
        RecurrenceUnit.Months => reference.AddMonths(Amount),
        RecurrenceUnit.Years => reference.AddYears(Amount),
        _ => throw new DomainException("Unité de récurrence inconnue."),
    };
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter IntervalPolicyTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add interval recurrence policy"
```

---

### Task 4: FixedDatePolicy et OneOffPolicy

**Files:**
- Create: `backend/Hominder.Domain/Maintenance/Policies/FixedDatePolicy.cs`
- Create: `backend/Hominder.Domain/Maintenance/Policies/OneOffPolicy.cs`
- Test: `backend/Hominder.Test.Unit/Domain/FixedDateAndOneOffPolicyTests.cs`

**Interfaces:**
- Produces:
  - `sealed class FixedDatePolicy(DateOnly dueDate) : RecurrencePolicy` — `RequiresNextDueOverride => true`, `WithCompletion` renvoie `new FixedDatePolicy(nextDueOverride.Value)`.
  - `sealed class OneOffPolicy(DateOnly dueDate) : RecurrencePolicy` — `IsTerminal` vrai dès une complétion.

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class FixedDateAndOneOffPolicyTests
{
    [Fact]
    public void FixedDate_WindowIsTheStoredDate()
    {
        var policy = new FixedDatePolicy(new DateOnly(2027, 6, 30));

        var window = policy.NextDueWindow(new DateOnly(2026, 1, 1), []);

        Assert.Equal(new DateOnly(2027, 6, 30), window.OpenDate);
        Assert.Equal(new DateOnly(2027, 6, 30), window.DueDate);
        Assert.True(policy.RequiresNextDueOverride);
    }

    [Fact]
    public void FixedDate_WithCompletion_MovesToOverrideDate()
    {
        var policy = new FixedDatePolicy(new DateOnly(2025, 6, 30));

        var next = policy.WithCompletion(new DateOnly(2025, 6, 20), new DateOnly(2027, 6, 30));

        Assert.Equal(new DateOnly(2027, 6, 30), Assert.IsType<FixedDatePolicy>(next).DueDate);
    }

    [Fact]
    public void FixedDate_WithCompletion_WithoutOverride_Throws()
    {
        var policy = new FixedDatePolicy(new DateOnly(2025, 6, 30));

        Assert.Throws<DomainException>(() => policy.WithCompletion(new DateOnly(2025, 6, 20), null));
    }

    [Fact]
    public void OneOff_BecomesTerminalAfterCompletion()
    {
        var policy = new OneOffPolicy(new DateOnly(2026, 9, 1));

        Assert.False(policy.IsTerminal([]));
        Assert.True(policy.IsTerminal([new DateOnly(2026, 8, 20)]));
        Assert.False(policy.RequiresNextDueOverride);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter FixedDateAndOneOffPolicyTests`
Expected: FAIL (compilation) — `FixedDatePolicy` introuvable.

- [ ] **Step 3: Write minimal implementation**

`FixedDatePolicy.cs`:

```csharp
using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public sealed class FixedDatePolicy : RecurrencePolicy
{
    public FixedDatePolicy(DateOnly dueDate) => DueDate = dueDate;

    public DateOnly DueDate { get; }

    public override bool RequiresNextDueOverride => true;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions) =>
        new(DueDate, DueDate);

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => false;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride)
    {
        if (nextDueOverride is null)
        {
            throw new DomainException("Une échéance à date fixe exige la prochaine date à la complétion.");
        }

        return new FixedDatePolicy(nextDueOverride.Value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DueDate;
    }
}
```

`OneOffPolicy.cs`:

```csharp
namespace Hominder.Domain.Maintenance.Policies;

public sealed class OneOffPolicy : RecurrencePolicy
{
    public OneOffPolicy(DateOnly dueDate) => DueDate = dueDate;

    public DateOnly DueDate { get; }

    public override bool RequiresNextDueOverride => false;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions) =>
        new(DueDate, DueDate);

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => completions.Count > 0;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride) => this;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DueDate;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter FixedDateAndOneOffPolicyTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add fixed-date and one-off recurrence policies"
```

---

### Task 5: MonthWindowPolicy

**Files:**
- Create: `backend/Hominder.Domain/Maintenance/Policies/MonthWindowPolicy.cs`
- Test: `backend/Hominder.Test.Unit/Domain/MonthWindowPolicyTests.cs`

**Interfaces:**
- Produces: `sealed class MonthWindowPolicy(int startMonth, int endMonth) : RecurrencePolicy` — fenêtre annuelle, gère le passage d'année (`endMonth < startMonth`), choisit le cycle courant/suivant selon `today` et la dernière complétion.

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class MonthWindowPolicyTests
{
    private static readonly MonthWindowPolicy Spring = new(3, 5);

    [Fact]
    public void CurrentCycleWindow_SpansStartToEndOfMonths()
    {
        var window = Spring.NextDueWindow(new DateOnly(2026, 4, 15), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2026, 5, 31), window.DueDate);
    }

    [Fact]
    public void BeforeWindowOpens_ReturnsThisYearWindow()
    {
        var window = Spring.NextDueWindow(new DateOnly(2026, 1, 10), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.OpenDate);
    }

    [Fact]
    public void CompletedInsideWindow_MovesToNextYear()
    {
        var window = Spring.NextDueWindow(
            new DateOnly(2026, 4, 20),
            [new DateOnly(2026, 4, 18)]);

        Assert.Equal(new DateOnly(2027, 3, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2027, 5, 31), window.DueDate);
    }

    [Fact]
    public void PastWindowNotCompleted_StaysOnCurrentCycle()
    {
        var window = Spring.NextDueWindow(new DateOnly(2026, 8, 1), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2026, 5, 31), window.DueDate);
    }

    [Fact]
    public void WrapAroundMonths_HandlesYearBoundary()
    {
        var winter = new MonthWindowPolicy(11, 2);

        var window = winter.NextDueWindow(new DateOnly(2026, 1, 15), []);

        Assert.Equal(new DateOnly(2025, 11, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2026, 2, 28), window.DueDate);
    }

    [Fact]
    public void Construct_WithInvalidMonth_Throws()
    {
        Assert.Throws<DomainException>(() => new MonthWindowPolicy(0, 5));
        Assert.Throws<DomainException>(() => new MonthWindowPolicy(3, 13));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter MonthWindowPolicyTests`
Expected: FAIL (compilation) — `MonthWindowPolicy` introuvable.

- [ ] **Step 3: Write minimal implementation**

```csharp
using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public sealed class MonthWindowPolicy : RecurrencePolicy
{
    public MonthWindowPolicy(int startMonth, int endMonth)
    {
        if (startMonth is < 1 or > 12)
        {
            throw new DomainException("Le mois de début doit être compris entre 1 et 12.");
        }

        if (endMonth is < 1 or > 12)
        {
            throw new DomainException("Le mois de fin doit être compris entre 1 et 12.");
        }

        StartMonth = startMonth;
        EndMonth = endMonth;
    }

    public int StartMonth { get; }

    public int EndMonth { get; }

    public override bool RequiresNextDueOverride => false;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions)
    {
        var cycleStartYear = today.Month >= StartMonth ? today.Year : today.Year - 1;
        var current = WindowForCycle(cycleStartYear);

        var lastCompletion = completions.Count > 0 ? completions.Max() : (DateOnly?)null;
        var satisfied = lastCompletion is DateOnly completed && completed >= current.OpenDate;

        return satisfied ? WindowForCycle(cycleStartYear + 1) : current;
    }

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => false;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride) => this;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartMonth;
        yield return EndMonth;
    }

    private DueWindow WindowForCycle(int startYear)
    {
        var open = new DateOnly(startYear, StartMonth, 1);
        var endYear = EndMonth >= StartMonth ? startYear : startYear + 1;
        var due = new DateOnly(endYear, EndMonth, DateTime.DaysInMonth(endYear, EndMonth));
        return new DueWindow(open, due);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter MonthWindowPolicyTests`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add month-window recurrence policy"
```

---

### Task 6: Completion et domain event de complétion

**Files:**
- Create: `backend/Hominder.Domain/Maintenance/Completion.cs`
- Create: `backend/Hominder.Domain/Maintenance/MaintenanceTaskCompletedDomainEvent.cs`
- Test: `backend/Hominder.Test.Unit/Domain/CompletionTests.cs`

**Interfaces:**
- Consumes: `HouseholdMemberId`, `MaintenanceTaskId`, `IDomainEvent`.
- Produces:
  - `sealed class Completion(DateOnly completedOn, HouseholdMemberId completedBy) : ValueObject`.
  - `sealed record MaintenanceTaskCompletedDomainEvent(MaintenanceTaskId TaskId, DateOnly CompletedOn, HouseholdMemberId CompletedBy, DateTime OccurredOnUtc) : IDomainEvent`.

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Domain;

public class CompletionTests
{
    [Fact]
    public void Completions_WithSameValues_AreEqual()
    {
        var member = HouseholdMemberId.New();
        var date = new DateOnly(2026, 4, 18);

        Assert.Equal(new Completion(date, member), new Completion(date, member));
    }

    [Fact]
    public void CompletedEvent_ImplementsDomainEvent()
    {
        var domainEvent = new MaintenanceTaskCompletedDomainEvent(
            MaintenanceTaskId.New(),
            new DateOnly(2026, 4, 18),
            HouseholdMemberId.New(),
            DateTime.UtcNow);

        Assert.IsAssignableFrom<IDomainEvent>(domainEvent);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter CompletionTests`
Expected: FAIL (compilation) — `Completion` introuvable.

- [ ] **Step 3: Write minimal implementation**

`Completion.cs`:

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Household;

namespace Hominder.Domain.Maintenance;

public sealed class Completion : ValueObject
{
    public Completion(DateOnly completedOn, HouseholdMemberId completedBy)
    {
        CompletedOn = completedOn;
        CompletedBy = completedBy;
    }

    public DateOnly CompletedOn { get; }

    public HouseholdMemberId CompletedBy { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CompletedOn;
        yield return CompletedBy;
    }
}
```

`MaintenanceTaskCompletedDomainEvent.cs`:

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Household;

namespace Hominder.Domain.Maintenance;

public sealed record MaintenanceTaskCompletedDomainEvent(
    MaintenanceTaskId TaskId,
    DateOnly CompletedOn,
    HouseholdMemberId CompletedBy,
    DateTime OccurredOnUtc) : IDomainEvent;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter CompletionTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add completion value object and completed domain event"
```

---

### Task 7: Agrégat MaintenanceTask

**Files:**
- Create: `backend/Hominder.Domain/Maintenance/MaintenanceTask.cs`
- Test: `backend/Hominder.Test.Unit/Domain/MaintenanceTaskTests.cs`

**Interfaces:**
- Consumes: `MaintenanceTaskId`, `HouseholdMemberId`, `RecurrencePolicy`, `Completion`, `DueWindow`, `MaintenanceEvaluation`, `MaintenanceStatus`, `MaintenanceTaskCompletedDomainEvent`, `DomainException`, `AggregateRoot<TId>`.
- Produces:
  - `static MaintenanceTask Create(string title, string? notes, RecurrencePolicy policy, HouseholdMemberId? assigneeId)`
  - `void Update(string title, string? notes, RecurrencePolicy policy, HouseholdMemberId? assigneeId)`
  - `void MarkDone(DateOnly completedOn, HouseholdMemberId completedBy, DateOnly? nextDueOverride)`
  - `MaintenanceEvaluation Evaluate(DateOnly today)`
  - Propriétés : `Title`, `Notes`, `Policy`, `AssigneeId`, `IReadOnlyList<Completion> Completions`.

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class MaintenanceTaskTests
{
    private static MaintenanceTask NewSpringTask() =>
        MaintenanceTask.Create("Tailler l'olivier", null, new MonthWindowPolicy(3, 5), null);

    [Fact]
    public void Create_WithBlankTitle_Throws()
    {
        Assert.Throws<DomainException>(() =>
            MaintenanceTask.Create("  ", null, new OneOffPolicy(new DateOnly(2026, 9, 1)), null));
    }

    [Fact]
    public void Evaluate_InsideWindow_IsDue()
    {
        var task = NewSpringTask();

        var evaluation = task.Evaluate(new DateOnly(2026, 4, 15));

        Assert.Equal(MaintenanceStatus.Due, evaluation.Status);
        Assert.Equal(0, evaluation.DaysOverdue);
    }

    [Fact]
    public void Evaluate_BeforeWindow_IsUpcoming()
    {
        var task = MaintenanceTask.Create("CT", null, new FixedDatePolicy(new DateOnly(2027, 6, 30)), null);

        Assert.Equal(MaintenanceStatus.Upcoming, task.Evaluate(new DateOnly(2026, 1, 1)).Status);
    }

    [Fact]
    public void Evaluate_AfterDue_IsOverdueWithDayCount()
    {
        var task = MaintenanceTask.Create("CT", null, new FixedDatePolicy(new DateOnly(2026, 6, 30)), null);

        var evaluation = task.Evaluate(new DateOnly(2026, 7, 10));

        Assert.Equal(MaintenanceStatus.Overdue, evaluation.Status);
        Assert.Equal(10, evaluation.DaysOverdue);
    }

    [Fact]
    public void MarkDone_AddsCompletionAndRaisesEvent()
    {
        var task = NewSpringTask();
        var member = HouseholdMemberId.New();

        task.MarkDone(new DateOnly(2026, 4, 18), member, null);

        Assert.Single(task.Completions);
        Assert.Contains(task.DomainEvents, e => e is MaintenanceTaskCompletedDomainEvent);
    }

    [Fact]
    public void MarkDone_FixedDateWithoutOverride_Throws()
    {
        var task = MaintenanceTask.Create("CT", null, new FixedDatePolicy(new DateOnly(2026, 6, 30)), null);

        Assert.Throws<DomainException>(() =>
            task.MarkDone(new DateOnly(2026, 6, 20), HouseholdMemberId.New(), null));
    }

    [Fact]
    public void MarkDone_IntervalWithOverride_Throws()
    {
        var task = MaintenanceTask.Create(
            "Saturateur", null, new IntervalPolicy(2, RecurrenceUnit.Years, new DateOnly(2024, 5, 1)), null);

        Assert.Throws<DomainException>(() =>
            task.MarkDone(new DateOnly(2026, 5, 1), HouseholdMemberId.New(), new DateOnly(2028, 5, 1)));
    }

    [Fact]
    public void MarkDone_OneOffTwice_Throws()
    {
        var task = MaintenanceTask.Create("Poser étagère", null, new OneOffPolicy(new DateOnly(2026, 9, 1)), null);
        var member = HouseholdMemberId.New();
        task.MarkDone(new DateOnly(2026, 8, 20), member, null);

        Assert.Throws<DomainException>(() => task.MarkDone(new DateOnly(2026, 8, 21), member, null));
    }

    [Fact]
    public void Evaluate_OneOffCompleted_IsDone()
    {
        var task = MaintenanceTask.Create("Poser étagère", null, new OneOffPolicy(new DateOnly(2026, 9, 1)), null);
        task.MarkDone(new DateOnly(2026, 8, 20), HouseholdMemberId.New(), null);

        Assert.Equal(MaintenanceStatus.Done, task.Evaluate(new DateOnly(2026, 9, 5)).Status);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter MaintenanceTaskTests`
Expected: FAIL (compilation) — `MaintenanceTask.Create` introuvable.

- [ ] **Step 3: Write minimal implementation**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Domain.Maintenance;

public sealed class MaintenanceTask : AggregateRoot<MaintenanceTaskId>
{
    private readonly List<Completion> _completions = [];

    private MaintenanceTask(
        MaintenanceTaskId id,
        string title,
        string? notes,
        RecurrencePolicy policy,
        HouseholdMemberId? assigneeId)
        : base(id)
    {
        Title = title;
        Notes = notes;
        Policy = policy;
        AssigneeId = assigneeId;
    }

    public string Title { get; private set; }

    public string? Notes { get; private set; }

    public RecurrencePolicy Policy { get; private set; }

    public HouseholdMemberId? AssigneeId { get; private set; }

    public IReadOnlyList<Completion> Completions => _completions.AsReadOnly();

    public static MaintenanceTask Create(
        string title,
        string? notes,
        RecurrencePolicy policy,
        HouseholdMemberId? assigneeId)
    {
        var cleanTitle = RequireTitle(title);
        return new MaintenanceTask(MaintenanceTaskId.New(), cleanTitle, notes, policy, assigneeId);
    }

    public void Update(string title, string? notes, RecurrencePolicy policy, HouseholdMemberId? assigneeId)
    {
        Title = RequireTitle(title);
        Notes = notes;
        Policy = policy;
        AssigneeId = assigneeId;
    }

    public void MarkDone(DateOnly completedOn, HouseholdMemberId completedBy, DateOnly? nextDueOverride)
    {
        var completedDates = CompletedDates();

        if (Policy.IsTerminal(completedDates))
        {
            throw new DomainException("Cette tâche ponctuelle est déjà terminée.");
        }

        if (Policy.RequiresNextDueOverride && nextDueOverride is null)
        {
            throw new DomainException("Cette tâche exige la prochaine échéance à la complétion.");
        }

        if (!Policy.RequiresNextDueOverride && nextDueOverride is not null)
        {
            throw new DomainException("Cette tâche ne permet pas de saisir la prochaine échéance.");
        }

        _completions.Add(new Completion(completedOn, completedBy));
        Policy = Policy.WithCompletion(completedOn, nextDueOverride);

        RaiseDomainEvent(new MaintenanceTaskCompletedDomainEvent(Id, completedOn, completedBy, DateTime.UtcNow));
    }

    public MaintenanceEvaluation Evaluate(DateOnly today)
    {
        var completedDates = CompletedDates();
        var window = Policy.NextDueWindow(today, completedDates);

        if (Policy.IsTerminal(completedDates))
        {
            return new MaintenanceEvaluation(MaintenanceStatus.Done, window, 0);
        }

        var status = today < window.OpenDate
            ? MaintenanceStatus.Upcoming
            : today <= window.DueDate
                ? MaintenanceStatus.Due
                : MaintenanceStatus.Overdue;

        var daysOverdue = status == MaintenanceStatus.Overdue
            ? today.DayNumber - window.DueDate.DayNumber
            : 0;

        return new MaintenanceEvaluation(status, window, daysOverdue);
    }

    private IReadOnlyList<DateOnly> CompletedDates() =>
        _completions.Select(completion => completion.CompletedOn).ToList();

    private static string RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Le titre est obligatoire.");
        }

        return title.Trim();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter MaintenanceTaskTests`
Expected: PASS (9 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add maintenance task aggregate"
```

---

### Task 8: Agrégat HouseholdMember

**Files:**
- Create: `backend/Hominder.Domain/Household/HouseholdMember.cs`
- Test: `backend/Hominder.Test.Unit/Domain/HouseholdMemberTests.cs`

**Interfaces:**
- Produces: `static HouseholdMember Create(string name)` ; propriété `string Name`.

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Household;

namespace Hominder.Test.Unit.Domain;

public class HouseholdMemberTests
{
    [Fact]
    public void Create_TrimsName()
    {
        var member = HouseholdMember.Create("  Grégory  ");

        Assert.Equal("Grégory", member.Name);
        Assert.NotEqual(Guid.Empty, member.Id.Value);
    }

    [Fact]
    public void Create_WithBlankName_Throws()
    {
        Assert.Throws<DomainException>(() => HouseholdMember.Create("   "));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter HouseholdMemberTests`
Expected: FAIL (compilation) — `HouseholdMember.Create` introuvable.

- [ ] **Step 3: Write minimal implementation**

```csharp
using Hominder.Domain.Common;

namespace Hominder.Domain.Household;

public sealed class HouseholdMember : AggregateRoot<HouseholdMemberId>
{
    private HouseholdMember(HouseholdMemberId id, string name)
        : base(id) => Name = name;

    public string Name { get; private set; }

    public static HouseholdMember Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Le nom du membre est obligatoire.");
        }

        return new HouseholdMember(HouseholdMemberId.New(), name.Trim());
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter HouseholdMemberTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Domain backend/Hominder.Test.Unit
git commit -m "feat(domain): add household member aggregate"
```

---

## Couche Application

### Task 9: Abstractions CQRS, ports de persistance, factory de politique

**Files:**
- Create: `backend/Hominder.Application/Common/Messaging/IBaseCommand.cs`
- Create: `backend/Hominder.Application/Common/Messaging/ICommand.cs`
- Create: `backend/Hominder.Application/Common/Messaging/IQuery.cs`
- Create: `backend/Hominder.Application/Common/Persistence/IUnitOfWork.cs`
- Create: `backend/Hominder.Application/Common/Persistence/IMaintenanceTaskRepository.cs`
- Create: `backend/Hominder.Application/Common/Persistence/IHouseholdMemberRepository.cs`
- Create: `backend/Hominder.Application/Common/Exceptions/NotFoundException.cs`
- Create: `backend/Hominder.Application/Maintenance/RecurrencePolicyInput.cs`
- Create: `backend/Hominder.Application/Maintenance/RecurrencePolicyFactory.cs`
- Test: `backend/Hominder.Test.Unit/Application/RecurrencePolicyFactoryTests.cs`

**Interfaces:**
- Produces:
  - `interface IBaseCommand`
  - `interface ICommand : IRequest, IBaseCommand`
  - `interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand`
  - `interface IQuery<out TResponse> : IRequest<TResponse>`
  - `interface IUnitOfWork { Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default); }`
  - `interface IMaintenanceTaskRepository { AddAsync, GetByIdAsync, GetAllAsync, Remove }`
  - `interface IHouseholdMemberRepository { AddAsync, GetByIdAsync, GetAllAsync, Remove }`
  - `sealed class NotFoundException(string message) : Exception`
  - `enum RecurrenceKind { Interval, MonthWindow, FixedDate, OneOff }`
  - `sealed record RecurrencePolicyInput(RecurrenceKind Kind, int? IntervalAmount, RecurrenceUnit? IntervalUnit, DateOnly? StartReference, int? StartMonth, int? EndMonth, DateOnly? DueDate)`
  - `static class RecurrencePolicyFactory { RecurrencePolicy Create(RecurrencePolicyInput input) }`

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Application.Maintenance;
using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Application;

public class RecurrencePolicyFactoryTests
{
    [Fact]
    public void Create_Interval_BuildsIntervalPolicy()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.Interval, 2, RecurrenceUnit.Years, new DateOnly(2024, 5, 1), null, null, null);

        var policy = RecurrencePolicyFactory.Create(input);

        var interval = Assert.IsType<IntervalPolicy>(policy);
        Assert.Equal(2, interval.Amount);
        Assert.Equal(RecurrenceUnit.Years, interval.Unit);
    }

    [Fact]
    public void Create_MonthWindow_BuildsMonthWindowPolicy()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);

        Assert.IsType<MonthWindowPolicy>(RecurrencePolicyFactory.Create(input));
    }

    [Fact]
    public void Create_FixedDate_BuildsFixedDatePolicy()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.FixedDate, null, null, null, null, null, new DateOnly(2027, 6, 30));

        Assert.IsType<FixedDatePolicy>(RecurrencePolicyFactory.Create(input));
    }

    [Fact]
    public void Create_Interval_WithMissingFields_Throws()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.Interval, null, null, null, null, null, null);

        Assert.Throws<DomainException>(() => RecurrencePolicyFactory.Create(input));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter RecurrencePolicyFactoryTests`
Expected: FAIL (compilation) — `RecurrencePolicyInput` introuvable.

- [ ] **Step 3: Write minimal implementation**

`Common/Messaging/IBaseCommand.cs`:

```csharp
namespace Hominder.Application.Common.Messaging;

public interface IBaseCommand;
```

`Common/Messaging/ICommand.cs`:

```csharp
using MediatR;

namespace Hominder.Application.Common.Messaging;

public interface ICommand : IRequest, IBaseCommand;

public interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand;
```

`Common/Messaging/IQuery.cs`:

```csharp
using MediatR;

namespace Hominder.Application.Common.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
```

`Common/Persistence/IUnitOfWork.cs`:

```csharp
namespace Hominder.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
```

`Common/Persistence/IMaintenanceTaskRepository.cs`:

```csharp
using Hominder.Domain.Maintenance;

namespace Hominder.Application.Common.Persistence;

public interface IMaintenanceTaskRepository
{
    Task AddAsync(MaintenanceTask task, CancellationToken cancellationToken = default);

    Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default);

    void Remove(MaintenanceTask task);
}
```

`Common/Persistence/IHouseholdMemberRepository.cs`:

```csharp
using Hominder.Domain.Household;

namespace Hominder.Application.Common.Persistence;

public interface IHouseholdMemberRepository
{
    Task AddAsync(HouseholdMember member, CancellationToken cancellationToken = default);

    Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default);

    void Remove(HouseholdMember member);
}
```

`Common/Exceptions/NotFoundException.cs`:

```csharp
namespace Hominder.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
```

`Maintenance/RecurrencePolicyInput.cs`:

```csharp
using Hominder.Domain.Maintenance;

namespace Hominder.Application.Maintenance;

public enum RecurrenceKind
{
    Interval,
    MonthWindow,
    FixedDate,
    OneOff,
}

public sealed record RecurrencePolicyInput(
    RecurrenceKind Kind,
    int? IntervalAmount,
    RecurrenceUnit? IntervalUnit,
    DateOnly? StartReference,
    int? StartMonth,
    int? EndMonth,
    DateOnly? DueDate);
```

`Maintenance/RecurrencePolicyFactory.cs`:

```csharp
using Hominder.Domain.Common;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Application.Maintenance;

public static class RecurrencePolicyFactory
{
    public static RecurrencePolicy Create(RecurrencePolicyInput input) => input.Kind switch
    {
        RecurrenceKind.Interval => new IntervalPolicy(
            Required(input.IntervalAmount, "L'intervalle est obligatoire."),
            Required(input.IntervalUnit, "L'unité d'intervalle est obligatoire."),
            Required(input.StartReference, "La date de départ est obligatoire.")),
        RecurrenceKind.MonthWindow => new MonthWindowPolicy(
            Required(input.StartMonth, "Le mois de début est obligatoire."),
            Required(input.EndMonth, "Le mois de fin est obligatoire.")),
        RecurrenceKind.FixedDate => new FixedDatePolicy(
            Required(input.DueDate, "L'échéance est obligatoire.")),
        RecurrenceKind.OneOff => new OneOffPolicy(
            Required(input.DueDate, "L'échéance est obligatoire.")),
        _ => throw new DomainException("Type de récurrence inconnu."),
    };

    private static T Required<T>(T? value, string message)
        where T : struct =>
        value ?? throw new DomainException(message);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter RecurrencePolicyFactoryTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Application backend/Hominder.Test.Unit
git commit -m "feat(application): add cqrs abstractions, persistence ports, policy factory"
```

---

### Task 10: Behaviors MediatR (logging + transaction) et câblage AddApplication

**Files:**
- Create: `backend/Hominder.Application/Common/Behaviors/LoggingBehavior.cs`
- Create: `backend/Hominder.Application/Common/Behaviors/TransactionBehavior.cs`
- Modify: `backend/Hominder.Application/Hominder.Application.csproj` (ajout `Microsoft.Extensions.Logging.Abstractions`)
- Modify: `backend/Hominder.Application/DependencyInjection.cs`
- Test: `backend/Hominder.Test.Unit/Application/TransactionBehaviorTests.cs`

**Interfaces:**
- Consumes: `IBaseCommand`, `IUnitOfWork`, MediatR `IPipelineBehavior`.
- Produces: `LoggingBehavior<TRequest, TResponse>` (toutes requêtes), `TransactionBehavior<TRequest, TResponse> where TRequest : IBaseCommand`.

- [ ] **Step 1: Add the logging package**

Run:
```bash
dotnet add backend/Hominder.Application package Microsoft.Extensions.Logging.Abstractions
```
Expected: référence ajoutée au `.csproj`.

- [ ] **Step 2: Write the failing test**

```csharp
using Hominder.Application.Common.Behaviors;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using MediatR;

namespace Hominder.Test.Unit.Application;

public class TransactionBehaviorTests
{
    private sealed record FakeCommand : ICommand;

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool Executed { get; private set; }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            Executed = true;
            return await operation();
        }
    }

    [Fact]
    public async Task Handle_RunsHandlerInsideTransaction()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var behavior = new TransactionBehavior<FakeCommand, Unit>(unitOfWork);
        var handlerRan = false;

        var result = await behavior.Handle(
            new FakeCommand(),
            _ =>
            {
                handlerRan = true;
                return Task.FromResult(Unit.Value);
            },
            CancellationToken.None);

        Assert.True(unitOfWork.Executed);
        Assert.True(handlerRan);
        Assert.Equal(Unit.Value, result);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter TransactionBehaviorTests`
Expected: FAIL (compilation) — `TransactionBehavior` introuvable.

- [ ] **Step 4: Write minimal implementation**

`Common/Behaviors/LoggingBehavior.cs`:

```csharp
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hominder.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Traitement de {RequestName}", requestName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation(
                "{RequestName} traité en {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(
                exception,
                "{RequestName} a échoué après {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

`Common/Behaviors/TransactionBehavior.cs`:

```csharp
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using MediatR;

namespace Hominder.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        _unitOfWork.ExecuteInTransactionAsync(() => next(), cancellationToken);
}
```

`DependencyInjection.cs` (remplacer le contenu existant) :

```csharp
using Hominder.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Hominder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services;
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter TransactionBehaviorTests`
Expected: PASS (1 test).

- [ ] **Step 6: Commit**

```bash
git add backend/Hominder.Application backend/Hominder.Test.Unit
git commit -m "feat(application): add logging and transaction pipeline behaviors"
```

---

### Task 11: Commands MaintenanceTask (Create/Update/Delete/MarkDone) + doubles de test

**Files:**
- Create: `backend/Hominder.Test.Unit/Application/Fakes/InMemoryMaintenanceTaskRepository.cs`
- Create: `backend/Hominder.Test.Unit/Application/Fakes/InMemoryHouseholdMemberRepository.cs`
- Create: `backend/Hominder.Application/Maintenance/Commands/CreateMaintenanceTaskCommand.cs`
- Create: `backend/Hominder.Application/Maintenance/Commands/UpdateMaintenanceTaskCommand.cs`
- Create: `backend/Hominder.Application/Maintenance/Commands/DeleteMaintenanceTaskCommand.cs`
- Create: `backend/Hominder.Application/Maintenance/Commands/MarkMaintenanceTaskDoneCommand.cs`
- Test: `backend/Hominder.Test.Unit/Application/MaintenanceTaskCommandsTests.cs`

**Interfaces:**
- Consumes: `IMaintenanceTaskRepository`, `RecurrencePolicyFactory`, `RecurrencePolicyInput`, `NotFoundException`, `MaintenanceTask`, `MaintenanceTaskId`, `HouseholdMemberId`.
- Produces:
  - `CreateMaintenanceTaskCommand(string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId) : ICommand<Guid>`
  - `UpdateMaintenanceTaskCommand(Guid TaskId, string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId) : ICommand`
  - `DeleteMaintenanceTaskCommand(Guid TaskId) : ICommand`
  - `MarkMaintenanceTaskDoneCommand(Guid TaskId, DateOnly CompletedOn, Guid CompletedBy, DateOnly? NextDueOverride) : ICommand`
  - Fakes `InMemoryMaintenanceTaskRepository` (propriété `List<MaintenanceTask> Items`), `InMemoryHouseholdMemberRepository` (propriété `List<HouseholdMember> Items`).

- [ ] **Step 1: Write the fakes**

`Application/Fakes/InMemoryMaintenanceTaskRepository.cs`:

```csharp
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Application.Fakes;

public sealed class InMemoryMaintenanceTaskRepository : IMaintenanceTaskRepository
{
    public List<MaintenanceTask> Items { get; } = [];

    public Task AddAsync(MaintenanceTask task, CancellationToken cancellationToken = default)
    {
        Items.Add(task);
        return Task.CompletedTask;
    }

    public Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(task => task.Id == id));

    public Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceTask>>(Items);

    public void Remove(MaintenanceTask task) => Items.Remove(task);
}
```

`Application/Fakes/InMemoryHouseholdMemberRepository.cs`:

```csharp
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;

namespace Hominder.Test.Unit.Application.Fakes;

public sealed class InMemoryHouseholdMemberRepository : IHouseholdMemberRepository
{
    public List<HouseholdMember> Items { get; } = [];

    public Task AddAsync(HouseholdMember member, CancellationToken cancellationToken = default)
    {
        Items.Add(member);
        return Task.CompletedTask;
    }

    public Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(member => member.Id == id));

    public Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HouseholdMember>>(Items);

    public void Remove(HouseholdMember member) => Items.Remove(member);
}
```

- [ ] **Step 2: Write the failing test**

```csharp
using Hominder.Application.Common.Exceptions;
using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Commands;
using Hominder.Domain.Maintenance;
using Hominder.Test.Unit.Application.Fakes;

namespace Hominder.Test.Unit.Application;

public class MaintenanceTaskCommandsTests
{
    private static RecurrencePolicyInput SpringWindow() =>
        new(RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);

    private static RecurrencePolicyInput FixedDate(DateOnly due) =>
        new(RecurrenceKind.FixedDate, null, null, null, null, null, due);

    [Fact]
    public async Task Create_PersistsTaskAndReturnsId()
    {
        var repository = new InMemoryMaintenanceTaskRepository();
        var handler = new CreateMaintenanceTaskHandler(repository);

        var id = await handler.Handle(
            new CreateMaintenanceTaskCommand("Tailler l'olivier", null, SpringWindow(), null),
            CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(id, repository.Items[0].Id.Value);
    }

    [Fact]
    public async Task Update_UnknownTask_Throws()
    {
        var handler = new UpdateMaintenanceTaskHandler(new InMemoryMaintenanceTaskRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateMaintenanceTaskCommand(Guid.NewGuid(), "x", null, SpringWindow(), null),
            CancellationToken.None));
    }

    [Fact]
    public async Task MarkDone_RecordsCompletion()
    {
        var repository = new InMemoryMaintenanceTaskRepository();
        var task = MaintenanceTask.Create("CT", null, RecurrencePolicyFactory.Create(FixedDate(new DateOnly(2026, 6, 30))), null);
        repository.Items.Add(task);
        var handler = new MarkMaintenanceTaskDoneHandler(repository);

        await handler.Handle(
            new MarkMaintenanceTaskDoneCommand(task.Id.Value, new DateOnly(2026, 6, 20), Guid.NewGuid(), new DateOnly(2028, 6, 30)),
            CancellationToken.None);

        Assert.Single(task.Completions);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var repository = new InMemoryMaintenanceTaskRepository();
        var task = MaintenanceTask.Create("x", null, RecurrencePolicyFactory.Create(SpringWindow()), null);
        repository.Items.Add(task);
        var handler = new DeleteMaintenanceTaskHandler(repository);

        await handler.Handle(new DeleteMaintenanceTaskCommand(task.Id.Value), CancellationToken.None);

        Assert.Empty(repository.Items);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter MaintenanceTaskCommandsTests`
Expected: FAIL (compilation) — `CreateMaintenanceTaskHandler` introuvable.

- [ ] **Step 4: Write minimal implementation**

`Maintenance/Commands/CreateMaintenanceTaskCommand.cs`:

```csharp
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record CreateMaintenanceTaskCommand(
    string Title,
    string? Notes,
    RecurrencePolicyInput Policy,
    Guid? AssigneeId) : ICommand<Guid>;

public sealed class CreateMaintenanceTaskHandler : IRequestHandler<CreateMaintenanceTaskCommand, Guid>
{
    private readonly IMaintenanceTaskRepository _repository;

    public CreateMaintenanceTaskHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateMaintenanceTaskCommand request, CancellationToken cancellationToken)
    {
        var policy = RecurrencePolicyFactory.Create(request.Policy);
        var assignee = request.AssigneeId is Guid value ? new HouseholdMemberId(value) : (HouseholdMemberId?)null;
        var task = MaintenanceTask.Create(request.Title, request.Notes, policy, assignee);
        await _repository.AddAsync(task, cancellationToken);
        return task.Id.Value;
    }
}
```

`Maintenance/Commands/UpdateMaintenanceTaskCommand.cs`:

```csharp
using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record UpdateMaintenanceTaskCommand(
    Guid TaskId,
    string Title,
    string? Notes,
    RecurrencePolicyInput Policy,
    Guid? AssigneeId) : ICommand;

public sealed class UpdateMaintenanceTaskHandler : IRequestHandler<UpdateMaintenanceTaskCommand>
{
    private readonly IMaintenanceTaskRepository _repository;

    public UpdateMaintenanceTaskHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task Handle(UpdateMaintenanceTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new MaintenanceTaskId(request.TaskId), cancellationToken)
            ?? throw new NotFoundException("Tâche introuvable.");
        var policy = RecurrencePolicyFactory.Create(request.Policy);
        var assignee = request.AssigneeId is Guid value ? new HouseholdMemberId(value) : (HouseholdMemberId?)null;
        task.Update(request.Title, request.Notes, policy, assignee);
    }
}
```

`Maintenance/Commands/DeleteMaintenanceTaskCommand.cs`:

```csharp
using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record DeleteMaintenanceTaskCommand(Guid TaskId) : ICommand;

public sealed class DeleteMaintenanceTaskHandler : IRequestHandler<DeleteMaintenanceTaskCommand>
{
    private readonly IMaintenanceTaskRepository _repository;

    public DeleteMaintenanceTaskHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task Handle(DeleteMaintenanceTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new MaintenanceTaskId(request.TaskId), cancellationToken)
            ?? throw new NotFoundException("Tâche introuvable.");
        _repository.Remove(task);
    }
}
```

`Maintenance/Commands/MarkMaintenanceTaskDoneCommand.cs`:

```csharp
using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Commands;

public sealed record MarkMaintenanceTaskDoneCommand(
    Guid TaskId,
    DateOnly CompletedOn,
    Guid CompletedBy,
    DateOnly? NextDueOverride) : ICommand;

public sealed class MarkMaintenanceTaskDoneHandler : IRequestHandler<MarkMaintenanceTaskDoneCommand>
{
    private readonly IMaintenanceTaskRepository _repository;

    public MarkMaintenanceTaskDoneHandler(IMaintenanceTaskRepository repository) => _repository = repository;

    public async Task Handle(MarkMaintenanceTaskDoneCommand request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(new MaintenanceTaskId(request.TaskId), cancellationToken)
            ?? throw new NotFoundException("Tâche introuvable.");
        task.MarkDone(request.CompletedOn, new HouseholdMemberId(request.CompletedBy), request.NextDueOverride);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter MaintenanceTaskCommandsTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add backend/Hominder.Application backend/Hominder.Test.Unit
git commit -m "feat(application): add maintenance task commands"
```

---

### Task 12: Query GetMaintenanceTasks + vues

**Files:**
- Create: `backend/Hominder.Application/Maintenance/Queries/MaintenanceTaskView.cs`
- Create: `backend/Hominder.Application/Maintenance/Queries/GetMaintenanceTasksQuery.cs`
- Test: `backend/Hominder.Test.Unit/Application/GetMaintenanceTasksHandlerTests.cs`

**Interfaces:**
- Consumes: `IMaintenanceTaskRepository`, `IHouseholdMemberRepository`, `TimeProvider`, `MaintenanceStatus`, `MaintenanceTask.Evaluate`.
- Produces:
  - `sealed record MaintenanceTaskView(Guid Id, string Title, string? Notes, string Status, DateOnly OpenDate, DateOnly DueDate, int DaysOverdue, Guid? AssigneeId, string? AssigneeName, bool RequiresNextDueOverride)`
  - `sealed record GetMaintenanceTasksQuery : IQuery<IReadOnlyList<MaintenanceTaskView>>`
  - `GetMaintenanceTasksHandler(IMaintenanceTaskRepository, IHouseholdMemberRepository, TimeProvider)`

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Queries;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Test.Unit.Application.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace Hominder.Test.Unit.Application;

public class GetMaintenanceTasksHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 8, 0, 0, TimeSpan.Zero);

    private static RecurrencePolicyInput FixedDate(DateOnly due) =>
        new(RecurrenceKind.FixedDate, null, null, null, null, null, due);

    [Fact]
    public async Task Handle_OrdersOverdueBeforeUpcoming_AndResolvesAssignee()
    {
        var tasks = new InMemoryMaintenanceTaskRepository();
        var members = new InMemoryHouseholdMemberRepository();
        var member = HouseholdMember.Create("Grégory");
        members.Items.Add(member);

        var upcoming = MaintenanceTask.Create(
            "Futur", null, RecurrencePolicyFactory.Create(FixedDate(new DateOnly(2027, 1, 1))), member.Id);
        var overdue = MaintenanceTask.Create(
            "En retard", null, RecurrencePolicyFactory.Create(FixedDate(new DateOnly(2026, 6, 1))), null);
        tasks.Items.Add(upcoming);
        tasks.Items.Add(overdue);

        var handler = new GetMaintenanceTasksHandler(tasks, members, new FakeTimeProvider(Now));

        var result = await handler.Handle(new GetMaintenanceTasksQuery(), CancellationToken.None);

        Assert.Equal("En retard", result[0].Title);
        Assert.Equal("Overdue", result[0].Status);
        Assert.Equal("Grégory", result[1].AssigneeName);
    }
}
```

- [ ] **Step 2: Add the test-time TimeProvider package**

Run:
```bash
dotnet add backend/Hominder.Test.Unit package Microsoft.Extensions.TimeProvider.Testing
```
Expected: référence ajoutée (fournit `FakeTimeProvider`).

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter GetMaintenanceTasksHandlerTests`
Expected: FAIL (compilation) — `GetMaintenanceTasksHandler` introuvable.

- [ ] **Step 4: Write minimal implementation**

`Maintenance/Queries/MaintenanceTaskView.cs`:

```csharp
namespace Hominder.Application.Maintenance.Queries;

public sealed record MaintenanceTaskView(
    Guid Id,
    string Title,
    string? Notes,
    string Status,
    DateOnly OpenDate,
    DateOnly DueDate,
    int DaysOverdue,
    Guid? AssigneeId,
    string? AssigneeName,
    bool RequiresNextDueOverride);
```

`Maintenance/Queries/GetMaintenanceTasksQuery.cs`:

```csharp
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;

namespace Hominder.Application.Maintenance.Queries;

public sealed record GetMaintenanceTasksQuery : IQuery<IReadOnlyList<MaintenanceTaskView>>;

public sealed class GetMaintenanceTasksHandler
    : IRequestHandler<GetMaintenanceTasksQuery, IReadOnlyList<MaintenanceTaskView>>
{
    private readonly IMaintenanceTaskRepository _tasks;
    private readonly IHouseholdMemberRepository _members;
    private readonly TimeProvider _timeProvider;

    public GetMaintenanceTasksHandler(
        IMaintenanceTaskRepository tasks,
        IHouseholdMemberRepository members,
        TimeProvider timeProvider)
    {
        _tasks = tasks;
        _members = members;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<MaintenanceTaskView>> Handle(
        GetMaintenanceTasksQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var tasks = await _tasks.GetAllAsync(cancellationToken);
        var names = (await _members.GetAllAsync(cancellationToken))
            .ToDictionary(member => member.Id, member => member.Name);

        return tasks
            .Select(task => ToView(task, today, names))
            .OrderBy(view => UrgencyRank(view.Status))
            .ThenBy(view => view.DueDate)
            .ToList();
    }

    private static MaintenanceTaskView ToView(
        MaintenanceTask task, DateOnly today, IReadOnlyDictionary<HouseholdMemberId, string> names)
    {
        var evaluation = task.Evaluate(today);
        var assigneeName = task.AssigneeId is HouseholdMemberId id && names.TryGetValue(id, out var name)
            ? name
            : null;

        return new MaintenanceTaskView(
            task.Id.Value,
            task.Title,
            task.Notes,
            evaluation.Status.ToString(),
            evaluation.Window.OpenDate,
            evaluation.Window.DueDate,
            evaluation.DaysOverdue,
            task.AssigneeId?.Value,
            assigneeName,
            task.Policy.RequiresNextDueOverride);
    }

    private static int UrgencyRank(string status) => status switch
    {
        nameof(MaintenanceStatus.Overdue) => 0,
        nameof(MaintenanceStatus.Due) => 1,
        nameof(MaintenanceStatus.Upcoming) => 2,
        _ => 3,
    };
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter GetMaintenanceTasksHandlerTests`
Expected: PASS (1 test).

- [ ] **Step 6: Commit**

```bash
git add backend/Hominder.Application backend/Hominder.Test.Unit
git commit -m "feat(application): add maintenance tasks query"
```

---

### Task 13: Commands et query HouseholdMember

**Files:**
- Create: `backend/Hominder.Application/Household/Commands/CreateHouseholdMemberCommand.cs`
- Create: `backend/Hominder.Application/Household/Commands/DeleteHouseholdMemberCommand.cs`
- Create: `backend/Hominder.Application/Household/Queries/HouseholdMemberView.cs`
- Create: `backend/Hominder.Application/Household/Queries/GetHouseholdMembersQuery.cs`
- Test: `backend/Hominder.Test.Unit/Application/HouseholdMemberFeaturesTests.cs`

**Interfaces:**
- Produces:
  - `CreateHouseholdMemberCommand(string Name) : ICommand<Guid>`
  - `DeleteHouseholdMemberCommand(Guid MemberId) : ICommand`
  - `sealed record HouseholdMemberView(Guid Id, string Name)`
  - `GetHouseholdMembersQuery : IQuery<IReadOnlyList<HouseholdMemberView>>`

- [ ] **Step 1: Write the failing test**

```csharp
using Hominder.Application.Common.Exceptions;
using Hominder.Application.Household.Commands;
using Hominder.Application.Household.Queries;
using Hominder.Domain.Household;
using Hominder.Test.Unit.Application.Fakes;

namespace Hominder.Test.Unit.Application;

public class HouseholdMemberFeaturesTests
{
    [Fact]
    public async Task Create_PersistsMember()
    {
        var repository = new InMemoryHouseholdMemberRepository();
        var handler = new CreateHouseholdMemberHandler(repository);

        var id = await handler.Handle(new CreateHouseholdMemberCommand("Grégory"), CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(id, repository.Items[0].Id.Value);
    }

    [Fact]
    public async Task Delete_UnknownMember_Throws()
    {
        var handler = new DeleteHouseholdMemberHandler(new InMemoryHouseholdMemberRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new DeleteHouseholdMemberCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Get_ReturnsAllMembers()
    {
        var repository = new InMemoryHouseholdMemberRepository();
        repository.Items.Add(HouseholdMember.Create("Grégory"));
        var handler = new GetHouseholdMembersHandler(repository);

        var result = await handler.Handle(new GetHouseholdMembersQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Grégory", result[0].Name);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Unit --filter HouseholdMemberFeaturesTests`
Expected: FAIL (compilation) — `CreateHouseholdMemberHandler` introuvable.

- [ ] **Step 3: Write minimal implementation**

`Household/Commands/CreateHouseholdMemberCommand.cs`:

```csharp
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using MediatR;

namespace Hominder.Application.Household.Commands;

public sealed record CreateHouseholdMemberCommand(string Name) : ICommand<Guid>;

public sealed class CreateHouseholdMemberHandler : IRequestHandler<CreateHouseholdMemberCommand, Guid>
{
    private readonly IHouseholdMemberRepository _repository;

    public CreateHouseholdMemberHandler(IHouseholdMemberRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        var member = HouseholdMember.Create(request.Name);
        await _repository.AddAsync(member, cancellationToken);
        return member.Id.Value;
    }
}
```

`Household/Commands/DeleteHouseholdMemberCommand.cs`:

```csharp
using Hominder.Application.Common.Exceptions;
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using MediatR;

namespace Hominder.Application.Household.Commands;

public sealed record DeleteHouseholdMemberCommand(Guid MemberId) : ICommand;

public sealed class DeleteHouseholdMemberHandler : IRequestHandler<DeleteHouseholdMemberCommand>
{
    private readonly IHouseholdMemberRepository _repository;

    public DeleteHouseholdMemberHandler(IHouseholdMemberRepository repository) => _repository = repository;

    public async Task Handle(DeleteHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _repository.GetByIdAsync(new HouseholdMemberId(request.MemberId), cancellationToken)
            ?? throw new NotFoundException("Membre introuvable.");
        _repository.Remove(member);
    }
}
```

`Household/Queries/HouseholdMemberView.cs`:

```csharp
namespace Hominder.Application.Household.Queries;

public sealed record HouseholdMemberView(Guid Id, string Name);
```

`Household/Queries/GetHouseholdMembersQuery.cs`:

```csharp
using Hominder.Application.Common.Messaging;
using Hominder.Application.Common.Persistence;
using MediatR;

namespace Hominder.Application.Household.Queries;

public sealed record GetHouseholdMembersQuery : IQuery<IReadOnlyList<HouseholdMemberView>>;

public sealed class GetHouseholdMembersHandler
    : IRequestHandler<GetHouseholdMembersQuery, IReadOnlyList<HouseholdMemberView>>
{
    private readonly IHouseholdMemberRepository _repository;

    public GetHouseholdMembersHandler(IHouseholdMemberRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<HouseholdMemberView>> Handle(
        GetHouseholdMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _repository.GetAllAsync(cancellationToken);
        return members
            .Select(member => new HouseholdMemberView(member.Id.Value, member.Name))
            .OrderBy(view => view.Name)
            .ToList();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Unit --filter HouseholdMemberFeaturesTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/Hominder.Application backend/Hominder.Test.Unit
git commit -m "feat(application): add household member commands and query"
```

---

## Couche Infrastructure

### Task 14: DbContext, configurations EF, converter jsonb, factory design-time

**Files:**
- Modify: `backend/Hominder.Infrastructure/Hominder.Infrastructure.csproj` (packages Npgsql + EF Design)
- Create: `backend/Hominder.Infrastructure/Persistence/Serialization/RecurrencePolicyJsonConverter.cs`
- Create: `backend/Hominder.Infrastructure/Persistence/Serialization/StronglyTypedIdConverters.cs`
- Create: `backend/Hominder.Infrastructure/Persistence/Configurations/MaintenanceTaskConfiguration.cs`
- Create: `backend/Hominder.Infrastructure/Persistence/Configurations/HouseholdMemberConfiguration.cs`
- Create: `backend/Hominder.Infrastructure/Persistence/HominderDbContext.cs`
- Create: `backend/Hominder.Infrastructure/Persistence/HominderDbContextFactory.cs`
- Modify: `backend/Hominder.Test.Integration/Hominder.Test.Integration.csproj` (référence projet Infrastructure)
- Test: `backend/Hominder.Test.Integration/Persistence/DbContextModelTests.cs`

**Interfaces:**
- Consumes: `MaintenanceTask`, `HouseholdMember`, `RecurrencePolicy` + variantes, `IUnitOfWork`.
- Produces: `sealed class HominderDbContext : DbContext, IUnitOfWork` avec `DbSet<MaintenanceTask> MaintenanceTasks`, `DbSet<HouseholdMember> HouseholdMembers` ; converters ; `HominderDbContextFactory`.

- [ ] **Step 1: Add EF packages to Infrastructure**

Run:
```bash
dotnet add backend/Hominder.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add backend/Hominder.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add backend/Hominder.Infrastructure package Microsoft.Extensions.Configuration.Abstractions
```
Expected: trois références ajoutées.

- [ ] **Step 2: Reference Infrastructure from the integration test project**

Run:
```bash
dotnet add backend/Hominder.Test.Integration reference backend/Hominder.Infrastructure
```
Expected: référence projet ajoutée.

- [ ] **Step 3: Write the failing test**

`Persistence/DbContextModelTests.cs`:

```csharp
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Test.Integration.Persistence;

public class DbContextModelTests
{
    [Fact]
    public void Model_MapsAggregatesAndJsonbPolicy()
    {
        var options = new DbContextOptionsBuilder<HominderDbContext>()
            .UseNpgsql("Host=localhost;Database=hominder;Username=hominder;Password=hominder")
            .Options;

        using var context = new HominderDbContext(options);
        var entity = context.Model.FindEntityType(typeof(MaintenanceTask));

        Assert.NotNull(entity);
        Assert.NotNull(context.Model.FindEntityType(typeof(HouseholdMember)));
        Assert.Equal("jsonb", entity!.FindProperty(nameof(MaintenanceTask.Policy))!.GetColumnType());
    }
}
```

- [ ] **Step 4: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Integration --filter DbContextModelTests`
Expected: FAIL (compilation) — `HominderDbContext` introuvable.

- [ ] **Step 5: Write minimal implementation**

`Persistence/Serialization/StronglyTypedIdConverters.cs`:

```csharp
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hominder.Infrastructure.Persistence.Serialization;

public sealed class MaintenanceTaskIdConverter : ValueConverter<MaintenanceTaskId, Guid>
{
    public MaintenanceTaskIdConverter()
        : base(id => id.Value, value => new MaintenanceTaskId(value))
    {
    }
}

public sealed class HouseholdMemberIdConverter : ValueConverter<HouseholdMemberId, Guid>
{
    public HouseholdMemberIdConverter()
        : base(id => id.Value, value => new HouseholdMemberId(value))
    {
    }
}
```

`Persistence/Serialization/RecurrencePolicyJsonConverter.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Infrastructure.Persistence.Serialization;

public sealed class RecurrencePolicyJsonConverter : JsonConverter<RecurrencePolicy>
{
    public override RecurrencePolicy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var kind = root.GetProperty("kind").GetString();

        return kind switch
        {
            "interval" => new IntervalPolicy(
                root.GetProperty("amount").GetInt32(),
                Enum.Parse<RecurrenceUnit>(root.GetProperty("unit").GetString()!),
                DateOnly.Parse(root.GetProperty("startReference").GetString()!)),
            "monthWindow" => new MonthWindowPolicy(
                root.GetProperty("startMonth").GetInt32(),
                root.GetProperty("endMonth").GetInt32()),
            "fixedDate" => new FixedDatePolicy(DateOnly.Parse(root.GetProperty("dueDate").GetString()!)),
            "oneOff" => new OneOffPolicy(DateOnly.Parse(root.GetProperty("dueDate").GetString()!)),
            _ => throw new JsonException($"Politique de récurrence inconnue: {kind}"),
        };
    }

    public override void Write(Utf8JsonWriter writer, RecurrencePolicy value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case IntervalPolicy interval:
                writer.WriteString("kind", "interval");
                writer.WriteNumber("amount", interval.Amount);
                writer.WriteString("unit", interval.Unit.ToString());
                writer.WriteString("startReference", interval.StartReference.ToString("O"));
                break;
            case MonthWindowPolicy monthWindow:
                writer.WriteString("kind", "monthWindow");
                writer.WriteNumber("startMonth", monthWindow.StartMonth);
                writer.WriteNumber("endMonth", monthWindow.EndMonth);
                break;
            case FixedDatePolicy fixedDate:
                writer.WriteString("kind", "fixedDate");
                writer.WriteString("dueDate", fixedDate.DueDate.ToString("O"));
                break;
            case OneOffPolicy oneOff:
                writer.WriteString("kind", "oneOff");
                writer.WriteString("dueDate", oneOff.DueDate.ToString("O"));
                break;
            default:
                throw new JsonException($"Politique de récurrence non supportée: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}
```

`Persistence/Configurations/MaintenanceTaskConfiguration.cs`:

```csharp
using System.Text.Json;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;
using Hominder.Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hominder.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceTaskConfiguration : IEntityTypeConfiguration<MaintenanceTask>
{
    private static readonly JsonSerializerOptions PolicyJsonOptions = CreatePolicyJsonOptions();

    public void Configure(EntityTypeBuilder<MaintenanceTask> builder)
    {
        builder.ToTable("maintenance_tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id)
            .HasConversion(new MaintenanceTaskIdConverter())
            .ValueGeneratedNever();

        builder.Property(task => task.Title).IsRequired();
        builder.Property(task => task.Notes);

        builder.Property(task => task.AssigneeId)
            .HasConversion(new HouseholdMemberIdConverter());

        builder.Property(task => task.Policy)
            .HasColumnType("jsonb")
            .HasConversion(
                policy => JsonSerializer.Serialize(policy, PolicyJsonOptions),
                json => JsonSerializer.Deserialize<RecurrencePolicy>(json, PolicyJsonOptions)!);

        builder.OwnsMany(task => task.Completions, completions =>
        {
            completions.ToTable("maintenance_task_completions");
            completions.WithOwner().HasForeignKey("MaintenanceTaskId");
            completions.Property<int>("Id");
            completions.HasKey("Id");
            completions.Property(completion => completion.CompletedOn);
            completions.Property(completion => completion.CompletedBy)
                .HasConversion(new HouseholdMemberIdConverter());
        });

        builder.Navigation(task => task.Completions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static JsonSerializerOptions CreatePolicyJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new RecurrencePolicyJsonConverter());
        return options;
    }
}
```

`Persistence/Configurations/HouseholdMemberConfiguration.cs`:

```csharp
using Hominder.Domain.Household;
using Hominder.Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hominder.Infrastructure.Persistence.Configurations;

public sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ToTable("household_members");

        builder.HasKey(member => member.Id);
        builder.Property(member => member.Id)
            .HasConversion(new HouseholdMemberIdConverter())
            .ValueGeneratedNever();

        builder.Property(member => member.Name).IsRequired();
    }
}
```

`Persistence/HominderDbContext.cs`:

```csharp
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence;

public sealed class HominderDbContext : DbContext, IUnitOfWork
{
    public HominderDbContext(DbContextOptions<HominderDbContext> options)
        : base(options)
    {
    }

    public DbSet<MaintenanceTask> MaintenanceTasks => Set<MaintenanceTask>();

    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation();
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HominderDbContext).Assembly);
}
```

`Persistence/HominderDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hominder.Infrastructure.Persistence;

public sealed class HominderDbContextFactory : IDesignTimeDbContextFactory<HominderDbContext>
{
    public HominderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HominderDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=hominder;Username=hominder;Password=hominder")
            .Options;

        return new HominderDbContext(options);
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Integration --filter DbContextModelTests`
Expected: PASS (1 test).

- [ ] **Step 7: Commit**

```bash
git add backend/Hominder.Infrastructure backend/Hominder.Test.Integration
git commit -m "feat(infrastructure): add ef dbcontext, configurations, jsonb policy converter"
```

---

### Task 15: Repositories, AddInfrastructure, migration initiale

**Files:**
- Create: `backend/Hominder.Infrastructure/Persistence/Repositories/MaintenanceTaskRepository.cs`
- Create: `backend/Hominder.Infrastructure/Persistence/Repositories/HouseholdMemberRepository.cs`
- Create: `backend/Hominder.Infrastructure/DependencyInjection.cs`
- Create: `backend/Hominder.Infrastructure/Persistence/Migrations/*` (généré)
- Modify: `backend/Hominder.Api/Hominder.Api.csproj` (package EF Design pour l'outillage)

**Interfaces:**
- Produces: `MaintenanceTaskRepository`, `HouseholdMemberRepository`, `static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)` (chaîne de connexion nommée `Hominder`).

- [ ] **Step 1: Write the repositories**

`Persistence/Repositories/MaintenanceTaskRepository.cs`:

```csharp
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceTaskRepository : IMaintenanceTaskRepository
{
    private readonly HominderDbContext _context;

    public MaintenanceTaskRepository(HominderDbContext context) => _context = context;

    public async Task AddAsync(MaintenanceTask task, CancellationToken cancellationToken = default) =>
        await _context.MaintenanceTasks.AddAsync(task, cancellationToken);

    public Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default) =>
        _context.MaintenanceTasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.MaintenanceTasks.ToListAsync(cancellationToken);

    public void Remove(MaintenanceTask task) => _context.MaintenanceTasks.Remove(task);
}
```

`Persistence/Repositories/HouseholdMemberRepository.cs`:

```csharp
using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence.Repositories;

public sealed class HouseholdMemberRepository : IHouseholdMemberRepository
{
    private readonly HominderDbContext _context;

    public HouseholdMemberRepository(HominderDbContext context) => _context = context;

    public async Task AddAsync(HouseholdMember member, CancellationToken cancellationToken = default) =>
        await _context.HouseholdMembers.AddAsync(member, cancellationToken);

    public Task<HouseholdMember?> GetByIdAsync(HouseholdMemberId id, CancellationToken cancellationToken = default) =>
        _context.HouseholdMembers.FirstOrDefaultAsync(member => member.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HouseholdMember>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.HouseholdMembers.ToListAsync(cancellationToken);

    public void Remove(HouseholdMember member) => _context.HouseholdMembers.Remove(member);
}
```

`DependencyInjection.cs`:

```csharp
using Hominder.Application.Common.Persistence;
using Hominder.Infrastructure.Persistence;
using Hominder.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hominder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Hominder")
            ?? throw new InvalidOperationException("La chaîne de connexion 'Hominder' est absente.");

        services.AddDbContext<HominderDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<HominderDbContext>());
        services.AddScoped<IMaintenanceTaskRepository, MaintenanceTaskRepository>();
        services.AddScoped<IHouseholdMemberRepository, HouseholdMemberRepository>();

        return services;
    }
}
```

- [ ] **Step 2: Add EF Design to the Api startup project**

Run:
```bash
dotnet add backend/Hominder.Api package Microsoft.EntityFrameworkCore.Design
```
Expected: référence ajoutée (nécessaire à `dotnet ef` côté startup project).

- [ ] **Step 3: Ensure the EF CLI is available (repo-local tool manifest)**

Run:
```bash
dotnet new tool-manifest --force
dotnet tool install dotnet-ef
```
Expected: `dotnet-ef` installé dans `.config/dotnet-tools.json`.

- [ ] **Step 4: Generate the initial migration**

Run:
```bash
dotnet ef migrations add InitialCreate \
  --project backend/Hominder.Infrastructure \
  --startup-project backend/Hominder.Api \
  --output-dir Persistence/Migrations
```
Expected: fichiers de migration générés sous `backend/Hominder.Infrastructure/Persistence/Migrations/`, tables `maintenance_tasks` (colonne `Policy` en `jsonb`), `maintenance_task_completions`, `household_members`.

- [ ] **Step 5: Verify the solution builds**

Run: `dotnet build backend/Hominder.slnx`
Expected: build réussie, zéro warning.

- [ ] **Step 6: Commit**

```bash
git add backend/Hominder.Infrastructure backend/Hominder.Api .config
git commit -m "feat(infrastructure): add repositories, di wiring, initial migration"
```

---

### Task 16: Service PostgreSQL (docker-compose) et chaîne de connexion

**Files:**
- Create: `docker-compose.yml`
- Modify: `backend/Hominder.Api/appsettings.Development.json`
- Modify: `backend/Hominder.Api/appsettings.json`

- [ ] **Step 1: Create the compose file**

`docker-compose.yml`:

```yaml
services:
  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: hominder
      POSTGRES_USER: hominder
      POSTGRES_PASSWORD: hominder
    ports:
      - "5432:5432"
    volumes:
      - hominder-db:/var/lib/postgresql/data

volumes:
  hominder-db:
```

- [ ] **Step 2: Add the connection string (development)**

`appsettings.Development.json` (remplacer le contenu) :

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Hominder": "Host=localhost;Port=5432;Database=hominder;Username=hominder;Password=hominder"
  }
}
```

- [ ] **Step 3: Declare the connection string key (base settings)**

`appsettings.json` (remplacer le contenu) :

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Hominder": ""
  }
}
```

- [ ] **Step 4: Start the database and confirm it is reachable**

Run:
```bash
colima start
docker compose up -d db
docker compose exec db pg_isready -U hominder
```
Expected: `accepting connections`.

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml backend/Hominder.Api/appsettings.json backend/Hominder.Api/appsettings.Development.json
git commit -m "chore(infra): add postgres compose service and connection string"
```

---

## Couche API

### Task 17: Composition root, gestion d'erreurs, CORS, fixture d'intégration

**Files:**
- Create: `backend/Hominder.Api/ExceptionHandling/DomainExceptionHandler.cs`
- Modify: `backend/Hominder.Api/Program.cs` (réécriture complète, suppression WeatherForecast)
- Modify: `backend/Hominder.Test.Integration/Hominder.Test.Integration.csproj` (package Testcontainers)
- Create: `backend/Hominder.Test.Integration/HominderApiFactory.cs`
- Test: `backend/Hominder.Test.Integration/ApiSmokeTests.cs`

**Interfaces:**
- Consumes: `AddApplication`, `AddInfrastructure`, `HominderDbContext`, `NotFoundException`, `DomainException`.
- Produces: `Program` (partial, exposé aux tests), `DomainExceptionHandler`, `HominderApiFactory` (WebApplicationFactory + conteneur Postgres).

- [ ] **Step 1: Add Testcontainers to the integration test project**

Run:
```bash
dotnet add backend/Hominder.Test.Integration package Testcontainers.PostgreSql
```
Expected: référence ajoutée.

- [ ] **Step 2: Write the exception handler**

`ExceptionHandling/DomainExceptionHandler.cs`:

```csharp
using Hominder.Application.Common.Exceptions;
using Hominder.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Hominder.Api.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Ressource introuvable"),
            DomainException => (StatusCodes.Status400BadRequest, "Requête invalide"),
            _ => (StatusCodes.Status500InternalServerError, "Erreur interne"),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = statusCode, Title = title, Detail = exception.Message },
            cancellationToken);

        return true;
    }
}
```

- [ ] **Step 3: Rewrite Program.cs**

`Program.cs` (remplacer tout le fichier) :

```csharp
using System.Text.Json.Serialization;
using Autofac.Extensions.DependencyInjection;
using Hominder.Api.ExceptionHandling;
using Hominder.Application;
using Hominder.Infrastructure;
using Hominder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

const string FrontendCorsPolicy = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(
        FrontendCorsPolicy,
        policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<HominderDbContext>();
    database.Database.Migrate();
}

app.UseExceptionHandler();
app.UseCors(FrontendCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
```

- [ ] **Step 4: Write the integration test factory**

`HominderApiFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Hominder.Test.Integration;

public sealed class HominderApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Hominder", _database.GetConnectionString());

    async Task IAsyncLifetime.InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _database.DisposeAsync();
}
```

- [ ] **Step 5: Write the failing smoke test**

`ApiSmokeTests.cs`:

```csharp
namespace Hominder.Test.Integration;

public class ApiSmokeTests : IClassFixture<HominderApiFactory>
{
    private readonly HominderApiFactory _factory;

    public ApiSmokeTests(HominderApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
```

- [ ] **Step 6: Run the smoke test (requires colima/Docker running)**

Run:
```bash
colima start
dotnet test backend/Hominder.Test.Integration --filter ApiSmokeTests
```
Expected: PASS (1 test). Le conteneur Postgres démarre, la migration s'applique, `/health` répond 200.

- [ ] **Step 7: Commit**

```bash
git add backend/Hominder.Api backend/Hominder.Test.Integration
git commit -m "feat(api): add composition root, exception handling, cors, integration fixture"
```

---

### Task 18: Endpoints des tâches d'entretien

**Files:**
- Create: `backend/Hominder.Api/Endpoints/MaintenanceTaskEndpoints.cs`
- Modify: `backend/Hominder.Api/Program.cs` (ajout `using` + `app.MapMaintenanceTaskEndpoints();`)
- Test: `backend/Hominder.Test.Integration/MaintenanceTaskEndpointsTests.cs`

**Interfaces:**
- Consumes: `ISender` (MediatR), `GetMaintenanceTasksQuery`, `CreateMaintenanceTaskCommand`, `UpdateMaintenanceTaskCommand`, `DeleteMaintenanceTaskCommand`, `MarkMaintenanceTaskDoneCommand`, `RecurrencePolicyInput`.
- Produces: `static IEndpointRouteBuilder MapMaintenanceTaskEndpoints(this IEndpointRouteBuilder routes)`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Json;
using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Queries;

namespace Hominder.Test.Integration;

public class MaintenanceTaskEndpointsTests : IClassFixture<HominderApiFactory>
{
    private readonly HominderApiFactory _factory;

    public MaintenanceTaskEndpointsTests(HominderApiFactory factory) => _factory = factory;

    private sealed record CreateBody(string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId);

    private sealed record MarkDoneBody(DateOnly CompletedOn, Guid CompletedBy, DateOnly? NextDueOverride);

    private sealed record CreatedResponse(Guid Id);

    [Fact]
    public async Task CreateThenList_ReturnsTaskWithComputedStatus()
    {
        var client = _factory.CreateClient();
        var policy = new RecurrencePolicyInput(RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);

        var create = await client.PostAsJsonAsync(
            "/api/tasks", new CreateBody("Tailler l'olivier", null, policy, null));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var tasks = await client.GetFromJsonAsync<List<MaintenanceTaskView>>("/api/tasks");

        Assert.NotNull(tasks);
        Assert.Contains(tasks!, task => task.Title == "Tailler l'olivier");
    }

    [Fact]
    public async Task MarkDone_RecordsCompletion()
    {
        var client = _factory.CreateClient();
        var policy = new RecurrencePolicyInput(
            RecurrenceKind.FixedDate, null, null, null, null, null, new DateOnly(2026, 6, 30));

        var create = await client.PostAsJsonAsync(
            "/api/tasks", new CreateBody("Contrôle technique", null, policy, null));
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();

        var mark = await client.PostAsJsonAsync(
            $"/api/tasks/{created!.Id}/completions",
            new MarkDoneBody(new DateOnly(2026, 6, 20), Guid.NewGuid(), new DateOnly(2028, 6, 30)));

        Assert.Equal(HttpStatusCode.NoContent, mark.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Integration --filter MaintenanceTaskEndpointsTests`
Expected: FAIL — 404 (endpoints non mappés).

- [ ] **Step 3: Write the endpoints**

`Endpoints/MaintenanceTaskEndpoints.cs`:

```csharp
using Hominder.Application.Maintenance;
using Hominder.Application.Maintenance.Commands;
using Hominder.Application.Maintenance.Queries;
using MediatR;

namespace Hominder.Api.Endpoints;

public sealed record CreateMaintenanceTaskRequest(
    string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId);

public sealed record UpdateMaintenanceTaskRequest(
    string Title, string? Notes, RecurrencePolicyInput Policy, Guid? AssigneeId);

public sealed record MarkMaintenanceTaskDoneRequest(
    DateOnly CompletedOn, Guid CompletedBy, DateOnly? NextDueOverride);

public static class MaintenanceTaskEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks");

        group.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetMaintenanceTasksQuery(), cancellationToken)));

        group.MapPost("", async (
            CreateMaintenanceTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var id = await sender.Send(
                new CreateMaintenanceTaskCommand(request.Title, request.Notes, request.Policy, request.AssigneeId),
                cancellationToken);
            return Results.Created($"/api/tasks/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateMaintenanceTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new UpdateMaintenanceTaskCommand(id, request.Title, request.Notes, request.Policy, request.AssigneeId),
                cancellationToken);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteMaintenanceTaskCommand(id), cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/completions", async (
            Guid id, MarkMaintenanceTaskDoneRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new MarkMaintenanceTaskDoneCommand(id, request.CompletedOn, request.CompletedBy, request.NextDueOverride),
                cancellationToken);
            return Results.NoContent();
        });

        return routes;
    }
}
```

- [ ] **Step 4: Map the endpoints in Program.cs**

Add the using after the existing `using Hominder.Api.ExceptionHandling;` line:

```csharp
using Hominder.Api.Endpoints;
```

Add this line immediately before `app.MapGet("/health", ...)`:

```csharp
app.MapMaintenanceTaskEndpoints();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Integration --filter MaintenanceTaskEndpointsTests`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add backend/Hominder.Api backend/Hominder.Test.Integration
git commit -m "feat(api): add maintenance task endpoints"
```

---

### Task 19: Endpoints des membres du foyer

**Files:**
- Create: `backend/Hominder.Api/Endpoints/HouseholdMemberEndpoints.cs`
- Modify: `backend/Hominder.Api/Program.cs` (ajout `app.MapHouseholdMemberEndpoints();`)
- Test: `backend/Hominder.Test.Integration/HouseholdMemberEndpointsTests.cs`

**Interfaces:**
- Consumes: `ISender`, `GetHouseholdMembersQuery`, `CreateHouseholdMemberCommand`, `DeleteHouseholdMemberCommand`.
- Produces: `static IEndpointRouteBuilder MapHouseholdMemberEndpoints(this IEndpointRouteBuilder routes)`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Json;
using Hominder.Application.Household.Queries;

namespace Hominder.Test.Integration;

public class HouseholdMemberEndpointsTests : IClassFixture<HominderApiFactory>
{
    private readonly HominderApiFactory _factory;

    public HouseholdMemberEndpointsTests(HominderApiFactory factory) => _factory = factory;

    private sealed record CreateBody(string Name);

    private sealed record CreatedResponse(Guid Id);

    [Fact]
    public async Task CreateThenList_ReturnsMember()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/members", new CreateBody("Grégory"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var members = await client.GetFromJsonAsync<List<HouseholdMemberView>>("/api/members");

        Assert.NotNull(members);
        Assert.Contains(members!, member => member.Name == "Grégory");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/Hominder.Test.Integration --filter HouseholdMemberEndpointsTests`
Expected: FAIL — 404 (endpoints non mappés).

- [ ] **Step 3: Write the endpoints**

`Endpoints/HouseholdMemberEndpoints.cs`:

```csharp
using Hominder.Application.Household.Commands;
using Hominder.Application.Household.Queries;
using MediatR;

namespace Hominder.Api.Endpoints;

public sealed record CreateHouseholdMemberRequest(string Name);

public static class HouseholdMemberEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdMemberEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/members");

        group.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetHouseholdMembersQuery(), cancellationToken)));

        group.MapPost("", async (
            CreateHouseholdMemberRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var id = await sender.Send(new CreateHouseholdMemberCommand(request.Name), cancellationToken);
            return Results.Created($"/api/members/{id}", new { id });
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteHouseholdMemberCommand(id), cancellationToken);
            return Results.NoContent();
        });

        return routes;
    }
}
```

- [ ] **Step 4: Map the endpoints in Program.cs**

Add this line immediately after `app.MapMaintenanceTaskEndpoints();`:

```csharp
app.MapHouseholdMemberEndpoints();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test backend/Hominder.Test.Integration --filter HouseholdMemberEndpointsTests`
Expected: PASS (1 test).

- [ ] **Step 6: Run the full backend suite**

Run: `dotnet test backend/Hominder.slnx`
Expected: tous les tests passent (unit, integration, architecture).

- [ ] **Step 7: Commit**

```bash
git add backend/Hominder.Api backend/Hominder.Test.Integration
git commit -m "feat(api): add household member endpoints"
```

---

## Couche Frontend

### Task 20: Dépendances, client typé OpenAPI, QueryClient

**Files:**
- Modify: `frontend/package.json` (dépendances + scripts)
- Modify: `frontend/vite.config.ts` (config Vitest)
- Create: `frontend/src/query/queryClient.ts`
- Create: `frontend/src/api/client.ts`
- Create: `frontend/src/api/schema.d.ts` (généré)
- Modify: `frontend/src/main.tsx` (QueryClientProvider)

**Interfaces:**
- Produces: `api` (client openapi-fetch typé), `queryClient`, `paths` (types générés).

- [ ] **Step 1: Install dependencies**

Run:
```bash
npm --prefix frontend install @tanstack/react-query openapi-fetch
npm --prefix frontend install -D openapi-typescript vitest
```
Expected: dépendances ajoutées.

- [ ] **Step 2: Add scripts to package.json**

Dans `frontend/package.json`, ajouter aux `scripts` :

```json
"generate:api": "openapi-typescript http://localhost:5191/openapi/v1.json -o src/api/schema.d.ts",
"test": "vitest run"
```

- [ ] **Step 3: Configure Vitest via vite.config.ts**

`frontend/vite.config.ts` (remplacer le contenu) :

```typescript
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'node',
  },
})
```

- [ ] **Step 4: Generate the OpenAPI types from the running API**

Run:
```bash
colima start
docker compose up -d db
dotnet run --project backend/Hominder.Api &
sleep 10
npm --prefix frontend run generate:api
kill %1
```
Expected: `frontend/src/api/schema.d.ts` généré, exportant le type `paths` avec `/api/tasks`, `/api/members`.

- [ ] **Step 5: Write the query client and api client**

`src/query/queryClient.ts`:

```typescript
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient()
```

`src/api/client.ts`:

```typescript
import createClient from 'openapi-fetch'
import type { paths } from './schema'

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5191'

export const api = createClient<paths>({ baseUrl })
```

- [ ] **Step 6: Wire the provider in main.tsx**

`src/main.tsx` (remplacer le contenu) :

```typescript
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from './query/queryClient'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
)
```

- [ ] **Step 7: Verify the build**

Run: `npm --prefix frontend run build`
Expected: build TypeScript + Vite réussie.

- [ ] **Step 8: Commit**

```bash
git add frontend
git commit -m "feat(frontend): add react-query, typed openapi client, query provider"
```

---

### Task 21: Fonctionnalité Membres (hook + écran)

**Files:**
- Create: `frontend/src/features/members/useMembers.ts`
- Create: `frontend/src/features/members/MembersScreen.tsx`

**Interfaces:**
- Consumes: `api`.
- Produces: `type Member`, `useMembers()`, `useCreateMember()`, `useDeleteMember()`, `MembersScreen`.

- [ ] **Step 1: Write the hooks**

`src/features/members/useMembers.ts`:

```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'

export type Member = { id: string; name: string }

const membersKey = ['members'] as const

export function useMembers() {
  return useQuery({
    queryKey: membersKey,
    queryFn: async (): Promise<Member[]> => {
      const { data, error } = await api.GET('/api/members')
      if (error) {
        throw new Error('Chargement des membres impossible.')
      }
      return (data ?? []) as Member[]
    },
  })
}

export function useCreateMember() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (name: string) => {
      const { error } = await api.POST('/api/members', { body: { name } })
      if (error) {
        throw new Error('Création du membre impossible.')
      }
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: membersKey }),
  })
}

export function useDeleteMember() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/members/{id}', { params: { path: { id } } })
      if (error) {
        throw new Error('Suppression du membre impossible.')
      }
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: membersKey }),
  })
}
```

- [ ] **Step 2: Write the screen**

`src/features/members/MembersScreen.tsx`:

```typescript
import { useState } from 'react'
import { useCreateMember, useDeleteMember, useMembers } from './useMembers'

export function MembersScreen() {
  const members = useMembers()
  const createMember = useCreateMember()
  const deleteMember = useDeleteMember()
  const [name, setName] = useState('')

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    const trimmed = name.trim()
    if (trimmed.length === 0) {
      return
    }
    createMember.mutate(trimmed, { onSuccess: () => setName('') })
  }

  return (
    <section>
      <h2>Membres du foyer</h2>
      <form onSubmit={submit}>
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Nom du membre"
        />
        <button type="submit" disabled={createMember.isPending}>
          Ajouter
        </button>
      </form>

      {members.isLoading ? <p>Chargement…</p> : null}

      <ul>
        {(members.data ?? []).map((member) => (
          <li key={member.id}>
            <span>{member.name}</span>
            <button type="button" onClick={() => deleteMember.mutate(member.id)}>
              Supprimer
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}
```

- [ ] **Step 3: Verify the build**

Run: `npm --prefix frontend run build`
Expected: build réussie.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/members
git commit -m "feat(frontend): add household members screen"
```

---

### Task 22: Liste des tâches, statut, cartes

**Files:**
- Create: `frontend/src/features/tasks/taskStatus.ts`
- Create: `frontend/src/features/tasks/useTasks.ts`
- Create: `frontend/src/features/tasks/TaskCard.tsx`
- Create: `frontend/src/features/tasks/TaskList.tsx`
- Test: `frontend/src/features/tasks/taskStatus.test.ts`

**Interfaces:**
- Consumes: `api`, `Member`.
- Produces:
  - `type TaskStatus`, `type TaskView`, `statusLabel(status)`, `groupByStatus(tasks)`.
  - `useTasks()`, `useCreateTask()`, `useUpdateTask()`, `useDeleteTask()`, `useMarkTaskDone()`.
  - `TaskCard`, `TaskList`.
  - `type RecurrencePolicyInput` (contrat d'entrée pour les commandes, réutilisé par le formulaire T23).

- [ ] **Step 1: Write the failing test**

`src/features/tasks/taskStatus.test.ts`:

```typescript
import { expect, test } from 'vitest'
import { groupByStatus, statusLabel, type TaskView } from './taskStatus'

const view = (id: string, status: TaskView['status']): TaskView => ({
  id,
  title: id,
  notes: null,
  status,
  openDate: '2026-03-01',
  dueDate: '2026-05-31',
  daysOverdue: 0,
  assigneeId: null,
  assigneeName: null,
  requiresNextDueOverride: false,
})

test('statusLabel maps to French labels', () => {
  expect(statusLabel('Overdue')).toBe('En retard')
  expect(statusLabel('Due')).toBe('À faire')
  expect(statusLabel('Upcoming')).toBe('À venir')
  expect(statusLabel('Done')).toBe('Fait')
})

test('groupByStatus orders overdue first and done last', () => {
  const groups = groupByStatus([view('a', 'Upcoming'), view('b', 'Overdue'), view('c', 'Done')])

  expect(groups.map(([status]) => status)).toEqual(['Overdue', 'Upcoming', 'Done'])
})
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm --prefix frontend run test`
Expected: FAIL — `taskStatus` introuvable.

- [ ] **Step 3: Write the status utility**

`src/features/tasks/taskStatus.ts`:

```typescript
export type TaskStatus = 'Upcoming' | 'Due' | 'Overdue' | 'Done'

export type TaskView = {
  id: string
  title: string
  notes: string | null
  status: TaskStatus
  openDate: string
  dueDate: string
  daysOverdue: number
  assigneeId: string | null
  assigneeName: string | null
  requiresNextDueOverride: boolean
}

export type RecurrenceKind = 'Interval' | 'MonthWindow' | 'FixedDate' | 'OneOff'

export type RecurrenceUnit = 'Days' | 'Weeks' | 'Months' | 'Years'

export type RecurrencePolicyInput = {
  kind: RecurrenceKind
  intervalAmount: number | null
  intervalUnit: RecurrenceUnit | null
  startReference: string | null
  startMonth: number | null
  endMonth: number | null
  dueDate: string | null
}

const statusOrder: Record<TaskStatus, number> = {
  Overdue: 0,
  Due: 1,
  Upcoming: 2,
  Done: 3,
}

export function statusLabel(status: TaskStatus): string {
  switch (status) {
    case 'Overdue':
      return 'En retard'
    case 'Due':
      return 'À faire'
    case 'Upcoming':
      return 'À venir'
    case 'Done':
      return 'Fait'
  }
}

export function groupByStatus(tasks: TaskView[]): [TaskStatus, TaskView[]][] {
  const groups = new Map<TaskStatus, TaskView[]>()
  for (const task of tasks) {
    const list = groups.get(task.status) ?? []
    list.push(task)
    groups.set(task.status, list)
  }
  return [...groups.entries()].sort((left, right) => statusOrder[left[0]] - statusOrder[right[0]])
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm --prefix frontend run test`
Expected: PASS (2 tests).

- [ ] **Step 5: Write the task hooks**

`src/features/tasks/useTasks.ts`:

```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { RecurrencePolicyInput, TaskView } from './taskStatus'

const tasksKey = ['tasks'] as const

export type TaskInput = {
  title: string
  notes: string | null
  policy: RecurrencePolicyInput
  assigneeId: string | null
}

export type MarkDoneInput = {
  id: string
  completedOn: string
  completedBy: string
  nextDueOverride: string | null
}

export function useTasks() {
  return useQuery({
    queryKey: tasksKey,
    queryFn: async (): Promise<TaskView[]> => {
      const { data, error } = await api.GET('/api/tasks')
      if (error) {
        throw new Error('Chargement des tâches impossible.')
      }
      return (data ?? []) as TaskView[]
    },
  })
}

function invalidateTasks(queryClient: ReturnType<typeof useQueryClient>) {
  return queryClient.invalidateQueries({ queryKey: tasksKey })
}

export function useCreateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (input: TaskInput) => {
      const { error } = await api.POST('/api/tasks', { body: input })
      if (error) {
        throw new Error('Création de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}

export function useUpdateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: TaskInput }) => {
      const { error } = await api.PUT('/api/tasks/{id}', {
        params: { path: { id } },
        body: input,
      })
      if (error) {
        throw new Error('Mise à jour de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}

export function useDeleteTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/tasks/{id}', { params: { path: { id } } })
      if (error) {
        throw new Error('Suppression de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}

export function useMarkTaskDone() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, completedOn, completedBy, nextDueOverride }: MarkDoneInput) => {
      const { error } = await api.POST('/api/tasks/{id}/completions', {
        params: { path: { id } },
        body: { completedOn, completedBy, nextDueOverride },
      })
      if (error) {
        throw new Error('Complétion de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}
```

- [ ] **Step 6: Write the card and list**

`src/features/tasks/TaskCard.tsx`:

```typescript
import type { TaskView } from './taskStatus'

type TaskCardProps = {
  task: TaskView
  onMarkDone: (task: TaskView) => void
  onEdit: (task: TaskView) => void
  onDelete: (task: TaskView) => void
}

export function TaskCard({ task, onMarkDone, onEdit, onDelete }: TaskCardProps) {
  return (
    <article>
      <h3>{task.title}</h3>
      <p>Échéance : {task.dueDate}</p>
      {task.daysOverdue > 0 ? <p>En retard de {task.daysOverdue} jour(s)</p> : null}
      {task.assigneeName ? <p>Assigné à {task.assigneeName}</p> : null}
      <div>
        {task.status === 'Done' ? null : (
          <button type="button" onClick={() => onMarkDone(task)}>
            C'est fait
          </button>
        )}
        <button type="button" onClick={() => onEdit(task)}>
          Éditer
        </button>
        <button type="button" onClick={() => onDelete(task)}>
          Supprimer
        </button>
      </div>
    </article>
  )
}
```

`src/features/tasks/TaskList.tsx`:

```typescript
import { groupByStatus, statusLabel, type TaskView } from './taskStatus'
import { TaskCard } from './TaskCard'

type TaskListProps = {
  tasks: TaskView[]
  onMarkDone: (task: TaskView) => void
  onEdit: (task: TaskView) => void
  onDelete: (task: TaskView) => void
}

export function TaskList({ tasks, onMarkDone, onEdit, onDelete }: TaskListProps) {
  const groups = groupByStatus(tasks)

  if (groups.length === 0) {
    return <p>Aucune tâche pour l'instant.</p>
  }

  return (
    <div>
      {groups.map(([status, groupTasks]) => (
        <section key={status}>
          <h2>{statusLabel(status)}</h2>
          {groupTasks.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              onMarkDone={onMarkDone}
              onEdit={onEdit}
              onDelete={onDelete}
            />
          ))}
        </section>
      ))}
    </div>
  )
}
```

- [ ] **Step 7: Verify build and tests**

Run:
```bash
npm --prefix frontend run test
npm --prefix frontend run build
```
Expected: tests PASS, build réussie.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/features/tasks
git commit -m "feat(frontend): add task list, status grouping, hooks"
```

---

### Task 23: Formulaire de tâche, dialogue de complétion, assemblage App

**Files:**
- Create: `frontend/src/features/tasks/TaskForm.tsx`
- Create: `frontend/src/features/tasks/MarkDoneDialog.tsx`
- Modify: `frontend/src/App.tsx` (assemblage)

**Interfaces:**
- Consumes: `useTasks`, `useCreateTask`, `useUpdateTask`, `useDeleteTask`, `useMarkTaskDone`, `useMembers`, `TaskList`, `RecurrencePolicyInput`, `TaskView`.
- Produces: `TaskForm`, `MarkDoneDialog`, `App` complet.

- [ ] **Step 1: Write the task form**

`src/features/tasks/TaskForm.tsx`:

```typescript
import { useState } from 'react'
import type { Member } from '../members/useMembers'
import type { RecurrenceKind, RecurrencePolicyInput, RecurrenceUnit } from './taskStatus'
import type { TaskInput } from './useTasks'

type TaskFormProps = {
  members: Member[]
  onSubmit: (input: TaskInput) => void
  onCancel: () => void
}

const emptyPolicy: RecurrencePolicyInput = {
  kind: 'MonthWindow',
  intervalAmount: null,
  intervalUnit: null,
  startReference: null,
  startMonth: 3,
  endMonth: 5,
  dueDate: null,
}

export function TaskForm({ members, onSubmit, onCancel }: TaskFormProps) {
  const [title, setTitle] = useState('')
  const [notes, setNotes] = useState('')
  const [assigneeId, setAssigneeId] = useState('')
  const [policy, setPolicy] = useState<RecurrencePolicyInput>(emptyPolicy)

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    if (title.trim().length === 0) {
      return
    }
    onSubmit({
      title: title.trim(),
      notes: notes.trim().length === 0 ? null : notes.trim(),
      policy,
      assigneeId: assigneeId.length === 0 ? null : assigneeId,
    })
  }

  const setKind = (kind: RecurrenceKind) => setPolicy({ ...emptyPolicy, kind })

  return (
    <form onSubmit={submit}>
      <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Titre" />
      <textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Notes" />

      <select value={assigneeId} onChange={(event) => setAssigneeId(event.target.value)}>
        <option value="">Non assigné</option>
        {members.map((member) => (
          <option key={member.id} value={member.id}>
            {member.name}
          </option>
        ))}
      </select>

      <select value={policy.kind} onChange={(event) => setKind(event.target.value as RecurrenceKind)}>
        <option value="MonthWindow">Fenêtre de mois</option>
        <option value="Interval">Intervalle</option>
        <option value="FixedDate">Date fixe</option>
        <option value="OneOff">Ponctuelle</option>
      </select>

      {policy.kind === 'MonthWindow' ? (
        <fieldset>
          <input
            type="number"
            min={1}
            max={12}
            value={policy.startMonth ?? 1}
            onChange={(event) => setPolicy({ ...policy, startMonth: Number(event.target.value) })}
          />
          <input
            type="number"
            min={1}
            max={12}
            value={policy.endMonth ?? 1}
            onChange={(event) => setPolicy({ ...policy, endMonth: Number(event.target.value) })}
          />
        </fieldset>
      ) : null}

      {policy.kind === 'Interval' ? (
        <fieldset>
          <input
            type="number"
            min={1}
            value={policy.intervalAmount ?? 1}
            onChange={(event) => setPolicy({ ...policy, intervalAmount: Number(event.target.value) })}
          />
          <select
            value={policy.intervalUnit ?? 'Years'}
            onChange={(event) => setPolicy({ ...policy, intervalUnit: event.target.value as RecurrenceUnit })}
          >
            <option value="Days">Jours</option>
            <option value="Weeks">Semaines</option>
            <option value="Months">Mois</option>
            <option value="Years">Années</option>
          </select>
          <input
            type="date"
            value={policy.startReference ?? ''}
            onChange={(event) => setPolicy({ ...policy, startReference: event.target.value })}
          />
        </fieldset>
      ) : null}

      {policy.kind === 'FixedDate' || policy.kind === 'OneOff' ? (
        <input
          type="date"
          value={policy.dueDate ?? ''}
          onChange={(event) => setPolicy({ ...policy, dueDate: event.target.value })}
        />
      ) : null}

      <button type="submit">Enregistrer</button>
      <button type="button" onClick={onCancel}>
        Annuler
      </button>
    </form>
  )
}
```

- [ ] **Step 2: Write the mark-done dialog**

`src/features/tasks/MarkDoneDialog.tsx`:

```typescript
import { useState } from 'react'
import type { Member } from '../members/useMembers'
import type { TaskView } from './taskStatus'
import type { MarkDoneInput } from './useTasks'

type MarkDoneDialogProps = {
  task: TaskView
  members: Member[]
  requiresNextDue: boolean
  onConfirm: (input: MarkDoneInput) => void
  onCancel: () => void
}

const today = () => new Date().toISOString().slice(0, 10)

export function MarkDoneDialog({ task, members, requiresNextDue, onConfirm, onCancel }: MarkDoneDialogProps) {
  const [completedOn, setCompletedOn] = useState(today())
  const [completedBy, setCompletedBy] = useState(members[0]?.id ?? '')
  const [nextDueOverride, setNextDueOverride] = useState('')

  const confirm = () => {
    if (completedBy.length === 0) {
      return
    }
    if (requiresNextDue && nextDueOverride.length === 0) {
      return
    }
    onConfirm({
      id: task.id,
      completedOn,
      completedBy,
      nextDueOverride: requiresNextDue ? nextDueOverride : null,
    })
  }

  return (
    <div role="dialog">
      <h3>Marquer « {task.title} » comme fait</h3>
      <input type="date" value={completedOn} onChange={(event) => setCompletedOn(event.target.value)} />
      <select value={completedBy} onChange={(event) => setCompletedBy(event.target.value)}>
        {members.map((member) => (
          <option key={member.id} value={member.id}>
            {member.name}
          </option>
        ))}
      </select>
      {requiresNextDue ? (
        <label>
          Prochaine échéance
          <input type="date" value={nextDueOverride} onChange={(event) => setNextDueOverride(event.target.value)} />
        </label>
      ) : null}
      <button type="button" onClick={confirm}>
        Confirmer
      </button>
      <button type="button" onClick={onCancel}>
        Annuler
      </button>
    </div>
  )
}
```

- [ ] **Step 3: Assemble App.tsx**

`src/App.tsx` (remplacer le contenu) :

```typescript
import { useState } from 'react'
import './App.css'
import { MembersScreen } from './features/members/MembersScreen'
import { useMembers } from './features/members/useMembers'
import { MarkDoneDialog } from './features/tasks/MarkDoneDialog'
import { TaskForm } from './features/tasks/TaskForm'
import { TaskList } from './features/tasks/TaskList'
import type { TaskView } from './features/tasks/taskStatus'
import { useCreateTask, useDeleteTask, useMarkTaskDone, useTasks } from './features/tasks/useTasks'

function App() {
  const tasks = useTasks()
  const members = useMembers()
  const createTask = useCreateTask()
  const deleteTask = useDeleteTask()
  const markDone = useMarkTaskDone()

  const [showForm, setShowForm] = useState(false)
  const [taskToComplete, setTaskToComplete] = useState<TaskView | null>(null)

  const memberList = members.data ?? []

  return (
    <main>
      <header>
        <h1>Hominder</h1>
      </header>

      <button type="button" onClick={() => setShowForm(true)}>
        Nouvelle tâche
      </button>

      {showForm ? (
        <TaskForm
          members={memberList}
          onCancel={() => setShowForm(false)}
          onSubmit={(input) => {
            createTask.mutate(input, { onSuccess: () => setShowForm(false) })
          }}
        />
      ) : null}

      {tasks.isLoading ? <p>Chargement…</p> : null}

      <TaskList
        tasks={tasks.data ?? []}
        onMarkDone={(task) => setTaskToComplete(task)}
        onEdit={() => undefined}
        onDelete={(task) => deleteTask.mutate(task.id)}
      />

      {taskToComplete ? (
        <MarkDoneDialog
          task={taskToComplete}
          members={memberList}
          requiresNextDue={taskToComplete.requiresNextDueOverride}
          onCancel={() => setTaskToComplete(null)}
          onConfirm={(input) => {
            markDone.mutate(input, { onSuccess: () => setTaskToComplete(null) })
          }}
        />
      ) : null}

      <MembersScreen />
    </main>
  )
}

export default App
```

- [ ] **Step 4: Verify build and tests**

Run:
```bash
npm --prefix frontend run test
npm --prefix frontend run build
npm --prefix frontend run lint
```
Expected: tests PASS, build réussie, lint sans erreur.

- [ ] **Step 5: Manual end-to-end check**

Run (backend et frontend démarrés) :
```bash
docker compose up -d db
dotnet run --project backend/Hominder.Api &
npm --prefix frontend run dev
```
Vérifier dans le navigateur (`http://localhost:5173`) : ajouter un membre, créer une tâche « fenêtre de mois », la voir apparaître dans le bon groupe de statut, cliquer « C'est fait », constater le recalcul de l'échéance après rechargement.

- [ ] **Step 6: Commit**

```bash
git add frontend/src
git commit -m "feat(frontend): add task form, mark-done dialog, app assembly"
```

---

## Notes d'exécution transverses

- **Ordre des couches** : Domaine (T1–T8) → Application (T9–T13) → Infrastructure (T14–T16) → API (T17–T19) → Frontend (T20–T23). Chaque tâche se termine par un commit et un livrable testable.
- **Docker requis** dès T16 : `colima start` puis `docker compose up -d db`. Les tests d'intégration (T14, T17–T19) démarrent leurs propres conteneurs via Testcontainers et exigent colima actif.
- **Zéro commentaire** : aucun fichier produit (`.cs`, `.ts`, `.tsx`, `.yml`, `.json`) ne doit contenir de commentaire.
- **`requiresNextDue`** est piloté par le champ `requiresNextDueOverride` de `MaintenanceTaskView` (T12), remonté jusqu'au `MarkDoneDialog` via `App.tsx` (T23) : les tâches à date fixe demandent la prochaine échéance, les autres non.
- **Types front générés vs types manuels** : `src/api/schema.d.ts` (généré) fait foi pour les corps de requête. Les types `TaskView` / `RecurrencePolicyInput` / `TaskInput` écrits à la main doivent refléter la casse camelCase et la nullabilité produites par l'API ; si `tsc`/openapi-fetch signale un écart de nullabilité lors des builds (T20/T22/T23), aligner le type manuel sur le type généré.

## Self-Review

**1. Couverture de la spec :**

| Exigence de la spec | Tâche(s) |
| --- | --- |
| Récurrence intervalle depuis dernière fois | T3 |
| Récurrence fenêtre de mois | T5 |
| Récurrence date fixe (échéance saisie) | T4 |
| Tâche ponctuelle | T4 |
| Foyer unique, membres nommés | T8 |
| Assignation + historique « qui a fait quoi » | T6, T7 (Completion + CompletedBy) |
| Statut calculé à la lecture (à venir/dû/en retard/fait) | T7 (`Evaluate`), T12 (query) |
| Pas de notifications | aucune tâche de planification (conforme) |
| `ICommand`/`IQuery` custom + behaviors (logging global, transaction sur `ICommand`) | T9, T10 |
| Validation dans commands/domaine, sans FluentValidation | T7, T9, T11 |
| Persistance PostgreSQL, politique en `jsonb` | T14–T16 |
| Domain event de complétion (seam) | T6 |
| Suppression du template WeatherForecast | T17 |
| API Minimal + endpoints | T17–T19 |
| Frontend TanStack Query + client OpenAPI typé | T20–T23 |
| Tests domaine / handlers / intégration Testcontainers / architecture | T1–T13, T14, T17–T19 |

Aucune exigence sans tâche.

**2. Placeholders :** aucun `TBD`/`TODO`/« gérer les cas limites » ; chaque étape porte le code réel. Corrigé pendant la revue : le champ `RequiresNextDueOverride` remonté du domaine jusqu'à l'UI pour éviter un 400 à la complétion des tâches à date fixe.

**3. Cohérence des types :**
- Noms de handlers (`CreateMaintenanceTaskHandler`, `GetMaintenanceTasksHandler`, `GetHouseholdMembersHandler`, …) identiques entre définition et tests.
- Casse JSON : ASP.NET sérialise en camelCase ; les types front (`status`, `openDate`, `requiresNextDueOverride`, `intervalUnit`, …) correspondent.
- Enums transmis en chaînes (`JsonStringEnumConverter`, T17) ; unions front (`'MonthWindow'`, `'Years'`, `'Overdue'`, …) alignées sur les noms C# (`RecurrenceKind`, `RecurrenceUnit`, `MaintenanceStatus`).
- Routes : patterns racine `""` dans les groupes (T18/T19) → chemins OpenAPI `/api/tasks` et `/api/members` sans slash final, cohérents avec les appels openapi-fetch.
- Signature `IUnitOfWork.ExecuteInTransactionAsync<T>` identique entre port (T9), behavior (T10) et implémentation DbContext (T14).
