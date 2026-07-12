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
