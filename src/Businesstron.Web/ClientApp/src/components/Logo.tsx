/**
 * Businesstron mark — an angular "A" peak with a nested inner peak (registry / verified
 * "mountain range"). Monoline, uses currentColor so it themes to gold / white / ink.
 */
export default function Logo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" fill="none" className={className} aria-hidden="true">
      <path
        d="M5 40.5 L24 7 L43 40.5"
        stroke="currentColor"
        strokeWidth="6"
        strokeLinejoin="round"
        strokeLinecap="round"
      />
      <path
        d="M17 40.5 L24 26 L31 40.5"
        stroke="currentColor"
        strokeWidth="6"
        strokeLinejoin="round"
        strokeLinecap="round"
      />
    </svg>
  )
}
