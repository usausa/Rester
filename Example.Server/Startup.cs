namespace Example.Server
{
    using System.Linq;

    using Example.Server.Areas.Api;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.ResponseCompression;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    using Smart.AspNetCore;
    using Smart.AspNetCore.ApplicationModels;

    using Swashbuckle.AspNetCore.Swagger;

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options =>
                {
                    options.Conventions.Add(new LowercaseControllerModelConvention());
                })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
                options.EnableForHttps = true;
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("example", new Info { Title = "Example API", Version = "v1" });
                options.DescribeAllEnumsAsStrings();    // Enum
                options.OperationFilter<FormFileOperationFilter>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseResponseCompression();

            app.UseRequestDecompress();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/example/swagger.json", "Example API");
            });
        }
    }
}
