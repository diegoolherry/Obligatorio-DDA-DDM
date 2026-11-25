import { useRouter } from 'expo-router';
import { MyTickets } from '../components/Tickets/MyTickets';
import { FocusedOnly } from '../components/ui/FocusedOnly';
import { useAppServices } from '../context/AppServicesContext';

export default function TicketsScreen() {
  const { authentication, tickets } = useAppServices();
  const router = useRouter();
  return (
    <FocusedOnly>
      <MyTickets session={authentication} service={tickets} onSearchTrip={() => router.navigate('/')} />
    </FocusedOnly>
  );
}
