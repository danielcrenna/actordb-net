using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocols;
using Thrift.Transports.Client;

namespace ActorDb
{
	public class ActorDbClient : IActorDbClient
	{
		private readonly CancellationTokenSource _cancel;
		private readonly Actordb.Client _thrift;

		public ActorDbClient(string host = "localhost", int port = 33306)
		{
			var tcp = new TcpClient(host, port);
			var transport = new TSocketClientTransport(tcp);
			var protocol = new TBinaryProtocol(transport);

			_thrift = new Actordb.Client(protocol);
			_cancel = new CancellationTokenSource();
		}

		public static async Task<IActorDbClient> BeginSession(string username = "myuser", string password = "mypass", string host = "localhost", int port = 33306)
		{
			var client = new ActorDbClient(host, port);
			await client.LoginAsync(username, password);
			return client;
		}

		public async Task<bool> LoginAsync(string username, string password)
		{
			var result = await _thrift.loginAsync(username, password, _cancel.Token);
			return result.Success;
		}

		public async Task<IReadOnlyCollection<string>> GetActorTypesAsync()
		{
			var result = await _thrift.actor_typesAsync(_cancel.Token);
			return result?.AsReadOnly();
		}

		public async Task<IReadOnlyCollection<string>> GetActorTablesAsync(string actorType)
		{
			var result = await _thrift.actor_tablesAsync(actorType, _cancel.Token);
			return result?.AsReadOnly();
		}

		public async Task<IReadOnlyDictionary<string, string>> GetActorTableColumns(string actorType, string tableType)
		{
			return await _thrift.actor_columnsAsync(actorType, tableType, _cancel.Token);
		}

		public async Task<string> GetProtocolVersionAsync()
		{
			return await _thrift.protocolVersionAsync(_cancel.Token);
		}

		public void Dispose()
		{
			_cancel?.Cancel();
			_cancel?.Dispose();
		}
	}
}