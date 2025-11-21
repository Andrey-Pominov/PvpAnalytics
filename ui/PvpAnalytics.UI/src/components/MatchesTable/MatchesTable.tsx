import type { MatchSummary } from '../../types/stats'

interface MatchesTableProps {
  matches: MatchSummary[]
}

const MatchesTable = ({ matches }: MatchesTableProps) => {
  // Check if any match has a result value
  const hasAnyResult = matches.some((m) => m.result !== undefined)

  return (
    <div className="overflow-x-auto">
      <table className="min-w-[560px] w-full border-collapse text-sm text-text">
        <thead className="text-xs font-semibold uppercase tracking-[0.08em] text-text-muted/70">
          <tr>
            <th className="px-3 py-3 text-left">Date</th>
            <th className="px-3 py-3 text-left">Mode</th>
            <th className="px-3 py-3 text-left">Map</th>
            {hasAnyResult && <th className="px-3 py-3 text-left">Result</th>}
            <th className="px-3 py-3 text-left">Duration</th>
          </tr>
        </thead>
        <tbody>
          {matches.map((match) => (
            <tr
              key={match.id}
              className="border-t border-white/10 transition-colors hover:bg-white/5"
            >
              <td className="px-3 py-3">{match.date}</td>
              <td className="px-3 py-3">{match.mode}</td>
              <td className="px-3 py-3">{match.map}</td>
              {hasAnyResult && (
                <td className="px-3 py-3">
                  {match.result ? (
                    <span
                      className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${
                        match.result === 'Victory'
                          ? 'bg-emerald-500/20 text-emerald-200'
                          : 'bg-rose-500/20 text-rose-200'
                      }`}
                    >
                      {match.result}
                    </span>
                  ) : (
                    <span className="text-text-muted">—</span>
                  )}
                </td>
              )}
              <td className="px-3 py-3">{match.duration}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default MatchesTable

