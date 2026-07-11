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
    <main>
      <header>
        <h1>Hominder</h1>
      </header>

      <button
        type="button"
        onClick={() => {
          setTaskToEdit(null)
          setShowForm(true)
        }}
      >
        Nouvelle tâche
      </button>

      {taskError ? <p role="alert">{taskError}</p> : null}

      {showForm ? (
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
      ) : null}

      {tasks.isLoading ? <p>Chargement…</p> : null}

      <TaskList
        tasks={tasks.data ?? []}
        onMarkDone={(task) => setTaskToComplete(task)}
        onEdit={(task) => {
          setTaskToEdit(task)
          setShowForm(true)
        }}
        onDelete={(task) => deleteTask.mutate(task.id)}
      />

      {taskToComplete ? (
        <MarkDoneDialog
          task={taskToComplete}
          members={memberList}
          requiresNextDue={taskToComplete.requiresNextDueOverride}
          onCancel={() => setTaskToComplete(null)}
          onConfirm={(input) => {
            markDone.mutate(input, { onSuccess: () => setTaskToComplete(null) })
          }}
        />
      ) : null}

      <MembersScreen />
    </main>
  )
}

export default App
