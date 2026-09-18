import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

type Item = {
  id: string;
  label: string;
};

type Props = {
  items: Item[];
  isLoading: boolean;
  errorMessage?: string;
  onRetry: () => void;
};

export function AsyncListScreen({ items, isLoading, errorMessage, onRetry }: Props) {
  if (isLoading) {
    return <StateMessage message="Loading…" />;
  }

  if (errorMessage) {
    return (
      <View style={styles.stateContainer}>
        <Text accessibilityRole="alert">{errorMessage}</Text>
        <Pressable accessibilityRole="button" onPress={onRetry} style={styles.button}>
          <Text style={styles.buttonText}>Retry</Text>
        </Pressable>
      </View>
    );
  }

  if (items.length === 0) {
    return <StateMessage message="No results found." />;
  }

  return (
    <FlatList
      data={items}
      keyExtractor={(item) => item.id}
      renderItem={({ item }) => <Text style={styles.item}>{item.label}</Text>}
      contentContainerStyle={styles.list}
    />
  );
}

function StateMessage({ message }: { message: string }) {
  return (
    <View style={styles.stateContainer}>
      <Text>{message}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  list: { padding: 16, gap: 8 },
  item: { padding: 16 },
  stateContainer: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: 16 },
  button: { minHeight: 48, minWidth: 96, padding: 12, justifyContent: 'center' },
  buttonText: { textAlign: 'center' },
});
