namespace VSLauncher.DataModel
{
	/// <summary>
	/// Classifies the kind of Visual Studio Code workspace or project folder.
	/// </summary>
	public enum VsCodeProjectKind
	{
		/// <summary>
		/// A multi-root <c>.code-workspace</c> file.
		/// </summary>
		Workspace,

		/// <summary>
		/// A generic folder opened in Visual Studio Code.
		/// </summary>
		Folder,

		/// <summary>
		/// A Node.js or JavaScript/TypeScript project folder.
		/// </summary>
		NodeJs,

		/// <summary>
		/// A Python project folder.
		/// </summary>
		Python,

		/// <summary>
		/// A Rust Cargo project folder.
		/// </summary>
		Rust,

		/// <summary>
		/// A Go module project folder.
		/// </summary>
		Go,

		/// <summary>
		/// A Java project folder.
		/// </summary>
		Java,

		/// <summary>
		/// A CMake project folder.
		/// </summary>
		CMake,

		/// <summary>
		/// A Flutter/Dart project folder.
		/// </summary>
		Flutter,

		/// <summary>
		/// A PHP Composer project folder.
		/// </summary>
		Php,

		/// <summary>
		/// Any other folder project kind.
		/// </summary>
		Generic
	}
}
