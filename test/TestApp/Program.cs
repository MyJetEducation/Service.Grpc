using System;
using System.Threading.Tasks;
using ProtoBuf.Grpc.Client;
using Service.AssetsDictionary.Client;
using Service.AssetsDictionary.Grpc.Models;

namespace TestApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

            var factory = new AssetsDictionaryClientFactory("http://localhost:5001");
            var client = factory.GetAssetsDictionaryService();

            var resp = await  client.SayHelloAsync(new HelloGrpcRequest(){Name = "Alex"});
            Console.WriteLine(resp?.Message);

            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
