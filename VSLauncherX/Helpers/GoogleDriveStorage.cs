using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.Diagnostics;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// The google drive storage class, to up- and download the settings file, very experimental
	/// </summary>
	public class GoogleDriveStorage
	{
		private DriveService? driveService;
		private Google.Apis.Drive.v3.Data.File fileMetadata = new Google.Apis.Drive.v3.Data.File()
		{
			Name = "VSLauncherX.settings.json",
			Description = "VSLauncherX Solution and Project list",
			MimeType = "text/json"
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="GoogleDriveStorage"/> class.
		/// </summary>
		public GoogleDriveStorage() { }


		/// <summary>
		/// Authenticates the storage
		/// </summary>
		public void Authenticate()
		{
			var credential = GoogleCredential.FromFile("client_secret.json");
			
			// Create a Drive service object.
			this.driveService = new DriveService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "VSLauncherX",
			});
		}

		/// <summary>
		/// Uploads the.
		/// </summary>
		/// <param name="contents">The contents.</param>
		public void Upload(string contents)
		{
			if(this.driveService == null)
			{
				Authenticate();
			}

			FilesResource.CreateMediaUpload request;

			// create a stream to read from the string contents
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents)))
			{
				request = this.driveService.Files.Create(fileMetadata, stream, "text/json");
				request.Fields = "id";
				request.Upload();
			}

			var file = request.ResponseBody;
			Debug.WriteLine("File ID: " + file.Id);
		}

		/// <summary>
		/// Downloads the file
		/// </summary>
		/// <returns>A string.</returns>
		public string Download()
		{
			string contents;

			if (this.driveService == null)
			{
				Authenticate();
			}

			var query = "name is 'VSLauncherX.settings.json'";
			var info = this.driveService.Files.List();
			info.Q = query;

			var result = info.Execute();
			var file = result.Files.First();

			var request = this.driveService.Files.Get(file.Id);
			using (var stream = new System.IO.MemoryStream())
			{
				request.Download(stream);

				contents = Encoding.UTF8.GetString(stream.ToArray());
			}
			return contents;
		}
}
}
