﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Text.Json;

using idunno.AtProto.Models;
using idunno.AtProto.Repo;
using idunno.AtProto.Repo.Models;

namespace idunno.AtProto
{
    /// <summary>
    /// Represents an atproto server and provides methods to send messages and receive responses from the server.
    /// </summary>
    public static partial class AtProtoServer
    {
        // https://docs.bsky.app/docs/api/com-atproto-repo-apply-writes
        internal const string ApplyWritesEndpoint = "/xrpc/com.atproto.repo.applyWrites";

        // https://docs.bsky.app/docs/api/com-atproto-repo-create-record
        internal const string CreateRecordEndpoint = "/xrpc/com.atproto.repo.createRecord";

        // https://docs.bsky.app/docs/api/com-atproto-repo-delete-record
        internal const string DeleteRecordEndpoint = "/xrpc/com.atproto.repo.deleteRecord";

        // https://docs.bsky.app/docs/api/com-atproto-repo-put-record
        internal const string PutRecordEndpoint = "/xrpc/com.atproto.repo.putRecord";

        // https://docs.bsky.app/docs/api/com-atproto-repo-describe-repo
        internal const string DescribeRepoEndpoint = "xrpc/com.atproto.repo.describeRepo";

        // https://docs.bsky.app/docs/api/com-atproto-repo-get-record
        internal const string GetRecordEndpoint = "/xrpc/com.atproto.repo.getRecord";

        // https://docs.bsky.app/docs/api/com-atproto-repo-list-records
        internal const string ListRecordsEndpoint = "/xrpc/com.atproto.repo.ListRecords";

        // https://docs.bsky.app/docs/api/com-atproto-repo-upload-blob
        internal const string UploadBlobEndpoint = "/xrpc/com.atproto.repo.uploadBlob";

        /// <summary>
        /// Apply a batch transaction of repository creates, updates, and deletes. Requires authentication.
        /// </summary>
        /// <param name="writes"></param>
        /// <param name="repo"></param>
        /// <param name="validate">
        ///     Flag indicating what level of validation the api should perform.
        ///     If false skips lexicon schema validation of record data across all operations.
        ///     If true requires validation
        ///     if null validates only for known lexicons.
        ///</param>
        /// <param name="cid">
        ///   Optional commit ID. If provided, the entire operation will fail if the current repo commit CID does not match this value.
        ///   Used to prevent conflicting repo mutations.
        ///</param>
        /// <param name="service">The service to create the record on.</param>
        /// <param name="accessToken">An access token for the specified service.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to apply during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="writes"/>, <paramref name="repo"/>, <paramref name="service"/>,
        /// <paramref name="accessToken"/>, or <paramref name="httpClient"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="writes"/> is an empty collection.</exception>
        public static async Task<AtProtoHttpResult<ApplyWritesResponse>> ApplyWrites(
            ICollection<ApplyWritesRequestValueBase> writes,
            Did repo,
            bool? validate,
            Cid? cid,
            Uri service,
            string accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writes);
            ArgumentNullException.ThrowIfNull(repo);
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(accessToken);
            ArgumentNullException.ThrowIfNull(httpClient);

            if (writes.Count == 0)
            {
                throw new ArgumentException("cannot be an empty collection.", nameof(writes));
            }

            ApplyWritesRequest request = new(repo, validate, writes, cid);

            AtProtoHttpClient<ApplyWritesResponse> client = new();
            return await client.Post(
                service,
                ApplyWritesEndpoint,
                request,
                accessToken,
                httpClient,
                jsonSerializerOptions: jsonSerializerOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates an atproto record in the specified collection. Requires authentication.
        /// </summary>
        /// <param name="record"><para>A json representation of record to be created.</para></param>
        /// <param name="collection"><para>The NSID of collection the record should be created in.</para></param>
        /// <param name="creator"><para>The <see cref="Did"/> of the creating actor.</para></param>
        /// <param name="rKey"><para>The record key, if any, of the record to be created.</para></param>
        /// <param name="validate">
        ///   <para>Flag indicating what validation will be performed, if any.</para>
        ///   <para>A value of <keyword>true</keyword> requires lexicon schema validation of record data.</para>
        ///   <para>A value of <keyword>false</keyword> will skip Lexicon schema validation of record data.</para>
        ///   <para>A value of <keyword>null</keyword> to validate record data only for known lexicons.</para>
        ///   <para>Defaults to <keyword>true</keyword>.</para>
        /// </param>
        /// <param name="swapCommit"><para>The <see cref="Cid"/>, if any, to compare and swap with.</para></param>
        /// <param name="service"><para>The service to create the record on.</para></param>
        /// <param name="accessToken"><para>An access token for the specified service.</para></param>
        /// <param name="httpClient"><para>An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</para></param>
        /// <param name="jsonSerializerOptions"><para><see cref="JsonSerializerOptions"/> to apply during deserialization.</para></param>
        /// <param name="cancellationToken"><para>A cancellation token that can be used by other objects or threads to receive notice of cancellation.</para></param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="record"/>, <paramref name="collection"/>, <paramref name="creator"/>, <paramref name="service"/>,
        /// <paramref name="accessToken"/>, or <paramref name="httpClient"/> is null.
        /// </exception>
        public static async Task<AtProtoHttpResult<CreateRecordResponse>> CreateRecord(
            object record,
            Nsid collection,
            Did creator,
            RecordKey? rKey,
            bool? validate,
            Cid? swapCommit,
            Uri service,
            string accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(creator);
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(accessToken);
            ArgumentNullException.ThrowIfNull(httpClient);

            CreateRecordRequest request = new(record, collection, creator, validate, rKey, swapCommit);
            AtProtoHttpClient<CreateRecordResponse> client = new();

            return await client.Post(
                service,
                CreateRecordEndpoint,
                request,
                accessToken,
                httpClient,
                jsonSerializerOptions: jsonSerializerOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an atproto record, specified by its rKey, from specified repo/collection. Requires authentication.
        /// </summary>
        /// <param name="repo">The handle or Did of the repo to delete from. Typically this is the Did of the account that created the record.</param>
        /// <param name="collection">The NSID of the collection the record should be deleted from.</param>
        /// <param name="rKey">The record key, identifying the record to be deleted.</param>
        /// <param name="swapRecord">Specified if the operation should compare and swap with the previous record by cid.</param>
        /// <param name="swapCommit">Specified if the operation should compare and swap with the previous commit by cid.</param>
        /// <param name="service">The service to delete the record from.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</param>
        /// <param name="accessToken">An access token for the specified service.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to apply during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="repo"/>, <paramref name="collection"/>, <paramref name="rKey"/>, <paramref name="service"/>,
        /// <paramref name="accessToken"/>, or <paramref name="httpClient"/> is null.
        /// </exception>
        public static async Task<AtProtoHttpResult<Commit>> DeleteRecord(
            AtIdentifier repo,
            Nsid collection,
            RecordKey rKey,
            Cid? swapRecord,
            Cid? swapCommit,
            Uri service,
            string accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions=null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(repo);
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(rKey);
            ArgumentNullException.ThrowIfNull(accessToken);
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(httpClient);

            DeleteRecordRequest deleteRecordRequest = new(repo, collection, rKey) { SwapRecord = swapRecord, SwapCommit = swapCommit };

            AtProtoHttpClient<DeleteRecordResponse> client = new();
            AtProtoHttpResult<DeleteRecordResponse> response =  await client.Post(
                service,
                DeleteRecordEndpoint,
                deleteRecordRequest,
                accessToken,
                httpClient,
                jsonSerializerOptions : jsonSerializerOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (response.Succeeded)
            {
                return new AtProtoHttpResult<Commit>(
                    response.Result.Commit,
                    response.StatusCode,
                    response.AtErrorDetail,
                    response.RateLimit);
            }
            else
            {
                return new AtProtoHttpResult<Commit>(
                    null,
                    response.StatusCode,
                    response.AtErrorDetail,
                    response.RateLimit);
            }
        }

        /// <summary>
        /// Updates or creates an atproto record in the specified collection. Requires authentication.
        /// </summary>
        /// <param name="record"><para>A json representation of record to be created.</para></param>
        /// <param name="collection"><para>The NSID of collection the record should be created in.</para></param>
        /// <param name="creator"><para>The <see cref="Did"/> of the creating actor.</para></param>
        /// <param name="rKey"><para>The record key, if any, of the record to be created.</para></param>
        /// <param name="validate">
        ///   <para>Flag indicating what validation will be performed, if any.</para>
        ///   <para>A value of <keyword>true</keyword> requires lexicon schema validation of record data.</para>
        ///   <para>A value of <keyword>false</keyword> will skip Lexicon schema validation of record data.</para>
        ///   <para>A value of <keyword>null</keyword> to validate record data only for known lexicons.</para>
        ///   <para>Defaults to <keyword>true</keyword>.</para>
        /// </param>
        /// <param name="swapCommit"><para>The <see cref="Cid"/>, if any, to compare and swap with.</para></param>
        /// <param name="service"><para>The service to create the record on.</para></param>
        /// <param name="accessToken"><para>An access token for the specified service.</para></param>
        /// <param name="httpClient"><para>An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</para></param>
        /// <param name="jsonSerializerOptions"><para><see cref="JsonSerializerOptions"/> to apply during deserialization.</para></param>
        /// <param name="cancellationToken"><para>A cancellation token that can be used by other objects or threads to receive notice of cancellation.</para></param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="record"/>, <paramref name="collection"/>, <paramref name="creator"/>, <paramref name="rKey"/>, <paramref name="service"/>,
        /// <paramref name="accessToken"/>, or <paramref name="httpClient"/> is null.
        /// </exception>
        public static async Task<AtProtoHttpResult<PutRecordResponse>> PutRecord(
            object record,
            Nsid collection,
            Did creator,
            RecordKey rKey,
            bool? validate,
            Cid? swapCommit,
            Uri service,
            string accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(creator);
            ArgumentNullException.ThrowIfNull(rKey);
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(accessToken);
            ArgumentNullException.ThrowIfNull(httpClient);

            PutRecordRequest request = new(record, collection, creator, rKey, validate, swapCommit);
            AtProtoHttpClient<PutRecordResponse> client = new();

            return await client.Post(
                service,
                PutRecordEndpoint,
                request,
                accessToken,
                httpClient,
                jsonSerializerOptions: jsonSerializerOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the record specified by the identifying parameters. May require authentication.
        /// </summary>
        /// <typeparam name="T">The type of record to get.</typeparam>
        /// <param name="repo">The <see cref="AtIdentifier"/> of the repo to retrieve the record from.</param>
        /// <param name="collection">The NSID of the collection the record should be deleted from.</param>
        /// <param name="rKey">The record key, identifying the record to be deleted.</param>
        /// <param name="cid">The CID of the version of the record. If not specified, then return the most recent version.</param>
        /// <param name="service">The service to retrieve the record from.</param>
        /// <param name="accessToken">An access token for the specified service.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to apply during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="repo"/>, <paramref name="collection"/>, <paramref name="rKey"/>, <paramref name="service"/> or <paramref name="httpClient"/> is null.
        /// </exception>
        public static async Task<AtProtoHttpResult<T>> GetRecord<T>(
            AtIdentifier repo,
            Nsid collection,
            RecordKey rKey,
            Cid? cid,
            Uri service,
            string? accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) where T: class
        {
            ArgumentNullException.ThrowIfNull(repo);
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(rKey);
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(httpClient);

            AtProtoHttpClient<T> client = new();

            string queryString = $"repo={Uri.EscapeDataString(repo.ToString())}&collection={Uri.EscapeDataString(collection.ToString())}&rkey={Uri.EscapeDataString(rKey.ToString())}";

            if (cid is not null)
            {
                queryString += $"&cid={Uri.EscapeDataString(cid.ToString())}";
            }

            return await client.Get(
                service,
                $"{GetRecordEndpoint}?{queryString}",
                accessToken,
                httpClient,
                jsonSerializerOptions: jsonSerializerOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a page of records in the specified <paramref name="collection"/>. May requires authentication.
        /// </summary>
        /// <typeparam name="T">The type of records to get.</typeparam>
        /// <param name="repo">The <see cref="AtIdentifier"/> of the repo to retrieve the records from.</param>
        /// <param name="collection">The NSID of the collection the records should be retrieved from.</param>
        /// <param name="limit">The number of records to return in each page.</param>
        /// <param name="cursor">The cursor position to start retrieving records from.</param>
        /// <param name="reverse">A flag indicating if records should be listed in reverse order.</param>
        /// <param name="service">The service to retrieve the record from.</param>
        /// <param name="accessToken">An access token for the specified service.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to apply during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="repo"/>, <paramref name="collection"/>, <paramref name="service"/> or <paramref name="httpClient"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="limit"/> is not &gt;0 and &lt;=100.</exception>
        public static async Task<AtProtoHttpResult<PagedReadOnlyCollection<T>>> ListRecords<T>(
            AtIdentifier repo,
            Nsid collection,
            int? limit,
            string? cursor,
            bool reverse,
            Uri service,
            string? accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default) where T : AtProtoRecord
        {
            ArgumentNullException.ThrowIfNull(repo);
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(httpClient);

            if (limit is not null &&
               (limit < 1 || limit > 100))
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "{limit} must be between 1 and 100.");
            }

            string queryString = $"repo={Uri.EscapeDataString(repo.ToString())}&collection={Uri.EscapeDataString(collection.ToString())}";

            if (limit is not null)
            {
                queryString += $"&limit={limit}";
            }

            if (cursor is not null)
            {
                queryString += $"&cursor={Uri.EscapeDataString(cursor.ToString())}";
            }

            if (reverse)
            {
                queryString += "&reverse=true";
            }

            // We need to create an intermediate class to handle the deserialization of the response,
            // because trying to deserialize directly into a class that implements ICollection is
            // just too painful.
            AtProtoHttpClient<ListRecordsResponse<T>> client = new();
            AtProtoHttpResult<ListRecordsResponse<T>> response = await client.Get(
                service,
                $"{ListRecordsEndpoint}?{queryString}",
                accessToken,
                httpClient,
                jsonSerializerOptions: jsonSerializerOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // Flatten the results and into an AtProtoRecordList instance.
            PagedReadOnlyCollection<T> recordList;
            if (response.Succeeded)
            {
                recordList = new PagedReadOnlyCollection<T>(response.Result!.Records, response.Result.Cursor);
            }
            else
            {
                recordList = new PagedReadOnlyCollection<T>(new List<T>(), null);
            }

            return new AtProtoHttpResult<PagedReadOnlyCollection<T>>(recordList, response.StatusCode, response.AtErrorDetail, response.RateLimit);
        }

        /// <summary>
        /// Upload a new blob, to be referenced from a repository record.Requires authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        ///   The blob will be deleted if it is not referenced within a time window (eg, minutes).
        ///   Blob restrictions (mime type, size, etc) are enforced when the reference is created.
        /// </para>
        /// </remarks>
        /// <param name="blob">The blob to upload.</param>
        /// <param name="mimeType">The mime type of the blob to upload.</param>
        /// <param name="service">The service to upload the blob to.</param>
        /// <param name="accessToken">An access token for the specified service.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to apply during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="blob"/> is empty or the <paramref name="mimeType"/> is empty or not in the type/subtype.
        /// </exception>
        public static async Task<AtProtoHttpResult<Blob>> UploadBlob(
            byte[] blob,
            string mimeType,
            Uri service,
            string accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(blob);
            if (blob.Length == 0)
            {
                throw new ArgumentException("Blob cannot be empty.", nameof(blob));
            }

            ArgumentNullException.ThrowIfNullOrEmpty(mimeType);
            if (!mimeType.Contains('/', StringComparison.Ordinal) || mimeType.Count(c => c == '/') != 1)
            {
                throw new ArgumentException("Mime type must be in the format 'type/subtype'.", nameof(mimeType));
            }

            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNullOrEmpty(accessToken);
            ArgumentNullException.ThrowIfNull(httpClient);

            List<NameValueHeaderValue> requestHeaders = new()
            {
                new NameValueHeaderValue("Content-Type", mimeType)
            };

            AtProtoHttpClient<CreateBlobResponse> client = new();

            AtProtoHttpResult<CreateBlobResponse> response =
                await client.PostBlob(service, UploadBlobEndpoint, blob, requestHeaders, accessToken, httpClient, jsonSerializerOptions, cancellationToken).ConfigureAwait(false);

            if (response.Succeeded)
            {
                return new AtProtoHttpResult<Blob>(
                    response.Result.Blob,
                    response.StatusCode,
                    response.AtErrorDetail,
                    response.RateLimit);
            }
            else
            {
                return new AtProtoHttpResult<Blob>(
                    null,
                    response.StatusCode,
                    response.AtErrorDetail,
                    response.RateLimit);
            }
        }

        /// <summary>
        /// Gets information about an account and repository, including the list of collections.
        /// </summary>
        /// <param name="repo">The <see cref="AtIdentifier"/> of the repo to retrieve information for.</param>
        /// <param name="service">The service to delete the record from.</param>
        /// <param name="accessToken">An access token for the specified service.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> to use when making a request to the <paramref name="service"/>.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to apply during deserialization.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="repo"/>, <paramref name="service"/> or <paramref name="httpClient"/> is null.
        /// </exception>
        public static async Task<AtProtoHttpResult<RepoDescription>> DescribeRepo(
            AtIdentifier repo,
            Uri service,
            string? accessToken,
            HttpClient httpClient,
            JsonSerializerOptions? jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(repo);
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(httpClient);

            AtProtoHttpClient<RepoDescription> request = new();

            return await request.Get(
                service,
                $"{DescribeRepoEndpoint}?repo={Uri.EscapeDataString(repo.ToString())}",
                accessToken,
                httpClient,
                jsonSerializerOptions: jsonSerializerOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

    }
}
