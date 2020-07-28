using System;
using DropboxApi = Dropbox.Api;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for OAuth.xaml
	/// </summary>
	public partial class OAuth : System.Windows.Window
	{
		const string APP_KEY = "ebum9qouj6j5ds3";
		const string REDIRECT_URI = "http://localhost/dropbox-oauth";

		private string oauth2State;
		private string accessToken;
		private bool result = false;

		public OAuth()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.oauth2State = Guid.NewGuid().ToString("N");
			Uri authorizeUri = DropboxApi.DropboxOAuth2Helper.GetAuthorizeUri(DropboxApi.OAuthResponseType.Token, APP_KEY, REDIRECT_URI, state: oauth2State);

			this.Browser.Navigate(authorizeUri);
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public string AccessToken
		{
			get { return this.accessToken; }
		}

		public bool Result
		{
			get { return this.result; }
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Browser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
		{
			// We need to ignore all navigation that isn't to the redirect uri.
			if (!e.Uri.ToString().StartsWith(REDIRECT_URI, StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			try {
				DropboxApi.OAuth2Response result = DropboxApi.DropboxOAuth2Helper.ParseTokenFragment(e.Uri);
				// The state in the response doesn't match the state in the request.
				if (result.State != this.oauth2State) {
					return;
				}

				this.accessToken = result.AccessToken;
				this.Uid = result.Uid;
				this.result = true;
			} catch (ArgumentException) {
				// There was an error in the URI passed to ParseTokenFragment
			} finally {
				e.Cancel = true;

				this.Close();
			}
		}
	}
}
