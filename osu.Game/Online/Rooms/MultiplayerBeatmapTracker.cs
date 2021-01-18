// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Game.Beatmaps;

namespace osu.Game.Online.Rooms
{
    public class MultiplayerBeatmapTracker : DownloadTrackingComposite<BeatmapSetInfo, BeatmapManager>
    {
        public readonly IBindable<PlaylistItem> SelectedItem = new Bindable<PlaylistItem>();

        /// <summary>
        /// The availability state of the currently selected playlist item.
        /// </summary>
        public IBindable<BeatmapAvailability> Availability => availability;

        private readonly Bindable<BeatmapAvailability> availability = new Bindable<BeatmapAvailability>();

        public MultiplayerBeatmapTracker()
        {
            State.BindValueChanged(_ => updateAvailability());
            Progress.BindValueChanged(_ => updateAvailability());
            updateAvailability();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            SelectedItem.BindValueChanged(item => Model.Value = item.NewValue?.Beatmap.Value.BeatmapSet, true);
        }

        protected override bool VerifyDatabasedModel(BeatmapSetInfo databasedSet)
        {
            var verified = verifyDatabasedModel(databasedSet);
            if (!verified)
                Logger.Log("The imported beatmap set does not match the online version.", LoggingTarget.Runtime, LogLevel.Important);

            return verified;
        }

        private bool verifyDatabasedModel(BeatmapSetInfo databasedSet)
        {
            int? beatmapId = SelectedItem.Value.Beatmap.Value.OnlineBeatmapID;
            string checksum = SelectedItem.Value.Beatmap.Value.MD5Hash;

            var matchingBeatmap = databasedSet.Beatmaps.FirstOrDefault(b => b.OnlineBeatmapID == beatmapId && b.MD5Hash == checksum);
            return matchingBeatmap != null;
        }

        protected override bool IsModelAvailableLocally()
        {
            int? beatmapId = SelectedItem.Value.Beatmap.Value.OnlineBeatmapID;
            string checksum = SelectedItem.Value.Beatmap.Value.MD5Hash;

            var beatmap = Manager.QueryBeatmap(b => b.OnlineBeatmapID == beatmapId && b.MD5Hash == checksum);
            return beatmap?.BeatmapSet.DeletePending == false;
        }

        private void updateAvailability()
        {
            switch (State.Value)
            {
                case DownloadState.NotDownloaded:
                    availability.Value = BeatmapAvailability.NotDownloaded();
                    break;

                case DownloadState.Downloading:
                    availability.Value = BeatmapAvailability.Downloading((float)Progress.Value);
                    break;

                case DownloadState.Importing:
                    availability.Value = BeatmapAvailability.Importing();
                    break;

                case DownloadState.LocallyAvailable:
                    availability.Value = BeatmapAvailability.LocallyAvailable();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(State));
            }
        }
    }
}
