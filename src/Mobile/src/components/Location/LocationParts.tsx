import { Pressable, Text, View } from 'react-native';
import type { ServicioUbicacionDto } from '../../contracts/location';
import { localHour } from '../../services/Location/locationFormat';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';

export function Action({ label, disabled = false, secondary = false, onPress }: {
  label: string; disabled?: boolean; secondary?: boolean; onPress: () => void;
}) {
  return <Button label={label} variant={secondary ? 'secondary' : 'primary'} disabled={disabled} onPress={onPress} />;
}

/** Message box; the banner prefix keeps the meaning without relying on color. */
export function Notice({ kind, children }: { kind: 'error' | 'warning' | 'info'; children: string }) {
  return <Banner kind={kind}>{children}</Banner>;
}

export function ServiceChooser({ services, selectedId, disabled = false, onSelect }: {
  services: ServicioUbicacionDto[]; selectedId: string | undefined; disabled?: boolean; onSelect: (id: string) => void;
}) {
  return (
    <View className="gap-2" accessibilityRole="radiogroup">
      {services.map(item => {
        const selected = item.id === selectedId;
        const label = `${item.origen} a ${item.destino}, sale ${localHour(item.salida)}, llega ${localHour(item.llegada)}`;
        return (
          <Pressable key={item.id} accessibilityRole="radio" accessibilityLabel={label}
            accessibilityState={{ selected, disabled }} disabled={disabled} onPress={() => onSelect(item.id)}
            className={selected
              ? 'min-h-12 gap-1 rounded-md border-2 border-ink-900 bg-ink-900 p-3 active:opacity-80'
              : 'min-h-12 gap-1 rounded-md border-2 border-ink-300 bg-surface-0 p-3 active:bg-surface-100'}>
            <Text className={selected ? 'text-base font-semibold text-surface-0' : 'text-base font-semibold text-ink-900'}>
              {selected ? '● ' : '○ '}{item.origen} → {item.destino}
            </Text>
            <Text className={selected ? 'text-base text-surface-0' : 'text-base text-ink-500'}>
              Sale {localHour(item.salida)} · llega {localHour(item.llegada)} (hora de Uruguay)
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

/** Complete class strings shared by the location screens (NativeWind needs them literal). */
export const parts = {
  title: 'text-2xl font-bold text-ink-900',
  subtitle: 'text-lg font-bold text-ink-900',
  text: 'text-base text-ink-900',
  muted: 'text-base text-ink-500',
  section: 'gap-2',
  row: 'min-h-12 flex-row items-center gap-2',
  container: 'flex-grow gap-4 p-6',
} as const;
