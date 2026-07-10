namespace UrlShortener.Api.Exceptions;

public class AliasAlreadyExistsException(string alias) : Exception($"Alias '{alias}' is already taken.")
{
    public string Alias => alias;
}
