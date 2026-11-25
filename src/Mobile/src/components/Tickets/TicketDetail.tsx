import { ScrollView, Text, View } from 'react-native';
import type { TicketPasajeroDto } from '../../contracts/tickets';
import {
  detailPresentation, formatDate, formatTime, routeTitle,
} from '../../services/Tickets/ticketFormat';
import { Button } from '../ui/Button';
import { QrCode } from './QrCode';
import { TicketBadge } from './TicketCard';

export function TicketDetail({ ticket, onBack }: { ticket: TicketPasajeroDto; onBack: () => void }) {
  const { qr, notice } = detailPresentation(ticket);
  return (
    <ScrollView contentContainerClassName="gap-4 p-4">
      <View className="flex-row items-start justify-between gap-3">
        <Text accessibilityRole="header" className="flex-1 text-2xl font-bold text-ink-900">{routeTitle(ticket)}</Text>
        <TicketBadge estado={ticket.estado} />
      </View>
      <View className="gap-3 rounded-xl border border-border-200 bg-surface-0 p-4">
        <View className="items-center">
          <QrCode value={ticket.qrOpaco} dimmed={qr === 'dimmed'} />
        </View>
        {qr === 'active'
          ? <Text className="text-center text-base text-ink-700">{notice}</Text>
          : (
            <View className="rounded-lg bg-surface-100 p-3" accessibilityLiveRegion="polite">
              <Text className="text-base text-ink-700">{notice}</Text>
            </View>
          )}
        <View className="flex-row gap-6">
          <View className="flex-1">
            <Text className="text-sm font-semibold text-ink-500">Servicio</Text>
            <Text className="text-lg font-bold text-ink-900">
              {formatTime(ticket.salida)} · {ticket.origenNombre} – {ticket.destinoNombre}
            </Text>
          </View>
          <View className="flex-1">
            <Text className="text-sm font-semibold text-ink-500">Fecha</Text>
            <Text className="text-lg font-bold text-ink-900">{formatDate(ticket.salida)}</Text>
          </View>
        </View>
        {ticket.numeroAsiento !== null && (
          <View>
            <Text className="text-sm font-semibold text-ink-500">Asiento</Text>
            <Text className="text-lg font-bold text-ink-900">{ticket.numeroAsiento}</Text>
          </View>
        )}
      </View>
      <Button label="Volver a mis tickets" variant="secondary" onPress={onBack} />
    </ScrollView>
  );
}
