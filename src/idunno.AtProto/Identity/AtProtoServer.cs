﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the MIT License.

using DnsClient;
using DnsClient.Protocol;

namespace idunno.AtProto
{
    /// <summary>
    /// Represents an atproto server and provides methods to send messages and receive responses from the server.
    /// </summary>
    public static partial class AtProtoServer
    {
        /// <summary>
        /// Resolves a handle (domain name) to a DID.
        /// </summary>
        /// <param name="handle">The handle to resolve.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> or <paramref name="httpClient"/> is null.</exception>
        public static async Task<Did?> ResolveHandle(string handle, HttpClient httpClient, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(handle);
            ArgumentNullException.ThrowIfNull(httpClient);

            return await ResolveHandle(new Handle(handle), httpClient, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves a handle (domain name) to a DID.
        /// </summary>
        /// <param name="handle">The handle to resolve.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> or <paramref name="httpClient"/> is null.</exception>
        public static async Task<Did?> ResolveHandle(Handle handle, HttpClient httpClient, CancellationToken cancellationToken = default)
        {
            Did? did = null;

            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(httpClient);

            if (Uri.CheckHostName(handle.Value) != UriHostNameType.Dns)
            {
                throw new ArgumentOutOfRangeException(nameof(handle), "handle is not a valid DNS name.");
            }

            LookupClient lookupClient = new(new LookupClientOptions()
            {
                ContinueOnDnsError = true,
                ContinueOnEmptyResponse = true,
                ThrowDnsErrors = false,
                Timeout = TimeSpan.FromSeconds(15),
                UseCache = true
            });

            // First try DNS lookup
            string didTxtRecordHost = $"_atproto.{handle}";
            const string didTextRecordPrefix = "did=";

            IDnsQueryResponse dnsLookupResult = await lookupClient.QueryAsync(didTxtRecordHost, QueryType.TXT, QueryClass.IN, cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested && !dnsLookupResult.HasError)
            {
                foreach (TxtRecord? textRecord in dnsLookupResult.Answers.TxtRecords())
                {
                    foreach (string? text in textRecord.Text.Where(t => t.StartsWith(didTextRecordPrefix, StringComparison.InvariantCulture)))
                    {
                        did = new Did(text.Substring(didTextRecordPrefix.Length));
                    }
                }
            }

            if (!cancellationToken.IsCancellationRequested && did is null)
            {
                // Fall back to /well-known/did.json
                Uri didUri = new($"https://{handle}/.well-known/atproto-did");

                using (HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, didUri) { Headers = { Accept = { new("text/plain") } } })
                {
                    using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
                    {
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            string lookupResult = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                            if (!string.IsNullOrEmpty(lookupResult))
                            {
                                did = new Did(lookupResult);
                            }
                        }
                    }
                }
            }

            return did;
        }
    }
}
