namespace Minebot.Bootstrap
{
    public interface IMinebotServiceConsumer
    {
        void InjectServices(RuntimeServiceRegistry services, BootstrapConfig config);
    }
}
