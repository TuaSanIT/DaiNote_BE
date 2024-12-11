using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace dai.core.Mapping;

public class JsonModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (string.IsNullOrEmpty(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        try
        {
            var result = JsonConvert.DeserializeObject(value, bindingContext.ModelType);
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Invalid JSON format for {bindingContext.ModelName}");
        }

        return Task.CompletedTask;
    }
}
