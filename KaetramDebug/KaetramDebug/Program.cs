using System.Diagnostics;
using System.Net;
class Program
{
	internal static string[] Proxies = File.ReadAllLines("proxies.txt");
	internal static int ProxyIndex = 0;
	static WebProxy GetProxy()
	{
		try
		{
			string proxyAddress;

			lock (Proxies)
			{
				Random random = new Random();

				if (ProxyIndex >= Proxies.Length)
				{
					ProxyIndex = 0;
				}

				proxyAddress = Proxies[ProxyIndex++];
			}

			// Create a WebProxy object from the proxy address
			if (proxyAddress.Split(':').Length == 4)
			{
				var proxyHost = proxyAddress.Split(':')[0];
				int proxyPort = Int32.Parse(proxyAddress.Split(':')[1]);
				var username = proxyAddress.Split(':')[2];
				var password = proxyAddress.Split(':')[3];
				ICredentials credentials = new NetworkCredential(username, password);
				var proxyUri = new Uri($"http://{proxyHost}:{proxyPort}");
				return new WebProxy(proxyUri, false, null, credentials);
			}
			else if (proxyAddress.Split(':').Length == 2)
			{
				var proxyHost = proxyAddress.Split(':')[0];
				int proxyPort = Int32.Parse(proxyAddress.Split(':')[1]);
				return new WebProxy(proxyHost, proxyPort);
			}
			else
			{
				throw new ArgumentException("Invalid proxy format.");
			}
		}
		catch (Exception)
		{
			throw new ArgumentException("Invalid proxy.");
		}
	}
	public class MovementPacket
	{
		public bool KeyMovement { get; set; }
		public Opcodes.Movement Opcode { get; set; }
		public int PlayerX { get; set; }
		public int PlayerY { get; set; }
		public long Timestamp { get; set; }
	}
	static async Task Main(string[] args)
	{
		//while (true)
		//{
			try
			{
				// Ensure TLS 1.2 is used
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				// Configure the global proxy settings
				var proxy = GetProxy();

				Game game = new Game
				{
					App = new App
					{
						Config = new Config
						{
							HubEnabled = true,
							Hub = "kaetram.com",
							Host = "world3.kaetram.com",
							Port = 11001,
							Ssl = true
						}
					},
					Audio = new Audio()
				};
				Socket socket = new Socket(game);

				await socket.Connect(new SerializedServer
				{
					Host = "world3.kaetram.com",
					Port = 11001
				}, proxy);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		//}

		await WaitForExit();
	}

	private static async Task WaitForExit()
	{
		var tcs = new TaskCompletionSource<bool>();

		AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
		{
			tcs.SetResult(true);
		};

		await tcs.Task;
	}
}