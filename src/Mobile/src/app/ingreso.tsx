import { PassengerLogin } from '../components/Authentication/PassengerLogin';
import { FocusedOnly } from '../components/ui/FocusedOnly';
import { useAppServices } from '../context/AppServicesContext';

export default function IngresoScreen() {
  const { authentication, notifySessionChanged } = useAppServices();
  return (
    <FocusedOnly>
      <PassengerLogin service={authentication} onSessionChanged={notifySessionChanged} />
    </FocusedOnly>
  );
}
