using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay_API.Tests
{
    public class ResultsTests : ApiTestBase
    {
        // Pass the WebApplicationFactory into the base constructor
        public ResultsTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Organiser_CanCaptureResult_Returns201()
        {
            await AuthenticateAsOrganiserAsync();

            var resultDto = new
            {
                EnrolmentId = 1,
                FinishTime = "03:28:45",
                Position = 47,
                TotalFinishers = 312
            };

            var response = await Client.PostAsJsonAsync("/api/Results", resultDto);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CanViewOwnResults_Returns200()
        {
            await AuthenticateAsParticipantAsync();
            var response = await Client.GetAsync("/api/Results/my");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}