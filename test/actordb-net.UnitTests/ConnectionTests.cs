using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActorDb;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace actordb_net.UnitTests
{
    public class ConnectionTests
    {
	    private readonly ITestOutputHelper _output;

	    public ConnectionTests(ITestOutputHelper output)
	    {
		    _output = output;
	    }

	    [Fact]
	    public async Task Can_get_configuration()
	    {
		    using (IActorDbClient client = await ActorDbClient.BeginSession("root", "rootpass"))
		    {
			    var config = await client.GetConfigurationAsync();

				Assert.NotNull(config);

				_output.WriteLine(JsonConvert.SerializeObject(config));
		    }
	    }

		[Fact]
	    public async Task Can_create_and_delete_user()
	    {
		    using (IActorDbClient client = await ActorDbClient.BeginSession("root", "rootpass"))
		    {
			    var acl = new KeyValuePair<string, ActorPermissions>("*", ActorPermissions.Read | ActorPermissions.Write);

			    var username = $"{Guid.NewGuid()}";

				var create = await client.CreateUserAsync(username, "mypass2", acl);
				Assert.Equal(CreateUserResult.Success, create);
				
			    var exists = await client.CreateUserAsync(username, "mypass2", acl);
			    Assert.Equal(CreateUserResult.Exists, exists);

			    var delete = await client.DeleteUserAsync(username);
			    Assert.Equal(DeleteUserResult.Success, delete);

			    var gone = await client.DeleteUserAsync(username);
			    Assert.Equal(DeleteUserResult.DoesNotExist, gone);
			}
	    }

		[Fact]
        public async Task Can_login()
        {
	        using (IActorDbClient client = new ActorDbClient())
	        {
		        Assert.True(await client.LoginAsync("myuser", "mypass"));
			}
        }

	    [Fact]
	    public async Task Can_login_securely()
	    {
		    using (IActorDbClient client = new ActorDbClient())
		    {
			    Assert.True(await client.LoginSecureAsync("myuser", "mypass"));
		    }
	    }

		[Fact]
	    public async Task Can_get_protocol_version()
	    {
		    using (IActorDbClient client = new ActorDbClient())
		    {
			    var version = await client.GetProtocolVersionAsync();

				Assert.NotNull(version);
				Assert.NotEmpty(version);
			    _output.WriteLine(version);
			}
	    }

		[Fact]
	    public async Task Can_get_actor_types()
	    {
		    using (IActorDbClient client = await ActorDbClient.BeginSession("myuser", "mypass"))
		    {
			    var types = await client.GetActorTypesAsync();
				foreach(var type in types)
					_output.WriteLine(type);
			    Assert.NotNull(types);
		    }
	    }

	    [Fact]
	    public async Task Can_get_unique_id()
	    {
		    using (IActorDbClient client = await ActorDbClient.BeginSession("myuser", "mypass"))
		    {
			    var id = await client.GetUniqueIdAsync();
				_output.WriteLine($"{id}");
			    Assert.NotNull(id);
		    }
	    }

		[Theory]
		[InlineData("type1")]
	    public async Task Can_get_actor_tables(string actorType)
	    {
		    using (IActorDbClient client = await ActorDbClient.BeginSession("myuser", "mypass"))
		    {
			    var types = await client.GetActorTablesAsync(actorType);
			    foreach (var type in types)
				    _output.WriteLine(type);
			    Assert.NotNull(types);
		    }
	    }
	}
}