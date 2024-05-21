namespace SelfServeDemo
{
	partial class Service
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.JsonWatcher = new System.IO.FileSystemWatcher();
			this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.JsonWatcher)).BeginInit();
			// 
			// JsonWatcher
			// 
			this.JsonWatcher.EnableRaisingEvents = true;
			this.JsonWatcher.Filter = "pcmon.json";
			this.JsonWatcher.NotifyFilter = ((System.IO.NotifyFilters)((System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite)));
			// 
			// Service
			// 
			this.ServiceName = "SelfServe";
			((System.ComponentModel.ISupportInitialize)(this.JsonWatcher)).EndInit();

		}

		#endregion

		private System.IO.FileSystemWatcher JsonWatcher;
		private System.Windows.Forms.Timer UpdateTimer;
	}
}
