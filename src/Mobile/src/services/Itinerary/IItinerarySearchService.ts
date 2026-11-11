import type {
  BusquedaItinerarioRequest, ItinerarioDto, ItineraryResult, ParadaDto,
} from '../../contracts/itinerary';

export interface IItinerarySearchService {
  getStops(signal?: AbortSignal): Promise<ItineraryResult<ParadaDto[]>>;
  search(request: BusquedaItinerarioRequest, signal?: AbortSignal): Promise<ItineraryResult<ItinerarioDto[]>>;
}
