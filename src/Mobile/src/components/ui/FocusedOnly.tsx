import type { ReactNode } from 'react';
import { useIsFocused } from 'expo-router';

/** Renders its children only while the tab is focused, so they mount again on every visit. */
export function FocusedOnly({ children }: { children: ReactNode }) {
  return useIsFocused() ? <>{children}</> : null;
}
