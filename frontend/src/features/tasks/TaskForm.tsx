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
