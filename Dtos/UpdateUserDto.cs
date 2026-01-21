using System.ComponentModel.DataAnnotations;

namespace UserManagementApi.Dtos;

public class UpdateUserDto
{
    [Required, MinLength(2), MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Range(0, 130)]
    public int Age { get; set; }
}
