import { Pressable, Text, View } from 'react-native';
import type { BottomTabBarProps } from 'expo-router/tabs';

/** Pill tab bar drawn above the screens, the same look as the former manual tabs. */
export function TabBar({ state, descriptors, navigateToTab }: BottomTabBarProps) {
  return (
    <View className="flex-row border-b border-border-200 bg-surface-0 p-1" accessibilityRole="tablist">
      {state.routes.map((route, index) => {
        const label = descriptors[route.key]?.options.title ?? route.name;
        const selected = state.index === index;
        return (
          <Pressable
            key={route.key}
            accessibilityRole="tab" accessibilityLabel={label} accessibilityState={{ selected }}
            onPress={() => { if (!selected) navigateToTab(route.key); }}
            className={selected
              ? 'min-h-12 flex-1 items-center justify-center rounded-full bg-accent-500 px-1'
              : 'min-h-12 flex-1 items-center justify-center rounded-full bg-surface-0 px-1 active:bg-surface-100'}
          >
            <Text className={selected ? 'text-center text-xs font-bold text-ink-900' : 'text-center text-xs font-semibold text-ink-700'}>{label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}
