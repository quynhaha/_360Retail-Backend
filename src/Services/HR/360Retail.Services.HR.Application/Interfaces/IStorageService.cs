using Microsoft.AspNetCore.Http;

namespace _360Retail.Services.HR.Application.Interfaces;

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folderName);
    Task DeleteFileAsync(string fileUrl);
}
