using Palisades.Helpers;
using Palisades.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Palisades.Services
{
    public static class LayoutSnapshotService
    {
        private static readonly XmlSerializer SnapshotSerializer = new(typeof(LayoutSnapshot), new[] { typeof(SnapshotEntry) });

        public static LayoutSnapshot SaveSnapshot(string name)
        {
            var snapshot = new LayoutSnapshot
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                ScreenWidth = (int)System.Windows.SystemParameters.PrimaryScreenWidth,
                ScreenHeight = (int)System.Windows.SystemParameters.PrimaryScreenHeight,
                ScreenCount = 1
            };
            var savedDir = PDirectory.GetPalisadesDirectory();
            if (!Directory.Exists(savedDir))
                return snapshot;
            foreach (var dir in Directory.GetDirectories(savedDir))
            {
                var stateFile = Path.Combine(dir, "state.xml");
                if (!File.Exists(stateFile)) continue;
                var identifier = Path.GetFileName(dir);
                var content = File.ReadAllText(stateFile);
                snapshot.Entries.Add(new SnapshotEntry
                {
                    PalisadeIdentifier = identifier,
                    StateXmlContent = content
                });
            }
            var snapDir = Path.Combine(PDirectory.GetSnapshotsDirectory(), snapshot.Id);
            PDirectory.EnsureExists(PDirectory.GetSnapshotsDirectory());
            Directory.CreateDirectory(snapDir);
            using (var writer = new StreamWriter(Path.Combine(snapDir, "snapshot.xml")))
                SnapshotSerializer.Serialize(writer, snapshot);
            return snapshot;
        }

        public static List<LayoutSnapshot> ListSnapshots()
        {
            var list = new List<LayoutSnapshot>();
            var snapDir = PDirectory.GetSnapshotsDirectory();
            if (!Directory.Exists(snapDir)) return list;
            foreach (var dir in Directory.GetDirectories(snapDir))
            {
                var path = Path.Combine(dir, "snapshot.xml");
                if (!File.Exists(path)) continue;
                try
                {
                    using var reader = new StreamReader(path);
                    if (SnapshotSerializer.Deserialize(reader) is LayoutSnapshot s)
                        list.Add(s);
                }
                catch { }
            }
            return list.OrderByDescending(s => s.CreatedAt).ToList();
        }

        public static void RestoreSnapshot(string snapshotId)
        {
            var path = Path.Combine(PDirectory.GetSnapshotsDirectory(), snapshotId, "snapshot.xml");
            if (!File.Exists(path)) return;
            LayoutSnapshot? snapshot;
            using (var reader = new StreamReader(path))
            {
                snapshot = SnapshotSerializer.Deserialize(reader) as LayoutSnapshot;
            }
            if (snapshot?.Entries == null) return;
            var savedDir = PDirectory.GetPalisadesDirectory();
            PDirectory.EnsureExists(savedDir);
            foreach (var entry in snapshot.Entries)
            {
                var palDir = Path.Combine(savedDir, entry.PalisadeIdentifier);
                Directory.CreateDirectory(palDir);
                File.WriteAllText(Path.Combine(palDir, "state.xml"), entry.StateXmlContent);
            }
        }

        public static void DeleteSnapshot(string snapshotId)
        {
            var dir = Path.Combine(PDirectory.GetSnapshotsDirectory(), snapshotId);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
