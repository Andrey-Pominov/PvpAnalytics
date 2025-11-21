/**
 * Custom metric expression parser and calculator
 * Supports basic arithmetic operations and predefined variables
 */

export interface MetricVariable {
  name: string
  value: number
  description?: string
}

export interface ParsedMetric {
  expression: string
  variables: string[]
  isValid: boolean
  error?: string
}

/**
 * Extracts variable names from an expression
 */
function extractVariables(expression: string): string[] {
  // Match variable names (alphanumeric + underscore, must start with letter)
  const variablePattern = /\b[a-zA-Z_][a-zA-Z0-9_]*\b/g
  const matches = expression.match(variablePattern) || []
  // Filter out JavaScript keywords and operators
  const keywords = ['Math', 'abs', 'floor', 'ceil', 'round', 'max', 'min', 'sqrt', 'pow']
  return [...new Set(matches)].filter((v) => !keywords.includes(v))
}

/**
 * Validates and parses a metric expression
 */
export function parseMetric(expression: string): ParsedMetric {
  const trimmed = expression.trim()
  if (!trimmed) {
    return {
      expression: trimmed,
      variables: [],
      isValid: false,
      error: 'Expression cannot be empty',
    }
  }

  // Basic validation: check for balanced parentheses
  let parenCount = 0
  for (const char of trimmed) {
    if (char === '(') parenCount++
    if (char === ')') parenCount--
    if (parenCount < 0) {
      return {
        expression: trimmed,
        variables: [],
        isValid: false,
        error: 'Unbalanced parentheses',
      }
    }
  }
  if (parenCount !== 0) {
    return {
      expression: trimmed,
      variables: [],
      isValid: false,
      error: 'Unbalanced parentheses',
    }
  }

  // Check for dangerous patterns (function calls, etc.)
  const dangerousPatterns = [
    /eval\s*\(/i,
    /function\s*\(/i,
    /=>/,
    /import\s+/i,
    /require\s*\(/i,
    /\.\s*constructor/i,
    /__proto__/i,
  ]

  for (const pattern of dangerousPatterns) {
    if (pattern.test(trimmed)) {
      return {
        expression: trimmed,
        variables: [],
        isValid: false,
        error: 'Expression contains unsafe patterns',
      }
    }
  }

  const variables = extractVariables(trimmed)

  return {
    expression: trimmed,
    variables,
    isValid: true,
  }
}

/**
 * Evaluates a metric expression with provided variable values
 */
export function evaluateMetric(
  expression: string,
  variables: Record<string, number>
): { result: number; error?: string } {
  try {
    // Replace variables with their values
    let evaluated = expression
    for (const [name, value] of Object.entries(variables)) {
      // Use word boundaries to avoid partial matches
      const regex = new RegExp(`\\b${name}\\b`, 'g')
      evaluated = evaluated.replace(regex, value.toString())
    }

    // Check if all variables were replaced
    const remainingVars = extractVariables(evaluated)
    if (remainingVars.length > 0) {
      return {
        result: 0,
        error: `Missing variables: ${remainingVars.join(', ')}`,
      }
    }

    // Evaluate the expression safely
    // Use Function constructor in a controlled way (only allow Math operations)
    const allowedFunctions = ['Math.abs', 'Math.floor', 'Math.ceil', 'Math.round', 'Math.max', 'Math.min', 'Math.sqrt', 'Math.pow']
    const hasFunctionCalls = /Math\.\w+\s*\(/.test(evaluated)
    
    if (hasFunctionCalls) {
      // Validate that only allowed Math functions are used
      const functionMatches = evaluated.match(/Math\.\w+/g) || []
      for (const func of functionMatches) {
        if (!allowedFunctions.some((allowed) => func.startsWith(allowed))) {
          return {
            result: 0,
            error: `Unsupported function: ${func}`,
          }
        }
      }
    }

    // Use Function constructor to evaluate (safer than eval, but still need to be careful)
    const result = new Function('return ' + evaluated)()
    
    if (typeof result !== 'number' || !isFinite(result)) {
      return {
        result: 0,
        error: 'Expression did not evaluate to a valid number',
      }
    }

    return { result }
  } catch (error) {
    return {
      result: 0,
      error: error instanceof Error ? error.message : 'Evaluation error',
    }
  }
}

/**
 * Predefined metric templates with common formulas
 */
export const metricTemplates = [
  {
    name: 'Kills per Minute',
    expression: 'Kills / (Duration / 60)',
    description: 'Average number of kills per minute',
    variables: ['Kills', 'Duration'],
  },
  {
    name: 'Damage per Second',
    expression: 'DamageDone / Duration',
    description: 'Average damage dealt per second',
    variables: ['DamageDone', 'Duration'],
  },
  {
    name: 'Efficiency Score',
    expression: '(Kills + Assists) / Deaths',
    description: 'Kill/assist to death ratio',
    variables: ['Kills', 'Assists', 'Deaths'],
  },
  {
    name: 'Win Rate Percentage',
    expression: '(Wins / TotalMatches) * 100',
    description: 'Win rate as a percentage',
    variables: ['Wins', 'TotalMatches'],
  },
  {
    name: 'Average Match Duration',
    expression: 'TotalDuration / TotalMatches',
    description: 'Average duration of matches in seconds',
    variables: ['TotalDuration', 'TotalMatches'],
  },
]

/**
 * Validates that all required variables are provided
 */
export function validateVariables(
  expression: string,
  providedVariables: Record<string, number>
): { isValid: boolean; missing: string[] } {
  const parsed = parseMetric(expression)
  if (!parsed.isValid) {
    return { isValid: false, missing: [] }
  }

  const missing = parsed.variables.filter((v) => !(v in providedVariables))
  return {
    isValid: missing.length === 0,
    missing,
  }
}

