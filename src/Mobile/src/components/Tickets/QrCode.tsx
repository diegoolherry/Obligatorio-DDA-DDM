import { Text, View } from 'react-native';

/**
 * Contract of the QR renderer. `value` is the opaque code from the API: never show it as text.
 * `dimmed` marks a ticket that no longer works (utilizado o vencido).
 */
export interface QrCodeProps {
  value: string;
  dimmed: boolean;
  /** Side of the square in density-independent units. */
  size?: number;
}

/**
 * PLACEHOLDER. No QR library is installed in src/Mobile/package.json and adding one needs team approval,
 * so this draws a clearly labelled stand-in instead of a scannable code. Replace the body of this single
 * component with the approved library (same props) and nothing else in the flow changes.
 */
export function QrCode({ dimmed, size = 224 }: QrCodeProps) {
  return (
    <View
      accessibilityRole="image"
      accessibilityLabel={dimmed ? 'Código QR atenuado, ya no sirve' : 'Marcador de posición del código QR'}
      style={{ width: size, height: size }}
      className={dimmed
        ? 'items-center justify-center rounded-lg border-2 border-dashed border-ink-300 bg-surface-100 p-3 opacity-40'
        : 'items-center justify-center rounded-lg border-2 border-dashed border-ink-700 bg-surface-0 p-3'}
    >
      <Text className="text-center text-base font-bold text-ink-900">Marcador del QR</Text>
      <Text className="text-center text-sm text-ink-700">Falta elegir la librería para dibujar el código real.</Text>
    </View>
  );
}
