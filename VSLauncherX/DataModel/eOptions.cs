namespace VSLauncher.DataModel
{
	[Flags]
	public enum eOptions
	{
		None			= 0b00000000,
		RunBeforeOn		= 0b00000001,
		RunBeforeOff	= 0b00000010,
		RunAsAdminOn	= 0b00000100,
		RunAsAdminOff	= 0b00001000,
		RunAfterOn		= 0b00010000,
		RunAfterOff		= 0b00100000,
	}
}
