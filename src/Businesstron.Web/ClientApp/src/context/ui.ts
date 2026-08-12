import { createContext, useContext } from 'react'

export interface UiState {
  openNewSearch: () => void
  openPalette: () => void
}

export const UiContext = createContext<UiState | undefined>(undefined)

export function useUi() {
  const ctx = useContext(UiContext)
  if (!ctx) throw new Error('useUi must be used inside the app shell')
  return ctx
}
