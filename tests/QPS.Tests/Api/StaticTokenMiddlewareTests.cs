using Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyNewEncDec;

namespace QPS.Tests.Api
{
    /// <summary>
    /// The QPSApi gate: every request needs "Authorization: Bearer &lt;token&gt;", where the token is the decoded
    /// JwtSettings:SecretKey. Anything else is 401 and the controller is never reached.
    /// </summary>
    public class StaticTokenMiddlewareTests
    {
        private const string Token = "Qps-Test-Static-Token-2026";

        private static async Task<(int Status, bool Reached)> Send(string? authorization)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["JwtSettings:SecretKey"] = new New_Enc_Dec().My_Encode(Token) })
                .Build();
            var reached = false;
            var middleware = new StaticTokenMiddleware(_ => { reached = true; return Task.CompletedTask; }, config);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            if (authorization != null)
                ctx.Request.Headers.Authorization = authorization;

            await middleware.InvokeAsync(ctx);
            return (ctx.Response.StatusCode, reached);
        }

        [Fact]
        public async Task The_right_bearer_token_reaches_the_api()
        {
            var (status, reached) = await Send($"Bearer {Token}");
            Assert.True(reached);
            Assert.Equal(StatusCodes.Status200OK, status);
        }

        [Fact]
        public async Task Surrounding_spaces_after_Bearer_are_ignored() =>
            Assert.True((await Send($"Bearer   {Token}  ")).Reached);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Bearer wrong-token")]
        [InlineData("Basic " + Token)]
        [InlineData("bearer " + Token)]       // the scheme is matched case-sensitively
        [InlineData(Token)]                   // no scheme
        public async Task Missing_or_wrong_tokens_get_401(string? authorization)
        {
            var (status, reached) = await Send(authorization);
            Assert.False(reached);
            Assert.Equal(StatusCodes.Status401Unauthorized, status);
        }

        [Fact]
        public void The_secret_key_round_trips_through_the_encoder()
        {
            var codec = new New_Enc_Dec();
            Assert.Equal(Token, codec.My_Decode(codec.My_Encode(Token)));
        }
    }
}
