using CoralTime.BL.Interfaces;
using CoralTime.BL.Interfaces.Reports;
using CoralTime.BL.Services;
using CoralTime.BL.Services.Reports.DropDownsAndGrid;
using CoralTime.BL.Services.Reports.Export;
using CoralTime.Common.Attributes;
using CoralTime.Common.Constants;
using CoralTime.Common.Middlewares;
using CoralTime.DAL;
using CoralTime.DAL.Mapper;
using CoralTime.DAL.Models;
using CoralTime.DAL.Repositories;
using CoralTime.Services;
using CoralTime.ViewModels.Clients;
using CoralTime.ViewModels.Errors;
using CoralTime.ViewModels.Member;
using CoralTime.ViewModels.MemberProjectRoles;
using CoralTime.ViewModels.ProjectRole;
using CoralTime.ViewModels.Projects;
using CoralTime.ViewModels.Settings;
using CoralTime.ViewModels.Tasks;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using CoralTime.BL.Services.Notifications;
using CoralTime.ViewModels.MemberActions;
using Microsoft.IdentityModel.Logging;
using CoralTime.ViewModels.Vsts;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CoralTime
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            bool.TryParse(Configuration["UseMySql"], out var useMySql);
            if (useMySql)
            {
                // Add MySQL support (At first create DB on MySQL server.)
                services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(Configuration.GetConnectionString("DefaultConnectionMySQL"), ServerVersion.AutoDetect(Configuration.GetConnectionString("DefaultConnectionMySQL")),
                    b => b.MigrationsAssembly("CoralTime.MySqlMigrations")));
            }
            else
            {
                // Sql Server
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                    options.ConfigureWarnings(warnings =>
                                                         warnings.Default(WarningBehavior.Ignore)
                                                            .Log(CoreEventId.NavigationBaseIncludeIgnored));
                
                });
            }

            IdentityModelEventSource.ShowPII = true; 
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });

            AddApplicationServices(services);
            services.AddMemoryCache();
            services.AddAutoMapper(typeof(MappingProfile).Assembly);
            services.AddMvc();

            // Add OData
            services.AddControllers()
                    
                    .AddOData(opt => opt.AddRouteComponents("api/v1/odata",GetEdmModel()).Select().Filter().OrderBy().Expand().Count().SetMaxTop(null));

            services.AddMvcCore(options =>
            {
                options.EnableEndpointRouting = false;

                foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                {
                    outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                }
                foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                {
                    inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                }
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Insert(0, new TrimmingStringConverter());
            });

            SetupIdentity(services);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",new Microsoft.OpenApi.Models.OpenApiInfo { Title = "CoralTime", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [Obsolete]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            // Uses static file for the current path.
            SetupAngularRouting(app);

            app.UseDefaultFiles();

            // Uses static file for the current path.
            app.UseStaticFiles();


            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles")),
                RequestPath = "/StaticFiles"
            });

            app.UseIdentityServer();

            // Add middleware exceptions
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));

            //Make sure you add app.UseCors before app.UseMvc otherwise the request will be finished before the CORS middleware is applied
            app.UseCors("AllowAllOrigins");

            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoralTime V1");
            });


            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            Constants.EnvName = env.EnvironmentName;

            CombineFileWkhtmltopdf(env);

            AppDbContext.InitializeFirstTimeDataBaseAsync(app.ApplicationServices, Configuration).Wait();
        }

        private void AddApplicationServices(IServiceCollection services)
        {
            // Add application services.
            services.AddSingleton<IConfiguration>(sp => Configuration);

            services.AddScoped<BaseService>();
            services.AddScoped<UnitOfWork>();
            services.AddScoped<IPersistedGrantDbContext, AppDbContext>();

            services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();
            services.AddTransient<IdentityServer4.Services.IProfileService, IdentityWithAdditionalClaimsProfileService>();
            //services.AddTransient<IExtensionGrantValidator, AzureGrant>();
            //services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();

            services.AddScoped<IMemberService, MemberService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IMemberProjectRoleService, MemberProjectRoleService>();
            services.AddScoped<IMemberService, MemberService>();
            services.AddScoped<INotificationService, NotificationsService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ITasksService, TasksService>();
            services.AddScoped<ITimeEntryService, TimeEntryService>();
            services.AddScoped<IReportsService, ReportsService>();
            services.AddScoped<IReportExportService, ReportsExportService>();
            services.AddScoped<IReportsSettingsService, ReportsSettingsService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<CheckSecureHeaderServiceFilter>();
            services.AddScoped<CheckSecureHeaderNotificationFilter>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IMemberActionService, MemberActionService>();
            services.AddScoped<IVstsService, VstsService>();
            services.AddScoped<IVstsAdminService, VstsService>();
        }

        private static void SetupAngularRouting(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.HasValue && 
                    null != Constants.AngularRoutes.FirstOrDefault(ar => context.Request.Path.Value.StartsWith(ar, StringComparison.OrdinalIgnoreCase)) && 
                    !Path.HasExtension(context.Request.Path.Value))
                {
                    context.Request.Path = new PathString("/");

                    context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
                    context.Response.Headers.Add("Expires", "-1");
                }

                await next();
            });
        }

        private void SetupIdentity(IServiceCollection services)
        {
            var isDemo = bool.Parse(Configuration["DemoSiteMode"]);

            // Identity options.
            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                if (isDemo)
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                }
                else
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                }

                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            });

            var accessTokenLifetime = int.Parse(Configuration["AccessTokenLifetime"]);
            var refreshTokenLifetime = int.Parse(Configuration["RefreshTokenLifetime"]);
            var slidingRefreshTokenLifetime = int.Parse(Configuration["SlidingRefreshTokenLifetime"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Configuration["Authority"],
                ValidateAudience = true,
                ValidAudience = "WebAPI",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            if (isDemo)
            {
                services.AddIdentityServer()
                    .AddDeveloperSigningCredential()
                    .AddInMemoryIdentityResources(Config.GetIdentityResources())
                    .AddInMemoryApiScopes(Config.GetApiScopes())
                    .AddInMemoryApiResources(Config.GetApiResources())
                    .AddInMemoryClients(Config.GetClients(accessTokenLifetime: accessTokenLifetime, refreshTokenLifetime: refreshTokenLifetime, slidingRefreshTokenLifetime: slidingRefreshTokenLifetime))
                    .AddAspNetIdentity<ApplicationUser>()
                    .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
                    .AddProfileService<IdentityWithAdditionalClaimsProfileService>();
            }
            else
            {
                var cert = new X509Certificate2("coraltime.pfx", "", X509KeyStorageFlags.MachineKeySet);

                services.AddIdentityServer()
                    .AddInMemoryIdentityResources(Config.GetIdentityResources())
                    .AddInMemoryApiResources(Config.GetApiResources())
                    .AddInMemoryApiScopes(Config.GetApiScopes())
                    .AddInMemoryApiResources(Config.GetApiResources())
                    .AddInMemoryClients(Config.GetClients(accessTokenLifetime: accessTokenLifetime, refreshTokenLifetime: refreshTokenLifetime, slidingRefreshTokenLifetime: slidingRefreshTokenLifetime))
                    .AddAspNetIdentity<ApplicationUser>()
                    .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
                    .AddSigningCredential(cert)
                    .AddProfileService<IdentityWithAdditionalClaimsProfileService>()
                    .AddOperationalStore<AppDbContext>(options =>
                        {
                            options.EnableTokenCleanup = true;
                        }
                    );
                var key = new X509SecurityKey(cert);
                tokenValidationParameters.IssuerSigningKey = key;
                tokenValidationParameters.ValidateIssuerSigningKey = true;
                tokenValidationParameters.ValidateAudience = false;
            }


            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Bearer";
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
                options.DefaultSignInScheme = "Bearer";
                options.DefaultForbidScheme = "Identity.Application";
              

            }).AddJwtBearer(options =>
                {

                    options.Audience = "WebAPI";
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            // Add the access_token as a claim, as we may actually need it
                            var accessToken = context.SecurityToken as JwtSecurityToken;
                            if (accessToken != null)
                            {
                                ClaimsIdentity identity = context.Principal.Identity as ClaimsIdentity;
                                if (identity != null)
                                {
                                    identity.AddClaim(new Claim("access_token", accessToken.RawData));
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                    // name of the API resource
                    options.Audience = "WebAPI";
                    options.Authority = Configuration["Authority"];
                    options.RequireHttpsMetadata = false;
                 
                    options.TokenValidationParameters = tokenValidationParameters;
                });

            services.AddAuthorization(options =>
            {
                Config.CreateAuthorizatoinOptions(options);
            });
        }

        private static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ();
            builder.EntitySet<ClientView>("Clients");
            builder.EntitySet<ProjectView>("Projects");
            builder.EntitySet<MemberView>("Members");
            builder.EntitySet<MemberProjectRoleView>("MemberProjectRoles");
            builder.EntitySet<ProjectRoleView>("ProjectRoles");
            builder.EntitySet<TaskTypeView>("Tasks");
            builder.EntitySet<ErrorODataView>("Errors");
            builder.EntitySet<SettingsView>("Settings");
            builder.EntitySet<ManagerProjectsView>("ManagerProjects");
            builder.EntitySet<ProjectNameView>("ProjectsNames");
            builder.EntitySet<MemberActionView>("MemberActions");
            builder.EntitySet<VstsProjectIntegrationView>("VstsProjectIntegration");
            builder.EnableLowerCamelCase();
            return builder.GetEdmModel();
        }

        private void CombineFileWkhtmltopdf(IHostingEnvironment environment)
        {
            var fileNameWkhtmltopdf = "wkhtmltopdf.exe";
            var patchContentRoot = environment.ContentRootPath;

            var pathContentPDF = $"{patchContentRoot}\\Content\\PDF";
            var pathContentPDFSplitFile = $"{pathContentPDF}\\SplitFileWkhtmltopdf";

            var fileNotExist = !File.Exists(pathContentPDF + "\\" + fileNameWkhtmltopdf);
            if (fileNotExist)
            {
                var filePattern = "*.0**";
                var destFile = $"\"../{fileNameWkhtmltopdf}\"";

                var cmd = new ProcessStartInfo("cmd.exe", $@"/c copy /y /b {filePattern} {destFile}")
                {
                    WorkingDirectory = pathContentPDFSplitFile,
                    UseShellExecute = false
                };
                Process.Start(cmd);
            }
        }
    }
}