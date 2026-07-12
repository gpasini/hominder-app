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
