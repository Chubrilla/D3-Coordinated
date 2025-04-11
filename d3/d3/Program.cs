using System.ComponentModel.DataAnnotations;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();

var peaksStorage = new PeaksRepository();

app.MapGet("/peaks", (
    string? search,          // Поиск по названию вершины
    string? country,         // Фильтр по стране
    int? minHeight,          // Минимальная высота
    int? maxHeight,          // Максимальная высота
    string? sortBy,          // Поле для сортировки (name, height, country)
    bool? sortDescending,    // Сортировка по убыванию
    int? page,               // Номер страницы
    int? pageSize) =>        // Количество вершин на странице
{
    var peaks = peaksStorage.GetFilteredPeaks(
        searchText: search,
        countryName: country,
        minHeight: minHeight,
        maxHeight: maxHeight,
        sortField: sortBy ?? "Name",
        isSortDescending: sortDescending ?? false,
        pageNumber: page ?? 1,
        itemsPerPage: pageSize ?? 10
    );
    return Results.Ok(peaks);
});

app.MapPost("/peaks", (CreatePeakDTO newPeakData) =>
{
    var peak = new ConqueredPeak
    {
        Name = newPeakData.Name,
        Country = newPeakData.Country,
        Height = int.Parse(newPeakData.Height),
        Coordinates = newPeakData.Coordinates
    };

    // Проверяем корректность данных
    var errors = ValidatePeak(peak);
    if (errors.Any())
    {
        return Results.BadRequest(errors);
    }

    peaksStorage.AddPeak(peak);
    return Results.Created($"/peaks/{peaksStorage.TotalCount - 1}", peak);
});

app.MapPut("/peaks/{index}", (int index, UpdateCoordinatesDTO updatedCoordinates) =>
{
    var tempPeak = new ConqueredPeak { Coordinates = updatedCoordinates.Coordinates };
    var errors = ValidatePeak(tempPeak);
    if (errors.Any())
    {
        return Results.BadRequest(errors);
    }

    if (peaksStorage.UpdatePeakCoordinates(index, updatedCoordinates.Coordinates))
    {
        return Results.Ok(peaksStorage.GetPeakByIndex(index));
    }
    return Results.NotFound("Вершина не найдена.");
});

app.MapDelete("/peaks/{index}", (int index) =>
{
    if (peaksStorage.RemovePeak(index))
    {
        return Results.NoContent();
    }
    return Results.NotFound("Вершина не найдена.");
});

app.Run();

static List<string> ValidatePeak(ConqueredPeak peak)
{
    var context = new ValidationContext(peak);
    var errors = new List<ValidationResult>();
    Validator.TryValidateObject(peak, context, errors, true);
    return errors.Select(e => e.ErrorMessage ?? "Ошибка валидации").ToList();
}
public class PeaksRepository
{
    private readonly List<ConqueredPeak> _peaks = new();

    public void AddPeak(ConqueredPeak peak)
    {
        _peaks.Add(peak);
    }

    public IEnumerable<ConqueredPeak> GetFilteredPeaks(
        string? searchText,       // Текст для поиска по названию вершины
        string? countryName,      // Фильтр по стране
        int? minHeight,           // Минимальная высота вершины
        int? maxHeight,           // Максимальная высота вершины
        string sortField,         // Поле для сортировки (Name, Height, Country)
        bool isSortDescending,    // Сортировать по убыванию?
        int pageNumber,           // Номер страницы для пагинации
        int itemsPerPage)         // Количество вершин на странице
    {
        var query = _peaks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToLower();
            query = query.Where(peak => peak.Name.ToLower().Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(countryName))
        {
            query = query.Where(peak => peak.Country.ToLower() == countryName.ToLower());
        }

        if (minHeight.HasValue)
        {
            query = query.Where(peak => peak.Height >= minHeight.Value);
        }

        if (maxHeight.HasValue)
        {
            query = query.Where(peak => peak.Height <= maxHeight.Value);
        }

        query = sortField.ToLower() switch
        {
            "height" => isSortDescending
                ? query.OrderByDescending(peak => peak.Height)
                : query.OrderBy(peak => peak.Height),
            "country" => isSortDescending
                ? query.OrderByDescending(peak => peak.Country)
                : query.OrderBy(peak => peak.Country),
            _ => isSortDescending
                ? query.OrderByDescending(peak => peak.Name)
                : query.OrderBy(peak => peak.Name)
        };

        // Пагинация: выборка нужной страницы
        query = query
            .Skip((pageNumber - 1) * itemsPerPage)
            .Take(itemsPerPage);

        return query.ToList();
    }

    public bool UpdatePeakCoordinates(int index, string newCoordinates)
    {
        if (index >= 0 && index < _peaks.Count)
        {
            _peaks[index].Coordinates = newCoordinates;
            return true;
        }
        return false;
    }

    public bool RemovePeak(int index)
    {
        if (index >= 0 && index < _peaks.Count)
        {
            _peaks.RemoveAt(index);
            return true;
        }
        return false;
    }

    public ConqueredPeak? GetPeakByIndex(int index)
    {
        return index >= 0 && index < _peaks.Count ? _peaks[index] : null;
    }

    public int TotalCount => _peaks.Count;
}


public class ConqueredPeak
{
    public string Name { get; set; } = string.Empty;     
    public string Country { get; set; } = string.Empty;  
    public int Height { get; set; }                      

    [RegularExpression(
        @"^[-+]?([1-8]?[0-9](\.\d+)?|90(\.0+)?),\s?[-+]?(1[0-7][0-9]|[1-9]?[0-9])(\.\d+)?$",
        ErrorMessage = "Координаты должны быть в формате 'широта, долгота' (например, '43.35, 42.44').")]
    public string Coordinates { get; set; } = string.Empty;
}

public record CreatePeakDTO(
    string Name,       
    string Country,    
    string Height,     
    string Coordinates 
);

public record UpdateCoordinatesDTO(
    string Coordinates  // Новые координаты
);