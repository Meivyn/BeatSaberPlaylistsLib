﻿using BeatSaberPlaylistsLib.Types;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BeatSaberPlaylistsLib.Legacy
{
    /// <summary>
    /// <see cref="IPlaylistHandler"/> for legacy playlists (.bplist/.json).
    /// </summary>
    public class LegacyPlaylistHandler : IPlaylistHandler<LegacyPlaylist>
    {
        private static readonly JsonSerializer jsonSerializer = new JsonSerializer() { Formatting = Formatting.Indented };
        /// <summary>
        /// Array of the supported extensions (no leading '.').
        /// </summary>
        protected static string[] SupportedExtensions = new string[] { "bplist", "json" };

        ///<inheritdoc/>
        public string DefaultExtension => "bplist";

        ///<inheritdoc/>
        public string[] GetSupportedExtensions()
        {
            return SupportedExtensions.ToArray();
        }

        ///<inheritdoc/>
        public bool SupportsExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;
            extension = extension.TrimStart('.');
            string[] extensions = SupportedExtensions;
            for(int i = 0; i < extensions.Length; i++)
            {
                if (extensions[i].Equals(extension, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        ///<inheritdoc/>
        public Type HandledType { get; } = typeof(LegacyPlaylist);

        ///<inheritdoc/>
        public void Populate(Stream stream, LegacyPlaylist target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), $"{nameof(target)} cannot be null for {nameof(Populate)}.");
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), $"{nameof(stream)} cannot be null for {nameof(Populate)}.");
            try
            {
                using StreamReader sr = new StreamReader(stream);
                jsonSerializer.Populate(sr, target);
            }
            catch (Exception ex)
            {
                throw new PlaylistSerializationException(ex.Message, ex);
            }
        }

        ///<inheritdoc/>
        public LegacyPlaylist Deserialize<T>(Stream stream) where T : LegacyPlaylist
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), $"{nameof(stream)} cannot be null.");
            try
            {
                using StreamReader sr = new StreamReader(stream);
                if (!(jsonSerializer.Deserialize(sr, typeof(T)) is LegacyPlaylist playlist))
                    throw new PlaylistSerializationException("Deserialized playlist was null.");
                return playlist;
            }
            catch (Exception ex)
            {
                throw new PlaylistSerializationException(ex.Message, ex);
            }
        }

        ///<inheritdoc/>
        public virtual LegacyPlaylist Deserialize(Stream stream)
        {
            return Deserialize<LegacyPlaylist>(stream);
        }

        ///<inheritdoc/>
        public void Serialize(LegacyPlaylist playlist, Stream stream)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist), $"{nameof(playlist)} cannot be null.");
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), $"{nameof(stream)} cannot be null.");
            try
            {
                using StreamWriter sw = new StreamWriter(stream);
                jsonSerializer.Serialize(sw, playlist, typeof(LegacyPlaylist));
            }
            catch (Exception ex)
            {
                throw new PlaylistSerializationException(ex.Message, ex);
            }
        }

        ///<inheritdoc/>
        void IPlaylistHandler.Serialize(IPlaylist playlist, Stream stream)
        {
            LegacyPlaylist legacyPlaylist = (playlist as LegacyPlaylist)
                ?? throw new ArgumentException($"{playlist.GetType().Name} is not a supported Type for {nameof(LegacyPlaylistHandler)}");
            Serialize(legacyPlaylist, stream);
        }

        ///<inheritdoc/>
        void IPlaylistHandler.Populate(Stream stream, IPlaylist target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), $"{nameof(target)} cannot be null.");
            LegacyPlaylist legacyPlaylist = (target as LegacyPlaylist)
                ?? throw new ArgumentException($"{target.GetType().Name} is not a supported Type for {nameof(LegacyPlaylistHandler)}");
            Populate(stream, legacyPlaylist);
        }

        ///<inheritdoc/>
        IPlaylist IPlaylistHandler.Deserialize(Stream stream)
        {
            return Deserialize(stream);
        }

        /// <summary>
        /// Creates a new <see cref="LegacyPlaylist"/> using the given parameters.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="title"></param>
        /// <param name="author"></param>
        /// <param name="description"></param>
        /// <param name="suggestedExtension"></param>
        /// <returns></returns>
        public LegacyPlaylist CreatePlaylist(string fileName, string title, string? author, string? description, string? suggestedExtension)
        {
            LegacyPlaylist playlist = new LegacyPlaylist(fileName, title, author)
            {
                Description = description,
                SuggestedExtension = suggestedExtension
            };
            return playlist;
        }

        ///<inheritdoc/>
        IPlaylist IPlaylistHandler.CreatePlaylist(string fileName, string title, string? author, string? description, string? suggestedExtension)
        => CreatePlaylist(fileName, title, author, description, suggestedExtension);
    }
}
