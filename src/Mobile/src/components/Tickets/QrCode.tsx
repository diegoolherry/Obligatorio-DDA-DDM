import { View } from 'react-native';
import QRCode from 'react-native-qrcode-svg';

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

/** Draws the scannable QR with react-native-qrcode-svg (approved in MVP-005, L10). */
export function QrCode({ value, dimmed, size = 224 }: QrCodeProps) {
  return (
    <View
      accessibilityRole="image"
      accessibilityLabel={dimmed ? 'Código QR atenuado, ya no sirve' : 'Código QR del ticket'}
      className={dimmed
        ? 'items-center justify-center rounded-lg border-2 border-ink-300 bg-surface-0 p-3 opacity-25'
        : 'items-center justify-center rounded-lg border-2 border-ink-900 bg-surface-0 p-3'}
    >
      <QRCode value={value} size={size - 28} color="#141414" backgroundColor="#FFFFFF" quietZone={0} />
    </View>
  );
}
