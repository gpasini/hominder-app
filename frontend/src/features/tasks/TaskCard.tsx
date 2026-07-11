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
