using Cost.Infrastructure.Repositories;

namespace Cost.Application
{
    public class GettingDataFactory : IGettingDataFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GettingDataFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IGettingData Create(string type)
        {
            switch (type)
            {
                case "AFKDevelopment":
                    return _serviceProvider.GetRequiredService<GettingDataAFKDevelopment>();
                case "Vega":
                case "AFK":
                default:
                    return _serviceProvider.GetRequiredService<GettingDataAFKDevelopment>();
            }
        }
    }
}
