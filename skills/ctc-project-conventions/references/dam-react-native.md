# DAM: React Native, TypeScript, and Expo

## Platform rules

- Use the repository's Expo setup and verify the configured SDK. Class setup uses `create-expo-app`, `npx expo start`, and Expo Go on a device sharing the development network.
- Use `View`, `Text`, `Image`, `TextInput`, `Pressable`, `ScrollView`, and `FlatList`; do not use HTML tags.
- Keep visible strings inside `Text`. Give images non-zero dimensions.
- Use `StyleSheet.create`, camelCase style properties, numeric density-independent values, and Flexbox. React Native defaults to column direction.
- Use `ScrollView` for short static content and `FlatList` for collections that may grow, with a stable domain key.

## React and TypeScript

- Components render UI; props are read-only inputs; state belongs to the component that changes it.
- Define interfaces/types for props, API data, and domain values. Use `?` only when absence is valid. Never replace uncertainty with `any`.
- Update state through its setter and create a new array/object; never mutate existing state.
- Prefer props for shallow relationships. Use Context for real cross-cutting state such as authentication or theme, not as a default store.
- Extract reusable stateful logic into a `use...` hook. Hooks return data/actions and do not render UI.
- Use `useEffect` only to synchronize with HTTP, timers, subscriptions, or device APIs. Derive computable values during render.

## Data and forms

- Represent remote work with loading, empty, error, success, and retry states.
- Keep HTTP/business operations in an action or service, not in a large screen component.
- Controlled inputs use `value` and `onChangeText`. For a complex form, a typed form library is acceptable only if the project already uses it or the team approves the dependency.
- Preserve entered values after recoverable validation or network errors.

See `../assets/react-native-async-list-pattern.tsx` for the state shape and list boundary.

## Review checks

- No direct state mutation, unstable index keys, unexplained `any`, or monolithic `App.tsx`.
- A list item is a small typed component when it has meaningful presentation or interaction.
- Effects have correct dependencies and perform external synchronization.
- The first relevant terminal or device error is investigated before clearing cache; use a clean Expo cache only when cache is a plausible cause.

## Source basis

Derived from the supplied React Native Essentials, Expo Essentials, Mobile Blueprint, Universal React Blueprint, and React + TypeScript construction manual.

