using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.ComponentModel.DataAnnotations;
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

        private readonly IWebHostEnvironment _env;
        public TareasModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void OnGet(int pageNumber = 1)
        {
            // lee json
            var filePath = GetJsonPath();
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

        // -------------------- CREAR TAREA --------------------

        [BindProperty]
        public NuevaTareaInput NuevaTarea { get; set; } = new();

        public class NuevaTareaInput
        {
            [Required, StringLength(120)]
            public string? NombreTarea { get; set; }

            [Required, DataType(DataType.Date)]
            public DateTime? Fecha { get; set; } // viene del <input type="date">

            [Required]
            public string? Estado { get; set; } // Pendiente | En curso | Finalizado | Cancelado
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostCreate()
        {
            if (!ModelState.IsValid)
            {
                // Recargar datos para que la vista se renderice correctamente
                OnGet(1);
                return Page();
            }

            var listado = CargarTareas();

            // Convertimos la fecha al formato de tu JSON dd/MM/yyyy
            var fecha = NuevaTarea.Fecha!.Value.ToString("dd/MM/yyyy");

            // Insertar al inicio para que aparezca arriba
            listado.Insert(0, new Tarea
            {
                NombreTarea = NuevaTarea.NombreTarea!.Trim(),
                FechaVencimiento = fecha,
                Estado = NuevaTarea.Estado!
            });

            GuardarTareas(listado);

            TempData["ok"] = "Tarea creada correctamente";

            // Volvemos a GET manteniendo filtros/tamaño/búsqueda.
            return RedirectToPage(new
            {
                pageNumber = 1,                  // o PageNumber si quieres mantener la página actual
                SelectedSize,
                FilterEstado,
                Q
            });
        }

        // Utilidades JSON
        private string GetJsonPath()
            => Path.Combine(_env.WebRootPath, "data", "tareas.json");

        private List<Tarea> CargarTareas()
        {
            var path = GetJsonPath();
            if (!System.IO.File.Exists(path)) return new List<Tarea>();
            var json = System.IO.File.ReadAllText(path);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Tarea>>(json, opts) ?? new List<Tarea>();
        }

        private void GuardarTareas(List<Tarea> tareas)
        {
            var path = GetJsonPath();
            var json = JsonSerializer.Serialize(tareas, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }
    }
}
