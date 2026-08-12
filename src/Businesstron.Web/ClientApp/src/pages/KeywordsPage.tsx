import { useCallback, useEffect, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Plus, X, Check } from 'lucide-react'
import { toast } from 'sonner'
import { api, type FilterKeywordDto } from '../api/client'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export default function KeywordsPage() {
  const [keywords, setKeywords] = useState<FilterKeywordDto[]>([])
  const [newWord, setNewWord] = useState('')

  const load = useCallback(async () => {
    try {
      setKeywords(await api.keywords.list())
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load')
    }
  }, [])

  useEffect(() => { void load() }, [load])

  const add = async (e: React.FormEvent) => {
    e.preventDefault()
    const word = newWord.trim()
    if (!word) return
    setNewWord('')
    try {
      await api.keywords.create(word)
      await load()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to add')
    }
  }

  const toggle = async (k: FilterKeywordDto) => {
    setKeywords((prev) => prev.map((x) => (x.id === k.id ? { ...x, isActive: !x.isActive } : x)))
    try {
      await api.keywords.update(k.id, k.word, !k.isActive)
    } catch {
      void load()
    }
  }

  const remove = async (k: FilterKeywordDto) => {
    setKeywords((prev) => prev.filter((x) => x.id !== k.id))
    try {
      await api.keywords.remove(k.id)
      toast.success(`Removed “${k.word}”`)
    } catch {
      void load()
    }
  }

  const activeCount = keywords.filter((k) => k.isActive).length

  return (
    <div className="mx-auto max-w-3xl">
      <h1 className="font-display text-2xl font-semibold tracking-tight">Filter keywords</h1>
      <p className="mb-6 mt-1 text-sm text-muted-foreground">
        A business name containing any <span className="font-medium text-foreground">active</span> keyword (case-insensitive) is flagged
        unsuitable and left out of the Ontraport push. <span className="font-mono text-primary">{activeCount}</span> active ·{' '}
        <span className="font-mono">{keywords.length - activeCount}</span> muted.
      </p>

      <form onSubmit={add} className="mb-6 flex gap-2">
        <div className="flex flex-1 items-center gap-2 rounded-lg border bg-background/40 px-3 focus-within:ring-[3px] focus-within:ring-ring/40">
          <Plus className="size-4 text-muted-foreground" />
          <input
            value={newWord}
            onChange={(e) => setNewWord(e.target.value)}
            placeholder="add a keyword to exclude, e.g. legal"
            className="h-10 w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground"
          />
        </div>
        <Button type="submit">Add</Button>
      </form>

      <div className="rounded-xl border bg-card p-4">
        {keywords.length === 0 ? (
          <div className="py-8 text-center text-sm text-muted-foreground">No keywords yet.</div>
        ) : (
          <div className="flex flex-wrap gap-2">
            <AnimatePresence initial={false}>
              {keywords.map((k) => (
                <motion.div
                  key={k.id}
                  layout
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.8 }}
                  transition={{ duration: 0.15 }}
                  className={cn(
                    'group inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 font-mono text-sm transition-colors',
                    k.isActive
                      ? 'border-primary/40 bg-primary/12 text-primary'
                      : 'border-border bg-muted/40 text-muted-foreground line-through',
                  )}
                >
                  <button type="button" onClick={() => toggle(k)} className="inline-flex items-center gap-1.5" title={k.isActive ? 'Mute' : 'Activate'}>
                    {k.isActive ? <Check className="size-3.5" /> : <X className="size-3.5" />}
                    {k.word}
                  </button>
                  <button
                    type="button"
                    onClick={() => remove(k)}
                    className="ml-0.5 rounded-sm text-muted-foreground opacity-0 transition-opacity hover:text-destructive group-hover:opacity-100"
                    title="Remove"
                  >
                    <X className="size-3.5" />
                  </button>
                </motion.div>
              ))}
            </AnimatePresence>
          </div>
        )}
      </div>
      <p className="mt-2 text-xs text-muted-foreground">Click a chip to mute it · hover and press × to remove.</p>
    </div>
  )
}
