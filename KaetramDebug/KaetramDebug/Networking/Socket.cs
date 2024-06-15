using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static Messages;
using static Opcodes;
using static Program;
using static System.Runtime.InteropServices.JavaScript.JSType;



public class LoginData
{
	public Opcodes.Login Opcode { get; set; }
	public string Username { get; set; }
	public string Password { get; set; }
	public string Email { get; set; }
}
public class ClientHandshakePacketData : IHandshakePacketData
{
	public string Type { get; } = "client";
	public string Instance { get; set; }
	public int? ServerId { get; set; }
	public int? ServerTime { get; set; }
}

public class HubHandshakePacketData : IHandshakePacketData
{
	public string Type { get; } = "hub";
	public string GVer { get; set; }
	public string Name { get; set; }
	public int ServerId { get; set; }
	public string AccessToken { get; set; }
	public string RemoteHost { get; set; }
	public int Port { get; set; }
	public List<string> Players { get; set; }
	public int MaxPlayers { get; set; }
}

public class AdminHandshakePacketData : IHandshakePacketData
{
	public string Type { get; } = "admin";
	public string AccessToken { get; set; }
}

public interface IHandshakePacketData
{
	string Type { get; }
}
public class Socket
{
	private readonly Game game;
	private readonly Messages messages;
	private ClientWebSocket connection;
	private bool listening = false;
	private Config config;
	private bool reconnecting = false;

	public Socket(Game game)
	{
		this.game = game;
		this.config = game.App.Config;
		this.messages = new Messages(game.App);
	}

	private async Task<SerializedServer> GetServer()
	{
		if (!config.HubEnabled) return null;

		try
		{
			using (var httpClient = new HttpClient())
			{
				var response = await httpClient.GetStringAsync($"{config.Hub}/server");
				return JsonSerializer.Deserialize<SerializedServer>(response);
			}
		}
		catch
		{
			return null;
		}
	}

	public async Task Connect(SerializedServer server = null, WebProxy proxy = null)
	{
		try
		{
			if (connection?.State == WebSocketState.Open || reconnecting) return;

			var serverInfo = new SerializedServer
			{
				Host = config.Host,
				Port = config.Port
			};

			connection = new ClientWebSocket();
			connection.Options.Proxy = proxy;
			try
			{
				var uri = config.Ssl ? $"wss://{serverInfo.Host}" : $"ws://{serverInfo.Host}:{serverInfo.Port}";
				await connection.ConnectAsync(new Uri(uri), CancellationToken.None);

				OnOpen();
				await Task.Delay(1000);
				_ = Task.Run(Listen);
				await Task.Delay(1000);
				HandleConnected();
				await Task.Delay(1000);
			}
			catch (WebSocketException ex)
			{
				OnError();
				HandleConnectionError(serverInfo.Host, serverInfo.Port);
			}

			game.Audio.CreateContext();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to connect: {ex.Message}");
		}
	}

	private void OnOpen()
	{
		listening = true;
		HandleConnection();
	}

	private void HandleConnected()
	{
		// Send the handshake with the game version.
		Send(Packets.Handshake, new
		{
			gVer = "v2.0.4"
		});
	}


	private void OnError()
	{
		Console.WriteLine("An error occurred.");
		// Handle additional error logic here if necessary
	}
	private async Task Listen()
	{
		var buffer = new byte[1024 * 4];
		while (connection.State == WebSocketState.Open)
		{
			try
			{
				var result = await connection.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
				var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
				//Console.WriteLine($"Received message: {message}");
				Receive(message);

				if (result.MessageType == WebSocketMessageType.Close)
				{
					await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
					Console.WriteLine("Connection closed by the server.");
					break;
				}
				await Task.Delay(1000);
			}
			catch (WebSocketException ex)
			{
				Console.WriteLine($"WebSocket error: {ex.Message}");
				break;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unexpected error: {ex.Message}");
				break;
			}
		}
		Console.WriteLine("Connection closed.");
	}
	private async void Receive(string message)
	{
		try
		{
			using (JsonDocument document = JsonDocument.Parse(message))
			{
				var root = document.RootElement;

				if (root.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in root.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.Array)
						{
							var commandArray = item.EnumerateArray().ToArray();
							int commandId = commandArray[0].GetInt32();
							var data = commandArray[1];
							Console.WriteLine($"Received: {commandId} - {data}");
							switch (commandId)
							{
								case (int)Packets.Handshake:
									Send(Packets.Login, new { opcode = Opcodes.Login.Login, username = "", password = "" });
									Console.WriteLine($"Data: {data}");
									Send(Packets.Chat, "Hello from KaetramDebug");
									break;
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Exception while processing received message: {ex.Message}");
		}
	}

	public void Send(Packets packet, object data)
	{
		if (connection?.State != WebSocketState.Open) return;

		var message = JsonSerializer.Serialize(new object[] { packet, data });
		Console.WriteLine($"Sending: {message}");
		var buffer = Encoding.UTF8.GetBytes(message);

		Task.Run(async () =>
		{
			await connection.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
		});
	}

	private void HandleConnection()
	{
		Console.WriteLine("Connection established.");
		listening = true;
	}

	private void HandleConnectionError(string host, int port)
	{
		Console.WriteLine($"Failed to connect to: {host}:{port}");
	}
	private async void HandleHandshake(JsonElement data)
	{
		try
		{
			ClientHandshakePacketData clientData = JsonSerializer.Deserialize<ClientHandshakePacketData>(data.GetRawText());

			// Use the deserialized object as needed
			Console.WriteLine($"KeyMovement: {clientData.ServerTime}, Opcode: {clientData.ServerId}, PlayerX: {clientData.Instance}, PlayerY: {clientData.Type}");
			if (clientData == null || clientData.Type != "client")
			{
				game.App.UpdateLoader("Invalid client");
				return;
			}

			game.App.UpdateLoader("Connecting to server");

			// Calculate the offset of timing relative to the server.
			if (clientData.ServerTime != 0)
			{
				game.TimeOffset = (int)(DateTimeOffset.Now.ToUnixTimeMilliseconds() - clientData.ServerTime);
				Console.WriteLine($"Time offset: {game.TimeOffset}");
			}

			while (!listening)
			{
				await Task.Delay(1000);
			}
			
			await Task.Delay(1000);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to handle handshake: {ex.Message}");
		}
	}
}


// Additional classes and interfaces
public class Game
{
	public App App { get; set; }
	public Audio Audio { get; set; }
	public Player Player { get; set; } = new Player();
	public int TimeOffset { get; set; }
	public long LastEntityListRequest { get; set; } = 0;

	public void HandleDisconnection()
	{
		// Handle disconnection logic
	}
}

public class App
{
	public Config Config { get; set; }
	public event Action OnFocusEvent;

	public void OnFocus(Action callback)
	{
		OnFocusEvent += callback;
	}

	public void TriggerOnFocus()
	{
		OnFocusEvent?.Invoke();
	}
	public void UpdateLoader(string message)
	{
		Console.WriteLine($"Loader: {message}");
	}

	public void ToggleLogin(bool status)
	{
		Console.WriteLine($"Login: {status}");
	}

	public void SendError(string message)
	{
		Console.WriteLine($"Error: {message}");
	}
}

public class Config
{
	public bool HubEnabled { get; set; }
	public string Hub { get; set; }
	public string Host { get; set; }
	public int Port { get; set; }
	public bool Ssl { get; set; }
}
public class PlayerData
{
	public string Username { get; set; }
	public string Password { get; set; }
	public bool AutoLogin { get; set; }
	public bool RememberMe { get; set; }
	public int Orientation { get; set; }
	public double Zoom { get; set; }
}
public delegate void ConnectedPacketCallback();
public delegate void HandshakePacketCallback(IHandshakePacketData data);
public delegate void WelcomePacketCallback();

public class Messages
{
	private readonly App app;
	public Dictionary<Packets, Delegate> messages = new Dictionary<Packets, Delegate>();

	private ConnectedPacketCallback connectedCallback;
	private HandshakePacketCallback handshakeCallback;
	private WelcomePacketCallback welcomeCallback;

	public Messages(App app)
	{
		this.app = app;
		this.messages[Packets.Connected] = () => this.connectedCallback;
		this.messages[Packets.Handshake] = () => this.handshakeCallback;
		this.messages[Packets.Welcome] = () => this.welcomeCallback;
	}

	public void HandleCloseReason(string reason)
	{
		this.app.ToggleLogin(false);

		switch (reason)
		{
			case "worldfull":
				this.app.SendError("The servers are currently full!");
				break;
			case "error":
				this.app.SendError("The server has responded with an error!");
				break;
			case "banned":
				this.app.SendError("Your account has been disabled!");
				break;
			case "disabledregister":
				this.app.SendError("Registration is currently disabled.");
				break;
			case "development":
				this.app.SendError("The game is currently in development mode.");
				break;
			case "disallowed":
				this.app.SendError("The server is currently not accepting connections!");
				break;
			case "maintenance":
				this.app.SendError("Kaetram is currently under maintenance.");
				break;
			case "userexists":
				this.app.SendError("The username you have entered already exists.");
				break;
			case "emailexists":
				this.app.SendError("The email you have entered is not available.");
				break;
			case "invalidinput":
				this.app.SendError("The input you have entered is invalid. Please do not use special characters.");
				break;
			case "swappedworlds":
				this.app.SendError("You have recently swapped worlds, please wait 15 seconds.");
				break;
			case "loggedin":
				this.app.SendError("The player is already logged in!");
				break;
			case "invalidlogin":
				this.app.SendError("You have entered the wrong username or password.");
				break;
			case "toofast":
				this.app.SendError("You are trying to log in too fast from the same connection.");
				break;
			case "timeout":
				this.app.SendError("You have been disconnected for being inactive for too long.");
				break;
			case "updated":
				this.app.SendError("The game has been updated. Please clear your browser cache.");
				break;
			case "cheating":
				this.app.SendError("An error in client-server syncing has occurred.");
				break;
			case "lost":
				this.app.SendError("The connection to the server has been lost.");
				break;
			case "toomany":
				this.app.SendError("Too many devices from your IP address are connected.");
				break;
			case "ratelimit":
				this.app.SendError("You are sending packets too fast.");
				break;
			case "invalidpassword":
				this.app.SendError("The password you have entered is invalid.");
				break;
			default:
				this.app.SendError("An unknown error has occurred, please submit a bug report.");
				break;
		}
	}

	public void OnConnected(ConnectedPacketCallback callback)
	{
		this.connectedCallback = callback;
	}

	public void OnHandshake(HandshakePacketCallback callback)
	{
		this.handshakeCallback = callback;
	}

	public void OnWelcome(WelcomePacketCallback callback)
	{
		this.welcomeCallback = callback;
	}
}



public class Audio
{
	public void CreateContext()
	{
		// Create audio context logic
	}
}

public class SerializedServer
{
	public string Host { get; set; }
	public int Port { get; set; }
}
