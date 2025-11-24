import {useState, useEffect} from 'react'
import {useReportBuilderStore, type WidgetType, type DashboardWidget} from '../store/reportBuilderStore'
import Card from '../components/Card/Card'
import MetricCard from '../components/MetricCard/MetricCard'
import Sparkline from '../components/Sparkline/Sparkline'
import WinRateList from '../components/WinRateList/WinRateList'
import MatchesTable from '../components/MatchesTable/MatchesTable'
import {mockPlayerStatistics} from '../mocks/playerStats'

const WIDGET_TYPES: Array<{ type: WidgetType; label: string; icon: string }> = [
    {type: 'metric-card', label: 'Metric Card', icon: '📊'},
    {type: 'chart', label: 'Chart', icon: '📈'},
    {type: 'sparkline', label: 'Sparkline', icon: '📉'},
    {type: 'win-rate-list', label: 'Win Rate List', icon: '📋'},
    {type: 'table', label: 'Table', icon: '📄'},
]

const ReportBuilderPage = () => {
    const {
        layouts,
        addLayout,
        currentLayout,
        deleteLayout,
        setCurrentLayout,
        addWidget,
        deleteWidget,
        loadLayouts,
    } = useReportBuilderStore()

    const [draggedWidget, setDraggedWidget] = useState<WidgetType | null>(null)
    const [newLayoutName, setNewLayoutName] = useState('')

    useEffect(() => {
        loadLayouts()
    }, [loadLayouts])

    const handleDragStart = (widgetType: WidgetType) => {
        setDraggedWidget(widgetType)
    }

    const handleDragOver = (e: React.DragEvent) => {
        e.preventDefault()
    }

    const handleDrop = (e: React.DragEvent) => {
        e.preventDefault()
        if (draggedWidget && currentLayout) {
            addWidget(draggedWidget)
            setDraggedWidget(null)
        }
    }

    const handleCreateLayout = () => {
        if (newLayoutName.trim()) {
            addLayout(newLayoutName.trim())
            setNewLayoutName('')
        }
    }

    const renderWidget = (widget: DashboardWidget) => {
        const stats = mockPlayerStatistics

        switch (widget.type) {
            case 'metric-card':
                return (
                    <MetricCard
                        title="Win Rate"
                        value="65%"
                        subtitle="Recent matches"
                        trend="up"
                        trendValue="+5%"
                        tooltip="Custom metric card widget"
                    />
                )
            case 'sparkline':
                return (
                    <div className="h-full">
                        <Sparkline values={stats.overviewTrend}/>
                    </div>
                )
            case 'win-rate-list':
                return <WinRateList title="Win Rate by Bracket" entries={stats.winRateByBracket}/>
            case 'table':
                return <MatchesTable matches={stats.matches.slice(0, 5)}/>
            case 'chart':
                return (
                    <div
                        className="flex h-full items-center justify-center rounded-lg border border-accent-muted/40 bg-surface/50 text-text-muted">
                        Chart Widget (Placeholder)
                    </div>
                )
            default:
                return null
        }
    }

    return (
        <div className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-bold text-text">Report Builder</h1>
                <div className="flex gap-2">
                    <input
                        type="text"
                        value={newLayoutName}
                        onChange={(e) => setNewLayoutName(e.target.value)}
                        placeholder="New layout name"
                        className="rounded-lg border border-accent-muted/40 bg-surface/50 px-4 py-2 text-text focus:border-accent focus:outline-none"
                        onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                                handleCreateLayout()
                            }
                        }}
                    />
                    <button
                        onClick={handleCreateLayout}
                        disabled={!newLayoutName.trim()}
                        className="rounded-xl bg-gradient-to-r from-accent to-sky-400 px-6 py-2 text-sm font-semibold text-white transition-all hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        Create Layout
                    </button>
                </div>
            </div>

            {/* Layout Selector */}
            {layouts.length > 0 && (
                <Card title="Saved Layouts">
                    <div className="flex flex-wrap gap-2">
                        {layouts.map((layout) => (
                            <button
                                key={layout.id}
                                onClick={() => setCurrentLayout(layout.id)}
                                className={`rounded-lg px-4 py-2 text-sm font-semibold transition-colors ${
                                    currentLayout?.id === layout.id
                                        ? 'bg-gradient-to-r from-accent to-sky-400 text-white'
                                        : 'bg-surface/50 text-text hover:bg-surface/70'
                                }`}
                            >
                                {layout.name}
                            </button>
                        ))}
                    </div>
                </Card>
            )}

            {!currentLayout && layouts.length === 0 && (
                <Card>
                    <div className="text-center py-12 text-text-muted">
                        No layouts yet. Create a new layout to get started!
                    </div>
                </Card>
            )}

            {currentLayout && (
                <>
                    {/* Widget Palette */}
                    <Card title="Widgets" subtitle="Drag widgets to the canvas">
                        <div className="flex flex-wrap gap-3">
                            {WIDGET_TYPES.map((widget) => (
                                <div
                                    key={widget.type}
                                    draggable
                                    onDragStart={() => handleDragStart(widget.type)}
                                    className="flex cursor-move items-center gap-2 rounded-lg border border-accent-muted/40 bg-surface/50 px-4 py-2 text-sm text-text transition-colors hover:bg-surface/70"
                                >
                                    <span>{widget.icon}</span>
                                    <span>{widget.label}</span>
                                </div>
                            ))}
                        </div>
                    </Card>

                    {/* Canvas */}
                    <Card
                        title={currentLayout.name}
                        actions={
                            <button
                                onClick={() => {
                                    if (confirm('Are you sure you want to delete this layout?')) {
                                        deleteLayout(currentLayout.id)
                                        setCurrentLayout(null)
                                    }
                                }}
                                className="rounded-lg px-4 py-2 text-sm text-rose-300 hover:bg-rose-500/20 transition-colors"
                            >
                                Delete Layout
                            </button>
                        }
                    >
                        <div
                            className="min-h-[600px] rounded-lg border-2 border-dashed border-accent-muted/40 bg-surface/30 p-6"
                            onDragOver={handleDragOver}
                            onDrop={handleDrop}
                        >
                            {currentLayout.widgets.length === 0 ? (
                                <div className="flex h-full items-center justify-center text-text-muted">
                                    Drag widgets here to build your dashboard
                                </div>
                            ) : (
                                <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
                                    {currentLayout.widgets.map((widget) => (
                                        <div
                                            key={widget.id}
                                            className="group relative rounded-lg border border-accent-muted/40 bg-surface/50 p-4"
                                        >
                                            <button
                                                onClick={() => deleteWidget(widget.id)}
                                                className="absolute right-2 top-2 opacity-0 transition-opacity group-hover:opacity-100 rounded-full bg-rose-500/20 p-1 text-rose-300 hover:bg-rose-500/40"
                                                aria-label="Delete widget"
                                            >
                                                <svg
                                                    xmlns="http://www.w3.org/2000/svg"
                                                    className="h-4 w-4"
                                                    fill="none"
                                                    viewBox="0 0 24 24"
                                                    stroke="currentColor"
                                                >
                                                    <path
                                                        strokeLinecap="round"
                                                        strokeLinejoin="round"
                                                        strokeWidth={2}
                                                        d="M6 18L18 6M6 6l12 12"
                                                    />
                                                </svg>
                                            </button>
                                            {renderWidget(widget)}
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </Card>
                </>
            )}
        </div>
    )
}

export default ReportBuilderPage
