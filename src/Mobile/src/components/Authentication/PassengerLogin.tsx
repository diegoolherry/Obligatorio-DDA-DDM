import { useEffect, useRef, useState } from 'react';
import { Keyboard, KeyboardAvoidingView, Platform, ScrollView, Text, View } from 'react-native';
import type { TextInputInstance } from 'react-native';
import { Banner } from '../ui/Banner';
import type { BannerKind } from '../ui/Banner';
import { Button } from '../ui/Button';
import { StatusBadge } from '../ui/StatusBadge';
import { TextField } from '../ui/TextField';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { AuthenticationOutcome, AuthenticationResult } from '../../contracts/session';

type Source = 'login' | 'passenger' | 'collector' | 'logout' | 'discard';

interface Feedback {
  source: Source;
  outcome: AuthenticationOutcome;
  kind: BannerKind;
  message: string;
}

const retryable: readonly AuthenticationOutcome[] = ['network', 'server', 'invalidResponse', 'cancelled'];

function kindFor(outcome: AuthenticationOutcome): BannerKind {
  switch (outcome) {
    case 'success': return 'success';
    case 'configuration': case 'denied': case 'busy': case 'unauthenticated': return 'warning';
    default: return 'error';
  }
}

const rejectedTitle = 'No pudimos iniciar tu sesión';
const rejectedMessage = 'Correo o contraseña incorrectos. Revisalos e intentá de nuevo.';
const passwordError = 'Escribí tu contraseña de nuevo.';
const discardMessage = 'Token descartado en este dispositivo. La API no revoca JWT; cualquier copia seguirá válida hasta que expire.';
const logoutSuccessMessage = 'Token descartado en este dispositivo. La API no mantiene sesiones ni revoca JWT; cualquier copia seguirá válida hasta que expire.';
const logoutUnauthenticatedMessage = 'La API rechazó el token y se descartó la copia de este dispositivo.';
const logoutPendingMessage = 'No llegó una confirmación de la API. El token permanece en memoria y no hay una sesión de servidor que revocar. Reintentá o descartá el token local.';
const logoutRetryMessage = 'No se recibió confirmación de la API. El token sigue en memoria y puede usarse en este dispositivo; reintentá o descartalo localmente.';

export function PassengerLogin({ service, onSessionChanged }: {
  service: IAuthenticationService; onSessionChanged: () => void;
}) {
  const [correo, setCorreo] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [session, setSession] = useState(() => service.getState());
  const [busy, setBusy] = useState(false);
  const [progress, setProgress] = useState('');
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const mounted = useRef(false);
  const submitting = useRef(false);
  const focusPassword = useRef(false);
  const passwordInput = useRef<TextInputInstance>(null);

  useEffect(() => {
    mounted.current = true;
    // The session belongs to the app, not to this tab: App.tsx discards it on unmount.
    return () => {
      mounted.current = false;
    };
  }, [service]);

  // The password field is disabled while busy, so focus moves only after the request ends.
  useEffect(() => {
    if (!busy && focusPassword.current) {
      focusPassword.current = false;
      passwordInput.current?.focus();
    }
  }, [busy, feedback]);

  async function run(source: Source, action: () => Promise<AuthenticationResult>, inProgress: string) {
    if (submitting.current) return;
    submitting.current = true;
    setBusy(true);
    setProgress(inProgress);
    setFeedback(null);
    Keyboard.dismiss();
    try {
      const result = await action();
      if ((source === 'login' && result.outcome === 'success')
        || (source === 'logout' && (result.outcome === 'success' || result.outcome === 'unauthenticated'))) {
        onSessionChanged();
      }
      if (mounted.current) {
        setSession(service.getState());
        if (source === 'login' && result.outcome === 'success') setFeedback(null);
        else setFeedback({
          source,
          outcome: result.outcome,
          kind: kindFor(result.outcome),
          message: source === 'logout' && result.outcome === 'success' ? logoutSuccessMessage
            : source === 'logout' && result.outcome === 'unauthenticated' ? logoutUnauthenticatedMessage
              : source === 'logout' ? logoutRetryMessage
              : result.message,
        });
        if (source === 'login' && result.outcome === 'rejected') focusPassword.current = true;
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
    void run('login', () => service.login(request), 'Ingresando…');
  }

  function retryLogin() {
    if (contrasena.length > 0) login();
    else passwordInput.current?.focus();
  }

  function checkPassenger() {
    void run('passenger', () => service.checkPassenger(), 'Consultando permiso en la API…');
  }

  function checkCollector() {
    void run('collector', () => service.checkCollector(), 'Consultando permiso en la API…');
  }

  // Editing a field dismisses the errors that point at the credentials.
  function clearCredentialError() {
    setFeedback(current => (current !== null && current.source === 'login'
      && (current.outcome === 'rejected' || current.outcome === 'validation') ? null : current));
  }

  function renderFeedback() {
    if (feedback === null) return null;
    const { source, outcome, kind, message } = feedback;
    if (source === 'login' && outcome === 'rejected') return <Banner kind="error" title={rejectedTitle}>{rejectedMessage}</Banner>;
    if (source === 'login' && outcome === 'validation') return <Banner kind="error" title="Revisá los datos ingresados">{message}</Banner>;
    if (retryable.includes(outcome) && source !== 'logout' && source !== 'discard') {
      const onAction = source === 'login' ? retryLogin : source === 'passenger' ? checkPassenger : checkCollector;
      return <Banner kind={kind} actionLabel="Reintentar" onAction={onAction}>{message}</Banner>;
    }
    return <Banner kind={kind}>{message}</Banner>;
  }

  const signedIn = session.authenticated || session.logoutPending;
  const rejected = feedback !== null && feedback.source === 'login' && feedback.outcome === 'rejected';

  return (
    <KeyboardAvoidingView className="flex-1" behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
      <ScrollView contentContainerClassName="flex-grow gap-4 p-6" keyboardShouldPersistTaps="handled">
        <View className="gap-1">
          <Text className="text-3xl font-bold text-ink-900" accessibilityRole="header">{signedIn ? 'Tu sesión' : 'Ingresá a tu cuenta'}</Text>
          {!signedIn && <Text className="text-base text-ink-500">Usá el correo con el que te registraste.</Text>}
        </View>
        {!signedIn ? (
          <View className="gap-2">
            {renderFeedback()}
            <TextField
              label="Correo" labelId="correoLabel" value={correo}
              onChangeText={value => { setCorreo(value); clearCredentialError(); }}
              keyboardType="email-address" autoCapitalize="none" autoCorrect={false}
              autoComplete="off" textContentType="none" disabled={busy}
              returnKeyType="next" onSubmitEditing={() => passwordInput.current?.focus()}
            />
            <TextField
              ref={passwordInput} label="Contraseña" labelId="passwordLabel" value={contrasena}
              onChangeText={value => { setContrasena(value); clearCredentialError(); }}
              error={rejected ? passwordError : undefined}
              secureTextEntry autoCapitalize="none" autoCorrect={false}
              autoComplete="off" textContentType="none" disabled={busy}
              returnKeyType="go" onSubmitEditing={login}
            />
            <Button label={busy ? 'Ingresando…' : 'Ingresar'} loading={busy} onPress={login} />
          </View>
        ) : (
          <View className="gap-4">
            <View className="gap-2 rounded-lg border border-border-200 bg-surface-0 p-4">
              {session.logoutPending
                ? <StatusBadge kind="warning" label="Salida de API no confirmada" />
                : <StatusBadge kind="success" label="Sesión iniciada" />}
              <Text className="text-base text-ink-700">
                {session.logoutPending
                  ? logoutPendingMessage
                  : 'Podés verificar tus permisos o descartar el token de este dispositivo cuando termines.'}
              </Text>
            </View>
            {renderFeedback()}
            {busy && <Text className="text-base text-ink-500" accessibilityLiveRegion="polite">{progress}</Text>}
            {session.authenticated && (
              <View className="gap-1">
                <Text className="text-lg font-semibold text-ink-900" accessibilityRole="header">Verificar permisos</Text>
                <Button label="Comprobar acceso de pasajero" variant="secondary" disabled={busy} onPress={checkPassenger} />
                <Button label="Comprobar acceso de cobrador" variant="secondary" disabled={busy} onPress={checkCollector} />
              </View>
            )}
            <View className="gap-1">
              <Button
                label={busy ? 'Procesando…' : session.logoutPending ? 'Reintentar salida de API' : 'Solicitar salida a la API'}
                variant="destructive" loading={busy} onPress={() => { void run('logout', () => service.logout(), 'Cerrando sesión…'); }}
              />
              {session.logoutPending && (
                <Button label="Descartar token local" variant="secondary" disabled={busy} onPress={() => {
                  service.discard();
                  onSessionChanged();
                  setSession(service.getState());
                  setFeedback({ source: 'discard', outcome: 'denied', kind: 'warning', message: discardMessage });
                }} />
              )}
            </View>
          </View>
        )}
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
