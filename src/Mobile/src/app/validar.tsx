import { useIsFocused } from 'expo-router';
import { TicketValidationScreen } from '../components/TicketValidation/TicketValidationScreen';
import { useAppServices } from '../context/AppServicesContext';

export default function ValidarScreen() {
  const { authentication, validation, location } = useAppServices();
  // Stays mounted while hidden (lazy: false in the layout) so a validation in flight is not lost; the camera is released when inactive.
  const focused = useIsFocused();
  return <TicketValidationScreen session={authentication} service={validation} locations={location} active={focused} />;
}
