export function toKebab(value) {
  if (!value) return 'item';

  const trimmed = String(value).trim();
  if (!trimmed) return 'item';

  let result = '';
  let prevDash = false;

  for (const ch of trimmed) {
    if (/[A-Za-z0-9]/.test(ch)) {
      if (/[A-Z]/.test(ch) && result.length > 0 && !prevDash) {
        result += '-';
      }

      result += ch.toLowerCase();
      prevDash = false;
    } else {
      if (!prevDash && result.length > 0) {
        result += '-';
        prevDash = true;
      }
    }
  }

  result = result.replace(/-+$/g, '').replace(/^-+/g, '');
  return result || 'item';
}
