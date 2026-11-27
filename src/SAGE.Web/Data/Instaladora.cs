using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SAGE.Web.Data;

public class Instaladora
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Precision(18, 4)]
    public decimal BsPorKwp { get; set; }

    [Precision(18, 4)]
    public decimal MantenimientoBsKwpAnio { get; set; }

    public bool Activa { get; set; } = true;
}
