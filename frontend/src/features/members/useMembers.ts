import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { components } from '../../api/schema'

export type Member = components['schemas']['HouseholdMemberView']

const membersKey = ['members'] as const

export function useMembers() {
  return useQuery({
    queryKey: membersKey,
    queryFn: async (): Promise<Member[]> => {
      const { data, error } = await api.GET('/api/members')
      if (error) {
        throw new Error('Chargement des membres impossible.')
      }
      return data ?? []
    },
  })
}

export function useCreateMember() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (name: string) => {
      const { error } = await api.POST('/api/members', { body: { name } })
      if (error) {
        throw new Error('Création du membre impossible.')
      }
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: membersKey }),
  })
}

export function useDeleteMember() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/members/{id}', { params: { path: { id } } })
      if (error) {
        throw new Error('Suppression du membre impossible.')
      }
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: membersKey }),
  })
}
