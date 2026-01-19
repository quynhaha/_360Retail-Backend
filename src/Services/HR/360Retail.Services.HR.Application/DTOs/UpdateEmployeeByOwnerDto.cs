namespace _360Retail.Services.HR.Application.DTOs;

/// <summary>
/// DTO for Owner to update employee information (salary, position, status)
/// Only Owner can use this - Manager cannot
/// </summary>
public class UpdateEmployeeByOwnerDto
{
    public string? FullName { get; set; }
    
    /// <summary>
    /// Position: Staff, Manager
    /// </summary>
    public string? Position { get; set; }
    
    public decimal? BaseSalary { get; set; }
    
    /// <summary>
    /// IsActive: true/false (soft delete)
    /// </summary>
    public bool? IsActive { get; set; }
}
