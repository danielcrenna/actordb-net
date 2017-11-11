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
}