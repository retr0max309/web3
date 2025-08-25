using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Linq;
using tareasWeb3.Models;

namespace tareasWeb3.Pages
{
    public class TareasModel : PageModel
    {
        public List<Tarea> Tareas { get; set; } = new();

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedSize { get; set; } = "m";
        public string TableSizeClass =>
            SelectedSize == "s" ? "table-sm" :
            SelectedSize == "l" ? "table-lg" : "";

        [BindProperty(SupportsGet = true)]
        public string? FilterEstado { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public List<string> Estados { get; set; } = new();

        private readonly IWebHostEnvironment _env;
        public TareasModel(IWebHostEnvironment env) => _env = env;

        public void OnGet(int pageNumber = 1)
        {
            var filePath = GetJsonPath();
            if (!System.IO.File.Exists(filePath))
            {
                Estados = new List<string> { "Pendiente", "En curso", "Finalizado", "Cancelado" };
                PageNumber = 1;
                TotalPages = 1;
                Tareas = new();
                return;
            }

            var json = System.IO.File.ReadAllText(filePath);

            var allTareas = JsonSerializer.Deserialize<List<Tarea>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Tarea>();

            var baseEstados = new[] { "Pendiente", "En curso", "Finalizado", "Cancelado" };
            Estados = baseEstados
                .Union(
                    allTareas
                        .Select(t => t.Estado?.Trim() ?? "")
                        .Where(s => !string.IsNullOrWhiteSpace(s)),
                    StringComparer.OrdinalIgnoreCase
                )
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s == "Pendiente" ? 0
                             : s == "En curso" ? 1
                             : s == "Finalizado" ? 2
                             : s == "Cancelado" ? 3
                             : 99)
                .ToList();

            IEnumerable<Tarea> filtered = allTareas;

            if (!string.IsNullOrWhiteSpace(FilterEstado) &&
                !string.Equals(FilterEstado, "todos", StringComparison.OrdinalIgnoreCase))
            {
                var estado = FilterEstado.Trim();
                filtered = filtered.Where(t =>
                    string.Equals(t.Estado?.Trim(), estado, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                filtered = filtered.Where(t =>
                    !string.IsNullOrWhiteSpace(t.NombreTarea) &&
                    t.NombreTarea.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            const int pageSize = 10;
            int total = filtered.Count();

            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            PageNumber = Math.Min(Math.Max(1, pageNumber), TotalPages);

            Tareas = filtered
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        [BindProperty]
        public NuevaTareaInput NuevaTarea { get; set; } = new();

        public class NuevaTareaInput
        {
            [Required, StringLength(120)]
            public string? NombreTarea { get; set; }

            [Required, DataType(DataType.Date)]
            public DateTime? Fecha { get; set; }

            [Required]
            public string? Estado { get; set; } // Pendiente | En curso | Finalizado | Cancelado
        }

        // Quitar [ValidateAntiForgeryToken] en Razor Pages handlers
        public IActionResult OnPostCreate()
        {
            if (!ModelState.IsValid)
            {
                OnGet(1);
                return Page();
            }

            var listado = CargarTareas();
            var fecha = NuevaTarea.Fecha!.Value.ToString("dd/MM/yyyy");

            listado.Insert(0, new Tarea
            {
                NombreTarea = NuevaTarea.NombreTarea!.Trim(),
                FechaVencimiento = fecha,
                Estado = NuevaTarea.Estado!
            });

            GuardarTareas(listado);

            TempData["ok"] = "Tarea creada correctamente";

            return RedirectToPage(new
            {
                pageNumber = 1,
                SelectedSize,
                FilterEstado,
                Q
            });
        }

        private string GetJsonPath()
            => Path.Combine(_env.WebRootPath, "data", "tareas.json");

        private List<Tarea> CargarTareas()
        {
            var path = GetJsonPath();
            if (!System.IO.File.Exists(path)) return new List<Tarea>();
            var json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Tarea>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Tarea>();
        }

        private void GuardarTareas(List<Tarea> tareas)
        {
            var path = GetJsonPath();
            var json = JsonSerializer.Serialize(tareas, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }
    }
}
