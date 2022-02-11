using System.Text.Json;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Classeur.Core;
using Classeur.Core.CustomizableStructure;
using Classeur.Core.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using SwissKnife.Serverless;
using SwissKnife.Serverless.Services;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
       .AddScoped(_ => new HttpClient
       {
           BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
       })
       .AddBlazorise(options =>
       {
           options.ChangeTextOnKeyPress = true;
       })
       .AddBootstrapProviders()
       .AddFontAwesomeIcons()
       .AddSingleton(new JsonSerializerOptions
       {
           Converters =
           {
               StructureSchemaJsonConverter.Instance,
               IncoherentIdJsonConverter.Instance,
               FieldKeyJsonConverter.Instance,
               FieldTypeJsonConverter.Instance,
           },
       })
       .AddSingleton<IRepository<StructureSchema, string>>(sp =>
       {
           return new LocalStorageRepository<StructureSchema>(sp.GetRequiredService<IJSRuntime>(),
                                                              "schemas",
                                                              sp.GetRequiredService<JsonSerializerOptions>());
       });

await builder.Build().RunAsync();
