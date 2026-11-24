import { Pressable, Text, View } from 'react-native';
import type { BottomTabBarProps } from 'expo-router/tabs';
import { LogIn, MapPin, Navigation, Search, type LucideIcon } from 'lucide-react-native';

/** Icon per route file name in src/app; a route without an entry shows only its label. */
const TAB_ICONS: Record<string, LucideIcon> = {
  index: Search,
  ingreso: LogIn,
  compartir: Navigation,
  ubicacion: MapPin,
};

/** Pill tab bar drawn below the screens: an icon above each label, the same look as the former manual tabs. */
export function TabBar({ state, descriptors, navigateToTab }: BottomTabBarProps) {
  return (
    <View className="flex-row border-t border-border-200 bg-surface-0 p-1" accessibilityRole="tablist">
      {state.routes.map((route, index) => {
        const label = descriptors[route.key]?.options.title ?? route.name;
        const selected = state.index === index;
        const Icon = TAB_ICONS[route.name];
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
