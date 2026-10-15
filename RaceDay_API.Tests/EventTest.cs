using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay_API.Tests;

public class EventTest : ApiTestBase
{
    public EventTest(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task GetEvents_Public_Returns200AndList()
    {
        var response = await Client.GetAsync("/api/Events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOptions);
        Assert.NotNull(events);
        Assert.True(events.Length >= 0);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        await AuthenticateAsOrganiserAsync();

        // Assume event ID 1 belongs to the seeded organiser
        var updateDto = new
        {
            Name = "Updated Soweto Marathon",
            Description = "Updated description",
            Location = "Nasrec, Johannesburg"
        };

        var response = await Client.PutAsJsonAsync("/api/Events/1", updateDto);
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Organiser_CannotUpdateOtherOrganiserEvent_Returns403()
    {
        await AuthenticateAsOrganiserAsync(); // Thabo

        // Assume event ID belonging to Lerato
        var updateDto = new { Name = "Hacked Event" };
        var response = await Client.PutAsJsonAsync("/api/Events/3", updateDto); // adjust ID
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}