import { useNavigate } from 'react-router-dom'
import { Plus, ScanSearch, Tags, Users, Settings2, Activity } from 'lucide-react'
import {
  CommandDialog, CommandInput, CommandList, CommandEmpty, CommandGroup, CommandItem, CommandShortcut,
} from '@/components/ui/command'

interface Props {
  open: boolean
  onOpenChange: (v: boolean) => void
  onNewSearch: () => void
}

export default function CommandPalette({ open, onOpenChange, onNewSearch }: Props) {
  const navigate = useNavigate()
  const run = (fn: () => void) => {
    onOpenChange(false)
    fn()
  }

  return (
    <CommandDialog open={open} onOpenChange={onOpenChange}>
      <CommandInput placeholder="Type a command or search…" />
      <CommandList>
        <CommandEmpty>No results.</CommandEmpty>
        <CommandGroup heading="Actions">
          <CommandItem onSelect={() => run(onNewSearch)}>
            <Plus /> New search <CommandShortcut>N</CommandShortcut>
          </CommandItem>
        </CommandGroup>
        <CommandGroup heading="Go to">
          <CommandItem onSelect={() => run(() => navigate('/searches'))}><ScanSearch /> Searches</CommandItem>
          <CommandItem onSelect={() => run(() => navigate('/keywords'))}><Tags /> Keywords</CommandItem>
          <CommandItem onSelect={() => run(() => navigate('/users'))}><Users /> Users</CommandItem>
          <CommandItem onSelect={() => run(() => navigate('/settings'))}><Settings2 /> Settings</CommandItem>
          <CommandItem onSelect={() => run(() => window.open('/hangfire', '_blank'))}><Activity /> Jobs dashboard</CommandItem>
        </CommandGroup>
      </CommandList>
    </CommandDialog>
  )
}
