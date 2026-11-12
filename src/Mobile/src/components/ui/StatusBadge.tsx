import { Text, View } from 'react-native';

export type BadgeKind = 'success' | 'neutral' | 'error' | 'warning' | 'offline' | 'info' | 'accent';

const box: Record<BadgeKind, string> = {
  success: 'bg-success-100',
  neutral: 'bg-surface-100',
  error: 'bg-error-100',
  warning: 'bg-warning-100',
  offline: 'bg-ink-900',
  info: 'bg-info-100',
  accent: 'bg-accent-100',
};
const text: Record<BadgeKind, string> = {
  success: 'text-xs font-semibold text-success-700',
  neutral: 'text-xs font-semibold text-ink-700',
  error: 'text-xs font-semibold text-error-700',
  warning: 'text-xs font-semibold text-warning-800',
  offline: 'text-xs font-semibold text-surface-0',
  info: 'text-xs font-semibold text-info-700',
  accent: 'text-xs font-semibold text-accent-700',
};

/** The label is always shown: the state never depends on color alone. */
export function StatusBadge({ kind, label }: { kind: BadgeKind; label: string }) {
  return (
    <View className={`self-start rounded-full px-3 py-1 ${box[kind]}`}>
      <Text className={text[kind]}>{label}</Text>
    </View>
  );
}
