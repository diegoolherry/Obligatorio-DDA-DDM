import { Pressable, Text, View } from 'react-native';
import type { TicketPasajeroDto } from '../../contracts/tickets';
import {
  cardAccessibilityLabel, formatDate, formatTime, statusBadge,
} from '../../services/Tickets/ticketFormat';
import type { TicketBadgeKind } from '../../services/Tickets/ticketFormat';

// Complete literal class strings: NativeWind cannot see names built at runtime.
const badgeBox: Record<TicketBadgeKind, string> = {
  success: 'flex-row items-center gap-1 self-start rounded-full bg-success-100 px-3 py-1',
  neutral: 'flex-row items-center gap-1 self-start rounded-full bg-surface-100 px-3 py-1',
  error: 'flex-row items-center gap-1 self-start rounded-full bg-error-100 px-3 py-1',
};
const badgeText: Record<TicketBadgeKind, string> = {
  success: 'text-sm font-semibold text-success-700',
  neutral: 'text-sm font-semibold text-ink-700',
  error: 'text-sm font-semibold text-error-700',
};

/** Icon plus word: the state is never conveyed by color alone. */
export function TicketBadge({ estado }: { estado: TicketPasajeroDto['estado'] }) {
  const badge = statusBadge(estado);
  return (
    <View className={badgeBox[badge.kind]}>
      <Text accessibilityElementsHidden importantForAccessibility="no" className={badgeText[badge.kind]}>{badge.icon}</Text>
      <Text className={badgeText[badge.kind]}>{badge.label}</Text>
    </View>
  );
}

export function TicketCard({ ticket, onOpen }: { ticket: TicketPasajeroDto; onOpen: (ticketId: string) => void }) {
  const action = ticket.estado === 'sinUsar' ? 'Mostrar QR' : 'Ver detalle';
  return (
    <Pressable
      accessibilityRole="button" accessibilityLabel={`${cardAccessibilityLabel(ticket)}. ${action}`}
      onPress={() => onOpen(ticket.ticketId)}
      className="gap-3 rounded-xl border border-border-200 bg-surface-0 p-4 active:bg-surface-100"
    >
      <View className="flex-row items-start justify-between gap-3">
        <View className="flex-1">
          <Text className="text-xl font-bold text-ink-900">{ticket.origenNombre}</Text>
          <Text className="text-xl font-bold text-ink-900">↓ {ticket.destinoNombre}</Text>
        </View>
        <TicketBadge estado={ticket.estado} />
      </View>
      <View className="h-px bg-border-200" />
      <View className="flex-row gap-6">
        <View>
          <Text className="text-sm font-semibold text-ink-500">Servicio</Text>
          <Text className="text-lg font-bold text-ink-900">{formatTime(ticket.salida)}</Text>
        </View>
        <View>
          <Text className="text-sm font-semibold text-ink-500">Fecha</Text>
          <Text className="text-lg font-bold text-ink-900">{formatDate(ticket.salida)}</Text>
        </View>
      </View>
      <View className="min-h-12 flex-row items-center justify-between">
        <Text className="text-lg font-bold text-primary-700">{action}</Text>
        <Text className="text-2xl text-primary-700">›</Text>
      </View>
    </Pressable>
  );
}
