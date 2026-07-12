import { useCallback, useState } from 'react'

export type KioskMode = 'consultation' | 'gestion'

export function useKioskMode() {
  const [mode, setMode] = useState<KioskMode>('consultation')

  const toggle = useCallback(() => {
    setMode((current) => (current === 'consultation' ? 'gestion' : 'consultation'))
  }, [])

  return { isGestion: mode === 'gestion', toggle }
}
