import { Pressable, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

interface AppHeaderProps {
  /** Screen title; the Berrutti wordmark when omitted. */
  title?: string;
  onBack?: () => void;
}

export function AppHeader({ title = 'Berrutti', onBack }: AppHeaderProps) {
  const { top } = useSafeAreaInsets();
  return (
    <View className="border-b-4 border-accent-500 bg-ink-900 px-4 pb-3" style={{ paddingTop: top + 12 }}>
      <View className="min-h-10 flex-row items-center gap-3">
        {onBack && (
          <Pressable accessibilityRole="button" accessibilityLabel="Volver" onPress={onBack}
            className="min-h-12 min-w-12 items-center justify-center active:opacity-60">
            <Text className="text-2xl text-surface-0">←</Text>
          </Pressable>
        )}
        <Text accessibilityRole="header" className="text-2xl font-extrabold text-surface-0">{title}</Text>
      </View>
    </View>
  );
}
