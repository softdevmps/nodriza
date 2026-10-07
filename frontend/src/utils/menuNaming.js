export function toSafeName(value) {
  if (!value) return 'Vista'

  const normalized = value.normalize('NFD').replace(/[\u0300-\u036f]/g, '')
  const parts = normalized.split(/[^a-zA-Z0-9]+/).filter(Boolean)

  if (!parts.length) return 'Vista'

  return parts
    .map(part => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
    .join('')
}

export function normalizePath(path) {
  if (!path) return ''

  let cleaned = path.trim()
  if (!cleaned.startsWith('/')) cleaned = `/${cleaned}`
  if (cleaned.length > 1) cleaned = cleaned.replace(/\/+$/, '')

  return cleaned
}
