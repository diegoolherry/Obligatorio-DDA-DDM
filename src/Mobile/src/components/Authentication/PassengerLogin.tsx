import { useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator, Keyboard, KeyboardAvoidingView, Platform, Pressable,
  ScrollView, StyleSheet, Text, TextInput, View,
} from 'react-native';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { AuthenticationResult } from '../../contracts/session';

export function PassengerLogin({ service }: { service: IAuthenticationService }) {
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [session, setSession] = useState(() => service.getState());
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('Ingresá con una cuenta ficticia.');
  const mounted = useRef(false);
  const submitting = useRef(false);
  const passwordInput = useRef<TextInput>(null);

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      service.discard();
    };
  }, [service]);

  async function run(action: () => Promise<AuthenticationResult>, progress: string) {
    if (submitting.current) return;
    submitting.current = true;
    setBusy(true);
    setMessage(progress);
    Keyboard.dismiss();
    try {
      const result = await action();
      if (mounted.current) {
        setSession(service.getState());
        setMessage(result.message);
      }
    } finally {
      submitting.current = false;
      if (mounted.current) setBusy(false);
    }
  }

  function login() {
    if (submitting.current) return;
    const request = { correo, contrasena };
    setContrasena('');
    void run(() => service.login(request), 'Ingresando…');
  }

  return (
    <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <Text style={styles.title} accessibilityRole="header">Acceso de pasajero</Text>
        <Text>Demostración académica · cuentas ficticias</Text>
        <Text>La sesión se conserva solo en memoria, sin recordar el ingreso al reabrir.</Text>
        {!session.authenticated && !session.logoutPending ? (
          <View style={styles.form}>
            <Text nativeID="correoLabel">Correo</Text>
            <TextInput
              style={styles.input} value={correo} onChangeText={setCorreo}
              accessibilityLabel="Correo" accessibilityLabelledBy="correoLabel"
              keyboardType="email-address" autoCapitalize="none" autoCorrect={false}
              autoComplete="off" textContentType="none" editable={!busy}
              returnKeyType="next" onSubmitEditing={() => passwordInput.current?.focus()}
            />
            <Text nativeID="passwordLabel">Contraseña</Text>
            <TextInput
              ref={passwordInput} style={styles.input} value={contrasena} onChangeText={setContrasena}
              accessibilityLabel="Contraseña" accessibilityLabelledBy="passwordLabel"
              secureTextEntry autoCapitalize="none" autoCorrect={false}
              autoComplete="off" textContentType="none" editable={!busy}
              returnKeyType="go" onSubmitEditing={login}
            />
            <Action label={busy ? 'Ingresando…' : 'Ingresar'} disabled={busy} onPress={login} />
          </View>
        ) : (
          <View style={styles.form}>
            <Text>{session.logoutPending ? 'Cierre remoto no confirmado.' : 'Sesión iniciada con respuesta válida de la API.'}</Text>
            <Action
              label={busy ? 'Procesando…' : session.logoutPending ? 'Reintentar cierre de sesión' : 'Cerrar sesión'}
              disabled={busy} onPress={() => { void run(() => service.logout(), 'Cerrando sesión…'); }}
            />
            {session.authenticated && (
              <Action label="Comprobar acceso de pasajero" secondary disabled={busy}
                onPress={() => { void run(() => service.checkPassenger(), 'Consultando permiso en la API…'); }} />
            )}
            {session.logoutPending && (
              <Action label="Descartar solo la sesión local" secondary disabled={busy} onPress={() => {
                service.discard();
                setSession(service.getState());
                setMessage('Memoria local borrada. No se confirmó la revocación remota; la sesión del servidor puede seguir vigente hasta vencer.');
              }} />
            )}
          </View>
        )}
        {busy && <ActivityIndicator accessibilityLabel="Solicitud en curso" color="#222" />}
        <Text accessibilityRole="text" accessibilityLiveRegion="polite">{message}</Text>
        <Text>Los permisos los verifica la API, no esta pantalla.</Text>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

function Action({ label, disabled, secondary = false, onPress }: {
  label: string; disabled: boolean; secondary?: boolean; onPress: () => void;
}) {
  return (
    <Pressable accessibilityRole="button" accessibilityState={{ disabled }} disabled={disabled} onPress={onPress}
      style={({ pressed }) => [styles.button, secondary && styles.secondary, (disabled || pressed) && styles.dimmed]}>
      <Text style={secondary ? styles.secondaryLabel : styles.buttonLabel}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  container: { flexGrow: 1, padding: 24, gap: 16, justifyContent: 'center' },
  title: { color: '#111', fontSize: 24, fontWeight: '600' },
  form: { gap: 8 },
  input: { minHeight: 48, borderWidth: 1, borderColor: '#666', borderRadius: 4, padding: 12, color: '#111', backgroundColor: '#fff' },
  button: { minHeight: 48, padding: 12, borderRadius: 4, justifyContent: 'center', alignItems: 'center', backgroundColor: '#222', marginTop: 8 },
  secondary: { backgroundColor: '#eee', borderColor: '#666', borderWidth: 1 },
  buttonLabel: { color: '#fff', fontWeight: '600' },
  secondaryLabel: { color: '#111' },
  dimmed: { opacity: 0.6 },
});
