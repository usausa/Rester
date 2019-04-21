namespace Example.Server.Areas.Api
{
    using System.Linq;

    using Microsoft.AspNetCore.Http;

    using Swashbuckle.AspNetCore.Swagger;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public sealed class FormFileOperationFilter : IOperationFilter
    {
        private const string FormDataMimeType = "multipart/form-data";

        private static readonly string[] FormFilePropertyNames = typeof(IFormFile).GetProperties().Select(p => p.Name).ToArray();

        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                return;
            }

            var formFileParameters = context.ApiDescription.ActionDescriptor.Parameters
                .Where(x => x.ParameterType.IsAssignableFrom(typeof(IFormFile)))
                .Select(x => x.Name);
            var formFileSubParameters = context.ApiDescription.ActionDescriptor.Parameters
                .SelectMany(x => x.ParameterType.GetProperties())
                .Where(x => x.PropertyType.IsAssignableFrom(typeof(IFormFile)))
                .Select(x => x.Name);

            var allFileParamNames = formFileParameters.Union(formFileSubParameters).ToList();
            if (!allFileParamNames.Any())
            {
                return;
            }

            var removes = operation.Parameters.Where(x => FormFilePropertyNames.Contains(x.Name)).ToList();
            foreach (var remove in removes)
            {
                operation.Parameters.Remove(remove);
            }

            foreach (var paramName in allFileParamNames)
            {
                var fileParam = new NonBodyParameter
                {
                    Type = "file",
                    Name = paramName,
                    In = "formData"
                };
                operation.Parameters.Add(fileParam);
            }

            operation.Consumes.Add(FormDataMimeType);
        }
    }
}
