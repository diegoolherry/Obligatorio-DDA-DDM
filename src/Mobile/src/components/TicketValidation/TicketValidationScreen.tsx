import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from 'react';
import { ActivityIndicator, AppState, Linking, ScrollView, Text, View } from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import type { PermissionResponse } from 'expo-camera';
import type { ServicioUbicacionDto } from '../../contracts/location';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { ILocationService } from '../../services/Location/ILocationService';
import { formatUruguayDateTime } from '../../services/Location/locationFormat';
import { TicketValidationFlow } from '../../services/TicketValidation/TicketValidationFlow';
import type { CameraPermissionInfo } from '../../services/TicketValidation/TicketValidationFlow';
import type { ITicketValidationService } from '../../services/TicketValidation/ITicketValidationService';
import {
  canRetryFailure, describeResult, describeUnconfirmed,
} from '../../services/TicketValidation/ticketValidationFormat';
import type { ChosenService } from '../../services/TicketValidation/ticketValidationFormat';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { ServiceChooser } from '../Location/LocationParts';
import { ValidationResultCard } from './ValidationResultCard';

type ServicesState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'empty' }
  | { status: 'ready'; services: ServicioUbicacionDto[] };

function toInfo(permission: PermissionResponse): CameraPermissionInfo {
  return {
    status: permission.granted ? 'granted' : String(permission.status) === 'undetermined' ? 'undetermined' : 'denied',
    canAskAgain: permission.canAskAgain,
  };
}
const deniedInfo: CameraPermissionInfo = { status: 'denied', canAskAgain: false };

export function TicketValidationScreen({ session, service, locations, active }: {
  session: Pick<IAuthenticationService, 'getState' | 'getRoles'>;
  service: ITicketValidationService;
  /** Source of the collector's assigned services, the same one «Compartir ubicación» uses. */
  locations: Pick<ILocationService, 'listCollectorServices'>;
  /** False while another tab is shown: the camera is released but the flow (and any answer in flight) is kept. */
  active: boolean;
}) {
  const authenticated = session.getState().authenticated;
  const isCollector = session.getRoles().includes('cobrador');
  const allowed = authenticated && isCollector;
  const [flow] = useState(() => new TicketValidationFlow(service));
  const subscribe = useCallback((onChange: () => void) => flow.subscribe(() => onChange()), [flow]);
  const state = useSyncExternalStore(subscribe, () => flow.state);
  const [, requestPermission, getPermission] = useCameraPermissions();
  const [services, setServices] = useState<ServicesState>({ status: 'loading' });
  const busy = useRef(false);
  const mounted = useRef(true);
  const loadCounter = useRef(0);

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  const loadServices = useCallback(() => {
    const id = ++loadCounter.current;
    setServices({ status: 'loading' });
    void locations.listCollectorServices().then(result => {
      if (!mounted.current || id !== loadCounter.current) return;
      if (!result.ok) setServices({ status: 'error', message: result.message });
      else setServices(result.data.length === 0 ? { status: 'empty' } : { status: 'ready', services: result.data });
    });
  }, [locations]);

  useEffect(() => {
    if (!allowed) {
      flow.reset(); // the session ended: nothing from the previous collector stays on screen
      return undefined;
    }
    loadServices();
    return () => { loadCounter.current++; };
  }, [allowed, flow, loadServices]);

  // The permission may be changed in Ajustes: read it again when the app comes back to the foreground.
  useEffect(() => {
    if (state.kind !== 'permission-denied') return undefined;
    const subscription = AppState.addEventListener('change', next => {
      if (next !== 'active') return;
      getPermission().then(permission => {
        if (permission.granted) flow.permissionAnswered(toInfo(permission));
      }).catch(() => undefined);
    });
    return () => subscription.remove();
  }, [state.kind, getPermission, flow]);

  /** Runs one permission action at a time; a failing native call counts as «denied», never as granted. */
  async function withPermission(action: () => Promise<CameraPermissionInfo>, apply: (info: CameraPermissionInfo) => void) {
    if (busy.current) return;
    busy.current = true;
    try {
      apply(await action());
    } catch {
      apply(deniedInfo);
    } finally {
      busy.current = false;
    }
  }
  const readPermission = async () => toInfo(await getPermission());
  const askPermission = async () => toInfo(await requestPermission());
  const retryPermission = async () => {
    const current = await getPermission();
    if (current.granted || !current.canAskAgain) return toInfo(current);
    return toInfo(await requestPermission());
  };

  if (!allowed) {
    return (
      <View className="gap-3 p-4">
        <Text accessibilityRole="header" className="text-2xl font-bold text-ink-900">Validar ticket</Text>
        <Banner kind="warning">
          {!authenticated ? 'Ingresá como cobrador en la pestaña «Ingreso» para validar tickets.'
            : 'Esta pantalla es solo para cuentas de cobrador.'}
        </Banner>
      </View>
    );
  }

  const chosenService = services.status === 'ready' && state.servicioId !== undefined
    ? services.services.find(item => item.id === state.servicioId) : undefined;
  const chosen: ChosenService = chosenService ?? { origen: 'Tramo', destino: 'elegido', salida: '' };

  return (
    <ScrollView contentContainerClassName="gap-4 p-4">
      {state.kind === 'selecting' && (
        <>
          <View>
            <Text accessibilityRole="header" className="text-3xl font-bold text-ink-900">Validar ticket</Text>
            <Text className="text-base text-ink-500">Elegí el tramo del servicio actual.</Text>
          </View>
          {services.status === 'loading' && (
            <View className="min-h-12 flex-row items-center gap-2" accessibilityLiveRegion="polite">
              <ActivityIndicator accessibilityLabel="Cargando servicios" color="#141414" />
              <Text className="text-base text-ink-900">Cargando tus servicios…</Text>
            </View>
          )}
          {services.status === 'error' && (
            <Banner kind="error" actionLabel="Reintentar" onAction={loadServices}>{services.message}</Banner>
          )}
          {services.status === 'empty' && (
            <Banner kind="info" actionLabel="Actualizar" onAction={loadServices}>No tenés servicios asignados, así que no hay tramos para validar.</Banner>
          )}
          {services.status === 'ready' && (
            <>
              <ServiceChooser services={services.services} selectedId={state.servicioId} onSelect={id => flow.selectService(id)} />
              <Text className="text-base text-ink-500">Solo se aceptan tickets de este tramo.</Text>
              <Button label="Escanear" disabled={state.servicioId === undefined}
                onPress={() => { void withPermission(readPermission, info => flow.requestScan(info)); }} />
            </>
          )}
        </>
      )}

      {state.kind !== 'selecting' && (
        <View className="rounded-xl border border-border-200 bg-surface-0 p-3" accessible
          accessibilityLabel={`Tramo: ${chosen.origen} a ${chosen.destino}`}>
          <Text className="text-lg font-bold text-ink-900">{chosen.origen} a {chosen.destino}</Text>
          <Text className="text-base text-ink-500">Solo se aceptan tickets de este tramo.</Text>
        </View>
      )}

      {state.kind === 'permission-explain' && (
        <View className="items-center gap-3 rounded-xl border border-border-200 bg-surface-0 p-6">
          <Text className="text-5xl text-info-700" accessibilityElementsHidden importantForAccessibility="no">◉</Text>
          <Text accessibilityRole="header" className="text-center text-2xl font-bold text-ink-900">Para escanear, permití la cámara</Text>
          <Text className="text-center text-lg text-ink-500">Se usa solo para leer el código QR del ticket. No se guardan fotos.</Text>
          <Button label="Continuar" onPress={() => { void withPermission(askPermission, info => flow.permissionAnswered(info)); }} />
          <Text className="text-center text-base text-ink-500">Después, el teléfono te va a pedir confirmación.</Text>
          <Button label="Cancelar" variant="secondary" onPress={() => flow.cancel()} />
        </View>
      )}

      {state.kind === 'permission-denied' && (
        <View className="items-center gap-3 rounded-xl border border-border-200 bg-surface-0 p-6">
          <Text className="text-5xl text-error-700" accessibilityElementsHidden importantForAccessibility="no">⊘</Text>
          <Text accessibilityRole="header" className="text-center text-2xl font-bold text-ink-900">La cámara está bloqueada</Text>
          <Text className="text-center text-lg text-ink-500">
            Sin la cámara no se pueden leer los tickets. Activala en Ajustes, en los permisos de Berrutti, y volvé a esta pantalla.
          </Text>
          <Button label="Abrir Ajustes" onPress={() => { Linking.openSettings().catch(() => undefined); }} />
          <Button label="Volver a intentar" variant="secondary"
            onPress={() => { void withPermission(retryPermission, info => flow.permissionAnswered(info)); }} />
          <Button label="Cancelar" variant="secondary" onPress={() => flow.cancel()} />
        </View>
      )}

      {state.kind === 'scanning' && (
        <View className="gap-3">
          <View className="h-96 overflow-hidden rounded-xl bg-ink-900">
            {active && (
              <CameraView
                style={{ flex: 1 }} facing="back"
                barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
                onBarcodeScanned={({ data }) => flow.codeScanned(data)}
              />
            )}
          </View>
          <Text className="text-center text-lg font-semibold text-ink-900" accessibilityLiveRegion="polite">Apuntá al código QR del ticket</Text>
          <Button label="Cancelar" variant="secondary" onPress={() => flow.cancel()} />
        </View>
      )}

      {state.kind === 'validating' && (
        <View className="items-center gap-3 rounded-xl border border-border-200 bg-surface-0 p-8" accessibilityLiveRegion="polite">
          <ActivityIndicator accessibilityLabel="Validando" size="large" color="#141414" />
          <Text accessibilityRole="header" className="text-2xl font-bold text-ink-900">Validando…</Text>
          <Text className="text-base text-ink-500">Esperando la respuesta del servidor.</Text>
        </View>
      )}

      {state.kind === 'result' && (
        <>
          <ValidationResultCard view={describeResult(state.result, chosen, formatUruguayDateTime)} />
          <Button label="Escanear otro" onPress={() => flow.scanAnother()} />
          <Button label="Cambiar tramo" variant="secondary" onPress={() => flow.backToSelection()} />
        </>
      )}

      {state.kind === 'network-error' && (
        <>
          <ValidationResultCard view={describeUnconfirmed(state.message, chosen)} />
          {canRetryFailure(state.failure) && <Button label="Reintentar" onPress={() => flow.retry()} />}
          <Button label="Escanear otro" variant="secondary" onPress={() => flow.scanAnother()} />
          <Button label="Cambiar tramo" variant="secondary" onPress={() => flow.backToSelection()} />
        </>
      )}
    </ScrollView>
  );
}
