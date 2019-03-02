using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.Clients.Chucklefish;
using StardewModdingAPI.Web.Framework.Clients.GitHub;
using StardewModdingAPI.Web.Framework.Clients.ModDrop;
using StardewModdingAPI.Web.Framework.Clients.Nexus;
using StardewModdingAPI.Web.Framework.Clients.Pastebin;
using StardewModdingAPI.Web.Framework.ConfigModels;
using StardewModdingAPI.Web.Framework.RewriteRules;

namespace StardewModdingAPI.Web
{
    /// <summary>The web app startup configuration.</summary>
    internal class Startup
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The web app configuration.</summary>
        public IConfigurationRoot Configuration { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="env">The hosting environment.</param>
        public Startup(IHostingEnvironment env)
        {
            this.Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .Add(new BeanstalkEnvPropsConfigProvider())
                .Build();
        }

        /// <summary>The method called by the runtime to add services to the container.</summary>
        /// <param name="services">The service injection container.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // init configuration
            services
                .Configure<ModCompatibilityListConfig>(this.Configuration.GetSection("ModCompatibilityList"))
                .Configure<ModUpdateCheckConfig>(this.Configuration.GetSection("ModUpdateCheck"))
                .Configure<SiteConfig>(this.Configuration.GetSection("Site"))
                .Configure<RouteOptions>(options => options.ConstraintMap.Add("semanticVersion", typeof(VersionConstraint)))
                .AddMemoryCache()
                .AddMvc()
                .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new InternalControllerFeatureProvider()))
                .AddJsonOptions(options =>
                {
                    foreach (JsonConverter converter in new JsonHelper().JsonSettings.Converters)
                        options.SerializerSettings.Converters.Add(converter);

                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            // init API clients
            {
                ApiClientsConfig api = this.Configuration.GetSection("ApiClients").Get<ApiClientsConfig>();
                string version = this.GetType().Assembly.GetName().Version.ToString(3);
                string userAgent = string.Format(api.UserAgent, version);

                services.AddSingleton<IChucklefishClient>(new ChucklefishClient(
                    userAgent: userAgent,
                    baseUrl: api.ChucklefishBaseUrl,
                    modPageUrlFormat: api.ChucklefishModPageUrlFormat
                ));

                services.AddSingleton<IGitHubClient>(new GitHubClient(
                    baseUrl: api.GitHubBaseUrl,
                    stableReleaseUrlFormat: api.GitHubStableReleaseUrlFormat,
                    anyReleaseUrlFormat: api.GitHubAnyReleaseUrlFormat,
                    userAgent: userAgent,
                    acceptHeader: api.GitHubAcceptHeader,
                    username: api.GitHubUsername,
                    password: api.GitHubPassword
                ));

                services.AddSingleton<IModDropClient>(new ModDropClient(
                    userAgent: userAgent,
                    apiUrl: api.ModDropApiUrl,
                    modUrlFormat: api.ModDropModPageUrl
                ));

                services.AddSingleton<INexusClient>(new NexusWebScrapeClient(
                    userAgent: userAgent,
                    baseUrl: api.NexusBaseUrl,
                    modUrlFormat: api.NexusModUrlFormat,
                    modScrapeUrlFormat: api.NexusModScrapeUrlFormat
                ));

                services.AddSingleton<IPastebinClient>(new PastebinClient(
                    baseUrl: api.PastebinBaseUrl,
                    userAgent: userAgent,
                    userKey: api.PastebinUserKey,
                    devKey: api.PastebinDevKey
                ));
            }
        }

        /// <summary>The method called by the runtime to configure the HTTP request pipeline.</summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting environment.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseCors(policy => policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithOrigins("https://smapi.io", "https://*.smapi.io", "https://*.edge.smapi.io")
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                )
                .UseRewriter(this.GetRedirectRules())
                .UseStaticFiles() // wwwroot folder
                .UseMvc();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the redirect rules to apply.</summary>
        private RewriteOptions GetRedirectRules()
        {
            var redirects = new RewriteOptions();

            // redirect to HTTPS (except API for Linux/Mac Mono compatibility)
            redirects.Add(new ConditionalRedirectToHttpsRule(
                shouldRewrite: req =>
                    req.Host.Host != "localhost"
                    && !req.Path.StartsWithSegments("/api")
                    && !req.Host.Host.StartsWith("api.")
            ));

            // convert subdomain.smapi.io => smapi.io/subdomain for routing
            redirects.Add(new ConditionalRewriteSubdomainRule(
                shouldRewrite: req =>
                    req.Host.Host != "localhost"
                    && (req.Host.Host.StartsWith("api.") || req.Host.Host.StartsWith("log.") || req.Host.Host.StartsWith("mods."))
                    && !req.Path.StartsWithSegments("/content")
                    && !req.Path.StartsWithSegments("/favicon.ico")
            ));

            // shortcut redirects
            redirects.Add(new RedirectToUrlRule(@"^/3\.0\.?$", "https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_3.0"));
            redirects.Add(new RedirectToUrlRule(@"^/buildmsg(?:/?(.*))$", "https://github.com/Pathoschild/SMAPI/blob/develop/docs/mod-build-config.md#$1"));
            redirects.Add(new RedirectToUrlRule(@"^/compat\.?$", "https://mods.smapi.io"));
            redirects.Add(new RedirectToUrlRule(@"^/docs\.?$", "https://stardewvalleywiki.com/Modding:Index"));
            redirects.Add(new RedirectToUrlRule(@"^/install\.?$", "https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started#Install_SMAPI"));
            redirects.Add(new RedirectToUrlRule(@"^/troubleshoot(.*)$", "https://stardewvalleywiki.com/Modding:Player_Guide/Troubleshooting$1"));
            redirects.Add(new RedirectToUrlRule(@"^/xnb\.?$", "https://stardewvalleywiki.com/Modding:Using_XNB_mods"));

            // redirect legacy canimod.com URLs
            var wikiRedirects = new Dictionary<string, string[]>
            {
                ["Modding:Index#Migration_guides"] = new[] { "^/for-devs/updating-a-smapi-mod", "^/guides/updating-a-smapi-mod" },
                ["Modding:Modder_Guide"] = new[] { "^/for-devs/creating-a-smapi-mod", "^/guides/creating-a-smapi-mod", "^/for-devs/creating-a-smapi-mod-advanced-config" },
                ["Modding:Player_Guide"] = new[] { "^/for-players/install-smapi", "^/guides/using-mods", "^/for-players/faqs", "^/for-players/intro", "^/for-players/use-mods", "^/guides/asking-for-help", "^/guides/smapi-faq" },

                ["Modding:Editing_XNB_files"] = new[] { "^/for-devs/creating-an-xnb-mod", "^/guides/creating-an-xnb-mod" },
                ["Modding:Event_data"] = new[] { "^/for-devs/events", "^/guides/events" },
                ["Modding:Gift_taste_data"] = new[] { "^/for-devs/npc-gift-tastes", "^/guides/npc-gift-tastes" },
                ["Modding:IDE_reference"] = new[] { "^/for-devs/creating-a-smapi-mod-ide-primer" },
                ["Modding:Object_data"] = new[] { "^/for-devs/object-data", "^/guides/object-data" },
                ["Modding:Weather_data"] = new[] { "^/for-devs/weather", "^/guides/weather" }
            };
            foreach (KeyValuePair<string, string[]> pair in wikiRedirects)
            {
                foreach (string pattern in pair.Value)
                    redirects.Add(new RedirectToUrlRule(pattern, "https://stardewvalleywiki.com/" + pair.Key));
            }

            return redirects;
        }
    }
}
