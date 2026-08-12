import { useEffect, useMemo, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { ScanSearch, Tags, Users, Settings2, Activity, LogOut, Plus, Command, Sun, Moon, Menu, X } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'
import { useTheme } from '../hooks/useTheme'
import { UiContext } from '../context/ui'
import Logo from './Logo'
import NewSearchDialog from './NewSearchDialog'
import CommandPalette from './CommandPalette'
import { Button } from './ui/button'
import { cn } from '@/lib/utils'

interface NavItem { to: string; label: string; icon: LucideIcon; external?: boolean }

const nav: NavItem[] = [
  { to: '/searches', label: 'Searches', icon: ScanSearch },
  { to: '/keywords', label: 'Keywords', icon: Tags },
  { to: '/users', label: 'Users', icon: Users },
  { to: '/settings', label: 'Settings', icon: Settings2 },
  { to: '/hangfire', label: 'Jobs', icon: Activity, external: true },
]

function SidebarContent({
  email,
  theme,
  onNewSearch,
  onPalette,
  onLogout,
  onToggleTheme,
  onNavigate,
}: {
  email?: string
  theme: string
  onNewSearch: () => void
  onPalette: () => void
  onLogout: () => void
  onToggleTheme: () => void
  onNavigate?: () => void
}) {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2.5 border-b px-5">
        <div className="flex size-8 items-center justify-center rounded-md bg-[#17130d] ring-1 ring-primary/25 seal-glow">
          <Logo className="size-5 seal-on-dark" />
        </div>
        <div className="leading-none">
          <div className="font-display text-base font-semibold tracking-tight">Businesstron</div>
          <div className="mt-1 font-mono text-[10px] uppercase tracking-[0.2em] text-muted-foreground">registry console</div>
        </div>
      </div>

      <div className="p-3">
        <Button className="w-full justify-start" onClick={onNewSearch}>
          <Plus className="size-4" /> New search
        </Button>
      </div>

      <nav className="flex min-h-0 flex-1 flex-col gap-0.5 overflow-y-auto px-3">
        {nav.map((item) => {
          const Icon = item.icon
          const cls = ({ isActive }: { isActive: boolean }) =>
            cn(
              'group flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
              isActive
                ? 'bg-primary/10 text-primary ring-1 ring-primary/20'
                : 'text-muted-foreground hover:bg-accent hover:text-foreground',
            )
          return item.external ? (
            <a key={item.to} href={item.to} target="_blank" rel="noreferrer"
              className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground">
              <Icon className="size-4" /> {item.label}
            </a>
          ) : (
            <NavLink key={item.to} to={item.to} className={cls} onClick={onNavigate}>
              <Icon className="size-4" /> {item.label}
            </NavLink>
          )
        })}
      </nav>

      <div className="border-t p-3">
        <button
          onClick={onPalette}
          className="mb-2 flex w-full items-center gap-2 rounded-md border bg-background/40 px-3 py-2 text-xs text-muted-foreground transition-colors hover:text-foreground"
        >
          <Command className="size-3.5" /> Command
          <kbd className="ml-auto rounded bg-muted px-1.5 py-0.5 font-mono text-[10px]">⌘K</kbd>
        </button>
        <div className="mb-1 truncate px-1 text-xs text-muted-foreground" title={email}>{email}</div>
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="sm" className="flex-1 justify-start text-muted-foreground" onClick={onLogout}>
            <LogOut className="size-4" /> Log out
          </Button>
          <Button variant="ghost" size="icon" className="text-muted-foreground" onClick={onToggleTheme} title="Toggle theme">
            {theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}
          </Button>
        </div>
      </div>
    </>
  )
}

export default function Layout() {
  const { user, logout } = useAuth()
  const { theme, toggle: toggleTheme } = useTheme()
  const navigate = useNavigate()
  const [newSearchOpen, setNewSearchOpen] = useState(false)
  const [paletteOpen, setPaletteOpen] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(false)

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setPaletteOpen((o) => !o)
      }
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [])

  const ui = useMemo(
    () => ({ openNewSearch: () => setNewSearchOpen(true), openPalette: () => setPaletteOpen(true) }),
    [],
  )

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  const sidebarProps = {
    email: user?.email,
    theme,
    onLogout: handleLogout,
    onToggleTheme: toggleTheme,
  }

  return (
    <UiContext.Provider value={ui}>
      <div className="flex h-screen overflow-hidden">
        <aside className="hidden w-60 shrink-0 flex-col overflow-hidden border-r bg-card/60 md:flex">
          <SidebarContent
            {...sidebarProps}
            onNewSearch={() => setNewSearchOpen(true)}
            onPalette={() => setPaletteOpen(true)}
          />
        </aside>

        {/* Mobile drawer: same sidebar, slid over the content behind a backdrop. */}
        <AnimatePresence>
          {sidebarOpen && (
            <div className="fixed inset-0 z-50 md:hidden">
              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.15 }}
                className="absolute inset-0 bg-black/50"
                onClick={() => setSidebarOpen(false)}
              />
              <motion.aside
                initial={{ x: '-100%' }}
                animate={{ x: 0 }}
                exit={{ x: '-100%' }}
                transition={{ type: 'tween', duration: 0.18 }}
                className="absolute inset-y-0 left-0 flex w-64 max-w-[85vw] flex-col overflow-hidden border-r bg-card shadow-xl"
              >
                <button
                  className="absolute right-3 top-5 text-muted-foreground transition-colors hover:text-foreground"
                  onClick={() => setSidebarOpen(false)}
                  aria-label="Close menu"
                >
                  <X className="size-4" />
                </button>
                <SidebarContent
                  {...sidebarProps}
                  onNewSearch={() => { setSidebarOpen(false); setNewSearchOpen(true) }}
                  onPalette={() => { setSidebarOpen(false); setPaletteOpen(true) }}
                  onNavigate={() => setSidebarOpen(false)}
                />
              </motion.aside>
            </div>
          )}
        </AnimatePresence>

        <div className="relative flex min-w-0 flex-1 flex-col overflow-hidden">
          <div className="pointer-events-none absolute inset-0 registry-grid opacity-60" />
          <header className="flex h-16 shrink-0 items-center justify-between border-b bg-card/40 px-5 backdrop-blur md:hidden">
            <div className="flex items-center gap-1">
              <Button variant="ghost" size="icon" className="-ml-2" onClick={() => setSidebarOpen(true)} aria-label="Open menu">
                <Menu className="size-5" />
              </Button>
              <span className="font-display text-base font-semibold">Businesstron</span>
            </div>
            <div className="flex gap-1">
              <Button variant="ghost" size="icon" onClick={toggleTheme}>{theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}</Button>
              <Button variant="ghost" size="icon" onClick={() => setPaletteOpen(true)}><Command className="size-4" /></Button>
              <Button size="icon" onClick={() => setNewSearchOpen(true)}><Plus className="size-4" /></Button>
            </div>
          </header>
          <main className="relative flex-1 overflow-y-auto">
            <div className="mx-auto w-full max-w-6xl px-5 py-8">
              <Outlet />
            </div>
          </main>
        </div>

        <NewSearchDialog open={newSearchOpen} onOpenChange={setNewSearchOpen} />
        <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} onNewSearch={() => setNewSearchOpen(true)} />
      </div>
    </UiContext.Provider>
  )
}
