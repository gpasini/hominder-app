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
