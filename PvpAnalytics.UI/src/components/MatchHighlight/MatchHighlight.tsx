import type { MatchDetail } from '../../types/stats'

interface MatchHighlightProps {
  match: MatchDetail
}

const formatNumber = (value: number) =>
  new Intl.NumberFormat(undefined, { notation: 'compact', maximumFractionDigits: 1 }).format(value)

const MatchHighlight = ({ match }: MatchHighlightProps) => (
  <div className="flex flex-col gap-5">
    <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h3 className="text-lg font-semibold text-text">{match.mode}</h3>
        <p className="mt-1 text-sm text-text-muted">
          {match.map} • {match.ratingDelta.toFixed(2)} rating • {match.result}
        </p>
      </div>
      <span
        className={`inline-flex rounded-full px-4 py-2 text-sm font-semibold ${
          match.result === 'Victory'
            ? 'bg-emerald-500/20 text-emerald-200'
            : 'bg-rose-500/20 text-rose-200'
        }`}
      >
        {match.result}
      </span>
    </header>

    <div className="grid gap-4 md:grid-cols-2">
      {match.teams.map((team) => (
        <div
          key={team.name}
          className="rounded-2xl border border-accent-muted/30 bg-background/60 p-4 backdrop-blur-md"
        >
          <h4 className="mb-3 text-xs font-semibold uppercase tracking-[0.08em] text-text-muted">
            {team.name}
          </h4>
          <div className="flex flex-col gap-3">
            {team.players.map((player) => (
              <div
                key={`${team.name}-${player.name}`}
                className="flex items-start justify-between gap-3 rounded-xl bg-white/5 p-3"
              >
                <div>
                  <span className="block text-sm font-semibold text-text">{player.name}</span>
                  <span className="block text-xs text-text-muted">
                    {player.specialization} {player.className}
                  </span>
                </div>
                <div className="grid grid-cols-3 gap-3 text-xs text-text-muted">
                  <span>{formatNumber(player.damageDone)}</span>
                  <span>{formatNumber(player.healingDone)}</span>
                  <span>{player.crowdControl.toFixed(1)}s</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>

    <footer className="flex flex-wrap gap-4 text-xs text-text-muted">
      {match.timeline.map((event, index) => (
        <div
          key={`${event.timestamp}-${index}`}
          className="flex min-w-[160px] flex-col gap-1 rounded-xl border border-accent-muted/30 bg-background/60 px-3 py-2"
        >
          <span className="text-[0.65rem] font-semibold uppercase tracking-wide text-accent">
            {(event.timestamp / 60).toFixed(2)}
          </span>
          <span className="max-w-[220px]">{event.description}</span>
        </div>
      ))}
    </footer>
  </div>
)

export default MatchHighlight

