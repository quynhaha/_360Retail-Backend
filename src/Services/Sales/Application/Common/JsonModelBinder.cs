using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

namespace _360Retail.Services.Sales.Application.Common
{
    public class JsonModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;
            Console.WriteLine($"[DEBUG] JsonModelBinder - Looking for field: '{modelName}'");

            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            
            if (valueProviderResult == ValueProviderResult.None)
            {
                Console.WriteLine($"[DEBUG] JsonModelBinder - Field '{modelName}' NOT FOUND in request!");
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            Console.WriteLine($"[DEBUG] JsonModelBinder - Field '{modelName}' value: {value?.Substring(0, Math.Min(value?.Length ?? 0, 200))}");

            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"[DEBUG] JsonModelBinder - Field '{modelName}' is empty!");
                return Task.CompletedTask;
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize(value, bindingContext.ModelType, options);
                Console.WriteLine($"[DEBUG] JsonModelBinder - Successfully deserialized '{modelName}'");
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[DEBUG] JsonModelBinder - JSON parse error for '{modelName}': {ex.Message}");
                bindingContext.ModelState.TryAddModelError(modelName, "Invalid JSON format.");
            }

            return Task.CompletedTask;
        }
    }
}

