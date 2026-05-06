namespace Promatis.Net.ApplicationModel.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var bootstrapper = new ConsoleBootstrapper();
            bootstrapper.Run(args);
        }
    }
}
