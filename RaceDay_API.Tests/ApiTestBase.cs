using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RaceDay_API.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay_API.Tests
{
    public class ApiTestBase : IClassFixture<WebApplicationFactory<Program>>
    {
        protected readonly HttpClient Client;
        protected readonly WebApplicationFactory<Program> Factory;

        protected readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiTestBase(WebApplicationFactory<Program> factory)
        {
            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the real database with InMemory for tests
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<RaceDayContext>(options =>
                    {
                        options.UseInMemoryDatabase("RaceDayTestDb");
                    });
                });
            });

            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });
        }

        protected async Task AuthenticateAsOrganiserAsync()
        {
            // Register + Login an Organiser
            var email = $"org_{Guid.NewGuid():N}@test.com";
            var register = new
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                Password = "Organiser@123",
                Role = "Organiser"
            };

            await Client.PostAsJsonAsync("/api/Auth/register", register);

            var login = new { Email = email, Password = "Organiser@123" };
            var response = await Client.PostAsJsonAsync("/api/Auth/login", login);
            response.EnsureSuccessStatusCode();
        }

        protected async Task AuthenticateAsParticipantAsync()
        {
            var email = $"part_{Guid.NewGuid():N}@test.com";
            var register = new
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = email,
                Password = "Participant@123",
                Role = "Participant"
            };

            await Client.PostAsJsonAsync("/api/Auth/register", register);

            var login = new { Email = email, Password = "Participant@123" };
            var response = await Client.PostAsJsonAsync("/api/Auth/login", login);
            response.EnsureSuccessStatusCode();
        }
    }
}