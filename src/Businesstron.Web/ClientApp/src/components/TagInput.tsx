import { useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { X, Tag } from 'lucide-react'

/** Simple free-text tag editor: type + Enter/comma to add, × or Backspace to remove. */
export default function TagInput({
  value,
  onChange,
  placeholder = 'add a label…',
}: {
  value: string[]
  onChange: (tags: string[]) => void
  placeholder?: string
}) {
  const [input, setInput] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  const add = (raw: string) => {
    const tag = raw.trim()
    if (tag && !value.some((v) => v.toLowerCase() === tag.toLowerCase())) onChange([...value, tag])
    setInput('')
  }

  const remove = (tag: string) => onChange(value.filter((v) => v !== tag))

  const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault()
      add(input)
    } else if (e.key === 'Backspace' && !input && value.length) {
      remove(value[value.length - 1])
    }
  }

  return (
    <div
      className="flex flex-wrap items-center gap-1.5 rounded-lg border bg-background/40 p-2 focus-within:ring-[3px] focus-within:ring-ring/40"
      onClick={() => inputRef.current?.focus()}
    >
      <AnimatePresence initial={false}>
        {value.map((tag) => (
          <motion.span
            key={tag}
            layout
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.8 }}
            transition={{ duration: 0.14 }}
            className="inline-flex items-center gap-1 rounded-md border bg-secondary px-2 py-1 text-xs text-secondary-foreground"
          >
            {tag}
            <button type="button" onClick={(e) => { e.stopPropagation(); remove(tag) }} className="opacity-60 hover:opacity-100">
              <X className="size-3" />
            </button>
          </motion.span>
        ))}
      </AnimatePresence>
      <div className="flex min-w-24 flex-1 items-center gap-1 px-1">
        <Tag className="size-3.5 text-muted-foreground" />
        <input
          ref={inputRef}
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={onKeyDown}
          placeholder={placeholder}
          className="h-6 w-full bg-transparent text-xs outline-none placeholder:text-muted-foreground"
        />
      </div>
    </div>
  )
}
