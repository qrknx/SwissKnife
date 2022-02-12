using System.Collections.Immutable;
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
using SwissKnife.Serverless.Pages.TemplatesPage.Types;
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
               new StructureSchemaJsonConverter(ImmutableDictionary<string, Type>.Empty
                                                    .Add("String", typeof(StringFieldType))
                                                    .Add("Int64", typeof(Int64FieldType))),
               IncoherentIdJsonConverter.Instance,
               FieldKeyJsonConverter.Instance,
           },
       })
       .AddSingleton(_ => new List<KnownFieldTypeDescription>
       {
           new("String", typeof(StringUIFieldType)),
           new("Int64", typeof(Int64UIFieldType)),
       })
       .AddSingleton<IRepository<StructureSchema, string>>(sp =>
       {
           return new LocalStorageRepository<StructureSchema>(sp.GetRequiredService<IJSRuntime>(),
                                                              collectionId: "schemas",
                                                              sp.GetRequiredService<JsonSerializerOptions>());
       });

await builder.Build().RunAsync();
