using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CountryCurrencyAPI.Models;

public class Country
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [MaxLength(255)]
    public string? Capital { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [Required]
    public required long Population { get; set; }

    [MaxLength(10)]
    public required string CurrencyCode { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public required decimal ExchangeRate { get; set; }

    [Column(TypeName = "decimal(20,2)")]
    public decimal? EstimatedGdp { get; set; }

    [MaxLength(500)]
    public string? FlagUrl { get; set; }

    [Required]
    public DateTime LastRefreshedAt { get; set; }
}