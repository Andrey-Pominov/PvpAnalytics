import { useState, useMemo } from 'react'
import Card from '../components/Card/Card'
import { useCustomMetricsStore } from '../store/customMetricsStore'
import { parseMetric, metricTemplates, evaluateMetric } from '../utils/metricParser'

const CustomMetricsPage = () => {
  const { metrics, addMetric, updateMetric, deleteMetric } = useCustomMetricsStore()
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formName, setFormName] = useState('')
  const [formExpression, setFormExpression] = useState('')
  const [formDescription, setFormDescription] = useState('')
  const [testVariables, setTestVariables] = useState<Record<string, string>>({})

  const parsedExpression = useMemo(() => parseMetric(formExpression), [formExpression])

  const handleSave = () => {
    if (!formName.trim() || !formExpression.trim()) {
      return
    }

    if (editingId) {
      updateMetric(editingId, {
        name: formName.trim(),
        expression: formExpression.trim(),
        description: formDescription.trim() || undefined,
      })
    } else {
      addMetric(formName.trim(), formExpression.trim(), formDescription.trim() || undefined)
    }

    // Reset form
    setFormName('')
    setFormExpression('')
    setFormDescription('')
    setShowForm(false)
    setEditingId(null)
  }

  const handleEdit = (metric: typeof metrics[0]) => {
    setFormName(metric.name)
    setFormExpression(metric.expression)
    setFormDescription(metric.description || '')
    setEditingId(metric.id)
    setShowForm(true)
  }

  const handleDelete = (id: string) => {
    if (confirm('Are you sure you want to delete this metric?')) {
      deleteMetric(id)
    }
  }

  const handleUseTemplate = (template: typeof metricTemplates[0]) => {
    setFormName(template.name)
    setFormExpression(template.expression)
    setFormDescription(template.description)
    setShowForm(true)
  }

  const testResult = useMemo(() => {
    if (!formExpression.trim() || !parsedExpression.isValid) {
      return null
    }

    const variables: Record<string, number> = {}
    for (const [key, value] of Object.entries(testVariables)) {
      const numValue = Number.parseFloat(value)
      if (!Number.isNaN(numValue)) {
        variables[key] = numValue
      }
    }

    return evaluateMetric(formExpression, variables)
  }, [formExpression, parsedExpression, testVariables])

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-text">Custom Metrics</h1>
        <button
          onClick={() => {
            setShowForm(!showForm)
            setEditingId(null)
            setFormName('')
            setFormExpression('')
            setFormDescription('')
          }}
          className="rounded-xl bg-gradient-to-r from-accent to-sky-400 px-6 py-3 text-sm font-semibold text-white transition-all hover:shadow-lg"
        >
          {showForm ? 'Cancel' : '+ New Metric'}
        </button>
      </div>

      {showForm && (
        <Card title={editingId ? 'Edit Metric' : 'Create New Metric'}>
          <div className="space-y-4">
            <div>
              <label htmlFor="metric-name" className="block text-sm font-semibold text-text-muted">
                <span className="block mb-1">Name</span>
                <input
                  id="metric-name"
                  type="text"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="e.g., Kills per Minute"
                  className="w-full rounded-lg border border-accent-muted/40 bg-surface/50 px-4 py-2 text-text focus:border-accent focus:outline-none"
                />
              </label>
            </div>

            <div>
              <label htmlFor="metric-expression" className="block text-sm font-semibold text-text-muted">
                <span className="block mb-1">Expression</span>
                <textarea
                  id="metric-expression"
                  value={formExpression}
                  onChange={(e) => setFormExpression(e.target.value)}
                  placeholder="e.g., Kills / (Duration / 60)"
                  rows={3}
                  className="w-full rounded-lg border border-accent-muted/40 bg-surface/50 px-4 py-2 text-text focus:border-accent focus:outline-none font-mono text-sm"
                />
              </label>
              {parsedExpression.variables.length > 0 && (
                <p className="mt-1 text-xs text-text-muted">
                  Variables: {parsedExpression.variables.join(', ')}
                </p>
              )}
              {parsedExpression.error && (
                <p className="mt-1 text-xs text-rose-300">{parsedExpression.error}</p>
              )}
            </div>

            <div>
              <label htmlFor="metric-description" className="block text-sm font-semibold text-text-muted">
                <span className="block mb-1">Description (optional)</span>
                <input
                  id="metric-description"
                  type="text"
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  placeholder="Brief description of what this metric measures"
                  className="w-full rounded-lg border border-accent-muted/40 bg-surface/50 px-4 py-2 text-text focus:border-accent focus:outline-none"
                />
              </label>
            </div>

            {/* Test Section */}
            {parsedExpression.isValid && parsedExpression.variables.length > 0 && (
              <div className="rounded-lg border border-accent-muted/40 bg-surface/30 p-4">
                <label className="block text-sm font-semibold text-text-muted mb-2">Test Expression</label>
                <div className="space-y-2">
                  {parsedExpression.variables.map((varName) => (
                    <div key={varName} className="flex items-center gap-2">
                      <label htmlFor={`test-var-${varName}`} className="w-24 text-sm text-text-muted">{varName}:
                      <input
                        id={`test-var-${varName}`}
                        type="number"
                        value={testVariables[varName] || ''}
                        onChange={(e) =>
                          setTestVariables({ ...testVariables, [varName]: e.target.value })
                        }
                        placeholder="0"
                        className="flex-1 rounded-lg border border-accent-muted/40 bg-surface/50 px-3 py-1 text-text focus:border-accent focus:outline-none"
                      />
                      </label>
                    </div>
                  ))}
                </div>
                {testResult && (
                  <div className="mt-3 rounded-lg bg-accent/10 px-3 py-2">
                    <div className="text-sm font-semibold text-text">
                      Result: {testResult.error ? (
                        <span className="text-rose-300">{testResult.error}</span>
                      ) : (
                        <span className="text-accent">{testResult.result.toFixed(2)}</span>
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}

            <div className="flex gap-2">
              <button
                onClick={handleSave}
                disabled={!formName.trim() || !formExpression.trim() || !parsedExpression.isValid}
                className="rounded-lg bg-gradient-to-r from-accent to-sky-400 px-6 py-2 text-sm font-semibold text-white transition-all hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {editingId ? 'Update' : 'Create'}
              </button>
              <button
                onClick={() => {
                  setShowForm(false)
                  setEditingId(null)
                  setFormName('')
                  setFormExpression('')
                  setFormDescription('')
                }}
                className="rounded-lg border border-accent-muted/40 bg-surface/50 px-6 py-2 text-sm font-semibold text-text transition-colors hover:bg-surface/70"
              >
                Cancel
              </button>
            </div>
          </div>
        </Card>
      )}

      {/* Templates */}
      {!showForm && (
        <Card title="Metric Templates" subtitle="Start with a pre-built formula">
          <div className="grid gap-3 grid-cols-1 sm:grid-cols-2">
            {metricTemplates.map((template) => (
              <button
                key={template.name}
                onClick={() => handleUseTemplate(template)}
                className="rounded-lg border border-accent-muted/40 bg-surface/50 p-4 text-left transition-colors hover:bg-surface/70"
              >
                <div className="font-semibold text-text">{template.name}</div>
                <div className="mt-1 text-sm text-text-muted">{template.description}</div>
                <div className="mt-2 font-mono text-xs text-accent">{template.expression}</div>
              </button>
            ))}
          </div>
        </Card>
      )}

      {/* Existing Metrics */}
      <Card title={`Your Metrics (${metrics.length})`}>
        {metrics.length === 0 ? (
          <div className="text-center py-12 text-text-muted">
            No custom metrics yet. Create one to get started!
          </div>
        ) : (
          <div className="space-y-4">
            {metrics.map((metric) => (
              <div
                key={metric.id}
                className="rounded-lg border border-accent-muted/40 bg-surface/50 p-4"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <h3 className="font-semibold text-text">{metric.name}</h3>
                      {metric.parsed?.isValid === false && (
                        <span className="rounded-full bg-rose-500/20 px-2 py-1 text-xs text-rose-300">
                          Invalid
                        </span>
                      )}
                    </div>
                    {metric.description && (
                      <p className="mt-1 text-sm text-text-muted">{metric.description}</p>
                    )}
                    <div className="mt-2 font-mono text-sm text-accent">{metric.expression}</div>
                    {metric.parsed?.variables && metric.parsed.variables.length > 0 && (
                      <p className="mt-1 text-xs text-text-muted">
                        Variables: {metric.parsed.variables.join(', ')}
                      </p>
                    )}
                    {metric.parsed?.error && (
                      <p className="mt-1 text-xs text-rose-300">{metric.parsed.error}</p>
                    )}
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleEdit(metric)}
                      className="rounded-lg px-3 py-1 text-sm text-text-muted hover:bg-surface/70 hover:text-text transition-colors"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDelete(metric.id)}
                      className="rounded-lg px-3 py-1 text-sm text-rose-300 hover:bg-rose-500/20 transition-colors"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  )
}

export default CustomMetricsPage

