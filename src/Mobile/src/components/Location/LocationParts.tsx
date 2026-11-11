import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { ServicioUbicacionDto } from '../../contracts/location';
import { localHour } from '../../services/Location/locationFormat';

export function Action({ label, disabled = false, secondary = false, onPress }: {
  label: string; disabled?: boolean; secondary?: boolean; onPress: () => void;
}) {
  return (
    <Pressable accessibilityRole="button" accessibilityLabel={label} accessibilityState={{ disabled }}
      disabled={disabled} onPress={onPress}
      style={({ pressed }) => [parts.button, secondary && parts.secondary, (disabled || pressed) && parts.dimmed]}>
      <Text style={secondary ? parts.secondaryLabel : parts.buttonLabel}>{label}</Text>
    </Pressable>
  );
}

/** Message box; the prefix keeps the meaning without relying on color. */
export function Notice({ kind, children }: { kind: 'error' | 'warning' | 'info'; children: string }) {
  const prefix = kind === 'error' ? 'Error: ' : kind === 'warning' ? 'Atención: ' : '';
  return (
    <View style={[parts.notice, kind === 'error' && parts.noticeError, kind === 'warning' && parts.noticeWarning]}
      accessibilityLiveRegion="polite">
      <Text style={parts.noticeText}>{prefix}{children}</Text>
    </View>
  );
}

export function ServiceChooser({ services, selectedId, disabled = false, onSelect }: {
  services: ServicioUbicacionDto[]; selectedId: string | undefined; disabled?: boolean; onSelect: (id: string) => void;
}) {
  return (
    <View style={parts.list} accessibilityRole="radiogroup">
      {services.map(item => {
        const selected = item.id === selectedId;
        const label = `${item.origen} a ${item.destino}, sale ${localHour(item.salida)}, llega ${localHour(item.llegada)}`;
        return (
          <Pressable key={item.id} accessibilityRole="radio" accessibilityLabel={label}
            accessibilityState={{ selected, disabled }} disabled={disabled} onPress={() => onSelect(item.id)}
            style={({ pressed }) => [parts.option, selected && parts.optionSelected, (disabled || pressed) && parts.dimmed]}>
            <Text style={[parts.optionTitle, selected && parts.optionTitleSelected]}>
              {selected ? '● ' : '○ '}{item.origen} → {item.destino}
            </Text>
            <Text style={[parts.optionDetail, selected && parts.optionTitleSelected]}>
              Sale {localHour(item.salida)} · llega {localHour(item.llegada)} (hora de Uruguay)
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

export const parts = StyleSheet.create({
  title: { color: '#111', fontSize: 24, fontWeight: '600' },
  subtitle: { color: '#111', fontSize: 18, fontWeight: '600' },
  text: { color: '#111' },
  muted: { color: '#444' },
  section: { gap: 8 },
  row: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  button: { minHeight: 48, padding: 12, borderRadius: 4, justifyContent: 'center', alignItems: 'center', backgroundColor: '#222', marginTop: 8 },
  secondary: { backgroundColor: '#eee', borderColor: '#666', borderWidth: 1 },
  buttonLabel: { color: '#fff', fontWeight: '600' },
  secondaryLabel: { color: '#111', fontWeight: '600' },
  dimmed: { opacity: 0.6 },
  notice: { borderWidth: 1, borderColor: '#666', borderRadius: 4, padding: 12, backgroundColor: '#f5f5f5' },
  noticeError: { borderColor: '#8a1c1c', borderWidth: 2, backgroundColor: '#fdeeee' },
  noticeWarning: { borderColor: '#7a5200', borderWidth: 2, backgroundColor: '#fff6e0' },
  noticeText: { color: '#111' },
  list: { gap: 8 },
  option: { minHeight: 48, padding: 12, borderWidth: 1, borderColor: '#666', borderRadius: 4, backgroundColor: '#fff', gap: 2 },
  optionSelected: { backgroundColor: '#222' },
  optionTitle: { color: '#111', fontWeight: '600' },
  optionTitleSelected: { color: '#fff' },
  optionDetail: { color: '#444' },
});
