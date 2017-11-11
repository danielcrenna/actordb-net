using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActorDb
{
	public interface IActorDbClient : IDisposable
	{
		Task<CreateUserResult> CreateUserAsync(string username, string password, params KeyValuePair<string, ActorPermissions>[] acl);
		Task<DeleteUserResult> DeleteUserAsync(string username);

		Task<bool> LoginAsync(string username, string password);
		Task<bool> LoginSecureAsync(string username, string password);
		
		Task<IReadOnlyCollection<string>> GetActorTypesAsync();
		Task<IReadOnlyCollection<string>> GetActorTablesAsync(string actorType);
		Task<IReadOnlyDictionary<string, string>> GetActorTableColumns(string actorType, string tableType);

		Task<string> GetProtocolVersionAsync();
		Task<long> GetUniqueIdAsync();
	}
}