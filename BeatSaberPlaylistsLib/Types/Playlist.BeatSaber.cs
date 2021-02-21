﻿#if BeatSaber
extern alias BeatSaber;
using BeatSaber::UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeatSaberPlaylistsLib.Types
{
    /// <summary>
    /// Base class for a Playlist.
    /// </summary>
    public abstract partial class Playlist : IDeferredSpriteLoad
    {
        /// <summary>
        /// Queue of <see cref="Action"/>s to load playlist sprites.
        /// </summary>
        protected static readonly Queue<Action> SpriteQueue = new Queue<Action>();
        /// <summary>
        /// Default cover image to use if a playlist has no cover image.
        /// </summary>
        public static Sprite? GetDefaultCoverSprite(Playlist playlist)
        {
            // TODO: Generate playlist cover with title.
            return Utilities.GetSpriteFromStream(Utilities.GenerateCoverForPlaylist((IPlaylist)playlist));
        }

        /// <summary>
        /// Instance of the playlist cover sprite.
        /// </summary>
        protected Sprite? _sprite;
        /// <summary>
        /// Instance of the previous cover sprite.
        /// </summary>
        protected Sprite? _previousSprite;
        /// <summary>
        /// Returns true if the sprite for the playlist is already queued.
        /// </summary>
        protected bool SpriteLoadQueued;
        private static readonly object _loaderLock = new object();
        private static bool CoroutineRunning = false;

        /// <summary>
        /// Adds a playlist to the sprite load queue.
        /// </summary>
        /// <param name="playlist"></param>
        protected static void QueueLoadSprite(Playlist playlist)
        {
            SpriteQueue.Enqueue(() =>
            {
                //Console.WriteLine($"Loading sprite for playlist '{playlist.Title}'");
                if (!playlist.HasCover)
                {

                    playlist._sprite = GetDefaultCoverSprite(playlist);
                }
                else
                {
                    using Stream stream = playlist.GetCoverStream();
                    Sprite? sprite = Utilities.GetSpriteFromStream(stream);
                    playlist._sprite = sprite ?? GetDefaultCoverSprite(playlist);
                }
                playlist.SpriteWasLoaded = true;
                playlist.SpriteLoaded?.Invoke(playlist, null);
                playlist._previousSprite = null;
            });

            if (!CoroutineRunning)
                BeatSaber.SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
        }
        /// <summary>
        /// Wait <see cref="YieldInstruction"/> between sprite loads.
        /// </summary>
        public static YieldInstruction LoadWait = new WaitForEndOfFrame();

        /// <summary>
        /// Coroutine to load sprites in the queue.
        /// </summary>
        /// <returns></returns>
        protected static IEnumerator<YieldInstruction> SpriteLoadCoroutine()
        {
            lock (_loaderLock)
            {
                if (CoroutineRunning)
                    yield break;
                CoroutineRunning = true;
            }
            while (SpriteQueue.Count > 0)
            {
                yield return LoadWait;
                var loader = SpriteQueue.Dequeue();
                loader?.Invoke();
            }
            CoroutineRunning = false;
            if (SpriteQueue.Count > 0) // Just in case
                BeatSaber.SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
        }

        #region IDeferredSpriteLoad

        /// <inheritdoc/>
        public event EventHandler? SpriteLoaded;

        /// <inheritdoc/>
        public bool SpriteWasLoaded { get; protected set; }

        /// <inheritdoc/>
        public Sprite? Sprite
        {
            get
            {
                if (_sprite != null)
                    return _sprite;
                _sprite = _previousSprite ?? Utilities.DefaultSprite;
                if (!SpriteLoadQueued)
                {
                    SpriteLoadQueued = true;
                    QueueLoadSprite(this);
                }
                return _sprite;
            }
        }

        #endregion

    }


    public abstract partial class Playlist<T> : BeatSaber.IPlaylist, BeatSaber.IBeatmapLevelCollection
    {
        /// <summary>
        /// Name of the collection, uses <see cref="Playlist.Title"/>.
        /// </summary>
        string BeatSaber.IAnnotatedBeatmapLevelCollection.collectionName => Title;

        /// <summary>
        /// Cover image sprite.
        /// </summary>
        Sprite? BeatSaber.IAnnotatedBeatmapLevelCollection.coverImage => Sprite;

        /// <summary>
        /// BeatmapLevelPack ID.
        /// </summary>
        public string packID => BeatSaber.CustomLevelLoader.kCustomLevelPackPrefixId + Title;

        /// <summary>
        /// BeatmapLevelPack Name, same as name of the collection.
        /// </summary>
        public string packName => Title;

        /// <summary>
        /// BeatmapLevelPack Short Name, same as name of the collection.
        /// </summary>
        public string shortPackName => Title;

        /// <summary>
        /// Returns itself.
        /// </summary>
        BeatSaber.IBeatmapLevelCollection BeatSaber.IAnnotatedBeatmapLevelCollection.beatmapLevelCollection => this;
        /// <summary>
        /// Returns a new array of the songs in this playlist.
        /// </summary>
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        BeatSaber.IPreviewBeatmapLevel[] BeatSaber.IBeatmapLevelCollection.beatmapLevels
            => Songs
            .Where(s => s.PreviewBeatmapLevel != null)
            .Select(s => s)
            .ToArray();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        /// <inheritdoc/>
        public IPlaylistSong? Add(BeatSaber.IPreviewBeatmapLevel beatmap)
        {
            if (beatmap == null)
                return null;
            IPlaylistSong? song = Add(new T()
            {
                LevelId = beatmap.levelID,
                Name = beatmap.songName,
                LevelAuthorName = beatmap.levelAuthorName,
            });
            song?.SetPreviewBeatmap(beatmap);
            return song;
        }
    }
}
#endif