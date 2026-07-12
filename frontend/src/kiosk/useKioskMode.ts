import { useCallback, useState } from 'react'

export type KioskMode = 'consultation' | 'gestion'

export function useKioskMode() {
  const [mode, setMode] = useState<KioskMode>('consultation')

  const toggle = useCallback(() => {
    setMode((current) => (current === 'consultation' ? 'gestion' : 'consultation'))
  }, [])

  const reset = useCallback(() => {
    setMode('consultation')
  }, [])

  return { mode, isGestion: mode === 'gestion', toggle, reset }
}
