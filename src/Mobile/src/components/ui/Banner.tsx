import { Text, View } from 'react-native';
import { Button } from './Button';

export type BannerKind = 'error' | 'offline' | 'warning' | 'info';

const box: Record<BannerKind, string> = {
  error: 'bg-error-100',
  offline: 'bg-ink-900',
  warning: 'bg-warning-100',
  info: 'bg-info-100',
};
const text: Record<BannerKind, string> = {
  error: 'text-base text-error-700',
  offline: 'text-base text-surface-0',
  warning: 'text-base text-warning-800',
  info: 'text-base text-info-700',
};
// The prefix carries the meaning in words, so color is never the only signal.
const prefix: Record<BannerKind, string> = {
  error: 'Error: ',
  offline: 'Sin conexión: ',
  warning: 'Atención: ',
  info: '',
};

interface BannerProps {
  kind: BannerKind;
  children: string;
  actionLabel?: string;
  onAction?: () => void;
}

export function Banner({ kind, children, actionLabel, onAction }: BannerProps) {
  return (
    <View className={`gap-2 rounded-lg p-4 ${box[kind]}`} accessibilityLiveRegion="polite">
      <Text className={text[kind]}>{prefix[kind]}{children}</Text>
      {actionLabel !== undefined && onAction !== undefined && (
        <View className="self-start">
          <Button label={actionLabel} variant="secondary" onPress={onAction} />
        </View>
      )}
    </View>
  );
}
