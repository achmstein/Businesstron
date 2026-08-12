import { motion } from 'framer-motion'
import { Check, Loader2, Database, Search, Filter, Send } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { SearchRunDto } from '@/api/client'

type StageState = 'done' | 'active' | 'ready' | 'pending' | 'failed'

interface Stage { key: string; label: string; detail: string; icon: LucideIcon; state: StageState }

function computeStages(run: SearchRunDto): Stage[] {
  const running = run.status === 'Running'
  const completed = run.status === 'Completed'
  const failed = run.status === 'Failed'
  const started = run.status !== 'Pending'
  // A stage may only spin while the run is genuinely running — never after stop/finish.
  const harvestActive = running && run.totalItems === 0
  const enrichActive = running && run.totalItems > 0 && run.processedItems < run.totalItems
  const enriched = completed || (run.totalItems > 0 && run.processedItems >= run.totalItems)

  return [
    {
      key: 'harvest', label: 'Harvest', icon: Database,
      detail: run.totalItems > 0 || started ? `${run.totalItems} names` : 'fetching…',
      state: harvestActive ? 'active' : started ? 'done' : 'pending',
    },
    {
      key: 'asic', label: 'ASIC + ABR', icon: Search,
      detail: `${run.processedItems}/${run.totalItems || '–'}`,
      state: enriched ? 'done' : enrichActive ? 'active' : failed && run.processedItems > 0 ? 'failed' : 'pending',
    },
    {
      key: 'filter', label: 'Filter', icon: Filter,
      detail: `${run.suitableCount} suitable`,
      state: enriched ? 'done' : enrichActive ? 'active' : 'pending',
    },
    {
      key: 'ontraport', label: 'Ontraport', icon: Send,
      detail: run.pushedCount > 0 ? `${run.pushedCount} pushed` : enriched ? 'ready' : 'waiting',
      state: run.pushedCount > 0 ? 'done' : completed ? 'ready' : 'pending',
    },
  ]
}

// Bright -300 text works on the near-black theme but is unreadable on light, so
// each state pairs a dark shade with the original under dark:.
const ring: Record<StageState, string> = {
  done: 'border-emerald-600/30 bg-emerald-500/10 text-emerald-700 dark:border-emerald-500/30 dark:text-emerald-300',
  active: 'border-primary/40 bg-primary/10 text-amber-800 seal-glow dark:text-primary',
  ready: 'border-amber-600/30 bg-amber-500/10 text-amber-800 dark:border-amber-500/30 dark:text-amber-300',
  pending: 'border-border bg-muted/30 text-muted-foreground',
  failed: 'border-red-600/30 bg-red-500/10 text-red-700 dark:border-red-500/30 dark:text-red-300',
}

const iconWrap: Record<StageState, string> = {
  done: 'bg-emerald-500 text-white',
  active: 'bg-primary text-primary-foreground',
  ready: 'bg-amber-500 text-white',
  pending: 'bg-muted text-muted-foreground',
  failed: 'bg-red-500 text-white',
}

export default function PipelineMonitor({ run }: { run: SearchRunDto }) {
  const stages = computeStages(run)
  // 2×2 grid on phones (a scrolling row would show a scrollbar under the cards),
  // single connected row from `sm` up.
  return (
    <div className="grid grid-cols-2 gap-2 sm:flex sm:items-stretch">
      {stages.map((stage, i) => {
        const Icon = stage.icon
        return (
          <div key={stage.key} className="flex items-center gap-2 sm:flex-1">
            <motion.div
              layout
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.05 }}
              className={cn('flex min-w-0 flex-1 items-center gap-3 rounded-lg border px-3 py-2.5 transition-colors sm:min-w-[9.5rem]', ring[stage.state])}
            >
              <div className={cn('flex size-8 shrink-0 items-center justify-center rounded-md', iconWrap[stage.state])}>
                {stage.state === 'active' ? <Loader2 className="size-4 animate-spin" /> : stage.state === 'done' ? <Check className="size-4" /> : <Icon className="size-4" />}
              </div>
              <div className="min-w-0">
                <div className="text-xs font-semibold uppercase tracking-wide">{stage.label}</div>
                <div className="font-mono text-xs tabular-nums opacity-80">{stage.detail}</div>
              </div>
            </motion.div>
            {i < stages.length - 1 && <div className={cn('hidden h-px w-4 shrink-0 sm:block', stage.state === 'done' ? 'bg-emerald-400/60' : 'bg-border')} />}
          </div>
        )
      })}
    </div>
  )
}
