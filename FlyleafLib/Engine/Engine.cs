﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

using FlyleafLib.MediaFramework.MediaDevice;
using FlyleafLib.MediaPlayer;

namespace FlyleafLib;

/// <summary>
/// Flyleaf Engine
/// </summary>
public static class Engine
{
    /// <summary>
    /// Engine has been loaded and is ready for use
    /// </summary>
    public static bool              IsLoaded        { get; private set; }

    /// <summary>
    /// Engine's configuration
    /// </summary>
    public static EngineConfig      Config          { get; private set; }

    /// <summary>
    /// Audio Engine
    /// </summary>
    public static AudioEngine       Audio           { get; private set; }

    /// <summary>
    /// Video Engine
    /// </summary>
    public static VideoEngine       Video           { get; private set; }

    /// <summary>
    /// Plugins Engine
    /// </summary>
    public static PluginsEngine     Plugins         { get; private set; }

    /// <summary>
    /// FFmpeg Engine
    /// </summary>
    public static FFmpegEngine      FFmpeg          { get; private set; }

    /// <summary>
    /// List of active Players
    /// </summary>
    public static List<Player>      Players         { get; private set; }

    public static event EventHandler
                    Loaded;

    internal static LogHandler
                    Log;

    static Thread   tMaster;
    static object   lockEngine = new();
    static bool     isLoading;
    static int      timePeriod;

    /// <summary>
    /// Initializes Flyleaf's Engine (Must be called from UI thread)
    /// </summary>
    /// <param name="config">Engine's configuration</param>
    public static void Start(EngineConfig config = null) => StartInternal(config);

    /// <summary>
    /// Initializes Flyleaf's Engine Async (Must be called from UI thread)
    /// </summary>
    /// <param name="config">Engine's configuration</param>
    public static void StartAsync(EngineConfig config = null) => StartInternal(config, true);

    /// <summary>
    /// Requests timeBeginPeriod(1) - You should call TimeEndPeriod1 when not required anymore
    /// </summary>
    public static void TimeBeginPeriod1()
    {
        lock (lockEngine)
        {
            timePeriod++;

            if (timePeriod == 1)
            {
                Log.Trace("timeBeginPeriod(1)");
                Utils.NativeMethods.TimeBeginPeriod(1);
            }
        }
    }

    /// <summary>
    /// Stops previously requested timeBeginPeriod(1)
    /// </summary>
    public static void TimeEndPeriod1()
    {
        lock (lockEngine)
        {
            timePeriod--;

            if (timePeriod == 0)
            {
                Log.Trace("timeEndPeriod(1)");
                Utils.NativeMethods.TimeEndPeriod(1);
            }
        }
    }

    private static void StartInternal(EngineConfig config = null, bool async = false)
    {
        lock (lockEngine)
        {
            if (isLoading)
                return;

            isLoading = true;

            Config = config ?? new EngineConfig();

            if (Application.Current == null)
                new Application();

            StartInternalUI();

            if (async)
                Task.Run(() => StartInternalNonUI());
            else
                StartInternalNonUI();
        }
    }

    private static void StartInternalUI()
    {
        Application.Current.Exit += (o, e) =>
        {
            Config.UIRefresh = false;
            Config.UIRefreshInterval = 1;

            while (Players.Count != 0)
                Players[0].Dispose();
        };

        Logger.SetOutput();

        Log = new LogHandler("[FlyleafEngine] ");

        Audio = new AudioEngine();
        if (Config.FFmpegLoadProfile == LoadProfile.All)
            AudioDevice.RefreshDevices();
    }

    private static void StartInternalNonUI()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Log.Info($"FlyleafLib {version.Major }.{version.Minor}.{version.Build}");

        FFmpeg  = new FFmpegEngine();
        Video   = new VideoEngine();
        if (Config.FFmpegLoadProfile == LoadProfile.All)
            VideoDevice.RefreshDevices();
        Plugins = new PluginsEngine();
        Players = new List<Player>();

        IsLoaded = true;
        Loaded?.Invoke(null, null);

        if (Config.UIRefresh)
            StartThread();
    }
    internal static void AddPlayer(Player player)
    {
        lock (Players)
            Players.Add(player);
    }
    internal static int  GetPlayerPos(int playerId)
    {
        for (int i=0; i<Players.Count; i++)
            if (Players[i].PlayerId == playerId)
                return i;

        return -1;
    }
    internal static void DisposePlayer(Player player)
    {
        if (player == null) return;

        DisposePlayer(player.PlayerId);
    }
    internal static void DisposePlayer(int playerId)
    {
        lock (Players)
        {
            Log.Trace($"Disposing {playerId}");
            int pos = GetPlayerPos(playerId);
            if (pos == -1) return;

            var player = Players[pos];
            player.DisposeInternal();
            Players.RemoveAt(pos);
            Log.Trace($"Disposed {playerId}");
        }
    }

    internal static void StartThread()
    {
        if (!Config.UIRefresh || (tMaster != null && tMaster.IsAlive))
            return;

        tMaster = new Thread(() => { MasterThread(); })
        {
            Name = "FlyleafEngine",
            IsBackground = true
        };
        tMaster.Start();
    }
    internal static void MasterThread()
    {
        Log.Info("Thread started");

        int curLoop     = 0;
        int secondLoops = 1000 / Config.UIRefreshInterval;
        long prevTicks  = DateTime.UtcNow.Ticks;
        double curSecond= 0;

        do
        {
            try
            {
                if (Players.Count == 0)
                {
                    Thread.Sleep(Config.UIRefreshInterval);
                    continue;
                }

                curLoop++;
                if (curLoop == secondLoops)
                {
                    long curTicks = DateTime.UtcNow.Ticks;
                    curSecond = (curTicks - prevTicks) / 10000000.0;
                    prevTicks = curTicks;
                }

                lock (Players)
                    foreach (var player in Players)
                    {
                        /* Every UIRefreshInterval */
                        player.Activity.RefreshMode();

                        /* Every Second */
                        if (curLoop == secondLoops)
                        {
                            if (player.Config.Player.Stats)
                            {
                                var curStats = player.stats;
                                // TODO: L: including Subtitles bytes?
                                long curTotalBytes  = player.VideoDemuxer.TotalBytes + player.AudioDemuxer.TotalBytes;
                                long curVideoBytes  = player.VideoDemuxer.VideoPackets.Bytes + player.AudioDemuxer.VideoPackets.Bytes;
                                long curAudioBytes  = player.VideoDemuxer.AudioPackets.Bytes + player.AudioDemuxer.AudioPackets.Bytes;

                                player.bitRate      = (curTotalBytes - curStats.TotalBytes) * 8 / 1000.0;
                                player.Video.bitRate= (curVideoBytes - curStats.VideoBytes) * 8 / 1000.0;
                                player.Audio.bitRate= (curAudioBytes - curStats.AudioBytes) * 8 / 1000.0;

                                curStats.TotalBytes = curTotalBytes;
                                curStats.VideoBytes = curVideoBytes;
                                curStats.AudioBytes = curAudioBytes;

                                if (player.IsPlaying)
                                {
                                    player.Video.fpsCurrent = (player.Video.FramesDisplayed - curStats.FramesDisplayed) / curSecond;
                                    curStats.FramesDisplayed = player.Video.FramesDisplayed;
                                }
                            }
                        }
                    }

                if (curLoop == secondLoops)
                    curLoop = 0;

                Action action = () =>
                {
                    try
                    {
                        foreach (var player in Players)
                        {
                            /* Every UIRefreshInterval */

                            // Activity Mode Refresh & Hide Mouse Cursor (FullScreen only)
                            if (player.Activity.mode != player.Activity._Mode)
                                player.Activity.SetMode();

                            // CurTime / Buffered Duration (+Duration for HLS)
                            if (!Config.UICurTimePerSecond)
                                player.UpdateCurTime();
                            else if (player.Status == Status.Paused)
                            {
                                if (player.MainDemuxer.IsRunning)
                                    player.UpdateCurTime();
                                else
                                    player.UpdateBufferedDuration();
                            }

                            /* Every Second */
                            if (curLoop == 0)
                            {
                                // Stats Refresh (BitRates / FrameDisplayed / FramesDropped / FPS)
                                if (player.Config.Player.Stats)
                                {
                                    player.BitRate = player.BitRate;
                                    player.Video.BitRate = player.Video.BitRate;
                                    player.Audio.BitRate = player.Audio.BitRate;

                                    if (player.IsPlaying)
                                    {
                                        player.Audio.FramesDisplayed= player.Audio.FramesDisplayed;
                                        player.Audio.FramesDropped  = player.Audio.FramesDropped;

                                        player.Video.FramesDisplayed= player.Video.FramesDisplayed;
                                        player.Video.FramesDropped  = player.Video.FramesDropped;
                                        player.Video.FPSCurrent     = player.Video.FPSCurrent;
                                    }
                                }
                            }
                        }
                    } catch { }
                };

                Utils.UI(action);
                Thread.Sleep(Config.UIRefreshInterval);

            } catch { curLoop = 0; }

        } while (Config.UIRefresh);

        Log.Info("Thread stopped");
    }
}
