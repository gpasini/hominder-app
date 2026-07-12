import { useState } from 'react'
import type { Member } from '../members/useMembers'
import type { RecurrenceKind, RecurrencePolicyInput, RecurrenceUnit, TaskView } from './taskStatus'
import type { TaskInput } from './useTasks'

type TaskFormProps = {
  members: Member[]
  initialTask?: TaskView
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

function defaultPolicyFor(kind: RecurrenceKind): RecurrencePolicyInput {
  switch (kind) {
    case 'Interval':
      return { ...emptyPolicy, kind, intervalAmount: 1, intervalUnit: 'Years' }
    case 'MonthWindow':
      return { ...emptyPolicy, kind, startMonth: 3, endMonth: 5 }
    case 'FixedDate':
    case 'OneOff':
      return { ...emptyPolicy, kind }
  }
}

function isPolicyComplete(policy: RecurrencePolicyInput): boolean {
  switch (policy.kind) {
    case 'Interval':
      return (
        policy.intervalAmount !== null &&
        policy.intervalAmount >= 1 &&
        policy.intervalUnit !== null &&
        policy.startReference !== null &&
        policy.startReference.length > 0
      )
    case 'MonthWindow':
      return (
        policy.startMonth !== null &&
        policy.startMonth >= 1 &&
        policy.startMonth <= 12 &&
        policy.endMonth !== null &&
        policy.endMonth >= 1 &&
        policy.endMonth <= 12
      )
    case 'FixedDate':
    case 'OneOff':
      return policy.dueDate !== null && policy.dueDate.length > 0
  }
}

export function TaskForm({ members, initialTask, onSubmit, onCancel }: TaskFormProps) {
  const [title, setTitle] = useState(initialTask?.title ?? '')
  const [notes, setNotes] = useState(initialTask?.notes ?? '')
  const [assigneeId, setAssigneeId] = useState(initialTask?.assigneeId ?? '')
  const [policy, setPolicy] = useState<RecurrencePolicyInput>(initialTask?.policy ?? emptyPolicy)
  const [validationMessage, setValidationMessage] = useState<string | null>(null)

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    if (title.trim().length === 0) {
      return
    }
    if (!isPolicyComplete(policy)) {
      setValidationMessage('Merci de compléter les champs de récurrence.')
      return
    }
    setValidationMessage(null)
    onSubmit({
      title: title.trim(),
      notes: notes.trim().length === 0 ? null : notes.trim(),
      policy,
      assigneeId: assigneeId.length === 0 ? null : assigneeId,
    })
  }

  const setKind = (kind: RecurrenceKind) => setPolicy(defaultPolicyFor(kind))

  return (
    <form className="modal" onSubmit={submit}>
      <h3 className="modal__title">{initialTask ? 'Modifier la tâche' : 'Nouvelle tâche'}</h3>
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
          <label>
            Mois de début
            <input
              type="number"
              min={1}
              max={12}
              value={policy.startMonth ?? 1}
              onChange={(event) => setPolicy({ ...policy, startMonth: Number(event.target.value) })}
            />
          </label>
          <label>
            Mois de fin
            <input
              type="number"
              min={1}
              max={12}
              value={policy.endMonth ?? 1}
              onChange={(event) => setPolicy({ ...policy, endMonth: Number(event.target.value) })}
            />
          </label>
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

      {validationMessage ? (
        <p className="alert" role="alert">
          {validationMessage}
        </p>
      ) : null}

      <div className="modal__actions">
        <button type="button" className="btn btn--ghost" onClick={onCancel}>
          Annuler
        </button>
        <button type="submit" className="btn btn--primary">
          Enregistrer
        </button>
      </div>
    </form>
  )
}
