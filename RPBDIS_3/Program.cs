using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using RPBDIS_3.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Подключаем DbContext к строке подключения из appsettings.json
builder.Services.AddDbContext<MonitoringContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем поддержку кэширования и сессий
builder.Services.AddMemoryCache();
builder.Services.AddSession();

// Настраиваем контроллеры с поддержкой ReferenceHandler.Preserve
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Устанавливаем время жизни сессии
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(); // Добавляем поддержку MVC

var app = builder.Build();

// Включаем сессии и маршрутизацию
app.UseSession();

app.MapGet("/searchform1", async context =>
{
    // Получаем значения из куков и подставляем их в HTML-форму
    var savedData = context.Request.Cookies;
    var name = savedData["name"] ?? "";
    var age = savedData["age"] ?? "";

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync($@"
        <form action=""/searchform1"" method=""post"">
            <label>Name: <input type=""text"" name=""name"" value=""{name}"" /></label><br/>
            <label>Age: <input type=""number"" name=""age"" value=""{age}"" /></label><br/>
            <input type=""submit"" value=""Save to Cookies"" />
        </form>
    ");
});

app.MapPost("/searchform1", async context =>
{
    // Получаем данные из формы
    var form = context.Request.Form;
    var name = form["name"].ToString();
    var age = form["age"].ToString();
    context.Response.ContentType = "text/html";

    // Сохраняем данные в куки
    context.Response.Cookies.Append("name", name, new CookieOptions { Expires = DateTimeOffset.Now.AddDays(1) });
    context.Response.Cookies.Append("age", age, new CookieOptions { Expires = DateTimeOffset.Now.AddDays(1) });

    await context.Response.WriteAsync("Data saved to cookies! <a href=\"/searchform1\">Go back</a>");
});
app.MapGet("/searchform2", async context =>
{
    // Получаем данные из сессии и подставляем их в HTML-форму
    var name = context.Session.GetString("name") ?? "";
    var age = context.Session.GetString("age") ?? "";

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync($@"
        <form action=""/searchform2"" method=""post"">
            <label>Name: <input type=""text"" name=""name"" value=""{name}"" /></label><br/>
            <label>Age: <input type=""number"" name=""age"" value=""{age}"" /></label><br/>
            <input type=""submit"" value=""Save to Session"" />
        </form>
    ");
});

app.MapPost("/searchform2", async context =>
{
    // Получаем данные из формы
    var form = context.Request.Form;
    var name = form["name"].ToString();
    var age = form["age"].ToString();
    context.Response.ContentType = "text/html";

    // Сохраняем данные в сессии
    context.Session.SetString("name", name);
    context.Session.SetString("age", age);

    await context.Response.WriteAsync("Data saved to session! <a href=\"/searchform2\">Go back</a>");
});
app.UseRouting();

// Настраиваем middleware для кэширования данных
app.Use(async (context, next) =>
{
    var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
    if (!cache.TryGetValue("cachedData", out Dictionary<string, object> cachedData))
    {
        cachedData = new Dictionary<string, object>();

        using (var scope = context.RequestServices.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringContext>();

            // Кэшируем 20 записей из каждой таблицы
            cachedData["Equipments"] = dbContext.Equipments.Take(20).ToList();
            cachedData["Employees"] = dbContext.Employees.Take(20).ToList();
            cachedData["CompletedWorks"] = dbContext.CompletedWorks.Select(cw => new
            {
                cw.CompletedMaintenanceId,
                cw.MaintenanceTypeId,
                cw.EquipmentId,
                cw.CompletionDate,
                cw.ResponsibleEmployeeId,
                cw.ActualCost
            }).Take(20).ToList();
            cachedData["MaintenanceTypes"] = dbContext.MaintenanceTypes.Take(20).ToList();
            cachedData["MaintenanceSchedules"] = dbContext.MaintenanceSchedules
                .Select(ms => new
                {
                    ms.ScheduleId,
                    ms.EquipmentId,
                    ms.MaintenanceTypeId,
                    ms.ScheduledDate,
                    ms.ResponsibleEmployeeId,
                    ms.EstimatedCost
                })
                .Take(20)
                .ToList();
        }

        var cacheDuration = TimeSpan.FromSeconds(2 * 20 + 240);
        cache.Set("cachedData", cachedData, cacheDuration);
    }

    context.Items["cachedData"] = cachedData;
    await next();
});

// Определяем маршруты для запросов
app.MapGet("/info", async context =>
{
    var clientInfo = $"IP: {context.Connection.RemoteIpAddress}, Agent: {context.Request.Headers["User-Agent"]}";
    await context.Response.WriteAsync(clientInfo);
});


app.MapGet("/data/{tableName}", async context =>
{
    var tableName = (string)context.Request.RouteValues["tableName"];
    var cachedData = context.Items["cachedData"] as Dictionary<string, object>;

    if (cachedData != null && cachedData.ContainsKey(tableName))
    {
        var dataList = cachedData[tableName] as IEnumerable<object>;
        if (dataList == null || !dataList.Any())
        {
            await context.Response.WriteAsync($"No data found for table {tableName}");
            return;
        }

        context.Response.ContentType = "text/html";

        var firstItem = dataList.First();
        var itemType = firstItem.GetType();

        var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string));

        var html = $"<h2>Data from {tableName}</h2><table border='1' style='width:100%; border-collapse:collapse;'><thead><tr>";

        foreach (var property in properties)
        {
            html += $"<th>{property.Name}</th>";
        }

        html += "</tr></thead><tbody>";

        foreach (var item in dataList)
        {
            html += "<tr>";
            foreach (var property in properties)
            {
                var value = property.GetValue(item);
                html += $"<td>{value ?? "N/A"}</td>";
            }
            html += "</tr>";
        }

        html += "</tbody></table>";

        await context.Response.WriteAsync(html);
    }
    else
    {
        await context.Response.WriteAsync($"Table {tableName} data not found or not cached.");
    }
});


// Используем MVC маршрутизацию
app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
});

app.Run();
