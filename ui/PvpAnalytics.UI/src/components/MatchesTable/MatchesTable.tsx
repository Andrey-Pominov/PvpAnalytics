import { useNavigate } from 'react-router-dom'
import Tooltip from '../Tooltip/Tooltip'
import type { MatchSummary } from '../../types/stats'

interface MatchesTableProps {
  matches: MatchSummary[]
}

const MatchesTable = ({ matches }: MatchesTableProps) => {
  const navigate = useNavigate()
  // Check if any match has a result value
  const hasAnyResult = matches.some((m) => m.result !== undefined)

  return (
    <div className="overflow-x-auto">
      <table className="min-w-[560px] w-full border-collapse text-sm text-text">
        <thead className="text-xs font-semibold uppercase tracking-[0.08em] text-text-muted/70">
          <tr>
            <th className="px-3 py-3 text-left">
              <Tooltip content="Date when the match was played">
                <span className="cursor-help">Date</span>
              </Tooltip>
            </th>
            <th className="px-3 py-3 text-left">
              <Tooltip content="Game mode (2v2, 3v3, Solo Shuffle, etc.)">
                <span className="cursor-help">Mode</span>
              </Tooltip>
            </th>
            <th className="px-3 py-3 text-left">
              <Tooltip content="Arena map where the match took place">
                <span className="cursor-help">Map</span>
              </Tooltip>
            </th>
            {hasAnyResult && (
              <th className="px-3 py-3 text-left">
                <Tooltip content="Match outcome: Victory or Defeat">
                  <span className="cursor-help">Result</span>
                </Tooltip>
              </th>
            )}
            <th className="px-3 py-3 text-left">
              <Tooltip content="Total match duration in minutes:seconds format">
                <span className="cursor-help">Duration</span>
              </Tooltip>
            </th>
          </tr>
        </thead>
        <tbody>
          {matches.map((match) => (
            <tr
              key={match.id}
              onClick={() => navigate(`/matches/${match.id}`)}
              className="border-t border-white/10 transition-colors hover:bg-white/5 cursor-pointer"
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

