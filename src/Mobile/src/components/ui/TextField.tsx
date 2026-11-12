import { useState } from 'react';
import { Text, TextInput, View } from 'react-native';
import type { Ref } from 'react';
import type { TextInputInstance, TextInputProps } from 'react-native';

interface TextFieldProps extends Omit<TextInputProps, 'editable' | 'className'> {
  label: string;
  /** nativeID of the visible label; the input points at it with accessibilityLabelledBy. */
  labelId: string;
  error?: string;
  helperText?: string;
  disabled?: boolean;
  ref?: Ref<TextInputInstance>;
}

const stateBorder = {
  disabled: 'border-border-200 bg-surface-100 text-ink-500',
  error: 'border-error-700 bg-surface-0 text-ink-900',
  focused: 'border-primary-600 bg-surface-0 text-ink-900',
  idle: 'border-ink-300 bg-surface-0 text-ink-900',
} as const;

export function TextField({ label, labelId, error, helperText, disabled = false, ref, onFocus, onBlur, ...input }: TextFieldProps) {
  const [focused, setFocused] = useState(false);
  const state = disabled ? 'disabled' : error ? 'error' : focused ? 'focused' : 'idle';
  return (
    <View className="gap-1">
      <Text nativeID={labelId} className="mt-2 text-base font-semibold text-ink-900">{label}</Text>
      <TextInput
        {...input} ref={ref} editable={!disabled}
        accessibilityLabel={input.accessibilityLabel ?? label} accessibilityLabelledBy={labelId}
        onFocus={event => { setFocused(true); onFocus?.(event); }}
        onBlur={event => { setFocused(false); onBlur?.(event); }}
        className={`min-h-13 rounded-md border-2 px-3 text-base ${stateBorder[state]}`}
      />
      {error !== undefined && (
        <View className="rounded-sm bg-error-100 px-3 py-2" accessibilityLiveRegion="polite">
          <Text className="text-sm text-error-700">{error}</Text>
        </View>
      )}
      {error === undefined && helperText !== undefined && <Text className="text-sm text-ink-500">{helperText}</Text>}
    </View>
  );
}
