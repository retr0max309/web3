using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using tareasWeb3.Models;

namespace tareasWeb3.Pages
{
    public class TareasModel : PageModel
    {
        // lista que se muestra en la tabla
        public List<Tarea> Tareas { get; set; } = new();

        // paginacion
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        // selector de tamano
        [BindProperty(SupportsGet = true)]
        public string? SelectedSize { get; set; } = "m";
        public string TableSizeClass =>
            SelectedSize == "s" ? "table-sm" :
            SelectedSize == "l" ? "table-lg" : "";

        // filtros
        [BindProperty(SupportsGet = true)]
        public string? FilterEstado { get; set; }   // ejemplo Pendiente En curso Finalizado
        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }              // texto a buscar en nombre

        // para llenar combo de estados
        public List<string> Estados { get; set; } = new();

        public void OnGet(int pageNumber = 1)
        {
            // lee json
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "tareas.json");
            if (!System.IO.File.Exists(filePath))
            {
                Tareas = new();
                TotalPages = 1;
                PageNumber = 1;
                return;
            }

            var json = System.IO.File.ReadAllText(filePath);

            // deserializa
            var allTareas = JsonSerializer.Deserialize<List<Tarea>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Tarea>();

            // estados disponibles
            Estados = allTareas
                .Select(t => t.Estado?.Trim() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            // aplica filtros
            IEnumerable<Tarea> filtered = allTareas;

            // por estado
            if (!string.IsNullOrWhiteSpace(FilterEstado) &&
                !string.Equals(FilterEstado, "todos", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(t =>
                    string.Equals(t.Estado?.Trim(), FilterEstado.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            // por texto en nombre
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                filtered = filtered.Where(t =>
                    !string.IsNullOrWhiteSpace(t.NombreTarea) &&
                    t.NombreTarea.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            // paginado
            int pageSize = 10; // puedes ajustar
            int total = filtered.Count();

            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            PageNumber = Math.Min(Math.Max(1, pageNumber), TotalPages);

            Tareas = filtered
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
    }
}
