// Pure display helpers for locations. No React Native imports.

const uruguayOffsetMinutes = -3 * 60; // Uruguay has no daylight saving time.

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

/** Formats a UTC instant (ISO with Z) as dd/MM/yyyy HH:mm:ss in Uruguay time; undefined if invalid. */
export function formatUruguayDateTime(utc: string): string | undefined {
  const milliseconds = Date.parse(utc);
  if (!Number.isFinite(milliseconds)) return undefined;
  try {
    const parts = new Intl.DateTimeFormat('en-GB', {
      timeZone: 'America/Montevideo', hourCycle: 'h23', year: 'numeric', month: '2-digit',
      day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit',
    }).formatToParts(new Date(milliseconds));
    const get = (type: string) => parts.find(part => part.type === type)?.value;
    const [day, month, year, hour, minute, second] = ['day', 'month', 'year', 'hour', 'minute', 'second'].map(get);
    if (day && month && year && hour && minute && second) return `${day}/${month}/${year} ${hour}:${minute}:${second}`;
  } catch {
    // Engines without time-zone data fall through to the fixed UTC-3 offset.
  }
  const shifted = new Date(milliseconds + uruguayOffsetMinutes * 60000);
  return `${pad(shifted.getUTCDate())}/${pad(shifted.getUTCMonth() + 1)}/${shifted.getUTCFullYear()} `
    + `${pad(shifted.getUTCHours())}:${pad(shifted.getUTCMinutes())}:${pad(shifted.getUTCSeconds())}`;
}

/** Reads HH:mm from a local time without zone (the API already sends Uruguay time). */
export function localHour(value: string): string {
  const match = /T(\d{2}):(\d{2})/.exec(value);
  return match ? `${match[1]}:${match[2]}` : '--:--';
}

/** Human-readable age such as «hace 45 s» or «hace 3 min 5 s». */
export function formatAge(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds < 0) return 'antigüedad desconocida';
  const total = Math.round(seconds);
  if (total < 60) return `hace ${total} s`;
  const minutes = Math.floor(total / 60);
  if (minutes < 60) return total % 60 === 0 ? `hace ${minutes} min` : `hace ${minutes} min ${total % 60} s`;
  const hours = Math.floor(minutes / 60);
  return minutes % 60 === 0 ? `hace ${hours} h` : `hace ${hours} h ${minutes % 60} min`;
}
