import { ServiceLocation } from '../components/Location/ServiceLocation';
import { FocusedOnly } from '../components/ui/FocusedOnly';
import { useAppServices } from '../context/AppServicesContext';

export default function UbicacionScreen() {
  const { authentication, location } = useAppServices();
  return (
    <FocusedOnly>
      <ServiceLocation session={authentication} service={location} />
    </FocusedOnly>
  );
}
