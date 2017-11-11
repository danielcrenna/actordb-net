using System;

namespace ActorDb
{
	[Flags]
	public enum ActorPermissions
	{
		None = 0x0,
		Read = 0x2,
		Write = 0x4
	}
}