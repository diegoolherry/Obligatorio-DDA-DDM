import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { AuthenticationService } from '../services/Authentication/AuthenticationService';
import type { IAuthenticationService } from '../services/Authentication/IAuthenticationService';
import { ItinerarySearchService } from '../services/Itinerary/ItinerarySearchService';
import type { IItinerarySearchService } from '../services/Itinerary/IItinerarySearchService';
import type { ILocationService } from '../services/Location/ILocationService';
import { LocationService } from '../services/Location/LocationService';
import { PurchaseService } from '../services/Purchase/PurchaseService';

interface AppServices {
  authentication: IAuthenticationService;
  itineraries: IItinerarySearchService;
  purchase: PurchaseService;
  location: ILocationService;
  /** Grows on every login or logout so screens can reset session-bound state. */
  sessionEpoch: number;
  notifySessionChanged: () => void;
}

const AppServicesContext = createContext<AppServices | null>(null);

export function AppServicesProvider({ children }: { children: ReactNode }) {
  const [authentication] = useState<IAuthenticationService>(
    () => new AuthenticationService(process.env.EXPO_PUBLIC_API_URL),
  );
  const [itineraries] = useState<IItinerarySearchService>(
    () => new ItinerarySearchService(process.env.EXPO_PUBLIC_API_URL),
  );
  const [purchase] = useState(() => new PurchaseService(authentication));
  // The location service asks the session to send its requests: the token stays inside it.
  const [location] = useState<ILocationService>(() => new LocationService(authentication));
  const [sessionEpoch, setSessionEpoch] = useState(0);
  // The in-memory session outlives tab switches and is discarded only when the app root unmounts.
  useEffect(() => () => authentication.discard(), [authentication]);
  const value: AppServices = {
    authentication, itineraries, purchase, location, sessionEpoch,
    notifySessionChanged: () => setSessionEpoch(epoch => epoch + 1),
  };
  return <AppServicesContext.Provider value={value}>{children}</AppServicesContext.Provider>;
}

export function useAppServices(): AppServices {
  const services = useContext(AppServicesContext);
  if (!services) throw new Error('useAppServices must be used inside AppServicesProvider');
  return services;
}
