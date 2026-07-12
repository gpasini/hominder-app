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
    <div className="modal" role="dialog">
      <h3 className="modal__title">Marquer « {task.title} » comme fait</h3>
      <label>
        Fait le
        <input type="date" value={completedOn} onChange={(event) => setCompletedOn(event.target.value)} />
      </label>
      <label>
        Par
        <select value={completedBy} onChange={(event) => setCompletedBy(event.target.value)}>
          {members.map((member) => (
            <option key={member.id} value={member.id}>
              {member.name}
            </option>
          ))}
        </select>
      </label>
      {requiresNextDue ? (
        <label>
          Prochaine échéance
          <input type="date" value={nextDueOverride} onChange={(event) => setNextDueOverride(event.target.value)} />
        </label>
      ) : null}
      <div className="modal__actions">
        <button type="button" className="btn btn--ghost" onClick={onCancel}>
          Annuler
        </button>
        <button type="button" className="btn btn--primary" onClick={confirm}>
          Confirmer
        </button>
      </div>
    </div>
  )
}
