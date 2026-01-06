using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _360Retail.Services.Sales.Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace _360Retail.Services.Sales.Infrastructure.Services
{
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryStorageService(IConfiguration config)
        {
            // Lấy thông tin từ appsettings.json
            var cloudName = config["Cloudinary:CloudName"];
            var apiKey = config["Cloudinary:ApiKey"];
            var apiSecret = config["Cloudinary:ApiSecret"];

            // Khởi tạo Account Cloudinary
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return null;

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folderName, // Cloudinary tự tạo folder nếu chưa có
                    PublicId = Guid.NewGuid().ToString(), // Đặt tên file ngẫu nhiên để không trùng
                    Overwrite = true
                };

                // Upload lên Cloudinary
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                // Trả về đường dẫn ảnh (SecureUrl là link https)
                return uploadResult.SecureUrl.ToString();
            }
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            try
            {
                // Logic lấy PublicId từ URL để xóa
                // URL ví dụ: https://res.cloudinary.com/demo/image/upload/v123456/products/my-image.jpg
                // Cần lấy: "products/my-image"

                var uri = new Uri(fileUrl);
                var pathSegments = uri.AbsolutePath.Split('/');

                // Lấy phần cuối cùng (tên file + đuôi) và phần trước nó (folder)
                // Đây là cách xử lý đơn giản, thực tế có thể cần regex
                var fileName = pathSegments.Last();
                var folder = pathSegments[pathSegments.Length - 2];
                var publicId = $"{folder}/{Path.GetFileNameWithoutExtension(fileName)}";

                var deletionParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deletionParams);
            }
            catch
            {
                // Bỏ qua lỗi nếu xóa thất bại (để không crash app)
            }
        }
    }
}
