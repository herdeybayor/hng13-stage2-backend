using SkiaSharp;

namespace CountryCurrencyAPI.Services;

public class ImageService : IImageService
{
    private readonly string _imagePath;
    private readonly ILogger<ImageService> _logger;

    public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
    {
        var cacheDir = Path.Combine(environment.ContentRootPath, "cache");
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }
        _imagePath = Path.Combine(cacheDir, "summary.png");
        _logger = logger;
    }

    public string GetImagePath() => _imagePath;

    public bool ImageExists() => File.Exists(_imagePath);

    public async Task GenerateSummaryImageAsync(int totalCountries, List<(string Name, decimal? Gdp)> topCountries, DateTime lastRefreshed)
    {
        await Task.Run(() =>
        {
            try
            {
                const int width = 800;
                const int height = 600;
                const int padding = 40;

                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                var canvas = surface.Canvas;

                // Background
                canvas.Clear(SKColors.White);

                // Draw border
                using var borderPaint = new SKPaint
                {
                    Color = SKColors.LightGray,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                canvas.DrawRect(10, 10, width - 20, height - 20, borderPaint);

                // Title
                using var titlePaint = new SKPaint
                {
                    Color = SKColors.DarkBlue,
                    TextSize = 36,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                };
                var title = "Country Currency Summary";
                var titleBounds = new SKRect();
                titlePaint.MeasureText(title, ref titleBounds);
                canvas.DrawText(title, (width - titleBounds.Width) / 2, padding + 30, titlePaint);

                // Total countries
                using var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                };
                var totalText = $"Total Countries: {totalCountries}";
                canvas.DrawText(totalText, padding, padding + 90, textPaint);

                // Top 5 countries header
                using var headerPaint = new SKPaint
                {
                    Color = SKColors.DarkSlateGray,
                    TextSize = 28,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                };
                canvas.DrawText("Top 5 Countries by Estimated GDP:", padding, padding + 150, headerPaint);

                // List top countries
                using var listPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 20,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                };

                var yPosition = padding + 190;
                var rank = 1;
                foreach (var (name, gdp) in topCountries.Take(5))
                {
                    var gdpText = gdp.HasValue ? $"${gdp.Value:N2}" : "N/A";
                    var countryText = $"{rank}. {name}: {gdpText}";
                    canvas.DrawText(countryText, padding + 20, yPosition, listPaint);
                    yPosition += 35;
                    rank++;
                }

                // Timestamp
                using var timestampPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 18,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic)
                };
                var timestampText = $"Last Refreshed: {lastRefreshed:yyyy-MM-dd HH:mm:ss} UTC";
                canvas.DrawText(timestampText, padding, height - padding, timestampPaint);

                // Save image
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(_imagePath);
                data.SaveTo(stream);

                _logger.LogInformation($"Summary image generated successfully at {_imagePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary image");
                throw;
            }
        });
    }
}

