namespace CountryCurrencyAPI.Services;

public interface IImageService
{
    Task GenerateSummaryImageAsync(int totalCountries, List<(string Name, decimal? Gdp)> topCountries, DateTime lastRefreshed);
    string GetImagePath();
    bool ImageExists();
}

