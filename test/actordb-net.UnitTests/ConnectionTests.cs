using System.Threading.Tasks;
using ActorDb;
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
        public async Task Can_login()
        {
	        using (IActorDbClient client = new ActorDbClient())
	        {
		        Assert.True(await client.LoginAsync("myuser", "mypass"));
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
		    using (IActorDbClient client = await ActorDbClient.BeginSession())
		    {
			    var types = await client.GetActorTypesAsync();
				foreach(var type in types)
					_output.WriteLine(type);
			    Assert.NotNull(types);
		    }
	    }

	    [Theory]
		[InlineData("type1")]
	    public async Task Can_get_actor_tables(string actorType)
	    {
		    using (IActorDbClient client = await ActorDbClient.BeginSession())
		    {
			    var types = await client.GetActorTablesAsync(actorType);
			    foreach (var type in types)
				    _output.WriteLine(type);
			    Assert.NotNull(types);
		    }
	    }
	}
}
