using dotenv.net;
using RingCentral;

var builder = WebApplication.CreateBuilder(args);


DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 5));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// this is JWT workflow for one user, most people with an app with use 3 legged oauth for multiple clients
var rc = new RestClient(
    clientId: Environment.GetEnvironmentVariable("RC_APP_CLIENT_ID"),
    clientSecret: Environment.GetEnvironmentVariable("RC_APP_CLIENT_SECRET"),
    server: "https://platform.ringcentral.com",
    appName: "RingCentralAPIDemo",
    appVersion: "1.0.0"
); // easiest way here is to just use the rest client from RC

await rc.Authorize(Environment.GetEnvironmentVariable("RC_USER_JWT")); // we want an instance of the client to rc to auth after

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Use(async (context, next) => //add simple anonymous middleware here to make sure the user-agent header is set
{
    if (string.IsNullOrWhiteSpace(context.Request.Headers.UserAgent))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("User-Agent header is required.");
        return;
    }

    await next(context);
});

// Address Book Contact Endpoints
app.MapGet("/api/contacts", async (string? startsWith, int? page, int? perPage) =>
{
    var queryParams = new ListContactsParameters();
    if (!string.IsNullOrWhiteSpace(startsWith)) queryParams.startsWith = startsWith;
    if (page.HasValue) queryParams.page = page.Value;
    if (perPage.HasValue) queryParams.perPage = perPage.Value;

    var contacts = await rc.Restapi().Account().Extension().AddressBook().Contact().List(queryParams);
    return Results.Ok(contacts);
});

app.MapGet("/api/contacts/{contactId}", async (string contactId) =>
{
    var contact = await rc.Restapi().Account().Extension().AddressBook().Contact(contactId).Get();
    return Results.Ok(contact);
});

app.MapPost("/api/contacts", async (PersonalContactRequest newContact) =>
{
    var created = await rc.Restapi().Account().Extension().AddressBook().Contact().Post(newContact);
    return Results.Created($"/api/contacts/{created.id}", created);
});

app.MapPut("/api/contacts/{contactId}", async (string contactId, PersonalContactRequest updateRequest) =>
{
    var updated = await rc.Restapi().Account().Extension().AddressBook().Contact(contactId).Put(updateRequest);
    return Results.Ok(updated);
});

app.MapDelete("/api/contacts/{contactId}", async (string contactId) =>
{
    await rc.Restapi().Account().Extension().AddressBook().Contact(contactId).Delete();
    return Results.NoContent();
});

// Extension Information
app.MapGet("/api/extension", async () =>
{
    var extension = await rc.Restapi().Account().Extension().Get();
    return Results.Ok(extension);
});

// RingOut (Call) Endpoints
app.MapPost("/api/ringout", async (MakeRingOutRequest request) =>
{
    var ringout = await rc.Restapi().Account().Extension().RingOut().Post(request);
    return Results.Ok(ringout);
});

app.MapGet("/api/ringout/{ringoutId}", async (string ringoutId) =>
{
    var ringout = await rc.Restapi().Account().Extension().RingOut(ringoutId).Get();
    return Results.Ok(ringout);
});

app.MapDelete("/api/ringout/{ringoutId}", async (string ringoutId) =>
{
    await rc.Restapi().Account().Extension().RingOut(ringoutId).Delete();
    return Results.NoContent();
});

app.Run();


