import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Plus, ScanSearch, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { api, type SearchRunDto } from '../api/client'
import { useUi } from '../context/ui'
import StatusBadge from '../components/StatusBadge'
import Sparkline from '../components/Sparkline'
import ConfirmDialog from '../components/ConfirmDialog'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Skeleton } from '@/components/ui/skeleton'

export default function SearchesPage() {
  const [runs, setRuns] = useState<SearchRunDto[]>([])
  const [loading, setLoading] = useState(true)
  const [deleteTarget, setDeleteTarget] = useState<SearchRunDto | null>(null)
  const { openNewSearch } = useUi()

  const load = useCallback(async () => {
    try {
      setRuns((await api.searchRuns.list(1, 50)).items)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
    const timer = setInterval(load, 3000)
    return () => clearInterval(timer)
  }, [load])

  const remove = async () => {
    if (!deleteTarget) return
    try {
      await api.searchRuns.remove(deleteTarget.id)
      toast.success('Search deleted')
      await load()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete')
    }
  }

  const spark = useMemo(() => {
    const days = 14
    const buckets = new Array<number>(days).fill(0)
    const now = Date.now()
    for (const r of runs) {
      const d = Math.floor((now - new Date(r.created).getTime()) / 86_400_000)
      if (d >= 0 && d < days) buckets[days - 1 - d]++
    }
    return buckets
  }, [runs])

  const totals = useMemo(
    () => ({
      suitable: runs.reduce((s, r) => s + r.suitableCount, 0),
      pushed: runs.reduce((s, r) => s + r.pushedCount, 0),
      running: runs.filter((r) => r.status === 'Running').length,
    }),
    [runs],
  )

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-display text-2xl font-semibold tracking-tight">Search runs</h1>
          <p className="text-sm text-muted-foreground">Harvest, enrich, filter and push business-name leads.</p>
        </div>
        <Button onClick={openNewSearch}><Plus className="size-4" /> New search</Button>
      </div>

      <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[
          { label: 'Runs', value: runs.length },
          { label: 'Suitable', value: totals.suitable, tone: 'text-emerald-700 dark:text-emerald-300' },
          { label: 'Pushed', value: totals.pushed },
        ].map((s) => (
          <div key={s.label} className="rounded-xl border bg-card px-4 py-3">
            <div className="text-xs uppercase tracking-wide text-muted-foreground">{s.label}</div>
            <div className={`mt-0.5 font-mono text-xl font-semibold tabular-nums ${s.tone ?? ''}`}>{s.value}</div>
          </div>
        ))}
        <div className="flex items-center justify-between gap-3 overflow-hidden rounded-xl border bg-card px-4 py-3">
          {/* min-w-0 so the label can shrink instead of forcing the row wider than
              the card — at two columns on mobile there is ~128px to share. */}
          <div className="min-w-0 shrink">
            <div className="truncate text-xs uppercase tracking-wide text-muted-foreground">Activity</div>
            <div className="mt-0.5 font-mono text-xs text-muted-foreground">14 days</div>
          </div>
          <Sparkline data={spark} />
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border bg-card">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead>Created</TableHead>
              <TableHead>Source</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Progress</TableHead>
              <TableHead className="text-right">Found</TableHead>
              <TableHead className="text-right">Suitable</TableHead>
              <TableHead className="text-right">Pushed</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && runs.length === 0 &&
              Array.from({ length: 4 }).map((_, i) => (
                <TableRow key={i}><TableCell colSpan={8}><Skeleton className="h-5 w-full" /></TableCell></TableRow>
              ))}

            {!loading && runs.length === 0 && (
              <TableRow className="hover:bg-transparent">
                <TableCell colSpan={8}>
                  <div className="flex flex-col items-center gap-3 py-14 text-center">
                    <div className="flex size-11 items-center justify-center rounded-full bg-muted text-muted-foreground"><ScanSearch className="size-5" /></div>
                    <div className="text-sm text-muted-foreground">No searches yet. Start one to harvest new business names.</div>
                    <Button size="sm" onClick={openNewSearch}><Plus className="size-4" /> New search</Button>
                  </div>
                </TableCell>
              </TableRow>
            )}

            {runs.map((run, i) => (
              <motion.tr
                key={run.id}
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: Math.min(i * 0.015, 0.2) }}
                className="border-b transition-colors hover:bg-muted/40"
              >
                <TableCell className="text-muted-foreground">{new Date(run.created).toLocaleString()}</TableCell>
                <TableCell>
                  <div className="flex flex-col gap-1">
                    <span>{run.source === 'DataGov' ? 'data.gov.au' : 'ABN list'}</span>
                    {run.tags.length > 0 && (
                      <div className="flex flex-wrap gap-1">
                        {run.tags.map((t) => (
                          <span key={t} className="rounded border bg-secondary px-1.5 py-0.5 text-[10px] font-medium text-secondary-foreground">{t}</span>
                        ))}
                      </div>
                    )}
                  </div>
                </TableCell>
                <TableCell><StatusBadge status={run.status} /></TableCell>
                <TableCell className="font-mono text-xs tabular-nums text-muted-foreground">{run.processedItems}/{run.totalItems}</TableCell>
                <TableCell className="text-right font-mono tabular-nums">{run.foundRecords}</TableCell>
                <TableCell className="text-right font-mono tabular-nums text-emerald-700 dark:text-emerald-300">{run.suitableCount}</TableCell>
                <TableCell className="text-right font-mono tabular-nums">{run.pushedCount}</TableCell>
                <TableCell className="text-right">
                  <div className="flex items-center justify-end gap-1">
                    <Button asChild variant="ghost" size="sm"><Link to={`/searches/${run.id}`}>View</Link></Button>
                    <Button variant="ghost" size="icon" className="text-muted-foreground hover:text-destructive" onClick={() => setDeleteTarget(run)} title="Delete">
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                </TableCell>
              </motion.tr>
            ))}
          </TableBody>
        </Table>
      </div>

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(v) => !v && setDeleteTarget(null)}
        title="Delete this search?"
        description={`Permanently removes this run${deleteTarget && deleteTarget.foundRecords > 0 ? ` and its ${deleteTarget.foundRecords} records` : ''}. This can't be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={remove}
      />
    </div>
  )
}
