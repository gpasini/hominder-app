import type { ReactNode } from 'react'

export type KioskTab = 'agenda' | 'domotique'

type KioskShellProps = {
  now: Date
  isGestion: boolean
  onToggleMode: () => void
  activeTab: KioskTab
  onSelectTab: (tab: KioskTab) => void
  children: ReactNode
}

const tabs: { id: KioskTab; label: string }[] = [
  { id: 'agenda', label: 'Agenda' },
  { id: 'domotique', label: 'Domotique' },
]

function formatDate(now: Date): string {
  return now.toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long' })
}

function formatTime(now: Date): string {
  return now.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
}

export function KioskShell({ now, isGestion, onToggleMode, activeTab, onSelectTab, children }: KioskShellProps) {
  return (
    <main className="kiosk">
      <header className="kiosk__header">
        <div className="kiosk__brand">
          <span className="kiosk__logo">Hominder</span>
          <span className="kiosk__datetime">
            <span className="kiosk__date">{formatDate(now)}</span>
            <span className="kiosk__time">{formatTime(now)}</span>
          </span>
        </div>
        <nav className="kiosk__tabs">
          {tabs.map((tab) => (
            <button
              type="button"
              key={tab.id}
              className={`kiosk__tab ${activeTab === tab.id ? 'kiosk__tab--active' : ''}`}
              onClick={() => onSelectTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </nav>
        <button
          type="button"
          className={`btn ${isGestion ? 'btn--primary' : 'btn--ghost'}`}
          onClick={onToggleMode}
        >
          {isGestion ? '✓ Gestion' : '⚙ Gestion'}
        </button>
      </header>
      <div className="kiosk__body">{children}</div>
    </main>
  )
}
