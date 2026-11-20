namespace VSLauncher.DataModel
{
	/// <summary>
	/// Associates a default target executable or identifier with a specific IDE.
	/// </summary>
	public class IdeDefaultTarget
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IdeDefaultTarget"/> class.
		/// </summary>
		public IdeDefaultTarget()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IdeDefaultTarget"/> class.
		/// </summary>
		/// <param name="ideType">The IDE type.</param>
		/// <param name="targetIdentifier">The target identifier or executable path.</param>
		public IdeDefaultTarget(IdeType ideType, string targetIdentifier)
		{
			this.IdeType          = ideType;
			this.TargetIdentifier = targetIdentifier ?? throw new ArgumentNullException(nameof(targetIdentifier));
		}

		/// <summary>
		/// Gets or sets the IDE type.
		/// </summary>
		public IdeType IdeType { get; set; }

		/// <summary>
		/// Gets or sets the target identifier or executable path.
		/// </summary>
		public string TargetIdentifier { get; set; } = string.Empty;
	}
}
