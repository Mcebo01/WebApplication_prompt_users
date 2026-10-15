using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay_API.Tests;

public class RoleAccessTest : ApiTestBase
{
    public RoleAccessTest(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        await AuthenticateAsParticipantAsync();

        var eventDto = new
        {
            Name = "Illegal Event",
            Description = "Should be blocked",
            EventDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
            Location = "Test Location",
            DistanceKm = 10.0m,
            EventType = "Run"
        };

        var response = await Client.PostAsJsonAsync("/api/Events", eventDto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        await AuthenticateAsOrganiserAsync();

        var eventDto = new
        {
            Name = "Allowed Organiser Event",
            Description = "Created by organiser",
            EventDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(2)),
            Location = "Cape Town",
            DistanceKm = 21.1m,
            EventType = "Run"
        };

        var response = await Client.PostAsJsonAsync("/api/Events", eventDto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CannotAccessProtectedEndpoint_Returns401()
    {
        var response = await Client.GetAsync("/api/Users/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResults_Returns403()
    {
        await AuthenticateAsParticipantAsync();

        var resultDto = new
        {
            EnrolmentId = 1,
            FinishTime = "01:45:30",
            Position = 12,
            TotalFinishers = 150
        };

        var response = await Client.PostAsJsonAsync("/api/Results", resultDto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolAsParticipant_Returns403()
    {
        await AuthenticateAsOrganiserAsync();

        var enrolmentDto = new { EventId = 1, CategoryId = 1 };
        var response = await Client.PostAsJsonAsync("/api/Enrolments", enrolmentDto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}