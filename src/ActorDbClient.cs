using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Thrift.Protocols;
using Thrift.Transports.Client;

namespace ActorDb
{
	/*
	  // For safer login, get 20 bytes of cryptographically random data, use it to hash password for login call.
	  // It uses the same hashing algorithm as mysql:
	  // SHA1( password ) XOR SHA1( "20-bytes random data from server" <concat> SHA1( SHA1( password ) ) )
	  binary salt(), 

	  // Initialize instance/cluster(s), create users
	  Result exec_config(1: required string sql) throws (1:InvalidRequestException ire),

	  // Change schema
	  Result exec_schema(1: required string sql) throws (1:InvalidRequestException ire),

	  // query for a single actor of type
	  Result exec_single(1: required string actorname, 2: required string actortype, 3: required string sql, 4: list<string> flags = []) throws (1:InvalidRequestException ire),

	  // query for a single actor of type with parameterized query (ex.: "insert into tab values (?1,?2,?3)")
	  // This is faster and safer.
	  Result exec_single_param(1: required string actorname, 2: required string actortype, 3: required string sql, 4: list<string> flags = [], 5: list<list<Val>> bindingvals = []) throws (1:InvalidRequestException ire),

	  // query over multiple actors of type
	  Result exec_multi(1: required list<string> actors, 2: required string actortype, 3: required string sql, 4: list<string> flags = []) throws (1:InvalidRequestException ire),

	  // query over all actors for type
	  Result exec_all(1: required string actortype, 2: required string sql, 3: list<string> flags = []) throws (1:InvalidRequestException ire),

	  // all in sql: actor sometype(actorname) create; select * from mytab;
	  Result exec_sql(1: required string sql) throws (1:InvalidRequestException ire),

	  // all in sql but with parameterized query
	  Result exec_sql_param(1: required string sql, 2: list<list<Val>> bindingvals = []) throws (1:InvalidRequestException ire),

	  // Which actor types in schema.
	  list<string> actor_types() throws (1:InvalidRequestException ire),

	  // Which tables are in an actor type.
	  list<string> actor_tables(1: required string actor_type) throws (1:InvalidRequestException ire),

	  // Which columns for actor type and table.
	  map<string,string> actor_columns(1: required string actor_type, 2: required string actor_table) throws (1:InvalidRequestException ire),

	  // Returns a unique integer
	  i64 uniqid() throws (1:InvalidRequestException ire)
	 */


	public class ActorDbClient : IActorDbClient
	{
		#region ActorDB Literals

		private const string UserExistsInfo = "user_exists";
		private const string UserDoesNotExistInfo = "user_not_found";

		#endregion

		private readonly ILogger<ActorDbClient> _logger;
		private readonly CancellationTokenSource _cancel;
		private readonly Actordb.Client _thrift;

		public ActorDbClient(string host = "localhost", int port = 33306, ILogger<ActorDbClient> logger = null)
		{
			_logger = logger;
			var tcp = new TcpClient(host, port);
			var transport = new TSocketClientTransport(tcp);
			var protocol = new TBinaryProtocol(transport);

			_thrift = new Actordb.Client(protocol);
			_cancel = new CancellationTokenSource();
		}

		public static async Task<IActorDbClient> BeginSession(string username, string password, ILogger<ActorDbClient> logger = null, string host = "localhost", int port = 33306)
		{
			var client = new ActorDbClient(host, port, logger);
			await client.LoginSecureAsync(username, password);
			return client;
		}

		#region Configuration

		public async Task InitializeNodeAsync(string rootUser, string rootPassword, Configuration configuration)
		{
			try
			{
				var sql = new StringBuilder();
				sql.Append($"CREATE USER '{rootUser}' IDENTIFIED BY '{rootPassword}';");
				await _thrift.exec_configAsync(sql.ToString(), _cancel.Token);
			}
			catch (InvalidRequestException ire)
			{
				_logger?.LogError(ire, $"Error initializing node");
			}
		}

		public Task SetConfigurationAsync(Configuration config)
		{
			// See: https://github.com/biokoda/actordb/issues/9
			throw new NotImplementedException();
		}

		public async Task<string> GetLocalNodeNameAsync()
		{
			var result = await _thrift.exec_configAsync("SELECT localnode() AS node FROM dual", _cancel.Token);

			return result.RdRes?.Rows?[0]?["node"]?.Text;
		}

		public async Task<Configuration> GetConfigurationAsync()
		{
			try
			{
				var config = new Configuration();

				var groups = await _thrift.exec_configAsync("SELECT * FROM groups", _cancel.Token);
				if (groups.__isset.rdRes)
				{
					foreach (var row in groups.RdRes.Rows)
					{
						var group = new Group();
						group.Name = row["name"].Text;
						group.Type = (GroupType) Enum.Parse(typeof(GroupType), row["type"].Text, true);
						config.Groups.Add(group);
					}

					var nodes = await _thrift.exec_configAsync("SELECT * FROM nodes", _cancel.Token);
					if (nodes.__isset.rdRes)
					{
						foreach (var row in nodes.RdRes.Rows)
						{
							var node = new Node
							{ 
								Name = row["name"].Text,
								Group = config.Groups.FirstOrDefault(x => x.Name == row["group_name"].Text)
							};
							config.Nodes.Add(node);
						}

						return config;
					}
				}

				_logger?.LogError($"Unexpected result retrieving configuration");
				return null;
			}
			catch (InvalidRequestException ire)
			{
				_logger?.LogError(ire, $"Error retrieving configuration");
				return null;
			}
		}

		public async Task<CreateUserResult> CreateUserAsync(string username, string password, params KeyValuePair<string, ActorPermissions>[] acl)
		{
			try
			{
				var sql = new StringBuilder();
				sql.Append($"CREATE USER '{username}' IDENTIFIED BY '{password}' ");
				foreach (var entry in acl)
				{
					string grant;
					if (entry.Value.HasFlag(ActorPermissions.Read) && entry.Value.HasFlag(ActorPermissions.Write))
						grant = "read,write";
					else if (entry.Value.HasFlag(ActorPermissions.Read))
						grant = "read";
					else if (entry.Value.HasFlag(ActorPermissions.Write))
						grant = "write";
					else
						continue;
					sql.Append($"GRANT {grant} ON {entry.Key} to '{username}' ");
				}
				sql.Append(";");

				var q = sql.ToString();
				var r = await _thrift.exec_configAsync(q, _cancel.Token);
				_logger?.LogDebug(q);

				if (r.__isset.wrRes && r.WrRes?.RowsChanged == 1)
					return CreateUserResult.Success;

				_logger?.LogError("Unexpected result creating user, {0} rows changed", r.WrRes?.RowsChanged);
				return CreateUserResult.Failure;
			}
			catch (InvalidRequestException ire)
			{
				if (ire.Info == UserExistsInfo)
					return CreateUserResult.Exists;

				_logger?.LogError(ire, $"Error creating user '{username}'");
				return CreateUserResult.Failure;
			}
		}

		public async Task<DeleteUserResult> DeleteUserAsync(string username)
		{
			try
			{
				var q = $"DROP USER '{username}';";
				var r = await _thrift.exec_configAsync(q, _cancel.Token);
				_logger?.LogDebug(q);

				if (r.__isset.wrRes && r.WrRes?.RowsChanged == 1)
					return DeleteUserResult.Success;

				_logger?.LogError("Unexpected result deleting user, {0} rows changed", r.WrRes?.RowsChanged);
				return DeleteUserResult.Failure;
			}
			catch (InvalidRequestException ire)
			{
				if (ire.Info == UserDoesNotExistInfo)
					return DeleteUserResult.DoesNotExist;

				_logger?.LogError(ire, $"Error deleting user '{username}'");
				return DeleteUserResult.Failure;
			}
		}

		#endregion
		

		#region Login

		public async Task<bool> LoginAsync(string username, string password)
		{
			var result = await _thrift.loginAsync(username, Encoding.UTF8.GetBytes(password), _cancel.Token);
			return result.Success;
		}
		
		public async Task<bool> LoginSecureAsync(string username, string password)
		{
			var encoding = Encoding.UTF8;
			var pass = encoding.GetBytes(password);
			var seed = await _thrift.saltAsync(_cancel.Token);

			var md = SHA1.Create();
			var pass1 = md.ComputeHash(pass);           
			var pass2 = md.ComputeHash(pass1);

			var concat = new byte[seed.Length + pass2.Length];
			Buffer.BlockCopy(seed, 0, concat, 0, seed.Length);
			Buffer.BlockCopy(pass2, 0, concat, seed.Length, pass2.Length);
			
			var pass3 = md.ComputeHash(concat);         
			for (var i = 0; i < pass3.Length; i++)
				pass3[i] = (byte)(pass3[i] ^ pass1[i]);

			var hashed = pass3;
			var result = await _thrift.loginAsync(username, hashed, _cancel.Token);
			return result.Success;
		}

		#endregion



		#region Queries

		


		#endregion


		
		#region Actors

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

		#endregion


		public async Task<string> GetProtocolVersionAsync()
		{
			return await _thrift.protocolVersionAsync(_cancel.Token);
		}

		public async Task<long> GetUniqueIdAsync()
		{
			return await _thrift.uniqidAsync(_cancel.Token);
		}

		public void Dispose()
		{
			_cancel?.Cancel();
			_cancel?.Dispose();
		}
	}
}