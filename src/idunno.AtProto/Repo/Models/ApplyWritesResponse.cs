﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace idunno.AtProto.Repo.Models
{
    /// <summary>
    /// Encapsulates the response from an applyWrites API call.
    /// </summary>
    public record ApplyWritesResponse
    {
        [JsonConstructor]
        internal ApplyWritesResponse(Commit commit, IReadOnlyCollection<ApplyWritesResultBase> results)
        {
            Commit = commit;
            Results = results;
        }

        /// <summary>
        /// Gets the commit for the repo applyWrites operation.
        /// </summary>
        [JsonInclude]
        [JsonRequired]
        public Commit Commit { get; init; }

        /// <summary>
        /// Gets ths results of the repo applyWrites operation.
        /// </summary>
        [JsonInclude]
        [JsonRequired]
        public IReadOnlyCollection<ApplyWritesResultBase> Results { get; init; }
    }
}
