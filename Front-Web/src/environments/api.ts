import { environment } from './environment';

/**
 * Prepends the configured API base in development.
 * In production `apiBase` is empty, so paths stay relative (same-origin).
 */
export function apiUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  const base = environment.apiBase.replace(/\/+$/, '');

  return base ? `${base}${normalizedPath}` : normalizedPath;
}
