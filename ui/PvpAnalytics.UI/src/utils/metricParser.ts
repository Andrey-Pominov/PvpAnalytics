export interface ParseResult {
  success: boolean
  error?: string
  variables?: string[]
}

export interface ParsedMetric {
  expression: string
  variables: string[]
  isValid: boolean
  error?: string
}

/**
 * Escapes special regex characters in a string to make it safe for use in RegExp
 */
function escapeRegex(str: string): string {
  return str.replaceAll(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`)
}

/**
 * Checks if expression contains dangerous patterns
 */
function hasDangerousPatterns(cleaned: string): boolean {
  const dangerousPatterns = [
    /eval\s*\(/i,
    /function\s*\(/i,
    /=>/,
    /import\s+/i,
    /require\s*\(/i,
    /\.\s*constructor/i,
    /__proto__/i,
    /prototype/i,
    /constructor/i,
    /\[.*\]\s*\(/, // Array access followed by function call
  ]
  return dangerousPatterns.some((pattern) => pattern.test(cleaned))
}

/**
 * Checks if expression contains unsafe method calls (non-Math.*)
 */
function hasUnsafeMethodCalls(cleaned: string): boolean {
  // Check for method calls that are NOT Math.* (Math.* is allowed)
  // Use regex to capture the identifier immediately before the dot
  // Use bounded whitespace (\s{0,10}) instead of \s* to prevent ReDoS
  const methodCallPattern = /(\w+)\s{0,10}\.\s{0,10}\w+\s{0,10}\(/g
  let methodMatch
  while ((methodMatch = methodCallPattern.exec(cleaned)) !== null) {
    const identifierBeforeDot = methodMatch[1]
    // Allow Math.* but block other method calls - must be exactly "Math"
    if (identifierBeforeDot !== 'Math') {
      return true
    }
  }
  return false
}

/**
 * Validates that parentheses are balanced
 */
function areParenthesesBalanced(cleaned: string): boolean {
  let parenCount = 0
  for (const char of cleaned) {
    if (char === '(') parenCount++
    if (char === ')') parenCount--
    if (parenCount < 0) return false
  }
  return parenCount === 0
}

/**
 * Validates Math function calls are allowed
 */
function validateMathFunctions(cleaned: string): { valid: boolean; error?: string } {
  const mathFunctionPattern = /Math\.(\w+)/g
  const allowedMathFunctions = new Set(['abs', 'acos', 'asin', 'atan', 'ceil', 'cos', 'exp', 'floor', 'log', 'max', 'min', 'pow', 'round', 'sin', 'sqrt', 'tan'])
  let match
  while ((match = mathFunctionPattern.exec(cleaned)) !== null) {
    if (!allowedMathFunctions.has(match[1])) {
      return { valid: false, error: `Unsupported Math function: Math.${match[1]}` }
    }
  }
  return { valid: true }
}

/**
 * Validates that an expression only contains safe mathematical operations
 */
function isValidMathExpression(expression: string): { valid: boolean; error?: string } {
  // Remove whitespace for validation
  const cleaned = expression.replaceAll(/\s/g, '')

  if (hasDangerousPatterns(cleaned)) {
    return { valid: false, error: 'Expression contains unsafe patterns' }
  }

  if (hasUnsafeMethodCalls(cleaned)) {
    return { valid: false, error: 'Expression contains unsafe method calls (only Math.* functions are allowed)' }
  }

  if (!areParenthesesBalanced(cleaned)) {
    return { valid: false, error: 'Unbalanced parentheses' }
  }

  // Allow only: numbers, operators, parentheses, Math functions, and variable names
  const allowedPattern = /^[0-9+\-*/().\s,a-zA-Z_]+$/
  if (!allowedPattern.test(cleaned.replaceAll(/Math\.\w+/g, 'MATHFUNC'))) {
    return { valid: false, error: 'Expression contains invalid characters' }
  }

  // Normalize both bare "ln(" and "Math.ln(" to "Math.log(" before validation
  // Use word boundaries to ensure we match "ln(" as a function call, not inside other identifiers
  let normalized = cleaned.replaceAll(/\bln\(/g, 'Math.log(')
  normalized = normalized.replaceAll('Math.ln(', 'Math.log(')
  return validateMathFunctions(normalized)
}

/**
 * Extracts variable names from an expression
 */
function extractVariables(expression: string): string[] {
  // Match variable names (alphanumeric + underscore, must start with letter)
  const variablePattern = /\b[a-zA-Z_]\w*\b/g
  const matches = expression.match(variablePattern) || []
  
  // Filter out JavaScript keywords, Math functions, and operators
  const keywords = new Set([
    'Math', 'abs', 'acos', 'asin', 'atan', 'ceil', 'cos', 'exp', 'floor', 'ln', 'log', 'max', 'min', 'pow', 'round', 'sin', 'sqrt', 'tan',
    'and', 'or', 'not', 'if', 'true', 'false', 'null', 'undefined', 'NaN', 'Infinity',
  ])
  
  return [...new Set(matches)].filter((v) => !keywords.has(v) && !/^\d/.test(v))
}

/**
 * Validates and parses a metric expression to extract variable names
 * Returns ParsedMetric for backward compatibility with existing code
 */
export function parseMetric(expression: string): ParsedMetric {
  if (!expression || typeof expression !== 'string') {
    return {
      expression: expression || '',
      variables: [],
      isValid: false,
      error: 'Expression must be a non-empty string',
    }
  }

  const trimmed = expression.trim()
  if (!trimmed) {
    return {
      expression: trimmed,
      variables: [],
      isValid: false,
      error: 'Expression cannot be empty',
    }
  }

  // Validate expression safety
  const validation = isValidMathExpression(trimmed)
  if (!validation.valid) {
    return {
      expression: trimmed,
      variables: [],
      isValid: false,
      error: validation.error || 'Invalid expression syntax',
    }
  }

  // Extract variables
  const variables = extractVariables(trimmed)

  return {
    expression: trimmed,
    variables,
    isValid: true,
  }
}

/**
 * Safely evaluates a metric expression with provided variable values
 * Uses string substitution and a controlled evaluation context
 * Returns { result: number; error?: string } for backward compatibility
 */
export function evaluateMetric(
  expression: string,
  variables: Record<string, number>
): { result: number; error?: string } {
  // Validate expression first using parseMetric
  const parseResult = parseMetric(expression)
  if (!parseResult.isValid) {
    return {
      result: 0,
      error: parseResult.error || 'Expression validation failed',
    }
  }

  // Check that all required variables are provided
  const missingVars = parseResult.variables.filter((v) => !(v in variables))
  if (missingVars.length > 0) {
    return {
      result: 0,
      error: `Missing variables: ${missingVars.join(', ')}`,
    }
  }

  try {
    // Substitute variables with their values using safe string replacement
    let substituted = expression
    
    // Sort by length (longest first) to avoid partial replacements
    const varNames = Object.keys(variables).sort((a, b) => b.length - a.length)
    
    for (const varName of varNames) {
      // Escape regex metacharacters in variable name
      const escapedName = escapeRegex(varName)
      // Replace with actual value (use word boundaries to avoid partial matches)
      const regex = new RegExp(String.raw`\b${escapedName}\b`, 'g')
      substituted = substituted.replaceAll(regex, String(variables[varName]))
    }

    // Verify no variables remain (safety check)
    const remainingVars = extractVariables(substituted)
    if (remainingVars.length > 0) {
      return {
        result: 0,
        error: `Could not substitute all variables. Remaining: ${remainingVars.join(', ')}`,
      }
    }

    // Normalize both bare "ln(" and "Math.ln(" to "Math.log(" before evaluation
    // Use word boundaries to ensure we match "ln(" as a function call, not inside other identifiers
    substituted = substituted.replaceAll(/\bln\(/g, 'Math.log(')
    substituted = substituted.replaceAll('Math.ln(', 'Math.log(')

    // Create a safe evaluation context with only Math functions
    // Use Function constructor in a controlled way with a whitelist
    const allowedFunctions = ['Math.abs', 'Math.floor', 'Math.ceil', 'Math.round', 'Math.max', 'Math.min', 'Math.sqrt', 'Math.pow', 'Math.sin', 'Math.cos', 'Math.tan', 'Math.asin', 'Math.acos', 'Math.atan', 'Math.exp', 'Math.log']
    
    // Validate that only allowed Math functions are used
    const functionMatches = substituted.match(/Math\.\w+/g) || []
    for (const func of functionMatches) {
      if (!allowedFunctions.some((allowed) => func.startsWith(allowed))) {
        return {
          result: 0,
          error: `Unsupported function: ${func}`,
        }
      }
    }

    // Evaluate using Function constructor with strict validation
    // This is safer than eval but still requires careful validation (which we've done above)
    const result = new Function('Math', `"use strict"; return ${substituted}`)(Math)
    
    // Ensure result is a number
    if (typeof result !== 'number' || !Number.isFinite(result)) {
      return {
        result: 0,
        error: 'Expression evaluation did not produce a valid number',
      }
    }
    
    return {
      result,
    }
  } catch (error) {
    return {
      result: 0,
      error: error instanceof Error ? error.message : 'Expression evaluation failed',
    }
  }
}

/**
 * Substitutes variable placeholders in an expression with actual values
 * Escapes variable names for safe regex replacement
 */
export function substituteVariables(
  expression: string,
  variables: Record<string, number>
): string {
  let substituted = expression
  
  // Sort by length (longest first) to avoid partial replacements
  const varNames = Object.keys(variables).sort((a, b) => b.length - a.length)
  
  for (const varName of varNames) {
    // Escape regex metacharacters in variable name
    const escapedName = escapeRegex(varName)
    // Replace with actual value
    const regex = new RegExp(String.raw`\b${escapedName}\b`, 'g')
    substituted = substituted.replace(regex, String(variables[varName]))
  }
  
  return substituted
}

/**
 * Predefined metric templates with common formulas
 */
export const metricTemplates = [
  {
    name: 'Kills per Minute',
    expression: 'Kills / Math.max(Duration / 60, 1)',
    description: 'Average number of kills per minute',
    variables: ['Kills', 'Duration'],
  },
  {
    name: 'Damage per Second',
    expression: 'DamageDone / Math.max(Duration, 1)',
    description: 'Average damage dealt per second',
    variables: ['DamageDone', 'Duration'],
  },
  {
    name: 'Efficiency Score',
    expression: '(Kills + Assists) / Math.max(Deaths, 1)',
    description: 'Kill/assist to death ratio',
    variables: ['Kills', 'Assists', 'Deaths'],
  },
  {
    name: 'Win Rate Percentage',
    expression: '(Wins / Math.max(TotalMatches, 1)) * 100',
    description: 'Win rate as a percentage',
    variables: ['Wins', 'TotalMatches'],
  },
  {
    name: 'Average Match Duration',
    expression: 'TotalDuration / Math.max(TotalMatches, 1)',
    description: 'Average duration of matches in seconds',
    variables: ['TotalDuration', 'TotalMatches'],
  },
]
