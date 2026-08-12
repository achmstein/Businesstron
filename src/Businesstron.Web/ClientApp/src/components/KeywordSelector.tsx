import { useEffect, useMemo, useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Plus, X, Check } from 'lucide-react'
import { api } from '../api/client'
import { cn } from '@/lib/utils'

/**
 * Per-run keyword filter. Global keywords load as toggle chips (click to include /
 * exclude for THIS search); type to add ad-hoc terms. Reports the included set.
 */
export default function KeywordSelector({ onChange }: { onChange: (keywords: string[]) => void }) {
  const [pool, setPool] = useState<string[]>([])
  const [globals, setGlobals] = useState<Set<string>>(new Set())
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [input, setInput] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    void (async () => {
      try {
        const words = (await api.keywords.list()).filter((k) => k.isActive).map((k) => k.word)
        setPool(words)
        setGlobals(new Set(words))
        setSelected(new Set(words))
      } catch { /* keep empty */ }
    })()
  }, [])

  const selectedList = useMemo(() => Array.from(selected), [selected])
  useEffect(() => { onChange(selectedList) }, [selectedList, onChange])

  const toggle = (word: string) =>
    setSelected((prev) => {
      const next = new Set(prev)
      next.has(word) ? next.delete(word) : next.add(word)
      return next
    })

  const add = (raw: string) => {
    const word = raw.trim()
    if (!word) return
    if (!pool.some((w) => w.toLowerCase() === word.toLowerCase())) setPool((p) => [...p, word])
    setSelected((prev) => new Set(prev).add(word))
    setInput('')
  }

  const removeAdhoc = (word: string) => {
    setPool((p) => p.filter((w) => w !== word))
    setSelected((prev) => {
      const next = new Set(prev)
      next.delete(word)
      return next
    })
  }

  const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault()
      add(input)
    } else if (e.key === 'Backspace' && !input && pool.length) {
      const last = pool[pool.length - 1]
      if (!globals.has(last)) removeAdhoc(last)
    }
  }

  return (
    <div>
      <div
        className="flex flex-wrap gap-1.5 rounded-lg border bg-background/40 p-2 focus-within:ring-[3px] focus-within:ring-ring/40"
        onClick={() => inputRef.current?.focus()}
      >
        <AnimatePresence initial={false}>
          {pool.map((word) => {
            const on = selected.has(word)
            const adhoc = !globals.has(word)
            return (
              <motion.button
                type="button"
                key={word}
                layout
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.8 }}
                transition={{ duration: 0.14 }}
                onClick={(e) => { e.stopPropagation(); toggle(word) }}
                className={cn(
                  'group inline-flex items-center gap-1 rounded-md border px-2 py-1 font-mono text-xs transition-colors',
                  on
                    ? 'border-primary/40 bg-primary/15 text-primary'
                    : 'border-border bg-muted/40 text-muted-foreground line-through',
                )}
              >
                {on ? <Check className="size-3" /> : <X className="size-3" />}
                {word}
                {adhoc && (
                  <span
                    onClick={(e) => { e.stopPropagation(); removeAdhoc(word) }}
                    className="ml-0.5 rounded-sm opacity-50 hover:opacity-100"
                  >
                    <X className="size-3" />
                  </span>
                )}
              </motion.button>
            )
          })}
        </AnimatePresence>

        <div className="flex min-w-24 flex-1 items-center gap-1 px-1">
          <Plus className="size-3.5 text-muted-foreground" />
          <input
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={onKeyDown}
            placeholder={pool.length ? 'add term…' : 'add a keyword to exclude…'}
            className="h-6 w-full bg-transparent text-xs outline-none placeholder:text-muted-foreground"
          />
        </div>
      </div>
      <p className="mt-1.5 text-xs text-muted-foreground">
        {selected.size} filtering · click a chip to include or exclude it for this run.
      </p>
    </div>
  )
}
