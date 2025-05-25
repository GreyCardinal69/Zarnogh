namespace Zarnogh.Services
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void AddService<T>( T implementation ) where T : class
        {
            _services[typeof( T )] = implementation;
        }

        public void AddService( Type serviceType, object implementation )
        {
            _services[serviceType] = implementation;
        }

        public T GetService<T>() where T : class
        {
            return _services.TryGetValue( typeof( T ), out var service ) ? (T)service : null;
        }

        public object GetService( Type serviceType )
        {
            return _services.TryGetValue( serviceType, out var service ) ? service : null;
        }
    }
}
