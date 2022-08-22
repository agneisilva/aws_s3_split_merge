///https://github.com/sachabarber/AWS/blob/master/Storage/S3TrasferUtility/S3TrasferUtility/Program.cs

using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Nito.AsyncEx;

namespace S3TransferUtility
{
    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var program = new Program();
                AsyncContext.Run(() => program.MainAsync(args));
            }
            catch (Exception er)
            {
                Console.WriteLine(er.ToString());
            }
        }

        async void MainAsync(string[] args)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Console.WriteLine("Iniciando");

            //await new SplitFileFunction.Function().FunctionHandler(new SplitFileFunction.Function.FunctionInput(), null);

            //await new MergeFileFunction.Function().FunctionHandler(new MergeFileFunction.Function.FunctionInput(), null);

            watch.Stop();

            Console.WriteLine(watch.ElapsedMilliseconds);


            Console.WriteLine("press any key....");
            Console.ReadKey();


        }

    }
}