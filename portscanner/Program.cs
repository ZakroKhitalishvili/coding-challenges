
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

await ScanPorts(ParseArguments(), 100);

Arguments ParseArguments()
{
    var argDict = new Dictionary<string, string>();
    foreach (var arg in args.Where(x => x.StartsWith("--")))
    {
        var argData = arg.TrimStart('-').Split('=');
        argDict.TryAdd(argData[0], argData[1]);
    }

    int? port = null;
    string host = argDict["host"];

    ArgumentNullException.ThrowIfNullOrEmpty(host, "host");

    if (argDict.ContainsKey("port"))
    {
        if (ushort.TryParse(argDict["port"], out ushort portParsed))
        {
            port = portParsed;
        }
        else
        {
            throw new ArgumentException("port", "Port is invalid");
        }
    }

    return new Arguments(host, port);
}


async Task ScanPorts(Arguments args, int inParallel = 1)
{
    if (args.Port != null)
    {
        Console.WriteLine($"Scanning host: {args.Host} port: {args.Port}");
    }
    else
    {
        Console.WriteLine($"Scanning host: {args.Host}");
    }

    var stopWatch = Stopwatch.StartNew();

    IPHostEntry ipHostInfo = Dns.GetHostEntry(args.Host);
    IPAddress ipAddress = ipHostInfo.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
    if (args.Port.HasValue)
    {
        if (ScanSinglePort(ipAddress, args.Port.Value))
        {
            Console.WriteLine($"Port: {args.Port} is open");
        }
    }
    else
    {


        int chunkSize = ushort.MaxValue / inParallel;

        var portChucks = Enumerable.Range(1, ushort.MaxValue).Chunk(chunkSize);

        var portChuckTasks = new List<Task>();

        foreach (var portChunk in portChucks)
        {
            var task = Task.Factory.StartNew(() =>
            {
                foreach (var port in portChunk)
                {
                    if (ScanSinglePort(ipAddress, port))
                    {
                        Console.WriteLine($"Port: {port} is open");
                    }

                }
            });

            portChuckTasks.Add(task);
        }

        await Task.WhenAll(portChuckTasks);

        Console.WriteLine($"Timeelapsed: {stopWatch.ElapsedMilliseconds}");
        stopWatch.Stop();
    }

}


bool ScanSinglePort(IPAddress ipAddress, int port)
{
    using var client = new TcpClient();
    try
    {
        var asyncResult = client.BeginConnect(ipAddress, port, null, null);
        if (asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(300), false))
        {
            client.EndConnect(asyncResult);
        }

        return client.Connected;
    }
    catch (Exception)
    {
    }
    finally
    {
        client.Close();
    }

    return false;
}

record Arguments(string Host, int? Port);