import { useState } from 'react'
import './App.css'
import { MembersScreen } from './features/members/MembersScreen'
import { useMembers } from './features/members/useMembers'
import { MarkDoneDialog } from './features/tasks/MarkDoneDialog'
import { TaskForm } from './features/tasks/TaskForm'
import { TaskList } from './features/tasks/TaskList'
import type { TaskView } from './features/tasks/taskStatus'
import {
  useCreateTask,
  useDeleteTask,
  useMarkTaskDone,
  useTasks,
  useUpdateTask,
} from './features/tasks/useTasks'

function App() {
  const tasks = useTasks()
  const members = useMembers()
  const createTask = useCreateTask()
  const updateTask = useUpdateTask()
  const deleteTask = useDeleteTask()
  const markDone = useMarkTaskDone()

  const [showForm, setShowForm] = useState(false)
  const [taskToEdit, setTaskToEdit] = useState<TaskView | null>(null)
  const [taskToComplete, setTaskToComplete] = useState<TaskView | null>(null)

  const memberList = members.data ?? []

  const closeForm = () => {
    setShowForm(false)
    setTaskToEdit(null)
  }

  const taskError =
    createTask.error?.message ??
    updateTask.error?.message ??
    deleteTask.error?.message ??
    markDone.error?.message ??
    null

  return (
    <main className="app">
      <header className="app__bar">
        <h1 className="app__brand">Hominder</h1>
        <button
          type="button"
          className="btn btn--primary"
          onClick={() => {
            setTaskToEdit(null)
            setShowForm(true)
          }}
        >
          + Nouvelle tâche
        </button>
      </header>

      <div className="app__content">
        {taskError ? (
          <p className="alert" role="alert">
            {taskError}
          </p>
        ) : null}

        {tasks.isLoading ? <p className="loading">Chargement…</p> : null}

        <TaskList
          tasks={tasks.data ?? []}
          onMarkDone={(task) => setTaskToComplete(task)}
          onEdit={(task) => {
            setTaskToEdit(task)
            setShowForm(true)
          }}
          onDelete={(task) => deleteTask.mutate(task.id)}
        />

        <MembersScreen />
      </div>

      {showForm ? (
        <div className="modal-backdrop" onClick={closeForm}>
          <div onClick={(event) => event.stopPropagation()}>
            <TaskForm
              key={taskToEdit?.id ?? 'new'}
              members={memberList}
              initialTask={taskToEdit ?? undefined}
              onCancel={closeForm}
              onSubmit={(input) => {
                if (taskToEdit) {
                  updateTask.mutate({ id: taskToEdit.id, input }, { onSuccess: closeForm })
                } else {
                  createTask.mutate(input, { onSuccess: closeForm })
                }
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
              members={memberList}
              requiresNextDue={taskToComplete.requiresNextDueOverride}
              onCancel={() => setTaskToComplete(null)}
              onConfirm={(input) => {
                markDone.mutate(input, { onSuccess: () => setTaskToComplete(null) })
              }}
            />
          </div>
        </div>
      ) : null}
    </main>
  )
}

export default App
