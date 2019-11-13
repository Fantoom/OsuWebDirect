using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using CefSharp;
using CefSharp.WinForms;
using CefSharp.Handlers;
using NHotkey;
using NHotkey.WindowsForms;
using Newtonsoft.Json;
using System.IO;

namespace OsuWebDirect
{


	public partial class Form1 : Form
	{
		
		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		float screenSizeModifire = 0.8f;


		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
				return createParams;
			}
		}

		public ChromiumWebBrowser browser;
		public Settings settings;

		public Form1()
		{
			InitializeComponent();
			if (File.Exists("settings.json"))
			{
				var settingsPath = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "settings.json")[0];
				settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
			}
			else
			{
				settings = new Settings();
				string json = JsonConvert.SerializeObject(new Settings(), Formatting.Indented);

				using (FileStream fs = File.Create("settings.json"))
				{
					Byte[] info = new UTF8Encoding(true).GetBytes(json);
					fs.Write(info, 0, info.Length);
				}
			}
			screenSizeModifire = settings.UiScale;
			this.Height = (int)(SystemInformation.VirtualScreen.Height * screenSizeModifire);
			this.Width = (int)(SystemInformation.VirtualScreen.Width * screenSizeModifire);
			System.Windows.Forms.Cursor.Show();
			InitBrowser();
			attachToOsu();
			SetForegroundWindow(this.Handle);
			this.Icon = new Icon("icon.ico");
			notifyIcon1.Icon = new Icon("icon.ico");
			notifyIcon1.Visible = true;


			//(System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt),
			Keys key = Keys.F11;
			var hotkeyExp = settings.hotkey.Replace("Ctrl","Control").Split('+');
			foreach(var hKey in hotkeyExp) 
			{
				if (Enum.TryParse(hKey, out Keys myStatus))
				{
					key |= myStatus;
				}
			}
			
			
			HotkeyManager.Current.AddOrReplace("ToggleView", key, 
				OnToggleView
			);
		
		}
		public void InitBrowser()
		{
			Cef.Initialize(new CefSettings());
			browser = new ChromiumWebBrowser("https://osu.ppy.sh/beatmapsets");
			this.Controls.Add(browser);
			browser.Dock = DockStyle.Fill;
			DownloadHandler downloadHnadler = new DownloadHandler();
			if(!string.IsNullOrWhiteSpace(settings.downloadDir) && settings.downloadDir != "null")
            { 
				downloadHnadler.savePath = settings.downloadDir;
			}
			downloadHnadler.OnDownloadUpdatedFired += OnDownloadUpdated;
			downloadHnadler.OnBeforeDownloadFired += OnBeforeDownload;
			//downloadHnadler.OnProgressChanged += ;
			browser.LoadingStateChanged += Browser_LoadingStateChanged;
			browser.DownloadHandler = downloadHnadler;
		}

		private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
		{
			if (!e.IsLoading) 
			{
				browser.EvaluateScriptAsync($"document.querySelector('input[name=username]').value='{settings.username}';");
				browser.EvaluateScriptAsync($"document.querySelector('input[name=password]').value='{settings.password}';");
			}
		}

		private void OnBeforeDownload(object sender, DownloadItem e)
		{
			Console.WriteLine(e.FullPath);
		}

		private void OnDownloadUpdated(object sender, DownloadItem e)
		{
			int progress = (int)(e.ReceivedBytes *100f / e.TotalBytes);
			Action action = delegate () {
				downloadProgress.Value = progress;
			};
			this.BeginInvoke(action);
			Console.WriteLine(progress);
			if (e.IsComplete && !e.IsCancelled)
			{
				System.Diagnostics.Process.Start(e.FullPath);
			}
		}

		private void OnToggleView(object sender, HotkeyEventArgs e)
		{
			this.Visible = !this.Visible;
			if(this.Visible)
			{
				//attachToOsu();
				System.Windows.Forms.Cursor.Show();
			}
			else
			{
				System.Windows.Forms.Cursor.Hide();
				//this.ShowInTaskbar = true;
				}
			e.Handled = true;
		}


		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}


		public void attachToOsu()
		{
			Process hostProcess = Process.GetProcessesByName("Osu!").FirstOrDefault();
			if (hostProcess != null)
			{
				/*FormBorderStyle = FormBorderStyle.None;
				SetBounds(0, 0, 0, 0, BoundsSpecified.Location);

				IntPtr hostHandle = hostProcess.MainWindowHandle;
				IntPtr guestHandle = this.Handle;
				SetWindowLong(guestHandle, GWL_STYLE, GetWindowLong(guestHandle, GWL_STYLE) | WS_CHILD);
				SetParent(guestHandle, hostHandle);
				SetForegroundWindow(this.Handle);
				//Show();*/
				Hide();
				this.ShowInTaskbar = false;

				IntPtr hostHandle = hostProcess.MainWindowHandle;
				RECT rect = new RECT();
				GetWindowRect(hostHandle, ref rect);
				//this.Location = new Point(rect.Right, rect.Top);
				CenterToScreen();
				//this.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
				this.FormBorderStyle = FormBorderStyle.None;
			}
			else { Show(); }
		}

		private void notifyIcon1_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			Show();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			if (e.CloseReason == CloseReason.WindowsShutDown) return;
			Application.Exit();
			
		}
	}
}
