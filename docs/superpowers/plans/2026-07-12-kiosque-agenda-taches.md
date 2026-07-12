# Kiosque SP1 — coquille + agenda tâches — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retravailler le frontend `frontend/` pour qu'il devienne le kiosque : une coquille à tabs (Agenda / Domotique) avec un tab Agenda réunissant une colonne Tâches et un calendrier semaine, en modes consultation et gestion.

**Architecture:** Application React à page unique, sans routing. `App.tsx` devient la coquille (`KioskShell`) qui gère tabs et mode via state local. Le tab Agenda compose une `TaskSidebar` (tâches groupées par statut) et un `WeekCalendar` fait main (grille 7 jours, tâches placées à leur `dueDate`). Les composants CRUD existants (`TaskForm`, `MarkDoneDialog`, `MembersScreen`, `TaskCard`) sont conservés et rebranchés. Rafraîchissement auto via TanStack Query. Aucun changement backend.

**Tech Stack:** React 19, TypeScript, Vite, TanStack Query, Vitest. Client API typé existant (`api.GET/POST/PUT/DELETE`).

## Global Constraints

- **Zéro commentaire** dans tout code produit (aucun `//`, `/* */`, JSDoc). Auto-documenté par le nommage.
- **Aucune nouvelle dépendance** : pas de librairie de routing ni de calendrier. Tabs, mode et grille semaine faits main.
- **Aucune modification backend** ni du client API généré (`src/api/schema.d.ts`, `src/api/client.ts`).
- **Langue de l'UI : français** (labels, messages).
- **Déploiement local, aucune authentification** : ne rien ajouter côté auth.
- Toutes les commandes s'exécutent depuis `frontend/`.
- Composants CRUD existants **réutilisés, jamais réécrits** : `TaskForm`, `MarkDoneDialog`, `MembersScreen`, `TaskCard`.

**Signatures existantes réutilisées (ne pas les modifier) :**

- `useTasks()` → `UseQueryResult<TaskView[]>` ; `useCreateTask()`, `useUpdateTask()`, `useDeleteTask()`, `useMarkTaskDone()` (mutations). `useTasks` vit dans `src/features/tasks/useTasks.ts`.
- `useMembers()` → `UseQueryResult<Member[]>` (`src/features/members/useMembers.ts`).
- `TaskForm` props : `{ members: Member[]; initialTask?: TaskView; onCancel: () => void; onSubmit: (input: TaskInput) => void }`.
- `MarkDoneDialog` props : `{ task: TaskView; members: Member[]; requiresNextDue: boolean; onConfirm: (input: MarkDoneInput) => void; onCancel: () => void }`.
- `MembersScreen` props : aucune (gère ses propres hooks).
- `TaskView` (dans `src/features/tasks/taskStatus.ts`) : `{ id, title, notes, status, openDate, dueDate, daysOverdue, assigneeId, assigneeName, requiresNextDueOverride, policy }` ; `status: 'Upcoming' | 'Due' | 'Overdue' | 'Done'`. `dueDate` / `openDate` sont des chaînes `'YYYY-MM-DD'`.
- Helpers existants dans `taskStatus.ts` : `groupByStatus`, `statusLabel`, `statusModifier`.

---

### Task 1: Module de mapping calendrier `calendarItems.ts` (pur, testé)

Module de fonctions pures qui transforme les `TaskView` en structure de semaine pour le calendrier. Seule brique unitairement testable ; on la construit en TDD.

**Files:**
- Create: `frontend/src/features/agenda/calendarItems.ts`
- Test: `frontend/src/features/agenda/calendarItems.test.ts`

**Interfaces:**
- Consumes: `TaskView`, `TaskStatus` depuis `../tasks/taskStatus`.
- Produces:
  - `type CalendarItem = { taskId: string; title: string; date: string; status: TaskStatus }`
  - `type CalendarDay = { date: string; weekdayLabel: string; dayOfMonth: number; items: CalendarItem[] }`
  - `function isoDate(date: Date): string` — `'YYYY-MM-DD'` en composantes locales.
  - `function startOfWeek(reference: Date): Date` — lundi 00:00 local de la semaine de `reference`.
  - `function weekDates(reference: Date): string[]` — 7 dates ISO, lundi→dimanche.
  - `function taskToCalendarItem(task: TaskView): CalendarItem`
  - `function buildWeek(reference: Date, tasks: TaskView[]): CalendarDay[]` — 7 jours ; chaque jour porte les tâches dont `dueDate` tombe ce jour-là.

- [ ] **Step 1: Écrire les tests qui échouent**

Create `frontend/src/features/agenda/calendarItems.test.ts` :

```ts
import { describe, expect, it } from 'vitest'
import type { TaskView } from '../tasks/taskStatus'
import { buildWeek, isoDate, startOfWeek, taskToCalendarItem, weekDates } from './calendarItems'

function task(overrides: Partial<TaskView>): TaskView {
  return {
    id: 'task-1',
    title: 'Tailler l\'olivier',
    notes: null,
    status: 'Due',
    openDate: '2026-07-06',
    dueDate: '2026-07-08',
    daysOverdue: 0,
    assigneeId: null,
    assigneeName: null,
    requiresNextDueOverride: false,
    policy: {
      kind: 'Interval',
      intervalAmount: 1,
      intervalUnit: 'Years',
      startReference: null,
      startMonth: null,
      endMonth: null,
      dueDate: null,
    },
    ...overrides,
  }
}

describe('isoDate', () => {
  it('formats local date components as YYYY-MM-DD', () => {
    expect(isoDate(new Date(2026, 6, 8))).toBe('2026-07-08')
    expect(isoDate(new Date(2026, 0, 3))).toBe('2026-01-03')
  })
})

describe('startOfWeek', () => {
  it('returns the Monday of the week for a mid-week date', () => {
    expect(isoDate(startOfWeek(new Date(2026, 6, 8)))).toBe('2026-07-06')
  })

  it('returns the same Monday when given a Monday', () => {
    expect(isoDate(startOfWeek(new Date(2026, 6, 6)))).toBe('2026-07-06')
  })

  it('treats Sunday as the last day of the week', () => {
    expect(isoDate(startOfWeek(new Date(2026, 6, 12)))).toBe('2026-07-06')
  })
})

describe('weekDates', () => {
  it('returns seven ISO dates from Monday to Sunday', () => {
    expect(weekDates(new Date(2026, 6, 8))).toEqual([
      '2026-07-06',
      '2026-07-07',
      '2026-07-08',
      '2026-07-09',
      '2026-07-10',
      '2026-07-11',
      '2026-07-12',
    ])
  })
})

describe('taskToCalendarItem', () => {
  it('maps a task onto its due date with its status', () => {
    expect(taskToCalendarItem(task({ id: 'x', title: 'CT', dueDate: '2026-07-09', status: 'Overdue' }))).toEqual({
      taskId: 'x',
      title: 'CT',
      date: '2026-07-09',
      status: 'Overdue',
    })
  })
})

describe('buildWeek', () => {
  it('produces seven days with correct labels and day numbers', () => {
    const week = buildWeek(new Date(2026, 6, 8), [])
    expect(week).toHaveLength(7)
    expect(week[0]).toMatchObject({ date: '2026-07-06', weekdayLabel: 'lun', dayOfMonth: 6, items: [] })
    expect(week[6]).toMatchObject({ date: '2026-07-12', weekdayLabel: 'dim', dayOfMonth: 12 })
  })

  it('places each task on the day matching its due date', () => {
    const week = buildWeek(new Date(2026, 6, 8), [
      task({ id: 'a', dueDate: '2026-07-06' }),
      task({ id: 'b', dueDate: '2026-07-08' }),
      task({ id: 'c', dueDate: '2026-07-08' }),
    ])
    expect(week[0].items.map((item) => item.taskId)).toEqual(['a'])
    expect(week[2].items.map((item) => item.taskId)).toEqual(['b', 'c'])
  })

  it('ignores tasks whose due date falls outside the visible week', () => {
    const week = buildWeek(new Date(2026, 6, 8), [task({ id: 'z', dueDate: '2026-07-20' })])
    expect(week.every((day) => day.items.length === 0)).toBe(true)
  })
})
```

- [ ] **Step 2: Lancer les tests pour vérifier qu'ils échouent**

Run: `npm test -- calendarItems`
Expected: FAIL — `calendarItems.ts` n'existe pas encore (erreur de résolution de module).

- [ ] **Step 3: Écrire l'implémentation minimale**

Create `frontend/src/features/agenda/calendarItems.ts` :

```ts
import type { TaskStatus, TaskView } from '../tasks/taskStatus'

export type CalendarItem = {
  taskId: string
  title: string
  date: string
  status: TaskStatus
}

export type CalendarDay = {
  date: string
  weekdayLabel: string
  dayOfMonth: number
  items: CalendarItem[]
}

const weekdayLabels = ['lun', 'mar', 'mer', 'jeu', 'ven', 'sam', 'dim']

export function isoDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function startOfWeek(reference: Date): Date {
  const monday = new Date(reference.getFullYear(), reference.getMonth(), reference.getDate())
  const offset = (monday.getDay() + 6) % 7
  monday.setDate(monday.getDate() - offset)
  return monday
}

export function weekDates(reference: Date): string[] {
  const monday = startOfWeek(reference)
  return Array.from({ length: 7 }, (_, index) => {
    const day = new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + index)
    return isoDate(day)
  })
}

export function taskToCalendarItem(task: TaskView): CalendarItem {
  return {
    taskId: task.id,
    title: task.title,
    date: task.dueDate,
    status: task.status,
  }
}

export function buildWeek(reference: Date, tasks: TaskView[]): CalendarDay[] {
  const items = tasks.map(taskToCalendarItem)
  return weekDates(reference).map((date, index) => {
    const [, , day] = date.split('-')
    return {
      date,
      weekdayLabel: weekdayLabels[index],
      dayOfMonth: Number(day),
      items: items.filter((item) => item.date === date),
    }
  })
}
```

- [ ] **Step 4: Lancer les tests pour vérifier qu'ils passent**

Run: `npm test -- calendarItems`
Expected: PASS (tous les `describe` verts).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/agenda/calendarItems.ts frontend/src/features/agenda/calendarItems.test.ts
git commit -m "feat(kiosk): add calendar week mapping module"
```

---

### Task 2: Hooks coquille + rafraîchissement auto

Le state de la coquille (mode consultation/gestion), une horloge pour le header, et le rafraîchissement automatique des requêtes.

**Files:**
- Create: `frontend/src/kiosk/useKioskMode.ts`
- Create: `frontend/src/kiosk/useNow.ts`
- Modify: `frontend/src/query/queryClient.ts`

**Interfaces:**
- Consumes: rien.
- Produces:
  - `type KioskMode = 'consultation' | 'gestion'`
  - `function useKioskMode(): { mode: KioskMode; isGestion: boolean; toggle: () => void; reset: () => void }`
  - `function useNow(intervalMs?: number): Date` — se réévalue périodiquement (défaut 30000 ms).

- [ ] **Step 1: Écrire `useKioskMode`**

Create `frontend/src/kiosk/useKioskMode.ts` :

```ts
import { useCallback, useState } from 'react'

export type KioskMode = 'consultation' | 'gestion'

export function useKioskMode() {
  const [mode, setMode] = useState<KioskMode>('consultation')

  const toggle = useCallback(() => {
    setMode((current) => (current === 'consultation' ? 'gestion' : 'consultation'))
  }, [])

  const reset = useCallback(() => {
    setMode('consultation')
  }, [])

  return { mode, isGestion: mode === 'gestion', toggle, reset }
}
```

- [ ] **Step 2: Écrire `useNow`**

Create `frontend/src/kiosk/useNow.ts` :

```ts
import { useEffect, useState } from 'react'

export function useNow(intervalMs = 30000): Date {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), intervalMs)
    return () => window.clearInterval(timer)
  }, [intervalMs])

  return now
}
```

- [ ] **Step 3: Activer le rafraîchissement auto sur le queryClient**

Replace the full content of `frontend/src/query/queryClient.ts` :

```ts
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchInterval: 60000,
      refetchIntervalInBackground: true,
    },
  },
})
```

- [ ] **Step 4: Vérifier la compilation**

Run: `npm run build`
Expected: build réussie (tsc + vite), aucune erreur de type.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/kiosk/useKioskMode.ts frontend/src/kiosk/useNow.ts frontend/src/query/queryClient.ts
git commit -m "feat(kiosk): add mode/clock hooks and query auto-refresh"
```

---

### Task 3: Composant `WeekCalendar`

Grille 7 colonnes (lundi→dimanche de la semaine courante) affichant les tâches en puces sur leur jour d'échéance, couleur par statut. Tap sur une puce → callback de sélection.

**Files:**
- Create: `frontend/src/features/agenda/WeekCalendar.tsx`

**Interfaces:**
- Consumes: `buildWeek` de `./calendarItems` ; `statusModifier` de `../tasks/taskStatus` ; `TaskView` de `../tasks/taskStatus`.
- Produces: `function WeekCalendar(props: { tasks: TaskView[]; reference: Date; onSelectTask: (task: TaskView) => void }): JSX.Element`.

- [ ] **Step 1: Écrire le composant**

Create `frontend/src/features/agenda/WeekCalendar.tsx` :

```tsx
import { statusModifier, type TaskView } from '../tasks/taskStatus'
import { buildWeek } from './calendarItems'

type WeekCalendarProps = {
  tasks: TaskView[]
  reference: Date
  onSelectTask: (task: TaskView) => void
}

export function WeekCalendar({ tasks, reference, onSelectTask }: WeekCalendarProps) {
  const week = buildWeek(reference, tasks)
  const tasksById = new Map(tasks.map((task) => [task.id, task]))

  return (
    <div className="week">
      {week.map((day) => (
        <div className="week__day" key={day.date}>
          <div className="week__day-head">
            <span className="week__weekday">{day.weekdayLabel}</span>
            <span className="week__daynum">{day.dayOfMonth}</span>
          </div>
          <div className="week__items">
            {day.items.map((item) => {
              const task = tasksById.get(item.taskId)
              if (!task) {
                return null
              }
              return (
                <button
                  type="button"
                  key={item.taskId}
                  className={`chip chip--${statusModifier(item.status)}`}
                  onClick={() => onSelectTask(task)}
                >
                  {item.title}
                </button>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}
```

- [ ] **Step 2: Vérifier la compilation**

Run: `npm run build`
Expected: build réussie, aucune erreur de type.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/agenda/WeekCalendar.tsx
git commit -m "feat(kiosk): add week calendar component"
```

---

### Task 4: Composant `TaskSidebar`

Colonne de gauche : trois sections `En retard`, `À faire`, `Prochainement` (l'état `Done` est masqué). Chaque tâche est un bouton qui déclenche la sélection. En mode gestion, expose édition et suppression.

**Files:**
- Create: `frontend/src/features/agenda/TaskSidebar.tsx`

**Interfaces:**
- Consumes: `TaskView`, `statusLabel`, `statusModifier` de `../tasks/taskStatus`.
- Produces: `function TaskSidebar(props: { tasks: TaskView[]; isGestion: boolean; onSelectTask: (task: TaskView) => void; onEditTask: (task: TaskView) => void; onDeleteTask: (task: TaskView) => void }): JSX.Element`.

- [ ] **Step 1: Écrire le composant**

Create `frontend/src/features/agenda/TaskSidebar.tsx` :

```tsx
import { statusLabel, statusModifier, type TaskStatus, type TaskView } from '../tasks/taskStatus'

type TaskSidebarProps = {
  tasks: TaskView[]
  isGestion: boolean
  onSelectTask: (task: TaskView) => void
  onEditTask: (task: TaskView) => void
  onDeleteTask: (task: TaskView) => void
}

const sidebarSections: TaskStatus[] = ['Overdue', 'Due', 'Upcoming']

export function TaskSidebar({ tasks, isGestion, onSelectTask, onEditTask, onDeleteTask }: TaskSidebarProps) {
  return (
    <div className="sidebar">
      {sidebarSections.map((status) => {
        const sectionTasks = tasks.filter((task) => task.status === status)
        return (
          <section className="sidebar__section" key={status}>
            <h2 className="sidebar__title">
              <span className={`sidebar__dot sidebar__dot--${statusModifier(status)}`} />
              {statusLabel(status)}
              <span className="sidebar__count">{sectionTasks.length}</span>
            </h2>
            {sectionTasks.length === 0 ? (
              <p className="sidebar__empty">Rien ici.</p>
            ) : (
              <ul className="sidebar__list">
                {sectionTasks.map((task) => (
                  <li className={`sidebar__item sidebar__item--${statusModifier(task.status)}`} key={task.id}>
                    <button type="button" className="sidebar__task" onClick={() => onSelectTask(task)}>
                      <span className="sidebar__task-title">{task.title}</span>
                      {task.daysOverdue > 0 ? (
                        <span className="sidebar__task-late">{task.daysOverdue} j</span>
                      ) : null}
                    </button>
                    {isGestion ? (
                      <span className="sidebar__actions">
                        <button type="button" className="btn btn--ghost btn--sm" onClick={() => onEditTask(task)}>
                          Éditer
                        </button>
                        <button type="button" className="btn btn--danger btn--sm" onClick={() => onDeleteTask(task)}>
                          Suppr.
                        </button>
                      </span>
                    ) : null}
                  </li>
                ))}
              </ul>
            )}
          </section>
        )
      })}
    </div>
  )
}
```

- [ ] **Step 2: Vérifier la compilation**

Run: `npm run build`
Expected: build réussie, aucune erreur de type.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/agenda/TaskSidebar.tsx
git commit -m "feat(kiosk): add task sidebar with status sections"
```

---

### Task 5: Tab Domotique (placeholder)

Placeholder plein écran jusqu'au SP3.

**Files:**
- Create: `frontend/src/features/domotique/DomotiqueTab.tsx`

**Interfaces:**
- Produces: `function DomotiqueTab(): JSX.Element`.

- [ ] **Step 1: Écrire le composant**

Create `frontend/src/features/domotique/DomotiqueTab.tsx` :

```tsx
export function DomotiqueTab() {
  return (
    <div className="placeholder">
      <p className="placeholder__icon">⌂</p>
      <h2 className="placeholder__title">Domotique</h2>
      <p className="placeholder__text">Le contrôle de la maison arrivera bientôt.</p>
    </div>
  )
}
```

- [ ] **Step 2: Vérifier la compilation**

Run: `npm run build`
Expected: build réussie.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/domotique/DomotiqueTab.tsx
git commit -m "feat(kiosk): add domotique placeholder tab"
```

---

### Task 6: Tab Agenda (intégration)

Compose `TaskSidebar` + `WeekCalendar`. Gère la sélection d'une tâche (ouvre `MarkDoneDialog`), et en mode gestion le bouton `+ Nouvelle tâche`, l'ouverture de `TaskForm` (création/édition) et l'accès `MembersScreen`. Toute la logique de mutation est passée en props depuis `App.tsx` (Task 7) pour garder ce composant testable et découplé.

**Files:**
- Create: `frontend/src/features/agenda/AgendaTab.tsx`

**Interfaces:**
- Consumes: `TaskSidebar`, `WeekCalendar` ; `MarkDoneDialog`, `TaskForm` de `../tasks/*` ; `MembersScreen` de `../members/MembersScreen` ; `TaskView` de `../tasks/taskStatus` ; `Member` de `../members/useMembers` ; `TaskInput`, `MarkDoneInput` de `../tasks/useTasks`.
- Produces: `function AgendaTab(props: AgendaTabProps): JSX.Element` avec

  ```ts
  type AgendaTabProps = {
    tasks: TaskView[]
    members: Member[]
    isGestion: boolean
    reference: Date
    onCreateTask: (input: TaskInput) => void
    onUpdateTask: (id: string, input: TaskInput) => void
    onDeleteTask: (task: TaskView) => void
    onMarkDone: (input: MarkDoneInput) => void
  }
  ```

- [ ] **Step 1: Écrire le composant**

Create `frontend/src/features/agenda/AgendaTab.tsx` :

```tsx
import { useState } from 'react'
import { MembersScreen } from '../members/MembersScreen'
import type { Member } from '../members/useMembers'
import { MarkDoneDialog } from '../tasks/MarkDoneDialog'
import { TaskForm } from '../tasks/TaskForm'
import type { TaskView } from '../tasks/taskStatus'
import type { MarkDoneInput, TaskInput } from '../tasks/useTasks'
import { TaskSidebar } from './TaskSidebar'
import { WeekCalendar } from './WeekCalendar'

type AgendaTabProps = {
  tasks: TaskView[]
  members: Member[]
  isGestion: boolean
  reference: Date
  onCreateTask: (input: TaskInput) => void
  onUpdateTask: (id: string, input: TaskInput) => void
  onDeleteTask: (task: TaskView) => void
  onMarkDone: (input: MarkDoneInput) => void
}

export function AgendaTab({
  tasks,
  members,
  isGestion,
  reference,
  onCreateTask,
  onUpdateTask,
  onDeleteTask,
  onMarkDone,
}: AgendaTabProps) {
  const [taskToComplete, setTaskToComplete] = useState<TaskView | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [taskToEdit, setTaskToEdit] = useState<TaskView | null>(null)

  const closeForm = () => {
    setShowForm(false)
    setTaskToEdit(null)
  }

  return (
    <div className="agenda">
      <aside className="agenda__sidebar">
        {isGestion ? (
          <button
            type="button"
            className="btn btn--primary agenda__new"
            onClick={() => {
              setTaskToEdit(null)
              setShowForm(true)
            }}
          >
            + Nouvelle tâche
          </button>
        ) : null}
        <TaskSidebar
          tasks={tasks}
          isGestion={isGestion}
          onSelectTask={setTaskToComplete}
          onEditTask={(task) => {
            setTaskToEdit(task)
            setShowForm(true)
          }}
          onDeleteTask={onDeleteTask}
        />
        {isGestion ? <MembersScreen /> : null}
      </aside>

      <section className="agenda__calendar">
        <WeekCalendar tasks={tasks} reference={reference} onSelectTask={setTaskToComplete} />
      </section>

      {showForm ? (
        <div className="modal-backdrop" onClick={closeForm}>
          <div onClick={(event) => event.stopPropagation()}>
            <TaskForm
              key={taskToEdit?.id ?? 'new'}
              members={members}
              initialTask={taskToEdit ?? undefined}
              onCancel={closeForm}
              onSubmit={(input) => {
                if (taskToEdit) {
                  onUpdateTask(taskToEdit.id, input)
                } else {
                  onCreateTask(input)
                }
                closeForm()
              }}
            />
          </div>
        </div>
      ) : null}

      {taskToComplete ? (
        <div className="modal-backdrop" onClick={() => setTaskToComplete(null)}>
          <div onClick={(event) => event.stopPropagation()}>
            <MarkDoneDialog
              task={taskToComplete}
              members={members}
              requiresNextDue={taskToComplete.requiresNextDueOverride}
              onCancel={() => setTaskToComplete(null)}
              onConfirm={(input) => {
                onMarkDone(input)
                setTaskToComplete(null)
              }}
            />
          </div>
        </div>
      ) : null}
    </div>
  )
}
```

- [ ] **Step 2: Vérifier la compilation**

Run: `npm run build`
Expected: build réussie, aucune erreur de type.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/features/agenda/AgendaTab.tsx
git commit -m "feat(kiosk): add agenda tab composing sidebar and calendar"
```

---

### Task 7: Coquille `KioskShell` + réécriture `App.tsx` + styles

Header (marque, date/heure, bouton Gestion), tabs Agenda/Domotique, corps affichant le tab actif. `App.tsx` câble les hooks de données et les mutations. Styles kiosque (layout paysage, variables de statut) ajoutés à `App.css`.

**Files:**
- Create: `frontend/src/kiosk/KioskShell.tsx`
- Modify: `frontend/src/App.tsx` (réécriture complète)
- Modify: `frontend/src/App.css` (ajouts kiosque)

**Interfaces:**
- Consumes: `useKioskMode`, `useNow` de `../kiosk/*` ; `useTasks`, `useCreateTask`, `useUpdateTask`, `useDeleteTask`, `useMarkTaskDone` ; `useMembers` ; `AgendaTab` ; `DomotiqueTab`.
- Produces:
  - `type KioskTab = 'agenda' | 'domotique'`
  - `function KioskShell(props: { now: Date; isGestion: boolean; onToggleMode: () => void; activeTab: KioskTab; onSelectTab: (tab: KioskTab) => void; children: ReactNode }): JSX.Element`

- [ ] **Step 1: Écrire `KioskShell`**

Create `frontend/src/kiosk/KioskShell.tsx` :

```tsx
import type { ReactNode } from 'react'

export type KioskTab = 'agenda' | 'domotique'

type KioskShellProps = {
  now: Date
  isGestion: boolean
  onToggleMode: () => void
  activeTab: KioskTab
  onSelectTab: (tab: KioskTab) => void
  children: ReactNode
}

const tabs: { id: KioskTab; label: string }[] = [
  { id: 'agenda', label: 'Agenda' },
  { id: 'domotique', label: 'Domotique' },
]

function formatDate(now: Date): string {
  return now.toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long' })
}

function formatTime(now: Date): string {
  return now.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
}

export function KioskShell({ now, isGestion, onToggleMode, activeTab, onSelectTab, children }: KioskShellProps) {
  return (
    <main className="kiosk">
      <header className="kiosk__header">
        <div className="kiosk__brand">
          <span className="kiosk__logo">Hominder</span>
          <span className="kiosk__datetime">
            <span className="kiosk__date">{formatDate(now)}</span>
            <span className="kiosk__time">{formatTime(now)}</span>
          </span>
        </div>
        <nav className="kiosk__tabs">
          {tabs.map((tab) => (
            <button
              type="button"
              key={tab.id}
              className={`kiosk__tab ${activeTab === tab.id ? 'kiosk__tab--active' : ''}`}
              onClick={() => onSelectTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </nav>
        <button
          type="button"
          className={`btn ${isGestion ? 'btn--primary' : 'btn--ghost'}`}
          onClick={onToggleMode}
        >
          {isGestion ? '✓ Gestion' : '⚙ Gestion'}
        </button>
      </header>
      <div className="kiosk__body">{children}</div>
    </main>
  )
}
```

- [ ] **Step 2: Réécrire `App.tsx`**

Replace the full content of `frontend/src/App.tsx` :

```tsx
import { useState } from 'react'
import './App.css'
import { AgendaTab } from './features/agenda/AgendaTab'
import { DomotiqueTab } from './features/domotique/DomotiqueTab'
import { useMembers } from './features/members/useMembers'
import type { TaskView } from './features/tasks/taskStatus'
import {
  useCreateTask,
  useDeleteTask,
  useMarkTaskDone,
  useTasks,
  useUpdateTask,
} from './features/tasks/useTasks'
import { KioskShell, type KioskTab } from './kiosk/KioskShell'
import { useKioskMode } from './kiosk/useKioskMode'
import { useNow } from './kiosk/useNow'

function App() {
  const now = useNow()
  const tasks = useTasks()
  const members = useMembers()
  const createTask = useCreateTask()
  const updateTask = useUpdateTask()
  const deleteTask = useDeleteTask()
  const markDone = useMarkTaskDone()

  const { isGestion, toggle } = useKioskMode()
  const [activeTab, setActiveTab] = useState<KioskTab>('agenda')

  const taskError =
    createTask.error?.message ??
    updateTask.error?.message ??
    deleteTask.error?.message ??
    markDone.error?.message ??
    tasks.error?.message ??
    null

  return (
    <KioskShell
      now={now}
      isGestion={isGestion}
      onToggleMode={toggle}
      activeTab={activeTab}
      onSelectTab={setActiveTab}
    >
      {taskError ? (
        <p className="alert" role="alert">
          {taskError}
        </p>
      ) : null}

      {tasks.isLoading ? <p className="loading">Chargement…</p> : null}

      {activeTab === 'agenda' ? (
        <AgendaTab
          tasks={tasks.data ?? []}
          members={members.data ?? []}
          isGestion={isGestion}
          reference={now}
          onCreateTask={(input) => createTask.mutate(input)}
          onUpdateTask={(id, input) => updateTask.mutate({ id, input })}
          onDeleteTask={(task: TaskView) => deleteTask.mutate(task.id)}
          onMarkDone={(input) => markDone.mutate(input)}
        />
      ) : (
        <DomotiqueTab />
      )}
    </KioskShell>
  )
}

export default App
```

- [ ] **Step 3: Ajouter les styles kiosque**

Append to `frontend/src/App.css` :

```css
:root {
  --status-overdue: #dc2626;
  --status-due: #f59e0b;
  --status-upcoming: #6b7280;
  --status-done: #10b981;
  --kiosk-gap: 16px;
}

.kiosk {
  display: flex;
  flex-direction: column;
  height: 100vh;
  width: 100vw;
  overflow: hidden;
}

.kiosk__header {
  display: flex;
  align-items: center;
  gap: var(--kiosk-gap);
  padding: 12px 24px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.1);
}

.kiosk__brand {
  display: flex;
  align-items: baseline;
  gap: 16px;
}

.kiosk__logo {
  font-size: 1.6rem;
  font-weight: 700;
}

.kiosk__datetime {
  display: flex;
  align-items: baseline;
  gap: 8px;
  color: #6b7280;
}

.kiosk__time {
  font-variant-numeric: tabular-nums;
  font-weight: 600;
}

.kiosk__tabs {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.kiosk__tab {
  border: none;
  background: transparent;
  padding: 8px 20px;
  font-size: 1.1rem;
  border-radius: 999px;
  cursor: pointer;
}

.kiosk__tab--active {
  background: #111827;
  color: #fff;
}

.kiosk__body {
  flex: 1;
  min-height: 0;
  overflow: hidden;
  padding: var(--kiosk-gap);
}

.agenda {
  display: grid;
  grid-template-columns: minmax(280px, 340px) 1fr;
  gap: var(--kiosk-gap);
  height: 100%;
  min-height: 0;
}

.agenda__sidebar {
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.agenda__calendar {
  overflow: auto;
  min-width: 0;
}

.agenda__new {
  width: 100%;
}

.sidebar__section {
  margin-bottom: 8px;
}

.sidebar__title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
  margin: 0 0 6px;
}

.sidebar__dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.sidebar__dot--overdue {
  background: var(--status-overdue);
}

.sidebar__dot--due {
  background: var(--status-due);
}

.sidebar__dot--upcoming {
  background: var(--status-upcoming);
}

.sidebar__count {
  margin-left: auto;
  color: #9ca3af;
}

.sidebar__empty {
  color: #9ca3af;
  font-size: 0.85rem;
  margin: 0 0 8px;
}

.sidebar__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.sidebar__item {
  display: flex;
  align-items: center;
  gap: 8px;
  border-left: 4px solid transparent;
  padding-left: 8px;
}

.sidebar__item--overdue {
  border-left-color: var(--status-overdue);
}

.sidebar__item--due {
  border-left-color: var(--status-due);
}

.sidebar__item--upcoming {
  border-left-color: var(--status-upcoming);
}

.sidebar__task {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  border: none;
  background: transparent;
  text-align: left;
  padding: 6px 4px;
  cursor: pointer;
  font-size: 1rem;
}

.sidebar__task-late {
  color: var(--status-overdue);
  font-weight: 600;
  font-size: 0.85rem;
}

.sidebar__actions {
  display: flex;
  gap: 4px;
}

.week {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 8px;
  height: 100%;
  min-height: 0;
}

.week__day {
  display: flex;
  flex-direction: column;
  border: 1px solid rgba(0, 0, 0, 0.08);
  border-radius: 12px;
  overflow: hidden;
  min-height: 0;
}

.week__day-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  padding: 8px 10px;
  background: rgba(0, 0, 0, 0.03);
}

.week__weekday {
  text-transform: capitalize;
  color: #6b7280;
  font-size: 0.85rem;
}

.week__daynum {
  font-weight: 700;
}

.week__items {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 8px;
  overflow-y: auto;
}

.chip {
  border: none;
  border-radius: 8px;
  padding: 6px 8px;
  text-align: left;
  cursor: pointer;
  color: #fff;
  font-size: 0.85rem;
  line-height: 1.2;
}

.chip--overdue {
  background: var(--status-overdue);
}

.chip--due {
  background: var(--status-due);
}

.chip--upcoming {
  background: var(--status-upcoming);
}

.chip--done {
  background: var(--status-done);
}

.placeholder {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: #9ca3af;
}

.placeholder__icon {
  font-size: 4rem;
  margin: 0;
}

.placeholder__title {
  margin: 0;
  color: #6b7280;
}
```

- [ ] **Step 4: Vérifier la compilation**

Run: `npm run build`
Expected: build réussie, aucune erreur de type.

- [ ] **Step 5: Vérification manuelle**

Démarrer le backend (voir racine repo) puis :

Run: `npm run dev`
Attendu au chargement (`http://localhost:5173`) :
- Header « Hominder », date + heure, tabs `Agenda` / `Domotique`, bouton `⚙ Gestion`.
- Tab Agenda : colonne gauche avec sections `En retard` / `À faire` / `Prochainement`, calendrier semaine 7 colonnes à droite ; les tâches de la semaine courante apparaissent en puces colorées sur leur jour d'échéance.
- Clic sur une tâche (puce ou sidebar) → dialogue « c'est fait ».
- Clic sur `⚙ Gestion` → apparition de `+ Nouvelle tâche`, boutons Éditer/Suppr. sur les tâches, section Membres. Nouveau clic → retour consultation.
- Tab Domotique → placeholder « Bientôt ».

- [ ] **Step 6: Commit**

```bash
git add frontend/src/kiosk/KioskShell.tsx frontend/src/App.tsx frontend/src/App.css
git commit -m "feat(kiosk): assemble shell, tabs and kiosk layout"
```

---

## Notes de nettoyage

`TaskList.tsx` n'est plus référencé après cette refonte (`TaskSidebar` le remplace dans le kiosque). Ne pas le supprimer dans ce plan sauf si une vérification `grep -r "TaskList" frontend/src` ne renvoie que sa propre définition ; le cas échéant, sa suppression peut faire l'objet d'un commit `chore` séparé. `TaskCard.tsx` reste utilisable mais n'est plus monté ; le laisser en place.
