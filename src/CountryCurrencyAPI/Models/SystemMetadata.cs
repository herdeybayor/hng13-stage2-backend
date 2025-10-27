using System.ComponentModel.DataAnnotations;

namespace CountryCurrencyAPI.Models;

public class SystemMetadata
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string KeyName { get; set; }

    public string? KeyValue { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }
}