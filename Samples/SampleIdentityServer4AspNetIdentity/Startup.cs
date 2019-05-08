using IdentityServer4;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SampleIdentityServer4AspNetIdentity.Data;
using SampleIdentityServer4AspNetIdentity.Models;
using SampleIdentityServer4AspNetIdentity.Services;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Metadata;
using System.Security.Cryptography.X509Certificates;

namespace SampleIdentityServer4AspNetIdentity
{
	public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
			HostingEnvironment = hostingEnvironment;
		}

        public IConfiguration Configuration { get; }
		public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

			services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			// Add application services.
			services.AddTransient<IEmailSender, EmailSender>();
			services.AddMvc();

			services.AddIdentityServer()
				.AddDeveloperSigningCredential()
				.AddInMemoryPersistedGrants()
				.AddInMemoryIdentityResources(Config.GetIdentityResources())
				.AddInMemoryApiResources(Config.GetApiResources())
				.AddInMemoryClients(Config.GetClients())
				.AddTestUsers(Config.GetUsers())
				.AddAspNetIdentity<ApplicationUser>();

			services.AddAuthentication()
				.AddSaml2("Saml2", options =>
				{
					options.SPOptions.EntityId = new EntityId("http://localhost:5000/Saml2");
                    options.SPOptions.ReturnUrl = new System.Uri("http://localhost:5000/Account/SamlLoginCallback");
                    var idp = new IdentityProvider(
                            new EntityId("https://app.onelogin.com/saml/metadata/752b381f-bfbb-4d86-be5e-d072190e71da"), options.SPOptions)
                    {
                        LoadMetadata = true,
                        AllowUnsolicitedAuthnResponse = true
                    };
                    idp.SigningKeys.AddConfiguredKey(new X509Certificate2("onelogin.pem"));
                    options.SignInScheme = IdentityServerConstants.DefaultCookieAuthenticationScheme;
                    options.IdentityProviders.Add(idp);
					//options.SPOptions.ServiceCertificates.Add(new X509Certificate2("onelogin.pem"));
					//options.SPOptions.ServiceCertificates.Add(new X509Certificate2(
					//	HostingEnvironment.ContentRootPath + "\\App_Data\\Sustainsys.Saml2.SampleIdentityServer4AspNetIdentity.pfx"));
				})
				.AddGoogle("Google", options =>
				{
					options.ClientId = "708996912208-9m4dkjb5hscn7cjrn5u0r4tbgkbj1fko.apps.googleusercontent.com";
					options.ClientSecret = "wdfPY6t8H8cecgjlxud__4Gh";
				})
				.AddOpenIdConnect("demoidsrv", "IdentityServer", options =>
				{
					options.Authority = "https://demo.identityserver.io/";
					options.ClientId = "implicit";
					options.ResponseType = "id_token";
					options.SaveTokens = true;
					options.CallbackPath = new PathString("/signin-idsrv");
					options.SignedOutCallbackPath = new PathString("/signout-callback-idsrv");
					options.RemoteSignOutPath = new PathString("/signout-idsrv");

					options.TokenValidationParameters = new TokenValidationParameters
					{
						NameClaimType = "name",
						RoleClaimType = "role"
					};
				});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env,
			ApplicationDbContext dbContext)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
			app.UseIdentityServer();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

			dbContext.Database.EnsureCreated();
		}
    }
}
