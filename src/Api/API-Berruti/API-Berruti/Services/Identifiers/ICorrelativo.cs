namespace API_Berruti.Services.Identifiers;

public interface ICorrelativo
{
    string Siguiente(string prefijo, int ultimo);
}
