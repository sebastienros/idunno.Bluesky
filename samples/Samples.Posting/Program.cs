﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the MIT License.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using idunno.AtProto.Repo;
using idunno.AtProto;
using idunno.Bluesky;
using Microsoft.Extensions.Logging;
using Samples.Common;
using idunno.Bluesky.RichText;
using System.Reflection;
using idunno.Bluesky.Embed;
using idunno.AtProto.Repo.Models;

namespace Samples.Posting
{
    public sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Necessary to render emojis.
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var parser = Helpers.ConfigureCommandLine(PerformOperations);
            await parser.InvokeAsync(args);

            return 0;
        }

        static async Task PerformOperations(string? handle, string? password, string? authCode, Uri? proxyUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(handle);
            ArgumentNullException.ThrowIfNullOrEmpty(password);

            // Uncomment the next line to route all requests through Fiddler Everywhere
            proxyUri = new Uri("http://localhost:8866");

            // Uncomment the next line to route all requests  through Fiddler Classic
            // proxyUri = new Uri("http://localhost:8888");

            // Get an HttpClient configured to use a proxy, if proxyUri is not null.
            using (HttpClient? httpClient = Helpers.CreateOptionalHttpClient(proxyUri))

            // Change the log level in the ConfigureConsoleLogging() to enable logging
            using (ILoggerFactory? loggerFactory = Helpers.ConfigureConsoleLogging(LogLevel.Debug))

            // Create a new BlueSkyAgent
            using (var agent = new BlueskyAgent(httpClient: httpClient, loggerFactory: loggerFactory))
            {
                var loginResult = await agent.Login(handle, password, authCode, cancellationToken: cancellationToken);
                if (!loginResult.Succeeded)
                {
                    if (loginResult.AtErrorDetail is not null &&
                        string.Equals(loginResult.AtErrorDetail.Error!, "AuthFactorTokenRequired", StringComparison.OrdinalIgnoreCase))
                    {
                        ConsoleColor oldColor = Console.ForegroundColor;

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Login requires an authentication code.");
                        Console.WriteLine("Check your email and use --authCode to specify the authentication code.");
                        Console.ForegroundColor = oldColor;

                        return;
                    }
                    else
                    {
                        ConsoleColor oldColor = Console.ForegroundColor;

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Login failed.");
                        Console.ForegroundColor = oldColor;

                        if (loginResult.AtErrorDetail is not null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.WriteLine($"Server returned {loginResult.AtErrorDetail.Error} / {loginResult.AtErrorDetail.Message}");
                            Console.ForegroundColor = oldColor;

                            return;
                        }
                    }
                }

                {
                    // Simple post creation and deletion.
                    AtProtoHttpResult<CreateRecordResponse> createPostResult = await agent.Post("Hello world", cancellationToken: cancellationToken);
                    if (!createPostResult.Succeeded)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{createPostResult.StatusCode} occurred when creating the post.");
                        return;
                    }

                    Console.WriteLine("Post created");
                    Console.WriteLine($"  {createPostResult.Result.StrongReference}");
                    Debugger.Break();

                   // Delete the post we just made
                    AtProtoHttpResult<Commit> delete = await agent.DeletePost(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                    if (!delete.Succeeded)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{delete.StatusCode} occurred when deleting the post.");
                        return;
                    }
                }

                {
                    // Simple post creation and reply post creation.
                    AtProtoHttpResult<CreateRecordResponse> createPostResult = await agent.Post("Another test post, this time to check replying.", cancellationToken: cancellationToken);
                    if (createPostResult.Succeeded)
                    {
                        // Let's pretend we didn't just create the post, and we just have the strong reference of the post we want to reply to.
                        StrongReference postToReplyTo = createPostResult.Result.StrongReference;

                        AtProtoHttpResult<CreateRecordResponse> replyToHttpResult = await agent.ReplyTo(postToReplyTo, "This is a reply.", cancellationToken: cancellationToken);
                        if (!replyToHttpResult.Succeeded)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{replyToHttpResult.StatusCode} occurred when creating the post.");
                            return;
                        }

                        AtProtoHttpResult<CreateRecordResponse> replyToReplyHttpResult = await agent.ReplyTo(replyToHttpResult.Result!.StrongReference, "This is a reply to the reply.", cancellationToken: cancellationToken);
                        Debugger.Break();

                        if (!replyToReplyHttpResult.Succeeded)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{replyToReplyHttpResult.StatusCode} occurred when creating the post.");
                            return;
                        }

                        // Clean up again
                        _ = await agent.DeletePost(replyToReplyHttpResult.Result.StrongReference, cancellationToken: cancellationToken);
                        _ = await agent.DeletePost(replyToHttpResult.Result.StrongReference, cancellationToken: cancellationToken);
                        _ = await agent.DeletePost(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Post failed: {createPostResult.StatusCode}");
                        if (createPostResult.AtErrorDetail is not null)
                        {
                            Console.WriteLine($"\t{createPostResult.AtErrorDetail.Error} {createPostResult.AtErrorDetail.Message}");
                        }
                        return;
                    }
                }

                {
                    // Simple post creation with an image
                    byte[] imageAsBytes;

                    using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.BlueskyLogo.jpg")!)
                    using (MemoryStream memoryStream = new())
                    {
                        resourceStream.CopyTo(memoryStream);
                        imageAsBytes = memoryStream.ToArray();
                    }

                    AtProtoHttpResult<EmbeddedImage> imageUploadResult = await agent.UploadImage(
                        imageAsBytes,
                        "image/jpg",
                        "The Bluesky Logo",
                        new AspectRatio(1000, 1000),
                        cancellationToken: cancellationToken);

                    if (!imageUploadResult.Succeeded)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{imageUploadResult.StatusCode} occurred when uploading the images.");
                        return;
                    }

                    AtProtoHttpResult<CreateRecordResponse> createPostResult = await agent.Post("Hello world with an image.", imageUploadResult.Result, cancellationToken: cancellationToken);
                    Debugger.Break();

                    if (!createPostResult.Succeeded)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{createPostResult.StatusCode} occurred when creating the post.");
                        return;
                    }

                    Console.WriteLine("Post created");
                    Console.WriteLine($"  {createPostResult.Result.StrongReference}");

                    using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.BlueskyLogoRotated180.jpg")!)
                    using (MemoryStream memoryStream = new())
                    {
                        resourceStream.CopyTo(memoryStream);
                        imageAsBytes = memoryStream.ToArray();
                    }

                    imageUploadResult = await agent.UploadImage(
                        imageAsBytes,
                        "image/jpg",
                        "The Bluesky Logo, upside down",
                        new AspectRatio(1000, 1000),
                        cancellationToken: cancellationToken);

                    var replyWithImageResult = await agent.ReplyTo(createPostResult.Result.StrongReference, "Reply with an image.", imageUploadResult.Result!, cancellationToken: cancellationToken);

                    using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.BlueskyLogoRotated90.jpg")!)
                    using (MemoryStream memoryStream = new())
                    {
                        resourceStream.CopyTo(memoryStream);
                        imageAsBytes = memoryStream.ToArray();
                    }

                    imageUploadResult = await agent.UploadImage(
                        imageAsBytes,
                        "image/jpg",
                        "The Bluesky Logo, rotated 90°",
                        new AspectRatio(1000, 1000),
                        cancellationToken: cancellationToken);

                    if (!replyWithImageResult.Succeeded)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{createPostResult.StatusCode} occurred when creating the post.");
                        return;
                    }

                    Debugger.Break();

                    // Delete the post we just made, the image will eventually get cleaned up by the backend.
                    await agent.DeletePost(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                    await agent.DeletePost(replyWithImageResult.Result.StrongReference, cancellationToken: cancellationToken);
                }

                {
                    // Repost
                    AtProtoHttpResult<CreateRecordResponse> createPostResult = await agent.Post("Another test post, for reposting.", cancellationToken: cancellationToken);
                    if (createPostResult.Succeeded)
                    {
                        AtProtoHttpResult<CreateRecordResponse> repostResult = await agent.Repost(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                        Debugger.Break();

                        if (!repostResult.Succeeded)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{repostResult.StatusCode} occurred when creating the repost.");
                            return;
                        }

                        // Clean up again
                        _ = await agent.DeleteRepost(repostResult.Result.StrongReference, cancellationToken: cancellationToken);
                        _ = await agent.DeletePost(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Post failed: {createPostResult.StatusCode}");
                        if (createPostResult.AtErrorDetail is not null)
                        {
                            Console.WriteLine($"\t{createPostResult.AtErrorDetail.Error} {createPostResult.AtErrorDetail.Message}");
                        }
                        return;
                    }
                }

                {
                    // Like
                    AtProtoHttpResult<CreateRecordResponse> createPostResult = await agent.Post("Another test post, for liking.", cancellationToken: cancellationToken);
                    if (createPostResult.Succeeded)
                    {
                        AtProtoHttpResult<CreateRecordResponse> likeResult = await agent.Like(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                        Debugger.Break();

                        if (!likeResult.Succeeded)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{likeResult.StatusCode} occurred when creating the repost.");
                            return;
                        }

                        // Clean up again
                        _ = await agent.DeleteLike(likeResult.Result.StrongReference, cancellationToken: cancellationToken);
                        _ = await agent.DeletePost(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Post failed: {createPostResult.StatusCode}");
                        if (createPostResult.AtErrorDetail is not null)
                        {
                            Console.WriteLine($"\t{createPostResult.AtErrorDetail.Error} {createPostResult.AtErrorDetail.Message}");
                        }
                        return;
                    }
                }

                {
                    // quote
                    AtProtoHttpResult<CreateRecordResponse> createPostResult = await agent.Post("Another test post, for quoting.", cancellationToken: cancellationToken);
                    if (createPostResult.Succeeded)
                    {
                        var quoteResponse = await agent.Quote(createPostResult.Result.StrongReference, "Quote Dunk!", cancellationToken: cancellationToken);
                        var silentQuoteResponse = await agent.Quote(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);

                        byte[] imageAsBytes;

                        using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.BlueskyLogo.jpg")!)
                        using (MemoryStream memoryStream = new())
                        {
                            resourceStream.CopyTo(memoryStream);
                            imageAsBytes = memoryStream.ToArray();
                        }

                        var imageUploadResult = await agent.UploadImage(
                                                    imageAsBytes,
                                                    "image/jpg",
                                                    "The Bluesky Logo",
                                                    new AspectRatio(1000, 1000),
                                                    cancellationToken: cancellationToken);

                        AtProtoHttpResult<CreateRecordResponse> imageDunkQuoteResponse = await agent.Quote(
                            createPostResult.Result.StrongReference,
                            "Dunk with an image",
                            imageUploadResult.Result!,
                            cancellationToken: cancellationToken);

                        Debugger.Break();

                        // Clean up again
                        _ = await agent.DeleteQuote(imageDunkQuoteResponse.Result!.StrongReference, cancellationToken: cancellationToken);
                        _ = await agent.DeleteQuote(silentQuoteResponse.Result!.StrongReference, cancellationToken: cancellationToken);
                        _ = await agent.DeleteQuote(quoteResponse.Result!.StrongReference, cancellationToken: cancellationToken);
                        _ = await agent.DeletePost(createPostResult.Result.StrongReference, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Post failed: {createPostResult.StatusCode}");
                        if (createPostResult.AtErrorDetail is not null)
                        {
                            Console.WriteLine($"\t{createPostResult.AtErrorDetail.Error} {createPostResult.AtErrorDetail.Message}");
                        }
                        return;
                    }
                }

                {
                    // facets - mentions, hashtags and links, using a PostBuilder.
                    if (agent.Session is null)
                    {
                        Console.WriteLine("Session went missing.");
                        return;
                    }

                    var userToTag = agent.Session.Handle;
                    var userDidToTag = await agent.ResolveHandle(userToTag, cancellationToken: cancellationToken);
                    if (userDidToTag is null)
                    {
                        Console.WriteLine("{userToTag} could not be resolved to a DID.");
                        return;
                    }

                    PostBuilder postBuilder = new("Hey ");

                    postBuilder.Append(new Mention(userDidToTag, $"@{userToTag}"));

                    postBuilder.Append(" why not try some delicious ");

                    var shroudedLink = new Link("https://www.heinz.com/en-GB/products/05000157152886-baked-beanz", "beans");
                    postBuilder.Append(shroudedLink);
                    postBuilder.Append("? ");

                    postBuilder.Append("\nRead more: ");
                    var link = new Link("https://en.wikipedia.org/wiki/Heinz_Baked_Beans");
                    postBuilder.Append(' ');
                    postBuilder.Append(link);
                    postBuilder.Append('.');

                    var hashTag = new HashTag("beans");
                    postBuilder.Append(hashTag);


                    byte[] imageAsBytes;
                    using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.bean.png")!)
                    using (MemoryStream memoryStream = new())
                    {
                        resourceStream.CopyTo(memoryStream);
                        imageAsBytes = memoryStream.ToArray();
                    }
                    var imageUploadResult = await agent.UploadImage(
                        imageAsBytes,
                        "image/png",
                        "Beans",
                        new AspectRatio(340, 338),
                        cancellationToken: cancellationToken);

                    postBuilder += imageUploadResult.Result!;

                    AtProtoHttpResult<CreateRecordResponse> facetedCreatePostResponse = await agent.Post(postBuilder, cancellationToken: cancellationToken);
                    Debugger.Break();

                    if (facetedCreatePostResponse.Succeeded)
                    {
                        await agent.DeletePost(facetedCreatePostResponse.Result.Uri, cancellationToken: cancellationToken);
                        Debugger.Break();
                    }
                }
            }
        }
    }
}


//        {
//            byte[] imageAsBytes;

//            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.BlueskyLogo.jpg")!)
//            using (MemoryStream memoryStream = new())
//            {
//                resourceStream.CopyTo(memoryStream);
//                imageAsBytes = memoryStream.ToArray();
//            }

//            AtProtoHttpResult<Blob?> imageBlobLink = await agent.UploadBlob(imageAsBytes, "image/jpg");

//            PostBuilder imagePostBuilder = new("Hello with images");
//            imagePostBuilder += new EmbeddedImage(imageBlobLink.Result!, "The Bluesky Logo", new AspectRatio(1000, 1000));
//            AtProtoHttpResult<StrongReference> imagePostResult = await agent.Post(imagePostBuilder);

//            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.BlueskyLogoRotated180.jpg")!)
//            using (MemoryStream memoryStream = new())
//            {
//                resourceStream.CopyTo(memoryStream);
//                imageAsBytes = memoryStream.ToArray();
//            }
//            AtProtoHttpResult<Blob?> replyImageBlobLink = await agent.UploadBlob(imageAsBytes, "image/jpg");

//            PostBuilder replyWithImage = new("And a reply with an image.");
//            replyWithImage += new EmbeddedImage(replyImageBlobLink.Result!, "The Bluesky Logo", new AspectRatio(1000, 1000));

//            AtProtoHttpResult<StrongReference> replyResult = await agent.ReplyTo(imagePostResult.Result!, replyWithImage);

//            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Samples.Posting.BlueskyLogoRotated90.jpg")!)
//            using (MemoryStream memoryStream = new())
//            {
//                resourceStream.CopyTo(memoryStream);
//                imageAsBytes = memoryStream.ToArray();
//            }
//            AtProtoHttpResult<Blob?> quoteImageBlobLink = await agent.UploadBlob(imageAsBytes, "image/jpg");

//            PostBuilder quotePostWithImage = new("A quote post with an image.");
//            quotePostWithImage +=
//                new EmbeddedImage(quoteImageBlobLink.Result!, "image alt text", new AspectRatio(1000, 1000));

//            AtProtoHttpResult<StrongReference> quoteResult = await agent.Quote(replyResult.Result!, quotePostWithImage);

//            _ = await agent.DeletePost(quoteResult.Result!);
//            _ = await agent.DeletePost(replyResult.Result!);
//            _ = await agent.DeletePost(imagePostResult.Result!);
//        }
//    }

//}
