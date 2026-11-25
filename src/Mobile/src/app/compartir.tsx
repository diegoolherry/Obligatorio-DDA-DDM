import { LocationSharing } from '../components/Location/LocationSharing';
import { useAppServices } from '../context/AppServicesContext';

export default function CompartirScreen() {
  const { authentication, location } = useAppServices();
  return <LocationSharing session={authentication} service={location} />;
}
