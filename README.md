# Country Currency & Exchange API

A RESTful API that fetches country data from external APIs, stores it in a MySQL database, and provides CRUD operations with filtering, sorting, and summary image generation capabilities.

## Features

- ✅ Fetch and cache country data with exchange rates
- ✅ Filter countries by region and currency
- ✅ Sort by GDP, population, or name
- ✅ CRUD operations for country records
- ✅ Automatic summary image generation
- ✅ Comprehensive error handling
- ✅ MySQL database with Entity Framework Core
- ✅ Docker support for MySQL

## Tech Stack

- **Framework:** ASP.NET Core 9.0
- **Database:** MySQL 8.0
- **ORM:** Entity Framework Core 9.0
- **Image Generation:** SkiaSharp
- **API Documentation:** Scalar (OpenAPI/Swagger)
- **Containerization:** Docker & Docker Compose

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) (for MySQL)
- [Git](https://git-scm.com/)

## Getting Started

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd hng13-stage2-backend
```

### 2. Start MySQL Database

```bash
docker-compose up -d
```

This will start a MySQL 8.0 container on port 3306 with:

- Database: `country_currency_db`
- Username: `root`
- Password: `password`

### 3. Restore Dependencies

```bash
cd src/CountryCurrencyAPI
dotnet restore
```

### 4. Run Database Migrations

```bash
dotnet ef database update
```

This will create the necessary database tables.

### 5. Run the Application

```bash
dotnet run
```

The API will be available at:

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- API Documentation: `https://localhost:5001/scalar/v1`

## API Endpoints

### POST /countries/refresh

Fetch all countries and exchange rates from external APIs, then cache them in the database.

**Response:**

```json
{
  "message": "Countries refreshed successfully",
  "total_countries": 250
}
```

**Error Response (503):**

```json
{
  "error": "External data source unavailable",
  "details": "Could not fetch data from RestCountries API"
}
```

### GET /countries

Get all countries from the database with optional filters and sorting.

**Query Parameters:**

- `region` - Filter by region (e.g., `Africa`, `Europe`, `Asia`)
- `currency` - Filter by currency code (e.g., `NGN`, `USD`, `GBP`)
- `sort` - Sort results:
  - `gdp_desc` - By GDP descending
  - `gdp_asc` - By GDP ascending
  - `population_desc` - By population descending
  - `population_asc` - By population ascending
  - `name_asc` - By name ascending (default)
  - `name_desc` - By name descending

**Examples:**

```bash
GET /countries?region=Africa
GET /countries?currency=NGN
GET /countries?sort=gdp_desc
GET /countries?region=Europe&sort=population_desc
```

**Response:**

```json
[
  {
    "id": 1,
    "name": "Nigeria",
    "capital": "Abuja",
    "region": "Africa",
    "population": 206139589,
    "currency_code": "NGN",
    "exchange_rate": 1600.23,
    "estimated_gdp": 25767448125.2,
    "flag_url": "https://flagcdn.com/ng.svg",
    "last_refreshed_at": "2025-10-22T18:00:00Z"
  }
]
```

### GET /countries/{name}

Get a single country by name (case-insensitive).

**Example:**

```bash
GET /countries/Nigeria
```

**Response:**

```json
{
  "id": 1,
  "name": "Nigeria",
  "capital": "Abuja",
  "region": "Africa",
  "population": 206139589,
  "currency_code": "NGN",
  "exchange_rate": 1600.23,
  "estimated_gdp": 25767448125.2,
  "flag_url": "https://flagcdn.com/ng.svg",
  "last_refreshed_at": "2025-10-22T18:00:00Z"
}
```

**Error Response (404):**

```json
{
  "error": "Country not found"
}
```

### DELETE /countries/{name}

Delete a country record by name.

**Example:**

```bash
DELETE /countries/Nigeria
```

**Response:**

```json
{
  "message": "Country 'Nigeria' deleted successfully"
}
```

### GET /status

Get the total number of countries and last refresh timestamp.

**Response:**

```json
{
  "total_countries": 250,
  "last_refreshed_at": "2025-10-22T18:00:00Z"
}
```

### GET /countries/image

Serve the generated summary image (PNG) containing:

- Total countries
- Top 5 countries by estimated GDP
- Last refresh timestamp

**Response:** Image file (image/png)

**Error Response (404):**

```json
{
  "error": "Summary image not found"
}
```

## Data Processing Logic

### Currency Handling

1. **Multiple Currencies:** If a country has multiple currencies, only the first one is stored.

2. **No Currencies:**

   - `currency_code` → `null`
   - `exchange_rate` → `null`
   - `estimated_gdp` → `0`
   - Country is still stored

3. **Currency Not Found in Exchange Rates:**
   - `exchange_rate` → `null`
   - `estimated_gdp` → `null`
   - Country is still stored

### GDP Calculation

```
estimated_gdp = (population × random(1000-2000)) ÷ exchange_rate
```

- A fresh random multiplier (1000-2000) is generated on each refresh for each country
- If exchange_rate is null, estimated_gdp is also null

### Update vs Insert

- Countries are matched by name (case-insensitive)
- Existing countries are updated with fresh data
- New countries are inserted
- All fields are recalculated on each refresh, including a new random GDP multiplier

## Environment Variables

The application uses the following configuration in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=country_currency_db;User=root;Password=password;"
  }
}
```

For production, you can override this using environment variables:

```bash
export ConnectionStrings__DefaultConnection="Server=your-host;Port=3306;Database=your-db;User=your-user;Password=your-password;"
```

## External APIs

1. **Countries API:** https://restcountries.com/v2/all?fields=name,capital,region,population,flag,currencies
2. **Exchange Rates API:** https://open.er-api.com/v6/latest/USD

## Error Handling

The API returns consistent JSON error responses:

- **400 Bad Request:** Validation failed
- **404 Not Found:** Resource not found
- **500 Internal Server Error:** Server error
- **503 Service Unavailable:** External API unavailable

**Example Error Response:**

```json
{
  "error": "Validation failed",
  "details": {
    "name": "is required"
  }
}
```

## Database Schema

### Countries Table

| Column          | Type          | Constraints                 |
| --------------- | ------------- | --------------------------- |
| Id              | int           | Primary Key, Auto-increment |
| Name            | varchar(255)  | Required, Unique            |
| Capital         | varchar(255)  | Optional                    |
| Region          | varchar(100)  | Optional                    |
| Population      | bigint        | Required                    |
| CurrencyCode    | varchar(10)   | Optional                    |
| ExchangeRate    | decimal(18,6) | Optional                    |
| EstimatedGdp    | decimal(20,2) | Optional                    |
| FlagUrl         | varchar(500)  | Optional                    |
| LastRefreshedAt | datetime      | Required                    |

### SystemMetadata Table

| Column    | Type         | Constraints                 |
| --------- | ------------ | --------------------------- |
| Id        | int          | Primary Key, Auto-increment |
| KeyName   | varchar(100) | Required, Unique            |
| KeyValue  | text         | Optional                    |
| UpdatedAt | datetime     | Required                    |

## Development

### Run Migrations

Create a new migration:

```bash
dotnet ef migrations add MigrationName
```

Apply migrations:

```bash
dotnet ef database update
```

Remove last migration:

```bash
dotnet ef migrations remove
```

### Build the Project

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Watch Mode (Auto-reload)

```bash
dotnet watch run
```

## Docker Commands

Start MySQL:

```bash
docker-compose up -d
```

Stop MySQL:

```bash
docker-compose down
```

View MySQL logs:

```bash
docker-compose logs -f mysql
```

Connect to MySQL:

```bash
docker exec -it <container-id> mysql -u root -p
# Password: password
```

## Project Structure

```
hng13-stage2-backend/
├── src/
│   └── CountryCurrencyAPI/
│       ├── Controllers/
│       │   └── CountriesController.cs
│       ├── Data/
│       │   └── AppDbContext.cs
│       ├── DTOs/
│       │   ├── CountryDto.cs
│       │   ├── ExchangeRateResponse.cs
│       │   └── RestCountriesResponse.cs
│       ├── Migrations/
│       │   └── [Migration files]
│       ├── Models/
│       │   ├── Country.cs
│       │   └── SystemMetadata.cs
│       ├── Services/
│       │   ├── CountryService.cs
│       │   ├── ICountryService.cs
│       │   ├── ImageService.cs
│       │   └── IImageService.cs
│       ├── cache/
│       │   └── summary.png (generated)
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Program.cs
│       └── CountryCurrencyAPI.csproj
├── docker-compose.yml
└── README.md
```

## Troubleshooting

### Database Connection Issues

1. Ensure MySQL container is running:

   ```bash
   docker ps
   ```

2. Check connection string in `appsettings.json`

3. Verify MySQL is accessible:
   ```bash
   docker exec -it <container-id> mysql -u root -p
   ```

### Migration Issues

If migrations fail, try:

```bash
dotnet ef database drop
dotnet ef database update
```

### External API Timeout

The HTTP client has a 30-second timeout. If external APIs are slow, you may get a 503 error. Wait and try again.

### Image Not Generated

Ensure the `cache` directory has write permissions. The image will be generated automatically after a successful `/countries/refresh` call.

## Deployment

### Prerequisites for Production

1. MySQL database instance
2. .NET 9.0 runtime
3. Environment variables configured

### Deployment Options

- **Railway:** Supports .NET 9.0
- **Heroku:** Use Docker deployment
- **AWS:** EC2, ECS, or Elastic Beanstalk
- **Azure:** App Service

### Production Considerations

1. Update `appsettings.Production.json` with production database connection
2. Enable HTTPS
3. Configure CORS for your frontend domain
4. Add rate limiting
5. Implement caching (Redis)
6. Add authentication if needed
7. Set up logging (Serilog, Application Insights)

## License

MIT License

## Author

[Sherifdeen Adebayo](https://github.com/herdeybayor)
