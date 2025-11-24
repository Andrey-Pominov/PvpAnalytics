import { create } from 'zustand'
import { parseMetric, evaluateMetric, type ParsedMetric } from '../utils/metricParser'

export interface CustomMetric {
  id: string
  name: string
  expression: string
  description?: string
  parsed?: ParsedMetric
  createdAt: string
  updatedAt: string
}

interface CustomMetricsState {
  metrics: CustomMetric[]
  addMetric: (name: string, expression: string, description?: string) => void
  updateMetric: (id: string, updates: Partial<CustomMetric>) => void
  deleteMetric: (id: string) => void
  evaluateMetric: (id: string, variables: Record<string, number>) => { result: number; error?: string }
  getMetric: (id: string) => CustomMetric | undefined
  loadMetrics: () => void
}

const STORAGE_KEY = 'pvp-analytics-custom-metrics'

const loadFromStorage = (): CustomMetric[] => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      const parsed = JSON.parse(stored) as CustomMetric[]
      // Re-parse expressions
      return parsed.map((m) => ({
        ...m,
        parsed: parseMetric(m.expression),
      }))
    }
  } catch (error) {
    console.error('Failed to load metrics from storage', error)
  }
  return []
}

const saveToStorage = (metrics: CustomMetric[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(metrics))
  } catch (error) {
    console.error('Failed to save metrics to storage', error)
  }
}

export const useCustomMetricsStore = create<CustomMetricsState>((set, get) => ({
  metrics: loadFromStorage(),

  loadMetrics: () => {
    set({ metrics: loadFromStorage() })
  },

  addMetric: (name, expression, description) => {
    const parsed = parseMetric(expression)
    const newMetric: CustomMetric = {
      id: crypto.randomUUID(),
      name,
      expression,
      description,
      parsed,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    const updated = [...get().metrics, newMetric]
    set({ metrics: updated })
    saveToStorage(updated)
  },

  updateMetric: (id, updates) => {
    const updated = get().metrics.map((m) => {
      if (m.id === id) {
        const result = { ...m, ...updates }
        // Re-parse expression if it changed
        if (updates.expression) {
          result.parsed = parseMetric(updates.expression)
        }
        result.updatedAt = new Date().toISOString()
        return result
      }
      return m
    })
    set({ metrics: updated })
    saveToStorage(updated)
  },

  deleteMetric: (id) => {
    const updated = get().metrics.filter((m) => m.id !== id)
    set({ metrics: updated })
    saveToStorage(updated)
  },

  evaluateMetric: (id, variables) => {
    const metric = get().getMetric(id)
    if (!metric) {
      return { result: 0, error: 'Metric not found' }
    }
    return evaluateMetric(metric.expression, variables)
  },

  getMetric: (id) => {
    return get().metrics.find((m) => m.id === id)
  },
}))

