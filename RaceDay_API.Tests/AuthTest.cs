using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
namespace RaceDay_API.Tests
{
    public class AuthTest : ApiTestBase
    {
        public AuthTest(WebApplicationFactory<Program> factory) : base(factory) { }

        [Fact]
        public async Task Register_WithValidData_Returns201()
        {
            var dto = new
            {
                FirstName = "John",
                LastName = "Doe",
                Email = $"john_{Guid.NewGuid():N}@test.com",
                Password = "Password@123",
                Role = "Participant"
            };

            var response = await Client.PostAsJsonAsync("/api/Auth/register", dto);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_Returns409()
        {
            var email = $"dup_{Guid.NewGuid():N}@test.com";

            var dto = new
            {
                FirstName = "A",
                LastName = "B",
                Email = email,
                Password = "Password@123",
                Role = "Participant"
            };

            await Client.PostAsJsonAsync("/api/Auth/register", dto);
            var response = await Client.PostAsJsonAsync("/api/Auth/register", dto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200()
        {
            var email = $"login_{Guid.NewGuid():N}@test.com";

            await Client.PostAsJsonAsync("/api/Auth/register", new
            {
                FirstName = "Test",
                LastName = "User",
                Email = email,
                Password = "Password@123",
                Role = "Participant"
            });

            var response = await Client.PostAsJsonAsync("/api/Auth/login", new
            {
                Email = email,
                Password = "Password@123"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var email = $"wrong_{Guid.NewGuid():N}@test.com";

            await Client.PostAsJsonAsync("/api/Auth/register", new
            {
                FirstName = "Test",
                LastName = "User",
                Email = email,
                Password = "Password@123",
                Role = "Participant"
            });

            var response = await Client.PostAsJsonAsync("/api/Auth/login", new
            {
                Email = email,
                Password = "WrongPassword"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}