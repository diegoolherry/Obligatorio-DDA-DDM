import { useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator, Keyboard, KeyboardAvoidingView, Platform, ScrollView, Text, View,
} from 'react-native';
import type { TextInputInstance } from 'react-native';
import { Button } from '../ui/Button';
import { TextField } from '../ui/TextField';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { AuthenticationResult } from '../../contracts/session';

export function PassengerLogin({ service }: { service: IAuthenticationService }) {
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [session, setSession] = useState(() => service.getState());
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('Ingresá con tu cuenta.');
  const mounted = useRef(false);
  const submitting = useRef(false);
  const passwordInput = useRef<TextInputInstance>(null);

  useEffect(() => {
    mounted.current = true;
    // The session belongs to the app, not to this tab: App.tsx discards it on unmount.
    return () => {
      mounted.current = false;
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
    <KeyboardAvoidingView className="flex-1" behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
      <ScrollView contentContainerClassName="flex-grow justify-center gap-4 p-6" keyboardShouldPersistTaps="handled">
        <Text className="text-2xl font-bold text-ink-900" accessibilityRole="header">Acceso de pasajero y cobrador</Text>
        <Text className="text-base text-ink-500">La sesión se conserva solo en memoria, sin recordar el ingreso al reabrir.</Text>
        {!session.authenticated && !session.logoutPending ? (
          <View className="gap-2">
            <TextField
              label="Correo" labelId="correoLabel" value={correo} onChangeText={setCorreo}
              keyboardType="email-address" autoCapitalize="none" autoCorrect={false}
              autoComplete="off" textContentType="none" disabled={busy}
              returnKeyType="next" onSubmitEditing={() => passwordInput.current?.focus()}
            />
            <TextField
              ref={passwordInput} label="Contraseña" labelId="passwordLabel" value={contrasena} onChangeText={setContrasena}
              secureTextEntry autoCapitalize="none" autoCorrect={false}
              autoComplete="off" textContentType="none" disabled={busy}
              returnKeyType="go" onSubmitEditing={login}
            />
            <Button label={busy ? 'Ingresando…' : 'Ingresar'} loading={busy} onPress={login} />
          </View>
        ) : (
          <View className="gap-2">
            <Text className="text-base text-ink-900">{session.logoutPending ? 'Cierre remoto no confirmado.' : 'Sesión iniciada con respuesta válida de la API.'}</Text>
            <Button
              label={busy ? 'Procesando…' : session.logoutPending ? 'Reintentar cierre de sesión' : 'Cerrar sesión'}
              variant="destructive" loading={busy} onPress={() => { void run(() => service.logout(), 'Cerrando sesión…'); }}
            />
            {session.authenticated && (
              <Button label="Comprobar acceso de pasajero" variant="secondary" disabled={busy}
                onPress={() => { void run(() => service.checkPassenger(), 'Consultando permiso en la API…'); }} />
            )}
            {session.authenticated && (
              <Button label="Comprobar acceso de cobrador" variant="secondary" disabled={busy}
                onPress={() => { void run(() => service.checkCollector(), 'Consultando permiso en la API…'); }} />
            )}
            {session.logoutPending && (
              <Button label="Descartar solo la sesión local" variant="secondary" disabled={busy} onPress={() => {
                service.discard();
                setSession(service.getState());
                setMessage('Memoria local borrada. No se confirmó la revocación remota; la sesión del servidor puede seguir vigente hasta vencer.');
              }} />
            )}
          </View>
        )}
        {busy && <ActivityIndicator accessibilityLabel="Solicitud en curso" color="#141414" />}
        <Text className="text-base font-semibold text-ink-900" accessibilityRole="text" accessibilityLiveRegion="polite">{message}</Text>
        <Text className="text-sm text-ink-500">Los permisos los verifica la API, no esta pantalla.</Text>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
