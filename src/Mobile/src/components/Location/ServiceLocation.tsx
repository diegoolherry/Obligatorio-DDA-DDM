import { useCallback, useEffect, useRef, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { ConsultaUbicacionResponse, ServicioUbicacionDto } from '../../contracts/location';
import type { IAuthenticationService } from '../../services/Authentication/IAuthenticationService';
import type { ILocationService } from '../../services/Location/ILocationService';
import { formatAge, formatUruguayDateTime, localHour } from '../../services/Location/locationFormat';
import { Action, Notice, ServiceChooser, parts } from './LocationParts';

type ServicesState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'empty' }
  | { status: 'ready'; services: ServicioUbicacionDto[] };

type DetailState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: ConsultaUbicacionResponse };

export function ServiceLocation({ session, service }: {
  session: Pick<IAuthenticationService, 'getState' | 'getRoles'>;
  service: ILocationService;
}) {
  const authenticated = session.getState().authenticated;
  const isPassenger = session.getRoles().includes('pasajero');
  const allowed = authenticated && isPassenger;
  const [services, setServices] = useState<ServicesState>({ status: 'loading' });
  const [selectedId, setSelectedId] = useState<string | undefined>();
  const [detail, setDetail] = useState<DetailState>({ status: 'idle' });
  const mounted = useRef(true);
  const listCounter = useRef(0);
  const detailCounter = useRef(0);

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  const loadServices = useCallback(() => {
    const id = ++listCounter.current;
    setServices({ status: 'loading' });
    void service.listPassengerServices().then(result => {
      if (!mounted.current || id !== listCounter.current) return;
      if (!result.ok) setServices({ status: 'error', message: result.message });
      else setServices(result.data.length === 0 ? { status: 'empty' } : { status: 'ready', services: result.data });
    });
  }, [service]);

  const loadDetail = useCallback((serviceId: string) => {
    const id = ++detailCounter.current;
    setDetail({ status: 'loading' });
    void service.getServiceLocation(serviceId).then(result => {
      // A newer selection or refresh wins over a late response.
      if (!mounted.current || id !== detailCounter.current) return;
      setDetail(result.ok ? { status: 'ready', data: result.data } : { status: 'error', message: result.message });
    });
  }, [service]);

  useEffect(() => {
    if (!allowed) return undefined;
    loadServices();
    return () => { listCounter.current++; detailCounter.current++; };
  }, [allowed, loadServices]);

  function select(serviceId: string) {
    setSelectedId(serviceId);
    loadDetail(serviceId);
  }

  if (!authenticated || !isPassenger) {
    return (
      <View style={styles.container}>
        <Text style={parts.title} accessibilityRole="header">Ubicación del servicio</Text>
        <Notice kind="warning">
          {!authenticated ? 'Ingresá como pasajero en la pestaña «Ingreso» para ver la ubicación de un servicio.'
            : 'Esta pantalla es para cuentas de pasajero. Para compartir la ubicación usá «Compartir ubicación».'}
        </Notice>
      </View>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={parts.title} accessibilityRole="header">Ubicación del servicio</Text>
      <Text style={parts.muted}>Datos ficticios y aproximados: demostración académica, no es información operativa real.</Text>

      <View style={parts.section}>
        <Text style={parts.subtitle} accessibilityRole="header">Servicios</Text>
        {services.status === 'loading' && (
          <View style={parts.row} accessibilityLiveRegion="polite">
            <ActivityIndicator accessibilityLabel="Cargando servicios" color="#222" />
            <Text style={parts.text}>Cargando servicios…</Text>
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
            <Notice kind="info">No hay servicios para mostrar.</Notice>
            <Action label="Actualizar lista" secondary onPress={loadServices} />
          </>
        )}
        {services.status === 'ready' && (
          <ServiceChooser services={services.services} selectedId={selectedId} onSelect={select} />
        )}
        {services.status === 'ready' && !selectedId && <Text style={parts.muted}>Elegí un servicio para ver dónde está.</Text>}
      </View>

      {selectedId && (
        <View style={parts.section}>
          {detail.status === 'loading' && (
            <View style={parts.row} accessibilityLiveRegion="polite">
              <ActivityIndicator accessibilityLabel="Consultando ubicación" color="#222" />
              <Text style={parts.text}>Consultando ubicación…</Text>
            </View>
          )}
          {detail.status === 'error' && (
            <>
              <Notice kind="error">{detail.message}</Notice>
              <Action label="Reintentar" secondary onPress={() => loadDetail(selectedId)} />
            </>
          )}
          {detail.status === 'ready' && <Detail data={detail.data} />}
          {detail.status !== 'loading' && detail.status !== 'idle' && (
            <Action label="Actualizar ubicación" secondary onPress={() => loadDetail(selectedId)} />
          )}
        </View>
      )}
    </ScrollView>
  );
}

function Detail({ data }: { data: ConsultaUbicacionResponse }) {
  const position = data.ultimaPosicion;
  const updated = position ? formatUruguayDateTime(position.recibidaUtc) : undefined;
  const hasArrival = data.llegadaAproximada !== null && data.estadoDemora !== 'sinDatos';
  return (
    <View style={parts.section}>
      <Text style={parts.subtitle} accessibilityRole="header">{data.servicio.origen} → {data.servicio.destino}</Text>
      <Text style={parts.text}>
        Programado: sale {localHour(data.servicio.salida)} · llega {localHour(data.servicio.llegada)} (hora de Uruguay)
      </Text>

      <Text style={parts.subtitle} accessibilityRole="header">Última posición</Text>
      {position === null ? (
        <Notice kind="info">Todavía no se recibieron posiciones de este servicio.</Notice>
      ) : (
        <>
          <Text style={parts.text}>Latitud {position.latitud.toFixed(6)} · longitud {position.longitud.toFixed(6)}</Text>
          <Text style={parts.text}>Actualizada: {updated ?? 'fecha no disponible'} (hora de Uruguay)</Text>
          <Text style={parts.text}>Antigüedad: {formatAge(position.antiguedadSegundos)}</Text>
          {data.desactualizada && (
            <Notice kind="warning">
              {`Desactualizada: la última posición supera el umbral de ${data.parametros.umbralDesactualizacionSegundos} s. No la tomes como la ubicación actual.`}
            </Notice>
          )}
        </>
      )}

      <Text style={parts.subtitle} accessibilityRole="header">Llegada y demora</Text>
      {hasArrival && data.llegadaAproximada !== null ? (
        <>
          <Text style={parts.text}>Llegada aproximada: {localHour(data.llegadaAproximada)} (hora de Uruguay)</Text>
          <Text style={parts.text}>
            Estado: {data.estadoDemora === 'conDemora' ? 'con demora' : 'en horario'} (tolerancia {data.parametros.toleranciaDemoraMinutos} min)
          </Text>
        </>
      ) : (
        <Notice kind="info">{`Llegada y demora no disponibles. Motivo: ${data.motivo ?? 'sin datos suficientes.'}`}</Notice>
      )}

      <Text style={parts.subtitle} accessibilityRole="header">Parámetros académicos</Text>
      <Text style={parts.text}>Periodicidad de publicación: {data.parametros.periodicidadSegundos} s</Text>
      <Text style={parts.text}>Umbral de desactualización: {data.parametros.umbralDesactualizacionSegundos} s</Text>
      <Text style={parts.text}>Velocidad promedio: {data.parametros.velocidadPromedioKmH} km/h</Text>
      <Text style={parts.text}>Tolerancia de demora: {data.parametros.toleranciaDemoraMinutos} min</Text>
      <Text style={parts.muted}>
        {data.datosFicticios ? 'Datos ficticios y aproximados: demostración académica.' : 'Datos aproximados: demostración académica.'}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, padding: 24, gap: 16 },
});
