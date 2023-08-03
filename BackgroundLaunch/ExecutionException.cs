using System.Runtime.Serialization;

namespace BackgroundLaunch
{
	/// <summary>
	/// The execution exception.
	/// </summary>
	[Serializable]
	public class ExecutionException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExecutionException"/> class.
		/// </summary>
		public ExecutionException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExecutionException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public ExecutionException(string? message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExecutionException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		public ExecutionException(string? message, Exception? innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExecutionException"/> class.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="context">The context.</param>
		protected ExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}