import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { RecurrencePolicyInput, TaskView } from './taskStatus'

const tasksKey = ['tasks'] as const

export type TaskInput = {
  title: string
  notes: string | null
  policy: RecurrencePolicyInput
  assigneeId: string | null
}

export type MarkDoneInput = {
  id: string
  completedOn: string
  completedBy: string
  nextDueOverride: string | null
}

export function useTasks() {
  return useQuery({
    queryKey: tasksKey,
    queryFn: async (): Promise<TaskView[]> => {
      const { data, error } = await api.GET('/api/tasks')
      if (error) {
        throw new Error('Chargement des tâches impossible.')
      }
      return (data ?? []) as TaskView[]
    },
  })
}

function invalidateTasks(queryClient: ReturnType<typeof useQueryClient>) {
  return queryClient.invalidateQueries({ queryKey: tasksKey })
}

export function useCreateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (input: TaskInput) => {
      const { error } = await api.POST('/api/tasks', { body: input })
      if (error) {
        throw new Error('Création de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}

export function useUpdateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, input }: { id: string; input: TaskInput }) => {
      const { error } = await api.PUT('/api/tasks/{id}', {
        params: { path: { id } },
        body: input,
      })
      if (error) {
        throw new Error('Mise à jour de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}

export function useDeleteTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/tasks/{id}', { params: { path: { id } } })
      if (error) {
        throw new Error('Suppression de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}

export function useMarkTaskDone() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, completedOn, completedBy, nextDueOverride }: MarkDoneInput) => {
      const { error } = await api.POST('/api/tasks/{id}/completions', {
        params: { path: { id } },
        body: { completedOn, completedBy, nextDueOverride },
      })
      if (error) {
        throw new Error('Complétion de la tâche impossible.')
      }
    },
    onSuccess: () => invalidateTasks(queryClient),
  })
}
