import { ActivityIndicator, Pressable, Text } from 'react-native';

export type ButtonVariant = 'primary' | 'secondary' | 'destructive';

// Complete literal class strings per variant and state: NativeWind cannot see names built at runtime.
const idle: Record<ButtonVariant, string> = {
  primary: 'border-primary-600 bg-primary-600 active:border-primary-700 active:bg-primary-700',
  secondary: 'border-primary-600 bg-surface-0 active:bg-primary-50',
  destructive: 'border-error-700 bg-error-700 active:border-error-800 active:bg-error-800',
};
const busy: Record<ButtonVariant, string> = {
  primary: 'border-primary-700 bg-primary-700',
  secondary: 'border-primary-600 bg-primary-50',
  destructive: 'border-error-800 bg-error-800',
};
const labelIdle: Record<ButtonVariant, string> = {
  primary: 'text-base font-semibold text-surface-0',
  secondary: 'text-base font-semibold text-primary-700',
  destructive: 'text-base font-semibold text-surface-0',
};
const spinner: Record<ButtonVariant, string> = {
  primary: '#ffffff',
  secondary: '#a3171c',
  destructive: '#ffffff',
};
const disabledBox = 'border-border-200 bg-surface-100';
const disabledLabel = 'text-base font-semibold text-ink-500';

interface ButtonProps {
  label: string;
  onPress: () => void;
  variant?: ButtonVariant;
  disabled?: boolean;
  /** Shows a spinner next to the label and ignores presses; the label should describe the progress. */
  loading?: boolean;
}

export function Button({ label, onPress, variant = 'primary', disabled = false, loading = false }: ButtonProps) {
  const inactive = disabled || loading;
  const box = disabled ? disabledBox : loading ? busy[variant] : idle[variant];
  return (
    <Pressable
      accessibilityRole="button" accessibilityLabel={label}
      accessibilityState={{ disabled: inactive, busy: loading }}
      disabled={inactive} onPress={onPress}
      className={`mt-2 min-h-12 flex-row items-center justify-center gap-2 rounded-md border-2 px-4 py-3 ${box}`}
    >
      {loading && !disabled && <ActivityIndicator color={spinner[variant]} />}
      <Text className={disabled ? disabledLabel : labelIdle[variant]}>{label}</Text>
    </Pressable>
  );
}
