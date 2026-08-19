import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { motion } from 'framer-motion'
import { ArrowLeft, Download, Send, AlertTriangle, Square, RotateCcw, Trash2, Pencil, Tag, X, Globe } from 'lucide-react'
import { toast } from 'sonner'
import { api, type RecordFilter, type SearchRunDetail, type WebEnrichmentStatus } from '../api/client'
import StatusBadge from '../components/StatusBadge'
import PipelineMonitor from '../components/PipelineMonitor'
import ConfirmDialog from '../components/ConfirmDialog'
import TagInput from '../components/TagInput'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'

// Keyword chips shown before collapsing behind "+N more" (runs can carry hundreds).
const KEYWORDS_PREVIEW = 8

// Friendly label for a record with no email/website yet, based on why.
function webStatusLabel(status: WebEnrichmentStatus): string {
  switch (status) {
    case 'Pending': return 'Queued…'
    case 'NoWebsite': return 'No website'
    case 'NoEmail': return 'No email'
    case 'Failed': return 'Failed'
    case 'Skipped': return 'Out of window'
    default: return '—'
  }
}

export default function SearchDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [data, setData] = useState<SearchRunDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pushing, setPushing] = useState(false)
  const [busy, setBusy] = useState(false)
  const [webBusy, setWebBusy] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [showAllKeywords, setShowAllKeywords] = useState(false)
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState<RecordFilter | null>(null)
  const [editingTags, setEditingTags] = useState(false)
  const [tagsDraft, setTagsDraft] = useState<string[]>([])
  const [savingTags, setSavingTags] = useState(false)
  const navigate = useNavigate()

  const load = useCallback(async () => {
    if (!id) return
    try {
      setData(await api.searchRuns.get(id, page, 100, filter))
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load')
    }
  }, [id, page, filter])

  // Reset to the first page whenever the run or the active filter changes.
  useEffect(() => setPage(1), [id, filter])

  useEffect(() => {
    void load()
    const timer = setInterval(load, 3000)
    return () => clearInterval(timer)
  }, [load])

  const push = async () => {
    if (!id) return
    setPushing(true)
    try {
      await api.searchRuns.push(id)
      toast.success('Ontraport push queued', { description: 'Row statuses update as leads are pushed.' })
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Push failed')
    } finally {
      setPushing(false)
    }
  }

  const stop = async () => {
    if (!id) return
    setBusy(true)
    try {
      await api.searchRuns.cancel(id)
      toast.success('Stopping…', { description: 'The run finishes after the current record.' })
      await load()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to stop')
    } finally {
      setBusy(false)
    }
  }

  const retry = async () => {
    if (!id) return
    setBusy(true)
    try {
      await api.searchRuns.retryFailed(id)
      toast.success('Retrying failed records')
      await load()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to retry')
    } finally {
      setBusy(false)
    }
  }

  const enrichWeb = async () => {
    if (!id) return
    setWebBusy(true)
    try {
      await api.searchRuns.enrichWeb(id)
      toast.success('Finding websites & contacts', { description: 'Reverse-whois then a contact-email lookup for suitable leads renewing within 12 months.' })
      await load()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to start web enrichment')
    } finally {
      setWebBusy(false)
    }
  }

  const remove = async () => {
    if (!id) return
    try {
      await api.searchRuns.remove(id)
      toast.success('Search deleted')
      navigate('/searches')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete')
    }
  }

  const saveTags = async () => {
    if (!id) return
    setSavingTags(true)
    try {
      await api.searchRuns.updateTags(id, tagsDraft)
      setEditingTags(false)
      await load()
      toast.success('Tags updated')
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update tags')
    } finally {
      setSavingTags(false)
    }
  }

  if (error && !data) return <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-red-700 dark:text-red-300">{error}</div>
  if (!data) return <div className="text-sm text-muted-foreground">Loading…</div>

  const { run, records, web } = data
  // Failure summary comes from the server (run.errorCount + a sample reason) so it
  // reflects the whole run, not just the records on the current page.
  const failedCount = run.errorCount
  // Retry reprocesses failed AND still-pending records (e.g. a run cut short by a
  // timeout), so the button counts both — not just the failures.
  const pendingCount = data.pendingCount
  const retryable = failedCount + pendingCount
  const sampleReason = data.sampleFailureReason ?? undefined
  const captchaIssue = /captcha/i.test(sampleReason ?? '')
  const captchaKeyMissing = /no 2captcha api key/i.test(sampleReason ?? '')
  // The heading already says the CAPTCHA failed; drop that generic prefix so the
  // sub-line shows just the specific cause (balance, bad key, timeout, …).
  const captchaDetail = (sampleReason ?? '').replace(/^The ASIC CAPTCHA could not be solved\.?\s*/i, '')
  const stats: { label: string; value: string | number; tone?: string; filter?: RecordFilter }[] = [
    { label: 'Progress', value: `${run.processedItems}/${run.totalItems}` },
    { label: 'Found', value: run.foundRecords },
    { label: 'Suitable', value: run.suitableCount, tone: 'text-emerald-700 dark:text-emerald-300', filter: 'suitable' },
    { label: 'Pushed', value: run.pushedCount, filter: 'pushed' },
    { label: 'Errors', value: run.errorCount, tone: run.errorCount > 0 ? 'text-red-700 dark:text-red-300' : undefined, filter: 'errors' },
  ]

  // Web-enrichment tiles double as table filters, so the enriched rows — otherwise
  // scattered across hundreds of pages — are one click away.
  const webTiles: { label: string; value: number; tone?: string; filter: RecordFilter }[] = [
    { label: 'Websites', value: web.withWebsite, filter: 'websites' },
    { label: 'Emails', value: web.withEmail, tone: 'text-emerald-700 dark:text-emerald-300', filter: 'emails' },
    { label: 'Queued', value: web.pending, filter: 'webpending' },
    { label: 'No website', value: web.noWebsite, filter: 'nowebsite' },
    { label: 'Failed', value: web.failed, tone: web.failed > 0 ? 'text-red-700 dark:text-red-300' : undefined, filter: 'webfailed' },
  ]
  const webPct = web.eligible > 0 ? Math.round((web.processed / web.eligible) * 100) : 0
  const showWeb = run.enableWebEnrichment && web.hasActivity
  // Friendly names for the active-filter hint (raw keys like "webpending" are ugly).
  const filterLabels: Record<RecordFilter, string> = {
    errors: 'errored', suitable: 'suitable', pushed: 'pushed',
    websites: 'with a website', emails: 'with an email', webpending: 'queued for web enrichment',
    webfailed: 'web-enrichment failed', nowebsite: 'with no website',
  }

  return (
    <div>
      <Button asChild variant="ghost" size="sm" className="mb-3 -ml-2 text-muted-foreground">
        <Link to="/searches"><ArrowLeft className="size-4" /> Searches</Link>
      </Button>

      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <h1 className="font-display text-2xl font-semibold tracking-tight">Run monitor</h1>
          <StatusBadge status={run.status} />
          <span className="font-mono text-xs text-muted-foreground">{run.source === 'DataGov' ? 'data.gov.au' : 'ABN list'}</span>
        </div>
        <div className="flex flex-wrap gap-2">
          {(run.status === 'Running' || run.status === 'Pending') && (
            <Button variant="destructive" size="sm" onClick={stop} disabled={busy || run.cancellationRequested}>
              <Square className="size-4" /> {run.cancellationRequested ? 'Stopping…' : 'Stop'}
            </Button>
          )}
          {retryable > 0 && run.status !== 'Running' && run.status !== 'Pending' && (
            <Button
              variant="outline"
              size="sm"
              onClick={retry}
              disabled={busy}
              title={`${failedCount.toLocaleString()} failed + ${pendingCount.toLocaleString()} pending`}
            >
              <RotateCcw className="size-4" /> Retry {retryable.toLocaleString()}
            </Button>
          )}
          {run.status !== 'Running' && run.status !== 'Pending' && run.suitableCount > 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={enrichWeb}
              disabled={webBusy}
              title="Reverse-whois suitable leads renewing within 12 months, then look up a contact email"
            >
              <Globe className="size-4" /> {webBusy ? 'Starting…' : run.enableWebEnrichment ? 'Re-run web enrichment' : 'Find websites & contacts'}
            </Button>
          )}
          <Button asChild variant="outline" size="sm"><a href={api.searchRuns.exportHref(run.id, false)}><Download className="size-4" /> CSV</a></Button>
          <Button asChild variant="outline" size="sm"><a href={api.searchRuns.exportHref(run.id, true)}><Download className="size-4" /> Suitable</a></Button>
          <Button size="sm" onClick={push} disabled={pushing || run.suitableCount === 0}><Send className="size-4" /> {pushing ? 'Queuing…' : 'Push to Ontraport'}</Button>
          <Button variant="ghost" size="icon" className="text-muted-foreground hover:text-destructive" onClick={() => setDeleteOpen(true)} title="Delete search">
            <Trash2 className="size-4" />
          </Button>
        </div>
      </div>

      {editingTags ? (
        <div className="mb-3 flex max-w-xl items-start gap-2">
          <div className="flex-1"><TagInput value={tagsDraft} onChange={setTagsDraft} /></div>
          <Button size="sm" onClick={saveTags} disabled={savingTags}>{savingTags ? 'Saving…' : 'Save'}</Button>
          <Button variant="ghost" size="sm" onClick={() => setEditingTags(false)} disabled={savingTags}>Cancel</Button>
        </div>
      ) : (
        <div className="mb-3 flex flex-wrap items-center gap-1.5">
          {run.tags.map((t) => (
            <span key={t} className="rounded-md border bg-secondary px-2 py-0.5 text-xs font-medium text-secondary-foreground">{t}</span>
          ))}
          <button
            type="button"
            className="flex items-center gap-1 rounded-md border border-dashed px-2 py-0.5 text-xs font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
            onClick={() => { setTagsDraft(run.tags); setEditingTags(true) }}
          >
            {run.tags.length > 0 ? <Pencil className="size-3" /> : <Tag className="size-3" />}
            {run.tags.length > 0 ? 'Edit' : 'Add tags'}
          </button>
        </div>
      )}

      {run.appliedKeywords.length > 0 && (
        <div className="mb-4 flex flex-wrap items-center gap-1.5 text-xs text-muted-foreground">
          <span>
            Excluding {run.appliedKeywords.length.toLocaleString()} keyword{run.appliedKeywords.length > 1 ? 's' : ''}
          </span>
          {(showAllKeywords ? run.appliedKeywords : run.appliedKeywords.slice(0, KEYWORDS_PREVIEW)).map((k) => (
            <span key={k} className="rounded-md border border-primary/30 bg-primary/10 px-1.5 py-0.5 font-mono text-primary">{k}</span>
          ))}
          {run.appliedKeywords.length > KEYWORDS_PREVIEW && (
            <button
              type="button"
              className="rounded-md border px-1.5 py-0.5 font-medium transition-colors hover:bg-secondary hover:text-foreground"
              onClick={() => setShowAllKeywords((v) => !v)}
            >
              {showAllKeywords ? 'Show fewer' : `+${(run.appliedKeywords.length - KEYWORDS_PREVIEW).toLocaleString()} more`}
            </button>
          )}
        </div>
      )}

      <motion.div initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} className="mb-5 rounded-xl border bg-card p-4">
        <PipelineMonitor run={run} web={web} />
      </motion.div>

      {run.error && (
        <div className="mb-5 flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-red-700 dark:text-red-300">
          <AlertTriangle className="mt-0.5 size-4 shrink-0" /> {run.error}
        </div>
      )}

      {failedCount > 0 && (
        <div className="mb-5 rounded-lg border border-amber-500/30 bg-amber-500/10 px-3 py-2.5 text-sm text-amber-900 dark:text-amber-200">
          <div className="flex items-start gap-2">
            <AlertTriangle className="mt-0.5 size-4 shrink-0" />
            <div className="min-w-0 flex-1">
              <div className="font-medium">
                {failedCount} record{failedCount > 1 ? 's' : ''} failed enrichment
                {captchaIssue ? ' — ASIC CAPTCHA could not be solved' : ''}.
              </div>
              <div className="mt-0.5 text-amber-900/80 dark:text-amber-200/80">
                {captchaKeyMissing
                  ? 'No 2Captcha API key is set. Add it in Settings → 2Captcha, then retry.'
                  : captchaIssue
                    ? `${captchaDetail || 'Check the 2Captcha account (balance and key), then retry.'}`
                    : sampleReason ?? 'Hover a Failed badge below to see why.'}
              </div>
            </div>
            {run.status !== 'Running' && (
              <Button variant="outline" size="sm" onClick={retry} disabled={busy}><RotateCcw className="size-4" /> Retry</Button>
            )}
          </div>
        </div>
      )}

      <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-5">
        {stats.map((s) =>
          s.filter ? (
            <button
              key={s.label}
              type="button"
              onClick={() => setFilter((f) => (f === s.filter ? null : s.filter!))}
              title={filter === s.filter ? 'Clear filter' : `Show only ${s.label.toLowerCase()} records`}
              className={cn(
                'rounded-lg border bg-card px-4 py-3 text-left transition-colors hover:border-primary/40',
                filter === s.filter && 'border-primary/60 bg-primary/10 ring-1 ring-primary/30',
              )}
            >
              <div className="flex items-center justify-between text-xs uppercase tracking-wide text-muted-foreground">
                {s.label}
                {filter === s.filter && <X className="size-3" />}
              </div>
              <div className={cn('mt-0.5 font-mono text-lg font-semibold tabular-nums', s.tone)}>{s.value}</div>
            </button>
          ) : (
            <div key={s.label} className="rounded-lg border bg-card px-4 py-3">
              <div className="text-xs uppercase tracking-wide text-muted-foreground">{s.label}</div>
              <div className={cn('mt-0.5 font-mono text-lg font-semibold tabular-nums', s.tone)}>{s.value}</div>
            </div>
          ),
        )}
      </div>

      {showWeb && (
        <motion.div initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} className="mb-6 rounded-xl border bg-card p-4">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <Globe className="size-4 text-muted-foreground" />
              <h2 className="text-sm font-semibold">Web enrichment</h2>
              {web.running ? (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-primary/15 px-2 py-0.5 text-xs font-medium text-amber-800 ring-1 ring-inset ring-primary/40 dark:text-primary">
                  <span className="size-1.5 animate-pulse rounded-full bg-amber-600 dark:bg-primary" /> Running
                </span>
              ) : (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/15 px-2 py-0.5 text-xs font-medium text-emerald-700 ring-1 ring-inset ring-emerald-600/30 dark:text-emerald-300">
                  <span className="size-1.5 rounded-full bg-emerald-600 dark:bg-emerald-400" /> {web.eligible > 0 ? 'Complete' : 'Idle'}
                </span>
              )}
            </div>
            <span className="font-mono text-xs tabular-nums text-muted-foreground">
              {web.processed.toLocaleString()} / {web.eligible.toLocaleString()} processed
              {web.running && ` · ${web.pending.toLocaleString()} queued`}
            </span>
          </div>

          {web.eligible > 0 && (
            <div className="mb-4">
              <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                <motion.div
                  className="h-full rounded-full bg-primary"
                  initial={{ width: 0 }}
                  animate={{ width: `${webPct}%` }}
                  transition={{ duration: 0.5 }}
                />
              </div>
              <div className="mt-1 text-right font-mono text-[11px] tabular-nums text-muted-foreground">{webPct}%</div>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
            {webTiles.map((t) => (
              <button
                key={t.label}
                type="button"
                onClick={() => setFilter((f) => (f === t.filter ? null : t.filter))}
                title={filter === t.filter ? 'Clear filter' : `Show only records ${filterLabels[t.filter]}`}
                className={cn(
                  'rounded-lg border bg-card px-4 py-3 text-left transition-colors hover:border-primary/40',
                  filter === t.filter && 'border-primary/60 bg-primary/10 ring-1 ring-primary/30',
                )}
              >
                <div className="flex items-center justify-between text-xs uppercase tracking-wide text-muted-foreground">
                  {t.label}
                  {filter === t.filter && <X className="size-3" />}
                </div>
                <div className={cn('mt-0.5 font-mono text-lg font-semibold tabular-nums', t.tone)}>{t.value.toLocaleString()}</div>
              </button>
            ))}
          </div>

          {web.skipped > 0 && (
            <p className="mt-3 text-xs text-muted-foreground">
              {web.skipped.toLocaleString()} record{web.skipped > 1 ? 's' : ''} skipped (renewal outside the lead window).
            </p>
          )}
        </motion.div>
      )}

      {filter && (
        <div className="mb-3 flex items-center gap-2 text-xs text-muted-foreground">
          <span>
            Showing only records <span className="font-medium text-foreground">{filterLabels[filter]}</span>
          </span>
          <button type="button" className="rounded-md border px-1.5 py-0.5 font-medium transition-colors hover:bg-secondary hover:text-foreground" onClick={() => setFilter(null)}>
            Clear filter
          </button>
        </div>
      )}

      <div className="overflow-x-auto rounded-xl border bg-card">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead>Business name</TableHead>
              <TableHead>Holder</TableHead>
              <TableHead>Holder ABN</TableHead>
              <TableHead>ABN status</TableHead>
              <TableHead>Enrichment</TableHead>
              <TableHead>Suitable</TableHead>
              {run.enableWebEnrichment && <TableHead>Websites &amp; email</TableHead>}
              <TableHead>Ontraport</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {records.length === 0 && (
              <TableRow className="hover:bg-transparent">
                <TableCell colSpan={run.enableWebEnrichment ? 8 : 7}>
                  <div className="py-12 text-center text-sm text-muted-foreground">
                    {filter
                      ? `No ${filter} records.`
                      : run.status === 'Running' || run.status === 'Pending'
                        ? 'Harvesting… rows appear here in real time as names are found.'
                        : 'No records.'}
                  </div>
                </TableCell>
              </TableRow>
            )}
            {records.map((r) => {
              const pending = r.enrichmentStatus === 'Pending'
              const websites = r.websites ? r.websites.split(',').filter(Boolean) : []
              const webEnriched = r.webEnrichmentStatus === 'Enriched'
              return (
                <TableRow key={r.id} className={cn(!r.isSuitable && !pending && 'bg-amber-500/5', webEnriched && 'bg-emerald-500/5', pending && 'opacity-60')}>
                  <TableCell className="max-w-[16rem] truncate font-medium" title={r.businessName ?? undefined}>{r.businessName || <span className="text-muted-foreground">—</span>}</TableCell>
                  <TableCell className="max-w-[14rem] truncate text-muted-foreground" title={r.holderName ?? undefined}>{r.holderName || '—'}</TableCell>
                  <TableCell className="font-mono text-xs tabular-nums">{r.holderAbn}</TableCell>
                  <TableCell className="text-muted-foreground">{r.abnStatus || '—'}</TableCell>
                  <TableCell>
                    {r.enrichmentError ? (
                      <Tooltip>
                        <TooltipTrigger><StatusBadge status={r.enrichmentStatus} /></TooltipTrigger>
                        <TooltipContent className="max-w-xs">{r.enrichmentError}</TooltipContent>
                      </Tooltip>
                    ) : (
                      <StatusBadge status={r.enrichmentStatus} />
                    )}
                  </TableCell>
                  <TableCell>
                    {pending ? (
                      <span className="text-xs text-muted-foreground">—</span>
                    ) : r.isSuitable ? (
                      <span className="text-sm text-emerald-700 dark:text-emerald-300">Yes</span>
                    ) : (
                      <Tooltip>
                        <TooltipTrigger className="text-sm text-amber-800 dark:text-amber-300">No</TooltipTrigger>
                        <TooltipContent>Matched keyword: {r.suitabilityReason}</TooltipContent>
                      </Tooltip>
                    )}
                  </TableCell>
                  {run.enableWebEnrichment && (
                    <TableCell className="max-w-[15rem] text-xs">
                      {r.contactEmail && (
                        <div className="truncate">
                          <a href={`mailto:${r.contactEmail}`} className="font-mono text-primary hover:underline" title={r.contactEmails ?? undefined}>{r.contactEmail}</a>
                        </div>
                      )}
                      {websites.length > 0 && (
                        <div className="truncate text-muted-foreground" title={websites.join(', ')}>
                          <a href={`https://${websites[0]}`} target="_blank" rel="noreferrer" className="hover:underline">{websites[0]}</a>
                          {websites.length > 1 && <span> +{websites.length - 1}</span>}
                        </div>
                      )}
                      {r.contactPhone && <div className="text-muted-foreground">{r.contactPhone}</div>}
                      {r.contactSocials && (
                        <div className="truncate text-muted-foreground" title={r.contactSocials}>{r.contactSocials}</div>
                      )}
                      {!r.contactEmail && websites.length === 0 && (
                        r.webEnrichmentError ? (
                          <Tooltip>
                            <TooltipTrigger className="text-muted-foreground">{webStatusLabel(r.webEnrichmentStatus)}</TooltipTrigger>
                            <TooltipContent className="max-w-xs">{r.webEnrichmentError}</TooltipContent>
                          </Tooltip>
                        ) : (
                          <span className="text-muted-foreground">{webStatusLabel(r.webEnrichmentStatus)}</span>
                        )
                      )}
                    </TableCell>
                  )}
                  <TableCell>
                    {r.ontraportError ? (
                      <Tooltip>
                        <TooltipTrigger><StatusBadge status={r.ontraportStatus} /></TooltipTrigger>
                        <TooltipContent>{r.ontraportError}</TooltipContent>
                      </Tooltip>
                    ) : (
                      <StatusBadge status={r.ontraportStatus} />
                    )}
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </div>

      {data.recordsTotalCount > 0 && (
        <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
          <span className="font-mono text-xs tabular-nums text-muted-foreground">
            {data.recordsTotalCount.toLocaleString()} record{data.recordsTotalCount > 1 ? 's' : ''}
            {data.recordsTotalPages > 1 && ` · page ${page} of ${data.recordsTotalPages}`}
          </span>
          {data.recordsTotalPages > 1 && (
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}>
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPage((p) => Math.min(data.recordsTotalPages, p + 1))}
                disabled={page >= data.recordsTotalPages}
              >
                Next
              </Button>
            </div>
          )}
        </div>
      )}

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Delete this search?"
        description={`Permanently removes this run${run.foundRecords > 0 ? ` and its ${run.foundRecords} records` : ''}. This can't be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={remove}
      />
    </div>
  )
}
