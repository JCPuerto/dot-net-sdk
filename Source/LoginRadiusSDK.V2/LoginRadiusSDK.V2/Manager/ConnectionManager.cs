using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LoginRadiusSDK.V2.Exception;

namespace LoginRadiusSDK.V2
{
    /// <summary>
    ///  ConnectionManager retrieves HttpConnection objects used by API service
    /// </summary>
    public sealed class ConnectionManager
    {
        /// <summary>
        /// System.Lazy type guarantees thread-safe lazy-construction
        /// static holder for instance, need to use lambda to construct since constructor private
        /// </summary>
        private static readonly Lazy<ConnectionManager> LazyConnectionManager =
            new Lazy<ConnectionManager>(() => new ConnectionManager());

        /// <summary>
        /// Accessor for the Singleton instance of ConnectionManager
        /// </summary>
        public static ConnectionManager Instance => LazyConnectionManager.Value;

        /// <summary>
        /// Create and Config a HttpWebRequest
        /// </summary>
        /// <param name="config">Config properties</param>
        /// <param name="url">Url to connect to</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static HttpWebRequest GetConnection(Dictionary<string, string> config, string url, Dictionary<string, string> headers = null)
        {
            HttpWebRequest httpRequest;
            try
            {
                httpRequest = (HttpWebRequest) WebRequest.Create(url);
            }
            catch (UriFormatException)
            {
                throw new ConfigException("Invalid URI: " + url);
            }

            // Set connection timeout
            int connectionTimeout;
            if (!config.ContainsKey(BaseConstants.HttpConnectionTimeoutConfig) ||
                !int.TryParse(config[BaseConstants.HttpConnectionTimeoutConfig], out connectionTimeout))
            {
                int.TryParse(ConfigManager.GetDefault(BaseConstants.HttpConnectionTimeoutConfig),
                    out connectionTimeout);
            }
            httpRequest.Timeout = connectionTimeout;

            // Set request proxy for tunnelling http requests via a proxy server
            Uri proxyUri;
            if (config.ContainsKey(BaseConstants.HttpProxyAddressConfig) &&
                !string.IsNullOrWhiteSpace(config[BaseConstants.HttpProxyAddressConfig]) &&
                Uri.TryCreate(config[BaseConstants.HttpProxyAddressConfig], UriKind.Absolute, out proxyUri))
            {
                WebProxy requestProxy = new WebProxy {Address = new Uri(config[BaseConstants.HttpProxyAddressConfig])};
                if (config.ContainsKey(BaseConstants.HttpProxyCredentialConfig))
                {
                    string proxyCredentials = config[BaseConstants.HttpProxyCredentialConfig];
                    string[] proxyDetails = proxyCredentials.Split(':');
                    if (proxyDetails.Length == 2)
                    {
                        requestProxy.Credentials = new NetworkCredential(proxyDetails[0], proxyDetails[1]);
                    }
                }
                httpRequest.Proxy = requestProxy;
            }

            if (headers != null && headers.Any())
            {
                foreach (var header in headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }

            // Don't set the Expect: 100-continue header as it's not supported and can negatively impact performance.
            httpRequest.ServicePoint.Expect100Continue = false;
            return httpRequest;
        }
    }
}