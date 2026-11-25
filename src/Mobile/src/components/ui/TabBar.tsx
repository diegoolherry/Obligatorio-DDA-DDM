import { useEffect } from 'react';
import { Pressable, Text, View } from 'react-native';
import type { BottomTabBarProps } from 'expo-router/tabs';
import { LogIn, MapPin, Navigation, ScanLine, Search, Ticket, type LucideIcon } from 'lucide-react-native';
import { useAppServices } from '../../context/AppServicesContext';
import { visibleTabs, type TabRoute } from '../../navigation/visibleTabs';

const TAB_ICONS: Record<TabRoute, LucideIcon> = {
  index: Search,
  ingreso: LogIn,
  tickets: Ticket,
  validar: ScanLine,
  compartir: Navigation,
  ubicacion: MapPin,
};

/**
 * Pill tab bar drawn below the screens: an icon above each label. Shows only the tabs of the session roles;
 * it re-renders on every login or logout because the services context changes with the session.
 */
export function TabBar({ state, descriptors, navigateToTab }: BottomTabBarProps) {
  const { authentication } = useAppServices();
  const visible: readonly string[] = visibleTabs(authentication.getRoles());
  const focused = state.routes[state.index];
  const ingreso = state.routes.find(route => route.name === 'ingreso');

  // A tab the session can no longer see (for example after a logout) gives way to Ingreso, which every session sees.
  useEffect(() => {
    if (focused && ingreso && !visible.includes(focused.name)) navigateToTab(ingreso.key);
  }, [focused, ingreso, visible, navigateToTab]);

  return (
    <View className="flex-row border-t border-border-200 bg-surface-0 p-1" accessibilityRole="tablist">
      {state.routes.filter(route => visible.includes(route.name)).map(route => {
        const label = descriptors[route.key]?.options.title ?? route.name;
        const selected = focused?.key === route.key;
        const Icon: LucideIcon | undefined = TAB_ICONS[route.name as TabRoute];
        return (
          <Pressable
            key={route.key}
            accessibilityRole="tab" accessibilityLabel={label} accessibilityState={{ selected }}
            onPress={() => { if (!selected) navigateToTab(route.key); }}
            className={selected
              ? 'min-h-14 flex-1 items-center justify-center gap-0.5 rounded-2xl bg-accent-500 px-1 py-1'
              : 'min-h-14 flex-1 items-center justify-center gap-0.5 rounded-2xl bg-surface-0 px-1 py-1 active:bg-surface-100'}
          >
            {/* Hex values of the ink-900 and ink-700 tokens in global.css: lucide takes a color prop, not a className. */}
            {Icon && <Icon size={20} color={selected ? '#141414' : '#3d3d3a'} strokeWidth={selected ? 2.5 : 2} />}
            <Text className={selected ? 'text-center text-xs font-bold text-ink-900' : 'text-center text-xs font-semibold text-ink-700'}>{label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}
