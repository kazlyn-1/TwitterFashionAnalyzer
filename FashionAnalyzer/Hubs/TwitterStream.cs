﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Tweetinvi;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Models;
using Tweetinvi.Streaming;

namespace FashionAnalyzer.Hubs
{
    public class TwitterStream
    {
        private IFilteredStream _stream;
        private readonly IHubContext _context = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();

        public async Task StartStream(CancellationToken token)
        {
            /*poof*/

            if (_stream == null)
            {
                _stream = Stream.CreateFilteredStream();
                _stream.AddTrack("#face");
                _stream.AddTrack("#ansikte");

                // Raised when any tweet that matches any condition.
                _stream.MatchingTweetReceived += async (sender, args) =>
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        await _context.Clients.All.updateStatus("Stopped");
                        _stream.StopStream();
                    }

                    ITweet tweet = args.Tweet;
                    if (!tweet.InReplyToStatusId.HasValue && !tweet.IsRetweet)
                    {
                        var embedTweet = Tweet.GetOEmbedTweet(args.Tweet);
                        await _context.Clients.All.updateTweet(embedTweet);
                    }
                };

                // if anything changes the state, update the UI.
                _stream.StreamPaused += async (sender, args) => { await _context.Clients.All.updateStatus("Paused."); };
                _stream.StreamResumed += async (sender, args) => { await _context.Clients.All.updateStatus("Streaming..."); };
                _stream.StreamStarted += async (sender, args) => { await _context.Clients.All.updateStatus("Started."); };
                _stream.StreamStopped += async (sender, args) =>
                {
                    string status = "Stopped";
                    Exception e = args.Exception;
                    if (e != null)
                        status += ": " + e.Message;

                    await _context.Clients.All.updateStatus(status);
                };

                // Start the stream.
                await _context.Clients.All.updateStatus("Started.");
                await _stream.StartStreamMatchingAnyConditionAsync();
            }

            // This condition will never be taken.
            else
            {
                _stream.ResumeStream();
            }
        }
    }
}