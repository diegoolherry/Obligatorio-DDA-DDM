import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, AppState, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Accuracy, watchPositionAsync } from 'expo-location';
import type { LocationSubscription } from 'expo-location';
import type {
  LocationFailure, PublicacionPosicionResponse, ServicioUbicacionDto,
} from '../../contracts/location';
import { useForegroundLocationPermission } from '../../hooks/useForegroundLocationPermission';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { ILocationService } from '../../services/Location/ILocationService';
import { formatUruguayDateTime } from '../../services/Location/locationFormat';
import { Action, Notice, ServiceChooser, parts } from './LocationParts';

const defaultPeriodSeconds = 30;

type ServicesState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'empty' }
  | { status: 'ready'; services: ServicioUbicacionDto[] };

interface Sample { latitude: number; longitude: number; accuracy: number | null }
interface Publication { response: PublicacionPosicionResponse; accuracy: number | null }
interface Problem { failure: LocationFailure | 'reading'; message: string }

// After these failures sharing cannot continue by itself: the user has to act.
const stopsSharing: readonly string[] = ['unauthenticated', 'forbidden', 'notFound', 'invalid'];

function periodicityText(publication: Publication): string {
  const { cumplePeriodicidad, intervaloSegundos, periodicidadSegundos } = publication.response;
  if (cumplePeriodicidad === null) return 'Primera publicación del servicio: todavía no hay intervalo para comparar.';
  const interval = intervaloSegundos === null ? '' : ` (intervalo ${intervaloSegundos.toFixed(1)} s, periodicidad ${periodicidadSegundos} s)`;
  return cumplePeriodicidad ? `Cumple la periodicidad${interval}.` : `No cumple la periodicidad${interval}.`;
}

export function LocationSharing({ session, service }: {
  session: Pick<IAuthenticationService, 'getState' | 'getRoles'>;
  service: ILocationService;
}) {
  const authenticated = session.getState().authenticated;
  const isCollector = session.getRoles().includes('cobrador');
  const allowed = authenticated && isCollector;
  const permission = useForegroundLocationPermission();
  const [services, setServices] = useState<ServicesState>({ status: 'loading' });
  const [selectedId, setSelectedId] = useState<string | undefined>();
  const [sharing, setSharing] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [last, setLast] = useState<Publication | undefined>();
  const [problem, setProblem] = useState<Problem | undefined>();
  const [notice, setNotice] = useState<string | undefined>();
  const periodSeconds = useRef(defaultPeriodSeconds);
  const lastAttemptAt = useRef(0);
  const inFlight = useRef(false);
  const latest = useRef<Sample | undefined>(undefined);
  const mounted = useRef(true);
  const loadCounter = useRef(0);

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  const loadServices = useCallback(() => {
    const id = ++loadCounter.current;
    setServices({ status: 'loading' });
    void service.listCollectorServices().then(result => {
      if (!mounted.current || id !== loadCounter.current) return;
      if (!result.ok) setServices({ status: 'error', message: result.message });
      else setServices(result.data.length === 0 ? { status: 'empty' } : { status: 'ready', services: result.data });
    });
  }, [service]);

  useEffect(() => {
    if (!allowed) return undefined;
    loadServices();
    return () => { loadCounter.current++; };
  }, [allowed, loadServices]);

  const publish = useCallback(async (sample: Sample) => {
    if (!selectedId || inFlight.current) return;
    inFlight.current = true;
    lastAttemptAt.current = Date.now();
    setPublishing(true);
    const result = await service.publishPosition(selectedId, sample.latitude, sample.longitude);
    inFlight.current = false;
    if (!mounted.current) return;
    setPublishing(false);
    if (result.ok) {
      // The API decides the cadence: use the value of the last response.
      periodSeconds.current = result.data.periodicidadSegundos;
      setLast({ response: result.data, accuracy: sample.accuracy });
      setProblem(undefined);
    } else {
      setProblem({ failure: result.failure, message: result.message });
      if (stopsSharing.includes(result.failure)) setSharing(false);
    }
  }, [service, selectedId]);

  // The only place that opens the location subscription; cleanup removes it on stop,
  // unmount, permission loss and background.
  useEffect(() => {
    if (!sharing || permission.state !== 'granted') return undefined;
    let active = true;
    let subscription: LocationSubscription | undefined;
    watchPositionAsync(
      { accuracy: Accuracy.High, distanceInterval: 0, timeInterval: 5000 },
      position => {
        if (!active) return; // an event after cleanup must not publish
        const sample: Sample = {
          latitude: position.coords.latitude, longitude: position.coords.longitude, accuracy: position.coords.accuracy,
        };
        latest.current = sample;
        // At most one publication per period; failed attempts also wait for the next period.
        if (Date.now() - lastAttemptAt.current >= periodSeconds.current * 1000) void publish(sample);
      },
      () => { if (active) setProblem({ failure: 'reading', message: 'No se pudo leer la ubicación del teléfono.' }); },
    ).then(created => {
      if (active) subscription = created;
      else created.remove();
    }).catch(() => {
      if (!active) return;
      setProblem({ failure: 'reading', message: 'No se pudo iniciar la lectura de ubicación. Revisá que la ubicación esté activada.' });
      setSharing(false);
    });
    return () => {
      active = false;
      subscription?.remove();
    };
  }, [sharing, permission.state, publish]);

  // The screen stays mounted across tabs: stop sharing when the collector session ends.
  useEffect(() => {
    if (!allowed) setSharing(false);
  }, [allowed]);

  useEffect(() => {
    if (!sharing) return undefined;
    const subscription = AppState.addEventListener('change', next => {
      if (next === 'background') {
        setSharing(false);
        setNotice('Se detuvo el envío porque la app pasó a segundo plano. Volvé a tocar «Compartir» para continuar.');
      }
    });
    return () => subscription.remove();
  }, [sharing]);

  useEffect(() => {
    if (sharing && (permission.state === 'askable' || permission.state === 'blocked')) {
      setSharing(false);
      setNotice('Se detuvo el envío: el permiso de ubicación ya no está concedido.');
    }
  }, [sharing, permission.state]);

  async function start() {
    if (!selectedId || sharing) return;
    // Re-check right before the action: the user may have changed it in Ajustes.
    if (!(await permission.recheck())) return;
    setProblem(undefined);
    setNotice(undefined);
    setSharing(true);
  }

  function stop() {
    setSharing(false);
    setNotice('Envío detenido. La última posición enviada sigue guardada hasta que otra la reemplace.');
  }

  function retry() {
    if (publishing || inFlight.current) return;
    if (!latest.current) {
      setNotice('Todavía no hay una lectura de ubicación; esperá unos segundos.');
      return;
    }
    // Manual retry still respects the at-most-once-per-period rule.
    const remaining = Math.ceil((lastAttemptAt.current + periodSeconds.current * 1000 - Date.now()) / 1000);
    if (remaining > 0) {
      setNotice(`Se reintenta como máximo una vez cada ${periodSeconds.current} s: esperá ${remaining} s.`);
      return;
    }
    setNotice(undefined);
    void publish(latest.current);
  }

  if (!authenticated) {
    return (
      <View style={styles.container}>
        <Text style={parts.title} accessibilityRole="header">Compartir ubicación</Text>
        <Notice kind="warning">Ingresá como cobrador en la pestaña «Ingreso» para compartir la ubicación.</Notice>
      </View>
    );
  }
  if (!isCollector) {
    return (
      <View style={styles.container}>
        <Text style={parts.title} accessibilityRole="header">Compartir ubicación</Text>
        <Notice kind="warning">Esta pantalla es solo para cuentas de cobrador. Para ver la ubicación de un servicio usá «Ubicación del servicio».</Notice>
      </View>
    );
  }

  const selected = services.status === 'ready' ? services.services.find(item => item.id === selectedId) : undefined;
  const lastTime = last ? formatUruguayDateTime(last.response.recibidaUtc) : undefined;

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={parts.title} accessibilityRole="header">Compartir ubicación</Text>
      <Text style={parts.muted}>Demostración académica: se envía la posición de este teléfono como la del ómnibus del servicio elegido.</Text>

      <View style={parts.section}>
        <Text style={parts.subtitle} accessibilityRole="header">1. Elegí el servicio</Text>
        {services.status === 'loading' && (
          <View style={parts.row} accessibilityLiveRegion="polite">
            <ActivityIndicator accessibilityLabel="Cargando servicios" color="#222" />
            <Text style={parts.text}>Cargando tus servicios…</Text>
          </View>
        )}
        {services.status === 'error' && (
          <>
            <Notice kind="error">{services.message}</Notice>
            <Action label="Reintentar" secondary onPress={loadServices} />
          </>
        )}
        {services.status === 'empty' && (
          <>
            <Notice kind="info">No tenés servicios asignados.</Notice>
            <Action label="Actualizar" secondary onPress={loadServices} />
          </>
        )}
        {services.status === 'ready' && (
          <ServiceChooser services={services.services} selectedId={selectedId} disabled={sharing} onSelect={setSelectedId} />
        )}
      </View>

      <View style={parts.section}>
        <Text style={parts.subtitle} accessibilityRole="header">2. Permiso de ubicación</Text>
        {permission.state === 'checking' && (
          <View style={parts.row} accessibilityLiveRegion="polite">
            <ActivityIndicator accessibilityLabel="Comprobando permiso" color="#222" />
            <Text style={parts.text}>Comprobando el permiso de ubicación…</Text>
          </View>
        )}
        {permission.state === 'askable' && (
          <>
            {permission.denied && (
              <Notice kind="warning">Permiso denegado: sin la ubicación no se puede compartir la posición. Podés volver a intentarlo cuando quieras.</Notice>
            )}
            <Text style={parts.text}>
              Para compartir la posición necesitamos tu ubicación solo mientras la app está abierta. No se usa en segundo plano y podés detener el envío cuando quieras.
            </Text>
            <Action label="Permitir ubicación" onPress={() => { void permission.request(); }} />
          </>
        )}
        {permission.state === 'blocked' && (
          <>
            <Notice kind="warning">El permiso de ubicación está denegado y el sistema no volverá a preguntar. Habilitalo en Ajustes y volvé a la app.</Notice>
            <Action label="Abrir Ajustes" onPress={permission.openSettings} />
          </>
        )}
        {permission.state === 'granted' && <Text style={parts.text}>Permiso concedido (ubicación mientras se usa la app).</Text>}
      </View>

      <View style={parts.section}>
        <Text style={parts.subtitle} accessibilityRole="header">3. Envío</Text>
        {!sharing ? (
          <Action label="Compartir" disabled={!selected || permission.state !== 'granted'} onPress={() => { void start(); }} />
        ) : (
          <Action label="Detener" onPress={stop} />
        )}
        {!selected && permission.state === 'granted' && <Text style={parts.muted}>Elegí un servicio para poder compartir.</Text>}
        {sharing && (
          <View style={parts.row} accessibilityLiveRegion="polite">
            <ActivityIndicator accessibilityLabel="Compartiendo" color="#222" />
            <Text style={parts.text}>
              {publishing ? 'Enviando posición…'
                : `Compartiendo para ${selected ? `${selected.origen} → ${selected.destino}` : 'el servicio'}. Se envía como máximo cada ${periodSeconds.current} s.`}
            </Text>
          </View>
        )}
        {notice && <Notice kind="info">{notice}</Notice>}
        {problem && (
          <>
            <Notice kind="error">{problem.message}</Notice>
            {sharing && !stopsSharing.includes(problem.failure) && (
              <Action label="Reintentar envío" secondary disabled={publishing} onPress={retry} />
            )}
          </>
        )}
        {last && (
          <View style={parts.section} accessibilityLiveRegion="polite">
            <Text style={parts.subtitle}>Última publicación aceptada</Text>
            <Text style={parts.text}>Hora de recepción: {lastTime ?? 'no disponible'} (hora de Uruguay)</Text>
            <Text style={parts.text}>
              Precisión de la lectura: {last.accuracy === null ? 'no informada' : `±${Math.round(last.accuracy)} m`}
            </Text>
            <Text style={parts.text}>{periodicityText(last)}</Text>
          </View>
        )}
        {!last && !problem && sharing && <Text style={parts.muted}>Esperando la primera lectura de ubicación…</Text>}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, padding: 24, gap: 16 },
});
