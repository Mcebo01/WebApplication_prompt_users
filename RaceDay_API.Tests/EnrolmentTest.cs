using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay_API.Tests;

public class EnrolmentTest : ApiTestBase
{
    public EnrolmentTest(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Participant_CanEnrolInEvent_Returns201AndRecordsEnrolment()
    {
        await AuthenticateAsParticipantAsync();

        var enrolmentDto = new
        {
            EventId = 2,          // use a seeded event the participant is not already in
            CategoryId = 5
        };

        var response = await Client.PostAsJsonAsync("/api/Enrolments", enrolmentDto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Confirmed", body.GetProperty("status").GetString()); // or whatever default you use
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameEvent_Returns409()
    {
        await AuthenticateAsParticipantAsync();

        var enrolmentDto = new { EventId = 1, CategoryId = 1 }; // already enrolled in seed data

        var response = await Client.PostAsJsonAsync("/api/Enrolments", enrolmentDto);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewOwnEnrolments_Returns200()
    {
        await AuthenticateAsParticipantAsync();
        var response = await Client.GetAsync("/api/Enrolments/my"); // or whatever route you use
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEventEnrolments_Returns200()
    {
        await AuthenticateAsOrganiserAsync();
        var response = await Client.GetAsync("/api/Events/1/enrolments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}