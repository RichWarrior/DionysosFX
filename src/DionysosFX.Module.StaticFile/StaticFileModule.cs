﻿using Autofac;
using DionysosFX.Swan.Associations;
using DionysosFX.Swan.Extensions;
using DionysosFX.Swan.Modules;
using DionysosFX.Swan.Net;
using DionysosFX.Swan.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DionysosFX.Module.StaticFile
{
    /// <summary>
    /// Static file module
    /// </summary>
    internal class StaticFileModule : IWebModule
    {
        /// <summary>
        /// Static file options
        /// </summary>
        StaticFileOptions options = null;
        /// <summary>
        /// Files
        /// </summary>
        ConcurrentDictionary<string, StaticFileItem> _files = null;

        /// <summary>
        /// Static file module started was  and trigged this method
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Start(CancellationToken cancellationToken)
        {
            _files = new();
            PeriodicTask.Create(HandleExpireFiles, 1, cancellationToken);
        }

        /// <summary>
        /// Static file module handle request and trigged this method
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleRequestAsync(IHttpContext context)
        {
            if (options == null)
                if(context.Container.TryResolve(out options))
                {
                }
            if (context.IsHandled)
                return;
            string fileName = string.Empty;
            if (context.Request.RawUrl == "/")
            {
                fileName = "index.html";
            }
            else
            {
                var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
                if (entryAssembly == null)
                    throw new Exception("Entry assembly not found");
                fileName = Path.GetDirectoryName(entryAssembly.Location);
                foreach (var dir in context.Request.RawUrl.Split('/'))
                    fileName = Path.Combine(fileName, dir);
            }

            byte[] bytes = null;
            if (_files.ContainsKey(fileName))
            {
                if (_files.TryGetValue(fileName, out StaticFileItem staticFileItem))
                {
                    bytes = staticFileItem.Data;
                    if (options.CacheActive)
                    {
                        var expire = (staticFileItem.ExpireDate - DateTime.Now).TotalSeconds;
                        context.AddCacheExpire(expire);
                    }
                }
            }
            else
            {
                try
                {
                    if (File.Exists(fileName))
                    {
                        bytes = Encoding.UTF8.GetBytes(File.ReadAllText(fileName));
                        if (options.CacheActive)
                        {
                            _files.TryAdd(fileName, new StaticFileItem(bytes, DateTime.Now.AddSeconds(options.ExpireTime)));
                            var expire = DateTime.Now.AddMinutes(5).Second;
                            context.AddCacheExpire(expire);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            if (bytes != null)
            {
                var contentType = MimeType.Associations.GetValueOrDefault(Path.GetExtension(fileName));
                if (options.AllowedMimeTypes.Any(f => f == "*" || f == contentType))
                {
                    context.Response.OutputStream.Write(bytes);
                    context.Response.ContentType = contentType;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.SetHandled();
                }
            }
        }

        /// <summary>
        ///  
        /// </summary>
        private void HandleExpireFiles()
        {
            List<KeyValuePair<string, StaticFileItem>> deleteItems = new();
            foreach (var item in _files)
            {
                if (item.Value.ExpireDate < DateTime.Now)
                    deleteItems.Add(item);
            }

            foreach (var item in deleteItems)
            {
                _files.TryRemove(item);
            }

        }

        public void Dispose()
        {
            _files.Clear();
            _files = null;
        }
    }
}
