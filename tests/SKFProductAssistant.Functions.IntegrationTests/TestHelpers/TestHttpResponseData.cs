using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;

namespace SKFProductAssistant.Functions.IntegrationTests.TestHelpers
{
    public class TestHttpResponseData : HttpResponseData
    {
        private readonly MemoryStream _bodyStream = new MemoryStream();

        public TestHttpResponseData(FunctionContext functionContext) : base(functionContext)
        {
            Headers = new HttpHeadersCollection();
            Cookies = new TestHttpCookies();
            Body = _bodyStream;
        }

        public override Stream Body { get; set; }

        public override HttpHeadersCollection Headers { get; set; }

        public override HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public override HttpCookies Cookies { get; }

        public string GetBodyAsString()
        {
            Body.Position = 0;
            using var reader = new StreamReader(Body, Encoding.UTF8, leaveOpen: true);
            var content = reader.ReadToEnd();
            Body.Position = 0;
            return content;
        }
    }

    public class TestHttpCookies : HttpCookies
    {
        private readonly List<IHttpCookie> _cookies = new();

        public override void Append(string name, string value)
        {
            _cookies.Add(new TestHttpCookie(name, value));
        }

        public override void Append(IHttpCookie cookie)
        {
            _cookies.Add(cookie);
        }

        public override IHttpCookie CreateNew() => new TestHttpCookie("test", "value");
    }

    public class TestHttpCookie : IHttpCookie
    {
        public TestHttpCookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
        public string? Path { get; set; }
        public string? Domain { get; set; }
        public DateTimeOffset? Expires { get; set; }
        public double? MaxAge { get; set; }
        public bool? HttpOnly { get; set; }
        public bool? Secure { get; set; }
        public SameSite SameSite { get; set; }
    }
}
