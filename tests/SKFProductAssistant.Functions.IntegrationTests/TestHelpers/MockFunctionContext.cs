using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections;
using System.Collections.Generic;

namespace SKFProductAssistant.Functions.IntegrationTests.TestHelpers
{
    public static class MockFunctionContext
    {
        public static FunctionContext Create(IServiceProvider? serviceProvider = null)
        {
            var mockContext = new Mock<FunctionContext>();
            var mockFeatures = new Mock<IInvocationFeatures>();

            // Setup basic properties
            mockContext.Setup(c => c.InvocationId).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(c => c.FunctionId).Returns("TestFunction");
            mockContext.Setup(c => c.Items).Returns(new Dictionary<object, object>());
            mockContext.Setup(c => c.Features).Returns(mockFeatures.Object);

            // Setup service provider
            if (serviceProvider == null)
            {
                var services = new ServiceCollection();
                services.AddLogging();
                serviceProvider = services.BuildServiceProvider();
            }
            mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);

            // FIXED: Avoid It.IsAnyType - use specific setups instead
            var featuresDict = new Dictionary<Type, object>();

            // Setup Get method without It.IsAnyType
            mockFeatures.Setup(f => f.Get<object>()).Returns((object?)null);
            mockFeatures.Setup(f => f.Get<string>()).Returns((string?)null);

            // Setup Set method without It.IsAnyType - use It.IsAny<object>() for the value
            mockFeatures.Setup(f => f.Set(It.IsAny<object>()));

            // Mock IEnumerable methods - return empty enumeration
            mockFeatures.Setup(f => f.GetEnumerator()).Returns(featuresDict.GetEnumerator());
            mockFeatures.As<IEnumerable>().Setup(f => f.GetEnumerator()).Returns(featuresDict.GetEnumerator());

            return mockContext.Object;
        }
    }
}
