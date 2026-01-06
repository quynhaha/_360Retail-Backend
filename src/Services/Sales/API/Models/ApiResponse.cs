namespace _360Retail.Services.Sales.API.Wrappers
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        public ApiResponse() { }

        // Constructor cho trường hợp thành công
        public ApiResponse(T data, string message = null)
        {
            Success = true;
            Message = message ?? "Succeeded";
            Data = data;
        }

        // Constructor cho trường hợp thất bại
        public ApiResponse(string message)
        {
            Success = false;
            Message = message;
        }
    }
}