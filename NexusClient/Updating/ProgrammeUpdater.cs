﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Nexus.Client.Util;

namespace Nexus.Client.Updating
{
	/// <summary>
	/// Updates the programme.
	/// </summary>
	public class ProgrammeUpdater : UpdaterBase
	{
		#region Properties

		/// <summary>
		/// Gets the updater's name.
		/// </summary>
		/// <value>The updater's name.</value>
		public override string Name
		{
			get
			{
				return String.Format("{0} Updater", EnvironmentInfo.Settings.ModManagerName);
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
		/// <param name="p_eifEnvironmentInfo">The application's envrionment info.</param>
		public ProgrammeUpdater(IEnvironmentInfo p_eifEnvironmentInfo)
			: base(p_eifEnvironmentInfo)
		{
			SetRequiresRestart(true);
		}

		#endregion

		/// <summary>
		/// Performs the update.
		/// </summary>
		/// <returns><c>true</c> if the update completed successfully;
		/// <c>false</c> otherwise.</returns>
		public override bool Update()
		{
			Trace.TraceInformation("Checking for new client version...");
			Trace.Indent();
			SetProgressMaximum(2);
			SetMessage(String.Format("Checking for new {0} version...", EnvironmentInfo.Settings.ModManagerName));
			Version verNew = GetNewProgrammeVersion();
			SetProgress(1);

			if (CancelRequested)
			{
				Trace.Unindent();
				return CancelUpdate();
			}

			if (verNew == new Version("0.0.0.0"))
			{
				SetMessage("Could not get version information from the update server.");
				return false;
			}

			if (verNew > new Version(ProgrammeMetadata.VersionString))
			{
				if (!Confirm(String.Format("A new version of {0} is available ({1}).{2}Would you like to download and install it?", EnvironmentInfo.Settings.ModManagerName, verNew, Environment.NewLine), "New Version"))
				{
					Trace.Unindent();
					return CancelUpdate();
				}

				SetMessage(String.Format("Downloading new {0} version...", EnvironmentInfo.Settings.ModManagerName));
				string strNewInstaller = DownloadFile(new Uri(String.Format("http://dev.tesnexus.com/client/releases/Nexus Mod Manager-{0}.exe", verNew.ToString())));
				SetProgress(2);

				if (CancelRequested)
				{
					Trace.Unindent();
					return CancelUpdate();
				}

				if (!String.IsNullOrEmpty(strNewInstaller))
				{
					string strOldPath = strNewInstaller;
					strNewInstaller = Path.Combine(Path.GetTempPath(), Path.GetFileName(strNewInstaller));
					FileUtil.ForceDelete(strNewInstaller);
					File.Move(strOldPath, strNewInstaller);

					SetMessage("Launching installer...");
					ProcessStartInfo psiInfo = new ProcessStartInfo(strNewInstaller);
					Process.Start(psiInfo);
					Trace.Unindent();
					return true;
				}
			}
			SetMessage(String.Format("{0} is already up to date.", EnvironmentInfo.Settings.ModManagerName));
			SetProgress(2);
			Trace.Unindent();
			return true;
		}

		/// <summary>
		/// Cancels the update.
		/// </summary>
		/// <remarks>
		/// This is a convience method that allows the setting of the message and
		/// the determination of the return value in one call.
		/// </remarks>
		/// <returns>Always <c>true</c>.</returns>
		private bool CancelUpdate()
		{
			SetMessage(String.Format("Cancelled {0} update.", EnvironmentInfo.Settings.ModManagerName));
			SetProgress(2);
			return true;
		}

		/// <summary>
		/// Gets the newest available programme version.
		/// </summary>
		/// <returns>The newest available programme version,
		/// or 0.0.0.0 if now information could be retrieved.</returns>
		private Version GetNewProgrammeVersion()
		{
			WebClient wclNewVersion = new WebClient();
			Version verNew = new Version("0.0.0.0");
			try
			{
				string strNewVersion = wclNewVersion.DownloadString("http://dev.tesnexus.com/client/releases/latestversion.php");
				if (!String.IsNullOrEmpty(strNewVersion))
					verNew = new Version(strNewVersion);
			}
			catch (WebException e)
			{
				Trace.TraceError(String.Format("Could not connect to update server: {0}", e.Message));
			}
			return verNew;
		}
	}
}
