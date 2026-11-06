using System.ComponentModel.DataAnnotations;

namespace SAGE.Web.Components.Pages.Dispositivos;

public class DispositivoFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Seleccioná una empresa.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná una empresa válida.")]
    public int? EmpresaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }

    public bool EsSimulado { get; set; } = true;
}