using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

List<ConqueredPeaks> repo = new List<ConqueredPeaks>();

app.MapGet("/peaks", () => Results.Ok(repo));

app.MapPost("/peaks", (CreateCoordinatedDTO dto) =>
{
    var peak = new ConqueredPeaks
    {
        NamePeak = dto.NamePeak,
        NameCountry = dto.NameCountry,
        Height = int.Parse(dto.Height),
        Coordinated = dto.Coordinated
    };

    repo.Add(peak);
    return Results.Created($"/peaks/{repo.Count - 1}", peak);
});

app.MapPut("/peaks/{index}", (int index, UpdateCoordunatedDTO dto) =>
{
    if (index >= 0 && index < repo.Count)
    {
        repo[index].Coordinated = dto.Coordinated;
        return Results.Ok(repo[index]);
    }

    return Results.NotFound("Вершина не найдена.");
});

app.MapDelete("/peaks/{index}", (int index) =>
{
    if (index >= 0 && index < repo.Count)
    {
        repo.RemoveAt(index);
        return Results.NoContent();
    }

    return Results.NotFound("Вершина не найдена.");
});

app.Run();

class ConqueredPeaks
{
    public string NamePeak { get; set; }
    public string NameCountry { get; set; }
    public int Height { get; set; }

    [RegularExpression(@"^[-+]?([1-8]?[0-9](\.\d+)?|90(\.0+)?),\s?[-+]?(1[0-7][0-9]|[1-9]?[0-9])(\.\d+)?$", ErrorMessage = "Неправильно набран номер.")]
    public string Coordinated { get; set; }
}

record class CreateCoordinatedDTO(string NamePeak, string NameCountry, string Height, string Coordinated);

record class UpdateCoordunatedDTO(string Coordinated);
