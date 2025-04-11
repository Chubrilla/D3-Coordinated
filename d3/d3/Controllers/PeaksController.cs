using Microsoft.AspNetCore.Mvc;
using PeaksApi;
using System.ComponentModel.DataAnnotations;

namespace d3.Controllers;

[ApiController]
[Route("peaks")]
public class PeaksController : ControllerBase
{
    private readonly PeaksRepository _peaksStorage;

    public PeaksController(PeaksRepository peaksStorage)
    {
        _peaksStorage = peaksStorage;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ConqueredPeak>> GetPeaks(
        [FromQuery] string? search,          // Поиск по названию вершины
        [FromQuery] string? country,        // Фильтр по стране
        [FromQuery] int? minHeight,         // Минимальная высота
        [FromQuery] int? maxHeight,         // Максимальная высота
        [FromQuery] string? sortBy,         // Поле для сортировки (name, height, country)
        [FromQuery] bool sortDescending = false, // Сортировка по убыванию
        [FromQuery] int page = 1,           // Номер страницы
        [FromQuery] int pageSize = 10)      // Количество вершин на странице
    {
        var peaks = _peaksStorage.GetFilteredPeaks(
            searchText: search,
            countryName: country,
            minHeight: minHeight,
            maxHeight: maxHeight,
            sortField: sortBy ?? "Name",
            isSortDescending: sortDescending,
            pageNumber: page,
            itemsPerPage: pageSize
        );
        return Ok(peaks);
    }

    [HttpPost]
    public ActionResult<ConqueredPeak> CreatePeak([FromBody] CreatePeakDTO newPeakData)
    {
        var peak = new ConqueredPeak
        {
            Name = newPeakData.Name,
            Country = newPeakData.Country,
            Height = int.Parse(newPeakData.Height),
            Coordinates = newPeakData.Coordinates
        };

        var errors = PeakValidator.Validate(peak);
        if (errors.Any())
        {
            return BadRequest(errors);
        }

        _peaksStorage.AddPeak(peak);
        return CreatedAtAction(
            nameof(GetPeaks),
            new { index = _peaksStorage.TotalCount - 1 },
            peak);
    }

    [HttpPut("{index}")]
    public ActionResult<ConqueredPeak> UpdatePeakCoordinates(int index, [FromBody] UpdateCoordinatesDTO updatedCoordinates)
    {
        var tempPeak = new ConqueredPeak { Coordinates = updatedCoordinates.Coordinates };
        var errors = PeakValidator.Validate(tempPeak);
        if (errors.Any())
        {
            return BadRequest(errors);
        }

        if (_peaksStorage.UpdatePeakCoordinates(index, updatedCoordinates.Coordinates))
        {
            return Ok(_peaksStorage.GetPeakByIndex(index));
        }
        return NotFound("Вершина не найдена.");
    }

    [HttpDelete("{index}")]
    public ActionResult DeletePeak(int index)
    {
        if (_peaksStorage.RemovePeak(index))
        {
            return NoContent();
        }
        return NotFound("Вершина не найдена.");
    }
}