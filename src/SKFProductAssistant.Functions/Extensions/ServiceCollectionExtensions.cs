// Extensions/ServiceCollectionExtensions.cs
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SKFProductAssistant.Application.Behaviors;
using SKFProductAssistant.Application.Interfaces;
using SKFProductAssistant.Application.Queries;
using SKFProductAssistant.Application.Services;
using SKFProductAssistant.Domain.Interfaces;
using SKFProductAssistant.Functions.Functions;
using SKFProductAssistant.Infrastructure.Repositories;
using SKFProductAssistant.Infrastructure.Services;

namespace SKFProductAssistant.Functions.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // MediatR - Updated syntax for version 12.x
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<ProcessProductQueryQuery>();
                cfg.RegisterServicesFromAssemblyContaining<ProductQueryFunction>();
            });

            // Pipeline behaviors
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

            // FluentValidation
            services.AddValidatorsFromAssemblyContaining<ProcessProductQueryQuery>();

            // Memory Cache
            services.AddMemoryCache();

            // Application Services
            services.AddScoped<IProductQueryService, ProductQueryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IConversationService, ConversationService>();

            // Infrastructure Services
            services.AddScoped<IAIService, AzureOpenAIService>();
            services.AddScoped<ICacheService, CacheService>();

            // Repositories
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();

            // Domain Services
            services.AddScoped<IProductSimilarityService, ProductSimilarityService>();

            return services;
        }
    }
}
