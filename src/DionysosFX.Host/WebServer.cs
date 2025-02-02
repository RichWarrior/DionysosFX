﻿using DionysosFX.Host.Net.Internal;
using DionysosFX.Swan;
using DionysosFX.Swan.Net;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DionysosFX.Host
{
    public partial class WebServer : WebServerBase<IHostBuilder>
    {
        /// <summary>
        /// Web Request listener object
        /// </summary>
        IHttpListener _listener;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostBuilder"></param>
        public WebServer([NotNull] IHostBuilder hostBuilder) : base(hostBuilder)
        {
            _listener = CreateHttpListener();
        }

        /// <summary>
        /// Await a new web request and process web request
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ProcessRequestAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && (_listener?.IsListening ?? false))
            {
                var context = await _listener.GetContextAsync(cancellationToken);

                Task.Run(() => DoHandleContextAsync(context));
            }
        }

        /// <summary>
        /// DionysosFX App Prepare method
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override void Prepare(CancellationToken cancellationToken)
        {
            _listener.Start();
        }

        /// <summary>
        /// DionysosFX App Create a listener and start listening web request
        /// </summary>
        /// <returns></returns>
        private IHttpListener CreateHttpListener()
        {
            IHttpListener DoCreate() => new SystemHttpListener(new System.Net.HttpListener()) as IHttpListener;

            var listener = DoCreate();

            foreach (var prefix in HostBuilder.Prefixes)
            {
                var urlPrefix = new string(prefix?.ToCharArray());

                if (!urlPrefix.EndsWith("/", StringComparison.Ordinal))
                    urlPrefix += "/";

                urlPrefix = urlPrefix.ToLowerInvariant();

                listener.AddPrefix(urlPrefix);
            }

            return listener;
        }

        /// <summary>
        /// Dispose DionysosFX App
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_listener != null)
                    {
                        _listener.Stop();
                        _listener.Dispose();
                    }
                }
                catch
                {
                }
            }

            base.Dispose(disposing);
        }
    }
}
