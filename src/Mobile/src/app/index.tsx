import { ItinerarySearch } from '../components/Itinerary/ItinerarySearch';
import { useAppServices } from '../context/AppServicesContext';

export default function BuscarScreen() {
  const { itineraries, authentication, purchase, sessionEpoch } = useAppServices();
  return <ItinerarySearch service={itineraries} authentication={authentication} purchase={purchase} sessionEpoch={sessionEpoch} />;
}
