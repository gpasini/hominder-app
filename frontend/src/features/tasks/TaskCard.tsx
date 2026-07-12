import { statusLabel, statusModifier, type TaskView } from './taskStatus'

type TaskCardProps = {
  task: TaskView
  onMarkDone: (task: TaskView) => void
  onEdit: (task: TaskView) => void
  onDelete: (task: TaskView) => void
}

function formatDate(value: string): string {
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return value
  }
  return parsed.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })
}

function initial(name: string): string {
  return name.trim().charAt(0).toUpperCase()
}

export function TaskCard({ task, onMarkDone, onEdit, onDelete }: TaskCardProps) {
  const modifier = statusModifier(task.status)

  return (
    <article className={`card card--${modifier}`}>
      <div className="card__main">
        <h3 className="card__title">{task.title}</h3>
        <div className="card__meta">
          <span className={`badge badge--${modifier}`}>{statusLabel(task.status)}</span>
          {task.daysOverdue > 0 ? (
            <span className="card__meta-item">⏱ {task.daysOverdue} jour(s) de retard</span>
          ) : null}
          <span className="card__meta-item">📅 {formatDate(task.dueDate)}</span>
          {task.assigneeName ? (
            <span className="card__meta-item">
              <span className="avatar">{initial(task.assigneeName)}</span>
              {task.assigneeName}
            </span>
          ) : null}
        </div>
      </div>
      <div className="card__actions">
        {task.status === 'Done' ? null : (
          <button type="button" className="btn btn--done btn--sm" onClick={() => onMarkDone(task)}>
            ✓ Fait
          </button>
        )}
        <button type="button" className="btn btn--ghost btn--sm" onClick={() => onEdit(task)}>
          Éditer
        </button>
        <button type="button" className="btn btn--danger btn--sm" onClick={() => onDelete(task)}>
          Supprimer
        </button>
      </div>
    </article>
  )
}
