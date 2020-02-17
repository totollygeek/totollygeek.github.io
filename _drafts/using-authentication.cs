public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.Audience = Configuration["AzureAd:ClientId"];
				options.Authority =
					$"{Configuration["AzureAd:Instance"]}{Configuration["AzureAd:TenantId"]}";
			})
			.AddAzureAD(options => Configuration.Bind("AzureAd", options));

		services.AddAuthorization(options =>
		{
			var defaultAuthorizationPolicyBuilder =
				new AuthorizationPolicyBuilder(
					JwtBearerDefaults.AuthenticationScheme,
					AzureADDefaults.AuthenticationScheme);

			defaultAuthorizationPolicyBuilder =
				defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
			options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
		});

		services.AddMvc(options =>
		{
			var policy = new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build();
			options.Filters.Add(new AuthorizeFilter(policy));
		})
		.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
	}

	public void Configure(IApplicationBuilder app, IHostingEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		else
		{
			app.UseHsts();
		}

		app.UseHttpsRedirection();
		app.UseStaticFiles();
		app.UseCookiePolicy();
		app.UseAuthentication();
		app.UseMvc(routes =>
		{
			routes.MapRoute(
				name: "default",
				template: "{controller=Home}/{action=Index}/{id?}");
		});
	}
}
