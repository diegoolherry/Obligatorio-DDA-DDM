import { Text, View } from 'react-native';
import { Button } from './Button';

export type BannerKind = 'error' | 'offline' | 'warning' | 'info' | 'success';

const box: Record<BannerKind, string> = {
  error: 'bg-error-100',
  offline: 'bg-ink-900',
  warning: 'bg-warning-100',
  info: 'bg-info-100',
  success: 'bg-success-100',
};
const text: Record<BannerKind, string> = {
  error: 'text-base text-error-700',
  offline: 'text-base text-surface-0',
  warning: 'text-base text-warning-800',
  info: 'text-base text-info-700',
  success: 'text-base text-success-700',
};
// The prefix carries the meaning in words, so color is never the only signal.
const titleText: Record<BannerKind, string> = {
  error: 'text-lg font-bold text-error-700',
  offline: 'text-lg font-bold text-surface-0',
  warning: 'text-lg font-bold text-warning-800',
  info: 'text-lg font-bold text-info-700',
  success: 'text-lg font-bold text-success-700',
};
const prefix: Record<BannerKind, string> = {
  error: 'Error: ',
  offline: 'Sin conexión: ',
  warning: 'Atención: ',
  info: '',
  success: '',
};

interface BannerProps {
  kind: BannerKind;
  children: string;
  /** Short headline; when present it replaces the prefix, since the words already name the state. */
  title?: string;
  actionLabel?: string;
  onAction?: () => void;
}

export function Banner({ kind, children, title, actionLabel, onAction }: BannerProps) {
  return (
    <View className={`gap-2 rounded-lg p-4 ${box[kind]}`} accessibilityLiveRegion="polite">
      {title !== undefined && <Text className={titleText[kind]} accessibilityRole="header">{title}</Text>}
      <Text className={text[kind]}>{title === undefined ? prefix[kind] : ''}{children}</Text>
      {actionLabel !== undefined && onAction !== undefined && (
        <View className="self-start">
          <Button label={actionLabel} variant="secondary" onPress={onAction} />
        </View>
      )}
    </View>
  );
}
