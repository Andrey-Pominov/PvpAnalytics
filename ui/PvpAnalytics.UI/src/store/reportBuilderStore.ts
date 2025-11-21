import { create } from 'zustand'

export type WidgetType = 'metric-card' | 'chart' | 'table' | 'sparkline' | 'win-rate-list'

export interface DashboardWidget {
  id: string
  type: WidgetType
  x: number
  y: number
  width: number
  height: number
  config?: Record<string, unknown>
}

export interface DashboardLayout {
  id: string
  name: string
  widgets: DashboardWidget[]
  createdAt: string
  updatedAt: string
}

interface ReportBuilderState {
  layouts: DashboardLayout[]
  currentLayout: DashboardLayout | null
  addLayout: (name: string) => void
  updateLayout: (id: string, updates: Partial<DashboardLayout>) => void
  deleteLayout: (id: string) => void
  setCurrentLayout: (id: string | null) => void
  addWidget: (type: WidgetType, config?: Record<string, unknown>) => void
  updateWidget: (widgetId: string, updates: Partial<DashboardWidget>) => void
  deleteWidget: (widgetId: string) => void
  loadLayouts: () => void
}

const STORAGE_KEY = 'pvp-analytics-dashboard-layouts'

const loadFromStorage = (): DashboardLayout[] => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      return JSON.parse(stored) as DashboardLayout[]
    }
  } catch (error) {
    console.error('Failed to load layouts from storage', error)
  }
  return []
}

const saveToStorage = (layouts: DashboardLayout[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(layouts))
  } catch (error) {
    console.error('Failed to save layouts to storage', error)
  }
}

export const useReportBuilderStore = create<ReportBuilderState>((set, get) => ({
  layouts: loadFromStorage(),
  currentLayout: null,

  loadLayouts: () => {
    set({ layouts: loadFromStorage() })
  },

  addLayout: (name) => {
    const newLayout: DashboardLayout = {
      id: crypto.randomUUID(),
      name,
      widgets: [],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    const updated = [...get().layouts, newLayout]
    set({ layouts: updated, currentLayout: newLayout })
    saveToStorage(updated)
  },

  updateLayout: (id, updates) => {
    const updated = get().layouts.map((l) => {
      if (l.id === id) {
        return { ...l, ...updates, updatedAt: new Date().toISOString() }
      }
      return l
    })
    set({ layouts: updated })
    if (get().currentLayout?.id === id) {
      set({ currentLayout: updated.find((l) => l.id === id) || null })
    }
    saveToStorage(updated)
  },

  deleteLayout: (id) => {
    const updated = get().layouts.filter((l) => l.id !== id)
    set({ layouts: updated })
    if (get().currentLayout?.id === id) {
      set({ currentLayout: null })
    }
    saveToStorage(updated)
  },

  setCurrentLayout: (id) => {
    if (id === null) {
      set({ currentLayout: null })
      return
    }
    const layout = get().layouts.find((l) => l.id === id)
    set({ currentLayout: layout || null })
  },

  addWidget: (type, config) => {
    const currentLayout = get().currentLayout
    if (!currentLayout) return

    const newWidget: DashboardWidget = {
      id: crypto.randomUUID(),
      type,
      x: 0,
      y: 0,
      width: type === 'table' ? 12 : 6,
      height: type === 'table' ? 8 : 4,
      config,
    }

    const updatedLayout = {
      ...currentLayout,
      widgets: [...currentLayout.widgets, newWidget],
      updatedAt: new Date().toISOString(),
    }

    const updatedLayouts = get().layouts.map((l) =>
      l.id === currentLayout.id ? updatedLayout : l
    )

    set({ layouts: updatedLayouts, currentLayout: updatedLayout })
    saveToStorage(updatedLayouts)
  },

  updateWidget: (widgetId, updates) => {
    const currentLayout = get().currentLayout
    if (!currentLayout) return

    const updatedLayout = {
      ...currentLayout,
      widgets: currentLayout.widgets.map((w) => (w.id === widgetId ? { ...w, ...updates } : w)),
      updatedAt: new Date().toISOString(),
    }

    const updatedLayouts = get().layouts.map((l) =>
      l.id === currentLayout.id ? updatedLayout : l
    )

    set({ layouts: updatedLayouts, currentLayout: updatedLayout })
    saveToStorage(updatedLayouts)
  },

  deleteWidget: (widgetId) => {
    const currentLayout = get().currentLayout
    if (!currentLayout) return

    const updatedLayout = {
      ...currentLayout,
      widgets: currentLayout.widgets.filter((w) => w.id !== widgetId),
      updatedAt: new Date().toISOString(),
    }

    const updatedLayouts = get().layouts.map((l) =>
      l.id === currentLayout.id ? updatedLayout : l
    )

    set({ layouts: updatedLayouts, currentLayout: updatedLayout })
    saveToStorage(updatedLayouts)
  },
}))
