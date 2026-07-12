import { useState } from 'react'
import './App.css'
import { AgendaTab } from './features/agenda/AgendaTab'
import { DomotiqueTab } from './features/domotique/DomotiqueTab'
import { useMembers } from './features/members/useMembers'
import type { TaskView } from './features/tasks/taskStatus'
import {
  useCreateTask,
  useDeleteTask,
  useMarkTaskDone,
  useTasks,
  useUpdateTask,
} from './features/tasks/useTasks'
import { KioskShell, type KioskTab } from './kiosk/KioskShell'
import { useKioskMode } from './kiosk/useKioskMode'
import { useNow } from './kiosk/useNow'

function App() {
  const now = useNow()
  const tasks = useTasks()
  const members = useMembers()
  const createTask = useCreateTask()
  const updateTask = useUpdateTask()
  const deleteTask = useDeleteTask()
  const markDone = useMarkTaskDone()

  const { isGestion, toggle } = useKioskMode()
  const [activeTab, setActiveTab] = useState<KioskTab>('agenda')

  const taskError =
    createTask.error?.message ??
    updateTask.error?.message ??
    deleteTask.error?.message ??
    markDone.error?.message ??
    tasks.error?.message ??
    null

  return (
    <KioskShell
      now={now}
      isGestion={isGestion}
      onToggleMode={toggle}
      activeTab={activeTab}
      onSelectTab={setActiveTab}
    >
      {taskError ? (
        <p className="alert" role="alert">
          {taskError}
        </p>
      ) : null}

      {tasks.isLoading ? <p className="loading">Chargement…</p> : null}

      {activeTab === 'agenda' ? (
        <AgendaTab
          tasks={tasks.data ?? []}
          members={members.data ?? []}
          isGestion={isGestion}
          reference={now}
          onCreateTask={(input) => createTask.mutate(input)}
          onUpdateTask={(id, input) => updateTask.mutate({ id, input })}
          onDeleteTask={(task: TaskView) => deleteTask.mutate(task.id)}
          onMarkDone={(input) => markDone.mutate(input)}
        />
      ) : (
        <DomotiqueTab />
      )}
    </KioskShell>
  )
}

export default App
