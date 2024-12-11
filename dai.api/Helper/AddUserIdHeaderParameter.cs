using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace dai.api.Helper
{
    public class AddUserIdHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "UserId",
                In = ParameterLocation.Header,
                Description = "User ID for authorization",
                Required = false, // Optional for non-protected endpoints
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }
    }
}
