# Kiosque — coquille + agenda tâches (SP1) — Design

Date : 2026-07-12
Statut : validé, prêt pour la planification

## Contexte

La spec du moteur de tâches récurrentes (`2026-07-10-moteur-taches-recurrentes-design.md`)
décomposait Hominder en trois briques : (1) moteur + UI de gestion — livré, (2) affichage
kiosque, (3) surcouche domotique Home Assistant.

Cette spec précise et **remplace** la vision d'origine de la brique 2 (« petit écran en
lecture seule »). Après cadrage, le kiosque n'est pas une application séparée : **le
frontend existant est retravaillé pour *devenir* le kiosque**, dans le même repo et au
même emplacement (`frontend/`). Il conserve les capacités de gestion (créer/éditer une
tâche, gérer les membres) et devient le tableau de bord d'entrée du foyer.

Le kiosque final vise trois surfaces : agenda (calendrier + tâches), planning Google
Calendar, contrôle domotique Home Assistant. Elles proviennent de **trois sources
distinctes** :

- **Tâches** → API Hominder (`GET /api/tasks`), déjà disponible.
- **Planning** → Google Calendar (OAuth), non encore construit.
- **Domotique** → Home Assistant, non encore construit.

Le projet se redécompose donc en sous-projets, chacun avec sa propre spec → plan →
implémentation :

| Sous-projet | Dépend de | Livrable |
|---|---|---|
| **SP1 — Coquille kiosque + agenda tâches** (cette spec) | existant uniquement | Coquille à tabs, tab Agenda (calendrier semaine + colonne tâches), modes consultation/gestion, tab Domotique en placeholder |
| **SP2 — Volet Planning (Google Calendar)** | proxy OAuth backend | Événements mêlés dans le même calendrier, lecture seule |
| **SP3 — Volet Domotique (Home Assistant)** | proxy HA backend | Tuiles de contrôle |

Chaque source externe (Google, Home Assistant) implique un **proxy backend** dans l'Api
Hominder, car les jetons OAuth/HA sont des secrets qui ne peuvent pas vivre dans le
frontend du kiosque. Ces proxys sont hors périmètre SP1.

## Décisions produit

- **Le frontend devient le kiosque.** Pas d'application séparée en lecture seule.
- **Support : écran / tablette en paysage.** Layout horizontal, lisible à distance.
- **Deux modes explicites :**
  - **Consultation** (défaut) — glanceable, gros, aucune action requise ; le kiosque
    reste allumé et se rafraîchit seul.
  - **Gestion** — bascule par un bouton `⚙ Gestion` ; débloque création/édition/suppression
    de tâches et gestion des membres via les overlays existants.
- **Tabs en haut de l'écran :** `Agenda` et `Domotique`.
- **Agenda = tâches + calendrier réunis :** à gauche une colonne Tâches (sections
  `En retard`, `À faire`, `Prochainement`), à droite un calendrier **semaine** où les
  tâches apparaissent placées sur leur échéance, mêlées (plus tard) aux événements du
  planning.
- **Vue calendrier : semaine** par défaut (7 colonnes de jours). Jour/Mois hors v1.
- **Tap sur une tâche** (colonne ou calendrier) → détail + « c'est fait » (dialogue
  existant `MarkDoneDialog`). Rien d'autre n'est mutable en consultation.
- **Domotique : placeholder** plein écran (« Bientôt ») jusqu'au SP3.

## Décisions techniques

- **Déploiement local, réseau de confiance, aucune authentification.** On conserve
  l'hypothèse « foyer unique » d'origine. Le kiosque n'est **pas** exposé sur un domaine
  public. Si l'exposition externe revient un jour, elle rouvrira la question de
  l'authentification comme brique dédiée (SP0) — hors périmètre ici.
- **Aucun changement backend.** SP1 consomme l'API existante telle quelle.
- **Pas de librairie de routing.** Tabs et mode sont du state local React
  (`useState` / contexte léger). Une seule page.
- **Calendrier semaine fait main, pas de librairie de calendrier.** Au SP1 les tâches
  sont des marqueurs « journée » : `dueDate` est une `DateOnly` sans heure, donc une
  grille de 7 colonnes suffit. On n'introduira une librairie que si les événements
  horaires de Google (SP2) l'imposent (YAGNI, deps minces).
- **Rafraîchissement auto** via `refetchInterval` TanStack Query (~60 s) + recalcul de
  `today` pour que statut et jours de retard restent frais sans rechargement.
- **CSS.** Premier vrai travail visuel (paysage, lisibilité à distance). Pas de design
  system lourd, mais des variables CSS cohérentes (couleurs de statut, espacements,
  échelle typographique).
- **Zéro commentaire** (convention repo) : noms explicites, structure claire.

## Structure frontend

Refonte de `App.tsx` en coquille kiosque. Les composants CRUD existants
(`TaskForm`, `MembersScreen`, `MarkDoneDialog`, `TaskCard`, `TaskList`) sont **conservés
et rebranchés**, pas réécrits.

```
frontend/src/
  App.tsx                    coquille : header + tabs + bascule mode
  kiosk/
    KioskShell.tsx           header (marque, horloge, date), tabs, bouton Gestion
    useKioskMode.ts          'consultation' | 'gestion'
  features/agenda/
    AgendaTab.tsx            colonne Tâches + calendrier semaine côte à côte
    TaskSidebar.tsx          sections En retard / À faire / Prochainement
    WeekCalendar.tsx         grille 7 jours, tâches placées à leur échéance
    calendarItems.ts         mappe TaskView -> item de calendrier (+ tests Vitest)
  features/domotique/
    DomotiqueTab.tsx         placeholder « Bientôt »
  features/tasks/            existant, ré-hébergé (TaskForm, MarkDoneDialog, TaskCard…)
  features/members/          existant, ouvert depuis le mode gestion
```

## Comportements par surface

### Coquille (`KioskShell`)

- Header : marque « Hominder », date + heure courantes, bouton `⚙ Gestion`
  (toggle consultation ↔ gestion).
- Tabs : `Agenda` (défaut) et `Domotique`. Sélection = state local.

### Tab Agenda — consultation

- **`TaskSidebar`** à gauche : trois sections triées par urgence, alimentées par
  `useTasks` (groupables via `groupByStatus` existant) —
  `En retard` (Overdue), `À faire` (Due), `Prochainement` (Upcoming). L'état `Done`
  n'apparaît pas dans la sidebar de consultation.
- **`WeekCalendar`** à droite : 7 colonnes (lun→dim de la semaine courante). Chaque tâche
  est rendue comme puce sur la colonne de sa `dueDate`, couleur selon le statut
  (Overdue / Due / Upcoming). Les tâches dont l'échéance tombe hors de la semaine visible
  restent listées dans la sidebar.
- **Tap sur une tâche** (sidebar ou puce calendrier) → ouvre `MarkDoneDialog` (détail +
  « c'est fait », choix de la date/membre, nouvelle date pour `FixedDatePolicy`).
- Rafraîchissement auto ; aucune autre action mutable.

### Tab Agenda — gestion

- Bascule via `⚙ Gestion`. Débloque :
  - bouton `+ Nouvelle tâche` (ouvre `TaskForm`),
  - édition / suppression sur les cartes de tâches,
  - accès à la gestion des **Membres** (`MembersScreen`).
- Réutilise les overlays modaux actuels. Retour en consultation d'un clic.

### Tab Domotique

- Placeholder plein écran (« Bientôt »). Aucune logique jusqu'au SP3.

## Mapping des données (`calendarItems.ts`)

- Entrée : `TaskView[]` (déjà enrichi : `status`, `openDate`, `dueDate`, `daysOverdue`,
  `assigneeName`).
- Sortie : items de calendrier `{ taskId, title, date: dueDate, status }` pour placement
  dans `WeekCalendar`, plus un helper de regroupement par jour de la semaine visible.
- Les politiques à fenêtre (`MonthWindowPolicy`) sont rendues à leur `dueDate` en v1
  (pas de span multi-jours) — simplification assumée, réévaluable plus tard.

## Tests

- **`calendarItems.ts`** — mapping `TaskView` → item, regroupement par jour, couleur de
  statut : testé en Vitest.
- **`taskStatus.test.ts`** existant — conservé.
- Le reste (layout paysage, modes, tabs) est surtout visuel/comportemental et peu
  testable unitairement ; vérifié manuellement.

## Hors périmètre (SP1)

- Événements Google Calendar dans le calendrier (SP2) et son proxy OAuth backend.
- Contrôle domotique réel (SP3) et son proxy Home Assistant backend.
- Authentification / exposition publique par domaine (rouvrirait une brique SP0 dédiée).
- Vues calendrier Jour et Mois (semaine seule d'abord).
- Toute modification backend.
