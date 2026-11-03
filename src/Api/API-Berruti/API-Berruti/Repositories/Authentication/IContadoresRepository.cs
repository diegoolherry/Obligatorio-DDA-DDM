namespace API_Berruti.Repositories.Authentication;

public interface IContadoresRepository
{
    Task<string> IncrementarAsync(string prefijo);
}
