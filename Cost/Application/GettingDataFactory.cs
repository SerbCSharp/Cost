using Cost.Infrastructure.Repositories.AFKDevelopment;

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
                case "A":
                    return _serviceProvider.GetService<GettingData>();
                case "B":
                    return _serviceProvider.GetService<GettingData>();
                default:
                    // Обработка ошибки или возвращение сервиса по умолчанию
                    return _serviceProvider.GetService<GettingData>();
            }
        }
    }
}
