import { useId } from 'react'

export default function Sparkline({
  data,
  width = 120,
  height = 32,
}: {
  data: number[]
  width?: number
  height?: number
}) {
  // Unique per instance: a hardcoded gradient id would collide if two sparklines
  // ever render on the same page, and the second would pick up the first's fill.
  const gradientId = useId()

  if (data.length === 0) return null

  // Inset the plot so the end dot and the stroke's round caps paint *inside* the
  // viewBox. Previously the svg needed overflow-visible to let them escape, which
  // meant the chart drew over the edge of its card.
  const pad = 2
  const plotWidth = width - pad * 2
  const plotHeight = height - pad * 2

  const max = Math.max(...data, 1)
  const step = data.length > 1 ? plotWidth / (data.length - 1) : plotWidth
  const points = data.map(
    (v, i) => [pad + i * step, pad + plotHeight - (v / max) * plotHeight] as const,
  )

  const line = points
    .map(([x, y], i) => `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${y.toFixed(1)}`)
    .join(' ')
  const [firstX] = points[0]
  const [lastX, lastY] = points[points.length - 1]
  const area = `${line} L${lastX.toFixed(1)},${height - pad} L${firstX.toFixed(1)},${height - pad} Z`

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      // Stretch to whatever width is spare rather than demanding a fixed 120px,
      // which overflowed the card in the two-column mobile grid. Strokes below opt
      // out of the resulting non-uniform scale so they stay the right weight.
      preserveAspectRatio="none"
      className="h-8 w-full min-w-0 flex-1"
      style={{ maxWidth: width }}
      role="presentation"
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="var(--seal)" stopOpacity="0.35" />
          <stop offset="100%" stopColor="var(--seal)" stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={area} fill={`url(#${gradientId})`} />
      <path
        d={line}
        fill="none"
        stroke="var(--seal)"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />
      {/* Zero-length path + round cap: a dot that stays circular at any width, where
          a <circle> would be squashed into an ellipse by preserveAspectRatio="none". */}
      <path
        d={`M${lastX.toFixed(1)},${lastY.toFixed(1)} L${lastX.toFixed(1)},${lastY.toFixed(1)}`}
        stroke="var(--seal)"
        strokeWidth="4"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  )
}
