import { useState } from 'react'
import './App.css'
import { MembersScreen } from './features/members/MembersScreen'
import { useMembers } from './features/members/useMembers'
import { MarkDoneDialog } from './features/tasks/MarkDoneDialog'
import { TaskForm } from './features/tasks/TaskForm'
import { TaskList } from './features/tasks/TaskList'
import type { TaskView } from './features/tasks/taskStatus'
import { useCreateTask, useDeleteTask, useMarkTaskDone, useTasks } from './features/tasks/useTasks'

function App() {
  const tasks = useTasks()
  const members = useMembers()
  const createTask = useCreateTask()
  const deleteTask = useDeleteTask()
  const markDone = useMarkTaskDone()

  const [showForm, setShowForm] = useState(false)
  const [taskToComplete, setTaskToComplete] = useState<TaskView | null>(null)

  const memberList = members.data ?? []

  return (
    <main>
      <header>
        <h1>Hominder</h1>
      </header>

      <button type="button" onClick={() => setShowForm(true)}>
        Nouvelle tâche
      </button>

      {showForm ? (
        <TaskForm
          members={memberList}
          onCancel={() => setShowForm(false)}
          onSubmit={(input) => {
            createTask.mutate(input, { onSuccess: () => setShowForm(false) })
          }}
        />
      ) : null}

      {tasks.isLoading ? <p>Chargement…</p> : null}

      <TaskList
        tasks={tasks.data ?? []}
        onMarkDone={(task) => setTaskToComplete(task)}
        onEdit={() => undefined}
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
