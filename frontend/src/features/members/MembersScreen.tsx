import { useState } from 'react'
import { useCreateMember, useDeleteMember, useMembers } from './useMembers'

export function MembersScreen() {
  const members = useMembers()
  const createMember = useCreateMember()
  const deleteMember = useDeleteMember()
  const [name, setName] = useState('')

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    const trimmed = name.trim()
    if (trimmed.length === 0) {
      return
    }
    createMember.mutate(trimmed, { onSuccess: () => setName('') })
  }

  return (
    <section>
      <h2>Membres du foyer</h2>
      <form onSubmit={submit}>
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Nom du membre"
        />
        <button type="submit" disabled={createMember.isPending}>
          Ajouter
        </button>
      </form>

      {members.isLoading ? <p>Chargement…</p> : null}

      <ul>
        {(members.data ?? []).map((member) => (
          <li key={member.id}>
            <span>{member.name}</span>
            <button type="button" onClick={() => deleteMember.mutate(member.id)}>
              Supprimer
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}
