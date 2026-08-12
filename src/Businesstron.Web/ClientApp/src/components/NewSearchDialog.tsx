import { useCallback, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Calendar, ListChecks } from 'lucide-react'
import { toast } from 'sonner'
import { api, type SearchSource } from '../api/client'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Switch } from '@/components/ui/switch'
import KeywordSelector from './KeywordSelector'
import TagInput from './TagInput'
import { cn } from '@/lib/utils'

export default function NewSearchDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (v: boolean) => void }) {
  const [source, setSource] = useState<SearchSource>('AbnList')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [abnListRaw, setAbnListRaw] = useState('')
  const [keywords, setKeywords] = useState<string[]>([])
  const [tags, setTags] = useState<string[]>([])
  const [enableWebEnrichment, setEnableWebEnrichment] = useState(false)
  const [busy, setBusy] = useState(false)
  const navigate = useNavigate()

  const onKeywords = useCallback((k: string[]) => setKeywords(k), [])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setBusy(true)
    try {
      const { id } = await api.searchRuns.create({
        source,
        startDate: source === 'DataGov' ? startDate : null,
        endDate: source === 'DataGov' ? endDate || startDate : null,
        abnListRaw: source === 'AbnList' ? abnListRaw : null,
        keywords,
        tags,
        enableWebEnrichment,
      })
      toast.success('Search started')
      onOpenChange(false)
      navigate(`/searches/${id}`)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to start search')
    } finally {
      setBusy(false)
    }
  }

  const options: { value: SearchSource; title: string; desc: string; icon: typeof Calendar }[] = [
    { value: 'AbnList', title: 'ABN list', desc: 'Look up pasted ABNs', icon: ListChecks },
    { value: 'DataGov', title: 'data.gov.au', desc: 'New names by date', icon: Calendar },
  ]

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle className="font-display text-xl">New search</DialogTitle>
          <DialogDescription>Harvest business names, then enrich, filter and push suitable leads.</DialogDescription>
        </DialogHeader>

        <form onSubmit={submit} className="space-y-5">
          <div>
            <Label className="mb-2">Data source</Label>
            <div className="grid grid-cols-2 gap-3">
              {options.map((opt) => {
                const Icon = opt.icon
                const active = source === opt.value
                return (
                  <button
                    type="button" key={opt.value} onClick={() => setSource(opt.value)}
                    className={cn(
                      'flex items-center gap-3 rounded-lg border p-3 text-left transition-colors',
                      active ? 'border-primary/50 bg-primary/10' : 'hover:bg-accent',
                    )}
                  >
                    <Icon className={cn('size-5', active ? 'text-primary' : 'text-muted-foreground')} />
                    <div>
                      <div className="text-sm font-medium">{opt.title}</div>
                      <div className="text-xs text-muted-foreground">{opt.desc}</div>
                    </div>
                  </button>
                )
              })}
            </div>
          </div>

          {source === 'DataGov' ? (
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="start">Start date</Label>
                <Input id="start" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="end">End date <span className="font-normal text-muted-foreground">(optional)</span></Label>
                <Input id="end" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
              </div>
            </div>
          ) : (
            <div className="space-y-1.5">
              <Label htmlFor="abns">ABNs <span className="font-normal text-muted-foreground">(one per line)</span></Label>
              <Textarea id="abns" value={abnListRaw} onChange={(e) => setAbnListRaw(e.target.value)} required rows={5} placeholder={'51824753556\n12345678901'} className="font-mono text-sm" />
            </div>
          )}

          <div className="space-y-1.5">
            <Label>Tags <span className="font-normal text-muted-foreground">(optional)</span></Label>
            <TagInput value={tags} onChange={setTags} placeholder="e.g. NSW, plumbers, test" />
          </div>

          <div className="space-y-1.5">
            <Label>Filter keywords for this run</Label>
            <KeywordSelector onChange={onKeywords} />
          </div>

          <div className="flex items-start justify-between gap-4 rounded-lg border p-3">
            <div className="space-y-0.5">
              <Label htmlFor="web-enrichment" className="cursor-pointer">Find websites &amp; contacts</Label>
              <p className="text-xs text-muted-foreground">
                After enrichment, reverse-whois suitable leads renewing within 12 months, then
                look up a contact email. Uses WhoisXML + CAPTCHA credits.
              </p>
            </div>
            <Switch id="web-enrichment" checked={enableWebEnrichment} onCheckedChange={setEnableWebEnrichment} />
          </div>

          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button type="submit" disabled={busy}>{busy ? 'Starting…' : 'Start search'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
