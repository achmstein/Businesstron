import { cn } from '@/lib/utils'

type Tone = 'emerald' | 'gold' | 'amber' | 'red' | 'slate'

// The -300 shades are tuned for the near-black background and wash out badly on
// light. Each tone carries a dark shade for light mode and keeps the bright one
// under dark:, so both themes stay legible.
const toneClass: Record<Tone, string> = {
  emerald: 'bg-emerald-500/15 text-emerald-700 ring-emerald-600/30 dark:text-emerald-300 dark:ring-emerald-500/25',
  gold: 'bg-primary/15 text-amber-800 ring-primary/40 dark:text-primary dark:ring-primary/30',
  amber: 'bg-amber-500/15 text-amber-800 ring-amber-600/30 dark:text-amber-300 dark:ring-amber-500/25',
  red: 'bg-red-500/15 text-red-700 ring-red-600/30 dark:text-red-300 dark:ring-red-500/25',
  slate: 'bg-muted text-muted-foreground ring-border',
}

const dotClass: Record<Tone, string> = {
  emerald: 'bg-emerald-600 dark:bg-emerald-400',
  gold: 'bg-amber-600 dark:bg-primary',
  amber: 'bg-amber-600 dark:bg-amber-400',
  red: 'bg-red-600 dark:bg-red-400',
  slate: 'bg-muted-foreground',
}

const toneFor: Record<string, Tone> = {
  Completed: 'emerald',
  Enriched: 'emerald',
  Pushed: 'emerald',
  Running: 'gold',
  Pending: 'slate',
  NotPushed: 'slate',
  Skipped: 'amber',
  Failed: 'red',
  Cancelled: 'slate',
}

const labelFor: Record<string, string> = { NotPushed: 'Not pushed' }

export default function StatusBadge({ status, pulse }: { status: string; pulse?: boolean }) {
  const tone = toneFor[status] ?? 'slate'
  const label = labelFor[status] ?? status
  const animate = pulse ?? status === 'Running'
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset', toneClass[tone])}>
      <span className={cn('size-1.5 rounded-full', dotClass[tone], animate && 'animate-pulse')} />
      {label}
    </span>
  )
}
