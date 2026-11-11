/** Validates the API origin: HTTPS, no credentials, path, query or fragment. */
export function apiRoot(value?: string): string | undefined {
  try {
    if (!value || !/^https:\/\/[^/@\s\?#]+\/?$/.test(value)) return undefined;
    const url = new URL(value);
    if (url.protocol !== 'https:' || !url.hostname || url.username || url.password) return undefined;
    if (url.pathname !== '/') return undefined;
    return url.origin;
  } catch { return undefined; }
}
