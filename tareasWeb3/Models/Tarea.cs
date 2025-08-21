namespace tareasWeb3.Models
{
    // modelo simple para mapear tu json
    public class Tarea
    {
        public string NombreTarea { get; set; } = "";
        public string FechaVencimiento { get; set; } = "";
        public string Estado { get; set; } = "";
    }
}
