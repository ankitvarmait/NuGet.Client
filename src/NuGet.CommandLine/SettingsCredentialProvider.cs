﻿using System;
using System.Linq;
using System.Net;

namespace NuGet.CommandLine
{
    public class SettingsCredentialProvider : ICredentialProvider
    {
        private readonly ICredentialProvider _credentialProvider;
        private readonly NuGet.Configuration.IPackageSourceProvider _packageSourceProvider;
        private readonly ILogger _logger;

        public SettingsCredentialProvider(ICredentialProvider credentialProvider, NuGet.Configuration.IPackageSourceProvider packageSourceProvider)
            : this(credentialProvider, packageSourceProvider, NullLogger.Instance)
        {
        }

        public SettingsCredentialProvider(ICredentialProvider credentialProvider, NuGet.Configuration.IPackageSourceProvider packageSourceProvider, ILogger logger)
        {
            if (credentialProvider == null)
            {
                throw new ArgumentNullException(nameof(credentialProvider));
            }

            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException(nameof(packageSourceProvider));
            }

            _credentialProvider = credentialProvider;
            _packageSourceProvider = packageSourceProvider;
            _logger = logger;
        }

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            NetworkCredential credentials;
            // If we are retrying, the stored credentials must be invalid.
            if (!retrying && (credentialType == CredentialType.RequestCredentials) && TryGetCredentials(uri, out credentials))
            {
                _logger.Log(
                    MessageLevel.Info,
                    LocalizedResourceManager.GetString(nameof(NuGetResources.SettingsCredentials_UsingSavedCredentials)),
                    credentials.UserName);
                return credentials;
            }
            return _credentialProvider.GetCredentials(uri, proxy, credentialType, retrying);
        }

        private bool TryGetCredentials(Uri uri, out NetworkCredential configurationCredentials)
        {
            var source = _packageSourceProvider.LoadPackageSources().FirstOrDefault(p =>
            {
                Uri sourceUri;
                return !String.IsNullOrEmpty(p.UserName)
                    && !String.IsNullOrEmpty(p.Password)
                    && Uri.TryCreate(p.Source, UriKind.Absolute, out sourceUri)
                    && UriEquals(sourceUri, uri);
            });
            if (source == null)
            {
                // The source is not in the config file
                configurationCredentials = null;
                return false;
            }
            configurationCredentials = new NetworkCredential(source.UserName, source.Password);
            return true;
        }

        /// <summary>
        /// Determines if the scheme, server and path of two Uris are identical.
        /// </summary>
        private static bool UriEquals(Uri uri1, Uri uri2)
        {
            uri1 = CreateODataAgnosticUri(uri1.OriginalString.TrimEnd('/'));
            uri2 = CreateODataAgnosticUri(uri2.OriginalString.TrimEnd('/'));

            return Uri.Compare(uri1, uri2, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
        }

        // Bug 2379: SettingsCredentialProvider does not work
        private static Uri CreateODataAgnosticUri(string uri)
        {
            if (uri.EndsWith("$metadata", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri.Substring(0, uri.Length - 9).TrimEnd('/');
            }
            return new Uri(uri);
        }
    }
}