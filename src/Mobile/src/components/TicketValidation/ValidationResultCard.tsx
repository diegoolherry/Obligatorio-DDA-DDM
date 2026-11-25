import { Text, View } from 'react-native';
import type { ResultTone, ResultView } from '../../services/TicketValidation/ticketValidationFormat';

// Complete literal class strings per tone: NativeWind cannot see names built at runtime.
const box: Record<ResultTone, string> = {
  success: 'items-center gap-2 rounded-xl border-2 border-success-700 bg-success-100 p-6',
  error: 'items-center gap-2 rounded-xl border-2 border-error-700 bg-error-100 p-6',
  warning: 'items-center gap-2 rounded-xl border-2 border-warning-800 bg-warning-100 p-6',
};
const glyphText: Record<ResultTone, string> = {
  success: 'text-6xl font-extrabold text-success-700',
  error: 'text-6xl font-extrabold text-error-700',
  warning: 'text-6xl font-extrabold text-warning-800',
};
const titleText: Record<ResultTone, string> = {
  success: 'text-center text-3xl font-extrabold text-success-700',
  error: 'text-center text-3xl font-extrabold text-error-700',
  warning: 'text-center text-3xl font-extrabold text-warning-800',
};
const messageText: Record<ResultTone, string> = {
  success: 'text-center text-lg text-success-700',
  error: 'text-center text-lg text-error-700',
  warning: 'text-center text-lg text-warning-800',
};

/** Result of a scan: icon + title + message + data rows. The words carry the meaning, not only the color. */
export function ValidationResultCard({ view }: { view: ResultView }) {
  return (
    <View className="gap-4">
      <View className={box[view.tone]} accessibilityLiveRegion="assertive" accessible accessibilityLabel={`${view.title}. ${view.message}`}>
        <Text className={glyphText[view.tone]} accessibilityElementsHidden importantForAccessibility="no">{view.glyph}</Text>
        <Text accessibilityRole="header" className={titleText[view.tone]}>{view.title}</Text>
        <Text className={messageText[view.tone]}>{view.message}</Text>
      </View>
      <View className="gap-3 rounded-xl border border-border-200 bg-surface-0 p-4">
        {view.rows.map(row => (
          <View key={row.label}>
            <Text className="text-sm font-semibold text-ink-500">{row.label}</Text>
            <Text className="text-lg font-bold text-ink-900">{row.value}</Text>
          </View>
        ))}
      </View>
    </View>
  );
}
