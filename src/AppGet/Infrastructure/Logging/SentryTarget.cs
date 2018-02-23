﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AppGet.HostSystem;
using NLog;
using NLog.Common;
using NLog.Targets;
using SharpRaven;
using SharpRaven.Data;

namespace AppGet.Infrastructure.Logging
{
    [Target("Sentry")]
    public class SentryTarget : TargetWithLayout
    {
        private readonly RavenClient _client;

        private const string DSN = "https://25fa72d971b846d6996386a33c194943:9a0a287a550448e39343c31212c91902@sentry.io/293456";

        private static readonly IDictionary<LogLevel, ErrorLevel> LoggingLevelMap = new Dictionary<LogLevel, ErrorLevel>
        {
            {LogLevel.Debug, ErrorLevel.Debug},
            {LogLevel.Error, ErrorLevel.Error},
            {LogLevel.Fatal, ErrorLevel.Fatal},
            {LogLevel.Info, ErrorLevel.Info},
            {LogLevel.Trace, ErrorLevel.Debug},
            {LogLevel.Warn, ErrorLevel.Warning},
        };

        public SentryTarget(string[] args)
        {
            _client = new RavenClient(new Dsn(DSN), new JsonPacketFactory(), new SentryRequestFactory(), new SentryUserFactory())
            {
                Compression = true,
                ErrorOnCapture = OnError,
                Release = BuildInfo.Version.ToString(),
                Environment = BuildInfo.IsProduction ? "Production": "Dev",
            };

            var osInfo = new OsInfo();

            _client.Tags.Add("culture", Thread.CurrentThread.CurrentCulture.Name);
            _client.Tags.Add("os_name", osInfo.Name);
            _client.Tags.Add("os_version", osInfo.Version.ToString());
            _client.Tags.Add("os_bit", osInfo.Is64BitOperatingSystem ? "64" : "32");
        }

        private void OnError(Exception ex)
        {
            InternalLogger.Error(ex, "Unable to send error to Sentry");
        }


        private static BreadcrumbLevel GetLevel(LogLevel level)
        {
            if (level == LogLevel.Trace || level == LogLevel.Debug) return BreadcrumbLevel.Debug;

            if (level == LogLevel.Info) return BreadcrumbLevel.Info;

            if (level == LogLevel.Warn) return BreadcrumbLevel.Warning;

            if (level == LogLevel.Error) return BreadcrumbLevel.Error;

            return BreadcrumbLevel.Critical;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            try
            {
                var message = logEvent.FormattedMessage;

                _client.AddTrail(new Breadcrumb(logEvent.LoggerName, BreadcrumbType.Navigation)
                {
                    Level = GetLevel(logEvent.Level),
                    Message = message
                });

                if (logEvent.Level.Ordinal < LogLevel.Error.Ordinal)
                {
                    return;
                }

                var extras = logEvent.Properties.ToDictionary(x => x.Key.ToString(), x => x.Value.ToString());
                _client.Logger = logEvent.LoggerName;

                if (logEvent.Exception != null)
                {
                    foreach (DictionaryEntry data in logEvent.Exception.Data)
                    {
                        extras.Add(data.Key.ToString(), data.Value.ToString());
                    }
                }

                var sentryMessage = new SentryMessage(message);

                var sentryEvent = new SentryEvent(logEvent.Exception)
                {
                    Level = LoggingLevelMap[logEvent.Level],
                    Message = sentryMessage,
                    Extra = extras,
                    Fingerprint =
                    {
                        logEvent.Level.ToString(),
                        logEvent.LoggerName,
                        logEvent.Message
                    }
                };

                if (logEvent.Exception != null)
                {
                    sentryEvent.Fingerprint.Add(logEvent.Exception.GetType().FullName);
                }

                _client.Capture(sentryEvent);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }
    }
}
