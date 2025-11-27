using Cost.Application;
using Cost.Infrastructure.Repositories;
using Cost.Infrastructure.Repositories.AFKDevelopment;
using Cost.Presentation.ReportsToExcel;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen(x => x.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".xml")));
builder.Services.AddScoped<GeneratingReports>();
builder.Services.AddScoped<ExportingReportsToExcel>();
builder.Services.Configure<Base1CConfiguration>(builder.Configuration.GetSection(Base1CConfiguration.Section));
//builder.Services.AddScoped<IGettingData, Cost.Infrastructure.Repositories.AFKDevelopment.GettingData>();


builder.Services.AddScoped<Func<string, IGettingData>>(serviceProvider => key =>
{
    return key switch
    {
        "A" => serviceProvider.GetRequiredService<GettingData>(),
        "B" => serviceProvider.GetRequiredService<GettingData>(),
        _ => throw new ArgumentException("Неверный ключ сервиса")
    };
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
