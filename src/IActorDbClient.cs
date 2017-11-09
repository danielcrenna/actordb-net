using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActorDb
{
	public interface IActorDbClient : IDisposable
	{
		Task<bool> LoginAsync(string username, string password);
		Task<string> GetProtocolVersionAsync();

		Task<IReadOnlyCollection<string>> GetActorTypesAsync();
		Task<IReadOnlyCollection<string>> GetActorTablesAsync(string actorType);
		Task<IReadOnlyDictionary<string, string>> GetActorTableColumns(string actorType, string tableType)
	}
}