using System.Collections.Generic;

namespace ActorDb
{
	public enum CreateUserResult
	{
		Success,
		Exists,
		Failure
	}

	public enum DeleteUserResult
	{
		Success,
		DoesNotExist,
		Failure
	}

	public class Configuration
	{
		public List<Group> Groups { get; } = new List<Group>();
		public List<Node> Nodes { get; } = new List<Node>();
	}

	public class Group
	{
		public string Name { get; set; }
		public GroupType Type { get; set; }
	}

	public enum GroupType
	{
		Cluster
	}

	public class Node
	{
		public Group Group { get; set; }
		public string Name { get; set; }
	}
}