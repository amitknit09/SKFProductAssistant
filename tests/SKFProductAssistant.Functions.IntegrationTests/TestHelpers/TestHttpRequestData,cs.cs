using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Security.Claims;
using System.Text;

namespace SKFProductAssistant.Functions.IntegrationTests.TestHelpers
{
    public class TestHttpRequestData : HttpRequestData
    {
        private readonly MemoryStream _body;
        private readonly Uri _url;

        public TestHttpRequestData(
            FunctionContext functionContext,
            string method = "POST",
            string url = "http://localhost/",
            string? body = null) : base(functionContext)
        {
            Method = method;
            _url = new Uri(url);
            Headers = new HttpHeadersCollection();

            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                _body = new MemoryStream(bytes);
            }
            else
            {
                _body = new MemoryStream();
            }

            Identities = Array.Empty<ClaimsIdentity>();
        }

        public override Stream Body => _body;

        public override HttpHeadersCollection Headers { get; }

        public override IReadOnlyCollection<ClaimsIdentity> Identities { get; }

        public override Uri Url => _url;

        public override string Method { get; }

        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();

        public override HttpResponseData CreateResponse()
        {
            return new TestHttpResponseData(FunctionContext);
        }

        // REMOVED: No Dispose methods - HttpRequestData doesn't implement IDisposable
        // The MemoryStream will be garbage collected automatically
    }
}
