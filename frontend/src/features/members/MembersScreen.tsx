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
    <section className="panel">
      <h2 className="panel__title">Membres du foyer</h2>
      <form className="row" onSubmit={submit}>
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Nom du membre"
        />
        <button type="submit" className="btn btn--primary" disabled={createMember.isPending}>
          Ajouter
        </button>
      </form>

      {members.isLoading ? <p className="loading">Chargement…</p> : null}

      <ul className="members">
        {(members.data ?? []).map((member) => (
          <li className="member" key={member.id}>
            <span className="member__name">
              <span className="avatar">{member.name.trim().charAt(0).toUpperCase()}</span>
              {member.name}
            </span>
            <button
              type="button"
              className="btn btn--danger btn--sm"
              onClick={() => deleteMember.mutate(member.id)}
            >
              Supprimer
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}
