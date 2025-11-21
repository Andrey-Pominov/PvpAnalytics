import { Parser } from 'expr-eval'

export interface ParseResult {
  success: boolean
  error?: string
  variables?: string[]
}

export interface EvaluateResult {
  success: boolean
  value?: number
  error?: string
}

/**
 * Escapes special regex characters in a string to make it safe for use in RegExp
 */
function escapeRegex(str: string): string {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

/**
 * Validates and parses a metric expression to extract variable names
 */
export function parseMetric(expression: string): ParseResult {
  if (!expression || typeof expression !== 'string') {
    return { success: false, error: 'Expression must be a non-empty string' }
  }

  const trimmed = expression.trim()
  if (!trimmed) {
    return { success: false, error: 'Expression cannot be empty' }
  }

  try {
    // Create a parser instance for validation
    const parser = new Parser()
    
    // Try to parse the expression to validate syntax
    // We'll use a dummy context to check if variables are valid identifiers
    const testExpr = parser.parse(trimmed)
    
    // Extract variable names from the expression
    // expr-eval variables are extracted from the parsed expression
    const variables: string[] = []
    
    // Walk through the expression to find variable references
    // This is a simplified approach - expr-eval handles variable extraction internally
    const variablePattern = /\b[a-zA-Z_][a-zA-Z0-9_]*\b/g
    const matches = trimmed.match(variablePattern) || []
    
    // Filter out known operators and functions
    const operators = ['and', 'or', 'not', 'if', 'abs', 'acos', 'asin', 'atan', 'ceil', 'cos', 'exp', 'floor', 'ln', 'log', 'max', 'min', 'pow', 'round', 'sin', 'sqrt', 'tan']
    const uniqueVariables = [...new Set(matches.filter((match) => !operators.includes(match.toLowerCase()) && !/^\d/.test(match)))]
    
    return {
      success: true,
      variables: uniqueVariables,
    }
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Invalid expression syntax',
    }
  }
}

/**
 * Safely evaluates a metric expression with provided variable values
 * Replaces unsafe new Function() with expr-eval for security
 */
export function evaluateMetric(
  expression: string,
  variables: Record<string, number>
): EvaluateResult {
  // Validate expression first using parseMetric
  const parseResult = parseMetric(expression)
  if (!parseResult.success) {
    return {
      success: false,
      error: parseResult.error || 'Expression validation failed',
    }
  }

  try {
    // Create a parser instance
    const parser = new Parser()
    
    // Parse the expression
    const expr = parser.parse(expression)
    
    // Safely evaluate with provided variables
    // expr-eval only evaluates mathematical expressions and doesn't allow arbitrary code execution
    const result = expr.evaluate(variables)
    
    // Ensure result is a number
    if (typeof result !== 'number' || !Number.isFinite(result)) {
      return {
        success: false,
        error: 'Expression evaluation did not produce a valid number',
      }
    }
    
    return {
      success: true,
      value: result,
    }
  } catch (error) {
    return {
      success: false,
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
    const regex = new RegExp(`\\b${escapedName}\\b`, 'g')
    substituted = substituted.replace(regex, String(variables[varName]))
  }
  
  return substituted
}
