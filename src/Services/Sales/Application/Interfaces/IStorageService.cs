using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; 

namespace _360Retail.Services.Sales.Application.Interfaces
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        Task DeleteFileAsync(string fileUrl);
    }
}
