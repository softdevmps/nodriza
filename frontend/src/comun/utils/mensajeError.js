// Extrae un mensaje legible de un error de axios, sea cual sea la forma de la respuesta:
// texto plano, { message }, o ValidationProblemDetails de ASP.NET ({ errors: { Campo: [..] } }).
export function mensajeError (error, porDefecto = 'No se pudo completar la operación.') {
  const data = error?.response?.data
  if (!data) return error?.message || porDefecto
  if (typeof data === 'string') return data
  if (data.errors && typeof data.errors === 'object') {
    const mensajes = Object.values(data.errors).flat().filter(Boolean)
    if (mensajes.length) return mensajes.join('\n')
  }
  return data.message || data.title || porDefecto
}

// Misma regla que el backend (Comun/NombresSql.cs) para nombres de tabla y columna.
const IDENTIFICADOR = /^[\p{L}_][\p{L}\p{Nd}_]{0,127}$/u

export const reglaIdentificadorSql = (v) =>
  IDENTIFICADOR.test(v ?? '') || 'Solo letras, números y guion bajo, sin espacios ni símbolos; no puede empezar con un número.'
