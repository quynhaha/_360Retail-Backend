using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace _360Retail.Services.Sales.Application.Common
{
    public class JsonModelBinder : IModelBinder
    {
        private readonly ILogger<JsonModelBinder> _logger;

        public JsonModelBinder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<JsonModelBinder>();
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;
            _logger.LogDebug("JsonModelBinder - Looking for field: '{ModelName}'", modelName);

            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            
            if (valueProviderResult == ValueProviderResult.None)
            {
                _logger.LogDebug("JsonModelBinder - Field '{ModelName}' NOT FOUND in request", modelName);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            _logger.LogDebug("JsonModelBinder - Field '{ModelName}' received", modelName);

            if (string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("JsonModelBinder - Field '{ModelName}' is empty", modelName);
                return Task.CompletedTask;
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize(value, bindingContext.ModelType, options);
                _logger.LogDebug("JsonModelBinder - Successfully deserialized '{ModelName}'", modelName);
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JsonModelBinder - JSON parse error for '{ModelName}'", modelName);
                bindingContext.ModelState.TryAddModelError(modelName, "Invalid JSON format.");
            }

            return Task.CompletedTask;
        }
    }
}
