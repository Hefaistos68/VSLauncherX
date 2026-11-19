using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using LibGit2Sharp;

using Newtonsoft.Json;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// Represents the outcome of a Git network operation.
	/// </summary>
	internal sealed class GitOperationResult
	{
		/// <summary>
		/// Gets a value indicating whether the operation succeeded.
		/// </summary>
		public bool Success { get; init; }

		/// <summary>
		/// Gets a value indicating whether the failure was caused by authentication.
		/// </summary>
		public bool AuthFailed { get; init; }

		/// <summary>
		/// Gets the resolved remote name, if applicable.
		/// </summary>
		public string RemoteName { get; init; } = string.Empty;

		/// <summary>
		/// Gets a concise summary of a successful operation.
		/// </summary>
		public string Summary { get; init; } = string.Empty;

		/// <summary>
		/// Gets a sanitized error message when the operation fails.
		/// </summary>
		public string ErrorMessage { get; init; } = string.Empty;
	}

	/// <summary>
	/// Provides Git credential resolution and credential-aware remote operations (fetch/pull).
	/// Uses the installed Git CLI (honoring Git Credential Manager, SSH, proxies, and SChannel)
	/// with fallback to LibGit2Sharp and bounded credential retries.
	/// </summary>
	internal static class GitHelper
	{
		private const int GitCommandTimeoutMs = 180000;

		private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
		private static readonly Regex UrlCredentialsRegex = new(@"(https?://)([^/\s@:]+):([^/\s@]+)@", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Lazy<string?> CachedGitExecutable = new(FindGitExecutable, LazyThreadSafetyMode.ExecutionAndPublication);

		private static readonly string[] AuthFailureMarkers =
		{
			"remote authentication required",
			"too many redirects or authentication replays",
			"authentication failed",
			"logon failed",
			"could not read username",
			"could not read password",
			"terminal prompts disabled",
			"permission denied (publickey",
			"failed to authenticate",
			"401",
			"403"
		};

		private static readonly object CredLock = new();
		private static bool _credsFileExisted;
		private static DateTime? _credsFileLastWriteUtc;
		private static Dictionary<string, string> _hostPats = new(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<string, string> _repoPats = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets the resolved path to the installed Git executable, or <c>null</c> if unavailable.
		/// </summary>
		public static string? GitExecutablePath => CachedGitExecutable.Value;

		/// <summary>
		/// Fetches updates from the configured remote for the specified Git repository.
		/// </summary>
		/// <param name="repoPath">The discovered Git repository path.</param>
		/// <param name="remoteName">Optional explicit remote name; defaults to tracking remote or origin.</param>
		/// <returns>A <see cref="GitOperationResult"/> describing the outcome.</returns>
		public static GitOperationResult Fetch(string repoPath, string? remoteName = null)
		{
			if (!TryResolveRepositoryContext(repoPath, remoteName, out string workDir, out string resolvedRemote, out string remoteUrl, out string? error))
			{
				return new GitOperationResult
				{
					Success      = false,
					ErrorMessage = error ?? "No remote is configured for this repository."
				};
			}

			string? gitExe = GitExecutablePath;

			if (!string.IsNullOrEmpty(gitExe))
			{
				GitOperationResult cliResult = RunGitCommand(gitExe, workDir, new[] { "fetch", "--", resolvedRemote }, remoteUrl);

				return new GitOperationResult
				{
					Success      = cliResult.Success,
					AuthFailed   = cliResult.AuthFailed,
					RemoteName   = resolvedRemote,
					Summary      = cliResult.Summary,
					ErrorMessage = cliResult.ErrorMessage
				};
			}

			return FetchWithLibGit2(repoPath, resolvedRemote, workDir);
		}

		/// <summary>
		/// Pulls updates for the current branch of the specified Git repository.
		/// </summary>
		/// <param name="repoPath">The discovered Git repository path.</param>
		/// <returns>A <see cref="GitOperationResult"/> describing the outcome.</returns>
		public static GitOperationResult Pull(string repoPath)
		{
			if (!TryResolveRepositoryContext(repoPath, null, out string workDir, out string resolvedRemote, out string remoteUrl, out string? error))
			{
				return new GitOperationResult
				{
					Success      = false,
					ErrorMessage = error ?? "No remote is configured for this repository."
				};
			}

			string? gitExe = GitExecutablePath;

			if (!string.IsNullOrEmpty(gitExe))
			{
				GitOperationResult cliResult = RunGitCommand(gitExe, workDir, new[] { "pull" }, remoteUrl);

				return new GitOperationResult
				{
					Success      = cliResult.Success,
					AuthFailed   = cliResult.AuthFailed,
					RemoteName   = resolvedRemote,
					Summary      = cliResult.Summary,
					ErrorMessage = cliResult.ErrorMessage
				};
			}

			return PullWithLibGit2(repoPath, resolvedRemote, workDir);
		}

		/// <summary>
		/// Creates a per-operation LibGit2Sharp credential session that supports PATs,
		/// Git Credential Manager (<c>git credential fill</c>), and Windows integrated auth without infinite retries.
		/// </summary>
		/// <param name="workingDirectory">The repository working directory for Git config resolution.</param>
		/// <returns>A new <see cref="GitCredentialSession"/>.</returns>
		public static GitCredentialSession CreateCredentialSession(string workingDirectory)
		{
			return new GitCredentialSession(workingDirectory);
		}

		/// <summary>
		/// Resolves a configured Personal Access Token (PAT) from environment variables or <c>git-credentials.json</c>.
		/// </summary>
		/// <param name="url">The remote repository URL.</param>
		/// <returns>The configured PAT, or <c>null</c> if none matches.</returns>
		internal static string? TryGetConfiguredPat(string? url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return null;
			}

			EnsureCredentialConfigLoaded();

			(string? host, string? repoName) = ParseHostAndRepo(url);

			if (!string.IsNullOrEmpty(repoName))
			{
				string? repoPat = Environment.GetEnvironmentVariable($"VSLX_GIT_PAT_REPO_{repoName.ToUpperInvariant()}");

				if (!string.IsNullOrWhiteSpace(repoPat))
				{
					return repoPat.Trim();
				}
			}

			if (!string.IsNullOrEmpty(host))
			{
				string hostKey = host.ToUpperInvariant().Replace('.', '_');
				string? hostPat = Environment.GetEnvironmentVariable($"VSLX_GIT_PAT_HOST_{hostKey}");

				if (!string.IsNullOrWhiteSpace(hostPat))
				{
					return hostPat.Trim();
				}
			}

			return LookupConfigOrGenericPat(host, repoName);
		}

		/// <summary>
		/// Looks up a PAT from the cached JSON config maps or the generic <c>VSLX_GIT_PAT</c> environment variable.
		/// </summary>
		/// <param name="host">The parsed remote host name.</param>
		/// <param name="repoName">The parsed repository name.</param>
		/// <returns>The matching PAT, or <c>null</c> if unavailable.</returns>
		private static string? LookupConfigOrGenericPat(string? host, string? repoName)
		{
			lock (CredLock)
			{
				if (!string.IsNullOrEmpty(repoName) && _repoPats.TryGetValue(repoName, out string? cfgRepoPat) && !string.IsNullOrWhiteSpace(cfgRepoPat))
				{
					return cfgRepoPat.Trim();
				}

				if (!string.IsNullOrEmpty(host) && _hostPats.TryGetValue(host, out string? cfgHostPat) && !string.IsNullOrWhiteSpace(cfgHostPat))
				{
					return cfgHostPat.Trim();
				}
			}

			string? genericPat = Environment.GetEnvironmentVariable("VSLX_GIT_PAT");

			return string.IsNullOrWhiteSpace(genericPat) ? null : genericPat.Trim();
		}

		/// <summary>
		/// Loads or reloads <c>%APPDATA%\VSLauncher\git-credentials.json</c> when the file changes on disk.
		/// </summary>
		private static void EnsureCredentialConfigLoaded()
		{
			string cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "git-credentials.json");

			lock (CredLock)
			{
				bool exists = File.Exists(cfgPath);
				DateTime? lastWrite = exists ? File.GetLastWriteTimeUtc(cfgPath) : null;

				if (exists == _credsFileExisted && lastWrite == _credsFileLastWriteUtc)
				{
					return;
				}

				_credsFileExisted      = exists;
				_credsFileLastWriteUtc = lastWrite;
				_hostPats              = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_repoPats              = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				if (!exists)
				{
					return;
				}

				try
				{
					string json = File.ReadAllText(cfgPath);
					GitCredConfig? root = JsonConvert.DeserializeObject<GitCredConfig?>(json);

					if (root != null)
					{
						_hostPats = new Dictionary<string, string>(root.Hosts ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
						_repoPats = new Dictionary<string, string>(root.Repos ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Failed to read git-credentials.json: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Extracts host and repository name from an HTTP/HTTPS/SSH or SCP-style Git remote URL.
		/// </summary>
		/// <param name="url">The remote URL.</param>
		/// <returns>A tuple containing the host name and repository name.</returns>
		internal static (string? Host, string? RepoName) ParseHostAndRepo(string url)
		{
			if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && !string.IsNullOrEmpty(uri.Host))
			{
				return (uri.Host, ExtractRepoNameFromPath(uri.AbsolutePath));
			}

			// Handle SCP-style SSH URLs such as git@github.com:owner/repo.git
			int atIndex = url.IndexOf('@');
			int colonIndex = url.IndexOf(':', atIndex + 1);

			if (colonIndex > 0)
			{
				string hostPart = url.Substring(atIndex + 1, colonIndex - atIndex - 1);
				string pathPart = url.Substring(colonIndex + 1);

				return (hostPart, ExtractRepoNameFromPath(pathPart));
			}

			return (null, null);
		}

		/// <summary>
		/// Extracts the repository name from the final non-empty segment of a URL path.
		/// </summary>
		/// <param name="path">The URL path portion.</param>
		/// <returns>The repository name without a trailing <c>.git</c> suffix, or <c>null</c>.</returns>
		private static string? ExtractRepoNameFromPath(string path)
		{
			string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

			if (segments.Length == 0)
			{
				return null;
			}

			string last = segments[^1];

			return last.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
				? last.Substring(0, last.Length - 4)
				: last;
		}

		/// <summary>
		/// Resolves repository working directory, target remote name, and remote URL.
		/// </summary>
		/// <param name="repoPath">The discovered Git repository path.</param>
		/// <param name="preferredRemote">Optional explicit remote name.</param>
		/// <param name="workDir">Receives the repository working directory (or bare repo path).</param>
		/// <param name="remoteName">Receives the resolved remote name.</param>
		/// <param name="remoteUrl">Receives the remote URL.</param>
		/// <param name="error">Receives an error message on failure.</param>
		/// <returns><c>true</c> if context was resolved; otherwise <c>false</c>.</returns>
		private static bool TryResolveRepositoryContext(
			string repoPath,
			string? preferredRemote,
			out string workDir,
			out string remoteName,
			out string remoteUrl,
			out string? error)
		{
			workDir    = string.Empty;
			remoteName = string.Empty;
			remoteUrl  = string.Empty;
			error      = null;

			using var repo = new Repository(repoPath);

			workDir = !string.IsNullOrWhiteSpace(repo.Info.WorkingDirectory)
				? repo.Info.WorkingDirectory
				: repo.Info.Path;

			string candidate = !string.IsNullOrWhiteSpace(preferredRemote)
				? preferredRemote
				: (!string.IsNullOrWhiteSpace(repo.Head?.RemoteName) ? repo.Head.RemoteName : "origin");

			Remote? remote = repo.Network.Remotes[candidate] ?? repo.Network.Remotes.FirstOrDefault();

			if (remote is null)
			{
				error = "No remote is configured for this repository.";

				return false;
			}

			remoteName = remote.Name;
			remoteUrl  = remote.Url ?? string.Empty;

			return true;
		}

		/// <summary>
		/// Executes a Git CLI command in the specified working directory with credential-manager support.
		/// </summary>
		/// <param name="gitExe">Full path to <c>git.exe</c>.</param>
		/// <param name="workingDirectory">Working directory for the command.</param>
		/// <param name="arguments">Arguments to pass to <c>git.exe</c>.</param>
		/// <param name="remoteUrl">Remote URL used for optional PAT header injection.</param>
		/// <returns>A <see cref="GitOperationResult"/> describing execution results.</returns>
		private static GitOperationResult RunGitCommand(string gitExe, string workingDirectory, IReadOnlyList<string> arguments, string? remoteUrl)
		{
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName               = gitExe,
				WorkingDirectory       = workingDirectory,
				UseShellExecute        = false,
				RedirectStandardInput  = true,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				StandardOutputEncoding = Utf8NoBom,
				StandardErrorEncoding  = Utf8NoBom,
				CreateNoWindow         = true
			};

			foreach (string arg in arguments)
			{
				psi.ArgumentList.Add(arg);
			}

			ConfigureGitEnvironment(psi, remoteUrl);

			try
			{
				using Process? process = Process.Start(psi);

				if (process is null)
				{
					return new GitOperationResult
					{
						Success      = false,
						ErrorMessage = "Failed to start git process."
					};
				}

				process.StandardInput.Close();

				Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
				Task<string> stderrTask = process.StandardError.ReadToEndAsync();

				if (!process.WaitForExit(GitCommandTimeoutMs))
				{
					TryKillProcessTree(process);

					return new GitOperationResult
					{
						Success      = false,
						ErrorMessage = "Git operation timed out while waiting for remote response or authentication."
					};
				}

				string stdout = stdoutTask.GetAwaiter().GetResult().Trim();
				string stderr = stderrTask.GetAwaiter().GetResult().Trim();

				if (process.ExitCode == 0)
				{
					string summary = !string.IsNullOrEmpty(stdout) ? stdout : stderr;

					return new GitOperationResult
					{
						Success = true,
						Summary = SanitizeErrorMessage(summary)
					};
				}

				string combinedError = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
				string sanitizedError = SanitizeErrorMessage(combinedError);

				return new GitOperationResult
				{
					Success      = false,
					AuthFailed   = IsAuthenticationError(sanitizedError),
					ErrorMessage = string.IsNullOrEmpty(sanitizedError) ? $"git exited with code {process.ExitCode}." : sanitizedError
				};
			}
			catch (Exception ex)
			{
				return new GitOperationResult
				{
					Success      = false,
					ErrorMessage = SanitizeErrorMessage(ex.Message)
				};
			}
		}

		/// <summary>
		/// Configures environment variables for headless Git CLI execution while preserving interactive GCM login
		/// and injecting any explicitly configured PAT without exposing secrets on the command line.
		/// </summary>
		/// <param name="psi">The <see cref="ProcessStartInfo"/> to configure.</param>
		/// <param name="remoteUrl">The remote URL for optional PAT lookup.</param>
		private static void ConfigureGitEnvironment(ProcessStartInfo psi, string? remoteUrl)
		{
			// Prevent Git itself from hanging on a hidden TTY prompt when no GUI helper is present.
			psi.Environment["GIT_TERMINAL_PROMPT"] = "0";

			// Ensure Git Credential Manager is permitted to open its GUI/browser login prompt when needed.
			if (!psi.Environment.TryGetValue("GCM_INTERACTIVE", out string? existingInteractive) || string.IsNullOrWhiteSpace(existingInteractive))
			{
				psi.Environment["GCM_INTERACTIVE"] = "always";
			}

			string? pat = TryGetConfiguredPat(remoteUrl);

			if (string.IsNullOrWhiteSpace(pat) || !Uri.TryCreate(remoteUrl, UriKind.Absolute, out Uri? uri))
			{
				return;
			}

			if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			string username = ExtractUsernameFromUri(uri);
			string basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{pat}"));
			int configIndex = 0;

			if (psi.Environment.TryGetValue("GIT_CONFIG_COUNT", out string? countStr) && int.TryParse(countStr, out int parsedCount) && parsedCount > 0)
			{
				configIndex = parsedCount;
			}

			psi.Environment["GIT_CONFIG_COUNT"]                = (configIndex + 1).ToString();
			psi.Environment[$"GIT_CONFIG_KEY_{configIndex}"]   = $"http.{uri.Scheme}://{uri.Authority}/.extraHeader";
			psi.Environment[$"GIT_CONFIG_VALUE_{configIndex}"] = $"Authorization: Basic {basicToken}";
		}

		/// <summary>
		/// Extracts the username portion from a URI's <see cref="Uri.UserInfo"/>, falling back to <c>"git"</c>.
		/// </summary>
		/// <param name="uri">The remote URI.</param>
		/// <returns>A non-empty username suitable for HTTP Basic authentication.</returns>
		private static string ExtractUsernameFromUri(Uri uri)
		{
			if (!string.IsNullOrEmpty(uri.UserInfo))
			{
				int colonIndex = uri.UserInfo.IndexOf(':');
				string rawUser = colonIndex >= 0 ? uri.UserInfo.Substring(0, colonIndex) : uri.UserInfo;

				if (!string.IsNullOrWhiteSpace(rawUser))
				{
					return Uri.UnescapeDataString(rawUser);
				}
			}

			return "git";
		}

		/// <summary>
		/// Fetches from a remote using LibGit2Sharp when the Git CLI is not installed.
		/// </summary>
		/// <param name="repoPath">The Git repository path.</param>
		/// <param name="remoteName">The remote name.</param>
		/// <param name="workDir">The repository working directory.</param>
		/// <returns>A <see cref="GitOperationResult"/> describing the outcome.</returns>
		private static GitOperationResult FetchWithLibGit2(string repoPath, string remoteName, string workDir)
		{
			GitCredentialSession session = CreateCredentialSession(workDir);

			try
			{
				using var repo = new Repository(repoPath);
				Remote? remote = repo.Network.Remotes[remoteName];

				if (remote is null)
				{
					return new GitOperationResult
					{
						Success      = false,
						RemoteName   = remoteName,
						ErrorMessage = $"Remote '{remoteName}' was not found."
					};
				}

				FetchOptions fetchOptions = new FetchOptions
				{
					CredentialsProvider = session.ProvideCredentials
				};

				Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(rs => rs.Specification), fetchOptions, null);
				session.ApproveLastCredential();

				return new GitOperationResult
				{
					Success    = true,
					RemoteName = remote.Name
				};
			}
			catch (Exception ex)
			{
				return BuildLibGit2Failure(ex, remoteName);
			}
		}

		/// <summary>
		/// Pulls the current branch using LibGit2Sharp when the Git CLI is not installed.
		/// </summary>
		/// <param name="repoPath">The Git repository path.</param>
		/// <param name="remoteName">The resolved remote name.</param>
		/// <param name="workDir">The repository working directory.</param>
		/// <returns>A <see cref="GitOperationResult"/> describing the outcome.</returns>
		private static GitOperationResult PullWithLibGit2(string repoPath, string remoteName, string workDir)
		{
			GitCredentialSession session = CreateCredentialSession(workDir);

			try
			{
				using var repo = new Repository(repoPath);
				PullOptions pullOptions = new PullOptions
				{
					FetchOptions = new FetchOptions
					{
						CredentialsProvider = session.ProvideCredentials
					}
				};
				Signature signature = repo.Config.BuildSignature(DateTimeOffset.Now) ?? new Signature("VSLauncherX", "user@example.com", DateTimeOffset.Now);

				MergeResult result = Commands.Pull(repo, signature, pullOptions);

				session.ApproveLastCredential();

				return new GitOperationResult
				{
					Success    = true,
					RemoteName = remoteName,
					Summary    = $"Status: {result.Status}"
				};
			}
			catch (Exception ex)
			{
				return BuildLibGit2Failure(ex, remoteName);
			}
		}

		/// <summary>
		/// Converts a LibGit2Sharp exception into a clean <see cref="GitOperationResult"/>.
		/// </summary>
		/// <param name="ex">The thrown exception.</param>
		/// <param name="remoteName">The remote name.</param>
		/// <returns>A failure <see cref="GitOperationResult"/>.</returns>
		private static GitOperationResult BuildLibGit2Failure(Exception ex, string remoteName)
		{
			string message = SanitizeErrorMessage(ex.Message);

			if (message.Contains("unsupported URL protocol", StringComparison.OrdinalIgnoreCase))
			{
				return new GitOperationResult
				{
					Success      = false,
					AuthFailed   = false,
					RemoteName   = remoteName,
					ErrorMessage = "SSH remotes require Git for Windows (git.exe) to be installed and available on PATH."
				};
			}

			return new GitOperationResult
			{
				Success      = false,
				AuthFailed   = IsAuthenticationError(message),
				RemoteName   = remoteName,
				ErrorMessage = message
			};
		}

		/// <summary>
		/// Queries <c>git credential fill</c> to obtain credentials from the configured Git credential helper (e.g. GCM).
		/// </summary>
		/// <param name="gitExe">Path to <c>git.exe</c>.</param>
		/// <param name="workingDirectory">Working directory for repository-scoped Git config.</param>
		/// <param name="url">Remote URL to authenticate against.</param>
		/// <param name="usernameFromUrl">Optional username embedded in the remote URL.</param>
		/// <returns>A tuple of username and password if resolved; otherwise <c>null</c>.</returns>
		internal static (string Username, string Password)? TryFillGitCredential(string gitExe, string workingDirectory, string url, string? usernameFromUrl)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
			{
				return null;
			}

			string input = BuildCredentialInput(uri, usernameFromUrl, null);
			(int exitCode, string stdout) = RunGitCredentialHelper(gitExe, workingDirectory, "fill", input);

			if (exitCode != 0 || string.IsNullOrWhiteSpace(stdout))
			{
				return null;
			}

			return ParseCredentialOutput(stdout);
		}

		/// <summary>
		/// Sends an <c>approve</c> or <c>reject</c> notification to <c>git credential</c>.
		/// </summary>
		/// <param name="gitExe">Path to <c>git.exe</c>.</param>
		/// <param name="workingDirectory">Working directory for repository-scoped Git config.</param>
		/// <param name="action">Either <c>"approve"</c> or <c>"reject"</c>.</param>
		/// <param name="url">Remote URL.</param>
		/// <param name="username">The credential username.</param>
		/// <param name="password">The credential password or token.</param>
		internal static void NotifyGitCredential(string gitExe, string workingDirectory, string action, string url, string username, string password)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
			{
				return;
			}

			string input = BuildCredentialInput(uri, username, password);

			_ = RunGitCredentialHelper(gitExe, workingDirectory, action, input);
		}

		/// <summary>
		/// Builds LF-delimited key/value lines for the Git credential helper protocol.
		/// </summary>
		/// <param name="uri">The remote URI.</param>
		/// <param name="username">Optional username.</param>
		/// <param name="password">Optional password.</param>
		/// <returns>Formatted Git credential protocol payload.</returns>
		private static string BuildCredentialInput(Uri uri, string? username, string? password)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("protocol=").Append(uri.Scheme).Append('\n');
			sb.Append("host=").Append(uri.Authority).Append('\n');

			string trimmedPath = uri.AbsolutePath.TrimStart('/');

			if (!string.IsNullOrEmpty(trimmedPath))
			{
				sb.Append("path=").Append(trimmedPath).Append('\n');
			}

			if (!string.IsNullOrEmpty(username))
			{
				sb.Append("username=").Append(username).Append('\n');
			}

			if (!string.IsNullOrEmpty(password))
			{
				sb.Append("password=").Append(password).Append('\n');
			}

			sb.Append('\n');

			return sb.ToString();
		}

		/// <summary>
		/// Executes <c>git credential &lt;action&gt;</c> using BOM-free UTF-8 stdin/stdout.
		/// </summary>
		/// <param name="gitExe">Path to <c>git.exe</c>.</param>
		/// <param name="workingDirectory">Repository working directory.</param>
		/// <param name="action">Credential subcommand (<c>fill</c>, <c>approve</c>, or <c>reject</c>).</param>
		/// <param name="stdinPayload">LF-delimited credential payload.</param>
		/// <returns>Process exit code and captured stdout.</returns>
		private static (int ExitCode, string StdOut) RunGitCredentialHelper(string gitExe, string workingDirectory, string action, string stdinPayload)
		{
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName               = gitExe,
				WorkingDirectory       = workingDirectory,
				UseShellExecute        = false,
				RedirectStandardInput  = true,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				StandardInputEncoding  = Utf8NoBom,
				StandardOutputEncoding = Utf8NoBom,
				StandardErrorEncoding  = Utf8NoBom,
				CreateNoWindow         = true
			};

			psi.ArgumentList.Add("credential");
			psi.ArgumentList.Add(action);
			psi.Environment["GIT_TERMINAL_PROMPT"] = "0";

			if (!psi.Environment.TryGetValue("GCM_INTERACTIVE", out string? existingInteractive) || string.IsNullOrWhiteSpace(existingInteractive))
			{
				psi.Environment["GCM_INTERACTIVE"] = "always";
			}

			try
			{
				using Process? process = Process.Start(psi);

				if (process is null)
				{
					return (-1, string.Empty);
				}

				process.StandardInput.Write(stdinPayload);
				process.StandardInput.Close();

				Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
				Task<string> stderrTask = process.StandardError.ReadToEndAsync();

				if (!process.WaitForExit(GitCommandTimeoutMs))
				{
					TryKillProcessTree(process);

					return (-1, string.Empty);
				}

				_ = stderrTask.GetAwaiter().GetResult();

				return (process.ExitCode, stdoutTask.GetAwaiter().GetResult());
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"git credential {action} failed: {ex.Message}");

				return (-1, string.Empty);
			}
		}

		/// <summary>
		/// Parses <c>username=</c> and <c>password=</c> lines from <c>git credential fill</c> output.
		/// </summary>
		/// <param name="output">Standard output from <c>git credential fill</c>.</param>
		/// <returns>Parsed credentials if password is present; otherwise <c>null</c>.</returns>
		private static (string Username, string Password)? ParseCredentialOutput(string output)
		{
			string? username = null;
			string? password = null;

			foreach (string rawLine in output.Split('\n'))
			{
				string line = rawLine.TrimEnd('\r');

				if (line.StartsWith("username=", StringComparison.Ordinal))
				{
					username = line.Substring("username=".Length);
				}
				else if (line.StartsWith("password=", StringComparison.Ordinal))
				{
					password = line.Substring("password=".Length);
				}
			}

			if (string.IsNullOrEmpty(password))
			{
				return null;
			}

			return (string.IsNullOrEmpty(username) ? "git" : username, password);
		}

		/// <summary>
		/// Searches <c>PATH</c> and standard Git for Windows installation folders for <c>git.exe</c>.
		/// </summary>
		/// <returns>The full path to <c>git.exe</c>, or <c>null</c> if not found.</returns>
		private static string? FindGitExecutable()
		{
			string? pathEnv = Environment.GetEnvironmentVariable("PATH");

			if (!string.IsNullOrWhiteSpace(pathEnv))
			{
				foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
				{
					try
					{
						string candidate = Path.Combine(dir.Trim().Trim('"'), "git.exe");

						if (File.Exists(candidate))
						{
							return candidate;
						}
					}
					catch
					{
						// Ignore malformed PATH entries
					}
				}
			}

			string[] fallbackPaths =
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "cmd", "git.exe"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git", "cmd", "git.exe")
			};

			foreach (string fallback in fallbackPaths)
			{
				if (File.Exists(fallback))
				{
					return fallback;
				}
			}

			return null;
		}

		/// <summary>
		/// Checks whether an error message indicates a Git authentication failure.
		/// </summary>
		/// <param name="message">The error message to inspect.</param>
		/// <returns><c>true</c> if the error is authentication-related; otherwise <c>false</c>.</returns>
		internal static bool IsAuthenticationError(string? message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return false;
			}

			foreach (string marker in AuthFailureMarkers)
			{
				if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Redacts embedded HTTP credentials from URLs in Git output or error text.
		/// </summary>
		/// <param name="text">Raw Git output or error message.</param>
		/// <returns>Sanitized text safe to display in UI dialogs.</returns>
		internal static string SanitizeErrorMessage(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}

			return UrlCredentialsRegex.Replace(text, "$1***@");
		}

		/// <summary>
		/// Best-effort termination of a timed-out Git child process tree.
		/// </summary>
		/// <param name="process">The process to terminate.</param>
		private static void TryKillProcessTree(Process process)
		{
			try
			{
				process.Kill(entireProcessTree: true);
			}
			catch
			{
				// Ignore if process already exited
			}
		}

		/// <summary>
		/// Represents the JSON schema for <c>%APPDATA%\VSLauncher\git-credentials.json</c>.
		/// </summary>
		private sealed class GitCredConfig
		{
			[JsonProperty("hosts")]
			public Dictionary<string, string>? Hosts { get; set; }

			[JsonProperty("repos")]
			public Dictionary<string, string>? Repos { get; set; }
		}

		/// <summary>
		/// Tracks per-operation credential attempts for LibGit2Sharp callbacks to prevent infinite auth loops
		/// and approve or reject Git Credential Manager credentials appropriately.
		/// </summary>
		internal sealed class GitCredentialSession
		{
			private readonly string _workingDirectory;
			private bool _triedPat;
			private int _gcmAttempts;
			private bool _triedDefault;
			private (string Url, string Username, string Password)? _lastGcmCredential;

			/// <summary>
			/// Initializes a new instance of the <see cref="GitCredentialSession"/> class.
			/// </summary>
			/// <param name="workingDirectory">Repository working directory.</param>
			public GitCredentialSession(string workingDirectory)
			{
				_workingDirectory = workingDirectory;
			}

			/// <summary>
			/// Supplies credentials to LibGit2Sharp while bounding retry attempts per source.
			/// </summary>
			/// <param name="url">The remote repository URL.</param>
			/// <param name="usernameFromUrl">Optional username embedded in the URL.</param>
			/// <param name="types">Supported credential types advertised by the remote.</param>
			/// <returns>Resolved <see cref="Credentials"/>, or <c>null</c> to stop retrying.</returns>
			public Credentials ProvideCredentials(string url, string? usernameFromUrl, SupportedCredentialTypes types)
			{
				if (types.HasFlag(SupportedCredentialTypes.UsernamePassword))
				{
					Credentials? userPass = TryResolveUserPassCredential(url, usernameFromUrl);

					if (userPass != null)
					{
						return userPass;
					}
				}

				if (types.HasFlag(SupportedCredentialTypes.Default) && !_triedDefault)
				{
					_triedDefault = true;

					return new DefaultCredentials();
				}

				return null!;
			}

			/// <summary>
			/// Approves the last credential returned by Git Credential Manager after a successful operation.
			/// </summary>
			public void ApproveLastCredential()
			{
				string? gitExe = GitExecutablePath;

				if (_lastGcmCredential is null || string.IsNullOrEmpty(gitExe))
				{
					return;
				}

				NotifyGitCredential(gitExe, _workingDirectory, "approve", _lastGcmCredential.Value.Url, _lastGcmCredential.Value.Username, _lastGcmCredential.Value.Password);
			}

			/// <summary>
			/// Resolves a username/password credential from configured PATs or Git Credential Manager.
			/// </summary>
			/// <param name="url">The remote URL.</param>
			/// <param name="usernameFromUrl">Optional username from the URL.</param>
			/// <returns>Resolved <see cref="UsernamePasswordCredentials"/> or <c>null</c>.</returns>
			private Credentials? TryResolveUserPassCredential(string url, string? usernameFromUrl)
			{
				if (!_triedPat)
				{
					_triedPat = true;
					string? pat = TryGetConfiguredPat(url);

					if (!string.IsNullOrWhiteSpace(pat))
					{
						return new UsernamePasswordCredentials
						{
							Username = !string.IsNullOrWhiteSpace(usernameFromUrl) ? usernameFromUrl : "git",
							Password = pat
						};
					}
				}

				return TryResolveGcmCredential(url, usernameFromUrl);
			}

			/// <summary>
			/// Queries Git Credential Manager up to twice (rejecting a stale token on the second attempt).
			/// </summary>
			/// <param name="url">The remote URL.</param>
			/// <param name="usernameFromUrl">Optional username from the URL.</param>
			/// <returns>Resolved <see cref="UsernamePasswordCredentials"/> or <c>null</c>.</returns>
			private Credentials? TryResolveGcmCredential(string url, string? usernameFromUrl)
			{
				string? gitExe = GitExecutablePath;

				if (string.IsNullOrEmpty(gitExe) || _gcmAttempts >= 2)
				{
					return null;
				}

				if (_gcmAttempts == 1 && _lastGcmCredential is not null)
				{
					NotifyGitCredential(gitExe, _workingDirectory, "reject", _lastGcmCredential.Value.Url, _lastGcmCredential.Value.Username, _lastGcmCredential.Value.Password);
					_lastGcmCredential = null;
				}

				_gcmAttempts++;
				(string Username, string Password)? filled = TryFillGitCredential(gitExe, _workingDirectory, url, usernameFromUrl);

				if (filled is null)
				{
					return null;
				}

				_lastGcmCredential = (url, filled.Value.Username, filled.Value.Password);

				return new UsernamePasswordCredentials
				{
					Username = filled.Value.Username,
					Password = filled.Value.Password
				};
			}
		}
	}
}
