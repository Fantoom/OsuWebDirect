// Copyright © 2013 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.IO;

namespace CefSharp.Handlers
{
    public class DownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;

        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

		//public event EventHandler<int> OnProgressChanged;

		public string savePath = null;

		public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            OnBeforeDownloadFired?.Invoke(this, downloadItem);
			string path;
			if (string.IsNullOrWhiteSpace(savePath)) {
				path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads", downloadItem.SuggestedFileName);
				savePath = path;
			}
			else
			{
				path = Path.Combine(savePath, downloadItem.SuggestedFileName);
			}
			if (!callback.IsDisposed)
            {
                using (callback)
                {
                    callback.Continue(path, showDialog: false); // path was downloadItem.SuggestedFileName , showDialog was true
				}
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            OnDownloadUpdatedFired?.Invoke(this, downloadItem);
			//ProgressChanged?.Invoke(this, (int)(downloadItem.ReceivedBytes / downloadItem.TotalBytes));

		}
    }
}
