import type { RolIngreso } from '../contracts/authentication';

/** Route file names in src/app, in tab bar order. */
export type TabRoute = 'index' | 'ingreso' | 'tickets' | 'validar' | 'compartir' | 'ubicacion';

const TAB_ORDER: readonly TabRoute[] = ['index', 'ingreso', 'tickets', 'validar', 'compartir', 'ubicacion'];

/** Shown without a session and to roles that do not use Mobile (the administrator works in Web). */
const PUBLIC_TABS: readonly TabRoute[] = ['index', 'ingreso'];

const TABS_BY_ROLE: Record<RolIngreso, readonly TabRoute[]> = {
  pasajero: ['index', 'ingreso', 'tickets', 'ubicacion'],
  cobrador: ['ingreso', 'validar', 'compartir', 'ubicacion'],
  administrador: PUBLIC_TABS,
};

/** Tabs of the session roles: the union over every role, or the public tabs when there is none. */
export function visibleTabs(roles: readonly RolIngreso[]): readonly TabRoute[] {
  const allowed = new Set<TabRoute>(roles.length === 0 ? PUBLIC_TABS : roles.flatMap(role => TABS_BY_ROLE[role]));
  return TAB_ORDER.filter(route => allowed.has(route));
}
