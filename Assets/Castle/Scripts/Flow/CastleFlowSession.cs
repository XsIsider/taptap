using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Castle.Flow
{
    [Serializable]
    public sealed class DialogueProgress
    {
        public string Id;
        public List<int> Shown = new List<int>();
        public bool Completed;
        public float Scroll = 1;
    }

    [Serializable]
    public sealed class TrackDraft
    {
        public string RecordingId;
        public float ShiftSeconds;
    }

    [Serializable]
    public sealed class PuzzleDraft
    {
        public string Id, ReferenceId;
        public List<string> Marked = new List<string>();
        public List<TrackDraft> Tracks = new List<TrackDraft>();
        public bool Matched, Aligned;
    }

    [Serializable]
    public sealed class DeviceLocation
    {
        public string Id, RoomId;
        public bool Verified;
    }

    [Serializable]
    public sealed class CastleFlowSession
    {
        public int Version = 1;
        public bool Started, InvitationExpanded, EndingSubmitted;
        public string Page = "Title", RoomId, ActiveDialogue, SelectedNpc, TapeReturnDialogue;
        public bool TapePlayback;
        public int Minutes, JournalPage, JournalCategory, PuzzleIndex;
        public string JournalSelection;
        public List<string> Entries = new List<string>();
        public List<string> Unlocked = new List<string>();
        public List<string> PendingClaims = new List<string>();
        public List<DialogueProgress> Dialogues = new List<DialogueProgress>();
        public List<PuzzleDraft> Puzzles = new List<PuzzleDraft>();
        public List<DeviceLocation> Devices = new List<DeviceLocation>();
        public List<string> Timeline = new List<string>();
        public float Volume = .7f, AutoSeconds = 3;
        public bool ReduceMotion;

        public static CastleFlowSession New(CastleFlowContent content)
        {
            return new CastleFlowSession
            {
                Started = true, Page = "Invitation", RoomId = content.InitialRoom,
                Minutes = content.InitialMinutes, AutoSeconds = content.AutoSeconds,
                Unlocked = content.Rooms.Where(room => room.InitiallyUnlocked).Select(room => room.Id).ToList(),
                Timeline = content.EndingOrder.Select(id => "").ToList()
            };
        }

        public DialogueProgress Progress(string id)
        {
            var result = Dialogues.FirstOrDefault(item => item.Id == id);
            if (result != null) return result;
            result = new DialogueProgress { Id = id };
            Dialogues.Add(result);
            return result;
        }

        public PuzzleDraft Draft(string id)
        {
            var result = Puzzles.FirstOrDefault(item => item.Id == id);
            if (result != null) return result;
            result = new PuzzleDraft { Id = id };
            Puzzles.Add(result);
            return result;
        }

        public bool Collect(string id)
        {
            if (Entries.Contains(id)) return false;
            Entries.Add(id);
            return true;
        }

        /// <summary>仅对实际显示的句子解锁地点并登记领取义务；未经过的选项分支不提前发放。</summary>
        public void Reveal(FlowDialogue dialogue, int index)
        {
            var progress = Progress(dialogue.Id);
            if (!progress.Shown.Contains(index)) progress.Shown.Add(index);
            foreach (var keyword in dialogue.Lines[index].Keywords)
            {
                if (keyword.Kind == KeywordKind.Location && !Unlocked.Contains(keyword.Target)) Unlocked.Add(keyword.Target);
                if (keyword.Kind == KeywordKind.Claim && !Entries.Contains(keyword.Target) && !PendingClaims.Contains(keyword.Target)) PendingClaims.Add(keyword.Target);
            }
        }

        public bool CanEnter(CastleFlowContent content, string roomId)
        {
            var room = content.Room(roomId);
            return string.IsNullOrEmpty(ActiveDialogue) && room != null && room.Navigable && Unlocked.Contains(roomId) && Minutes >= room.Opens && Minutes < room.Closes;
        }

        public void ChooseReference(PuzzleDraft draft, string recordingId)
        {
            var track = draft.Tracks.FirstOrDefault(item => item.RecordingId == recordingId);
            if (track == null) return;
            float origin = track.ShiftSeconds;
            foreach (var item in draft.Tracks) item.ShiftSeconds -= origin;
            draft.ReferenceId = recordingId;
            draft.Aligned = false;
        }

        public bool Match(FlowPuzzle puzzle)
        {
            var draft = Draft(puzzle.Id);
            draft.Matched = draft.Tracks.Select(track => track.RecordingId).OrderBy(id => id).SequenceEqual(puzzle.RecordingIds.OrderBy(id => id));
            draft.Aligned = false;
            return draft.Matched;
        }

        public bool Align(CastleFlowContent content, FlowPuzzle puzzle)
        {
            var draft = Draft(puzzle.Id);
            var reference = draft.Tracks.FirstOrDefault(track => track.RecordingId == draft.ReferenceId);
            if (!draft.Matched || reference == null) return draft.Aligned = false;
            Func<TrackDraft, float> time = track => content.Recording(track.RecordingId).StartSeconds + content.Recording(track.RecordingId).AtSeconds[puzzle.LineIndex] + track.ShiftSeconds;
            return draft.Aligned = draft.Tracks.All(track => Mathf.Abs(time(track) - time(reference)) <= puzzle.ToleranceSeconds);
        }

        public bool VerifySpace(CastleFlowContent content, FlowPuzzle puzzle)
        {
            if (!Align(content, puzzle)) return false;
            foreach (var id in puzzle.RecordingIds)
            {
                var record = content.Recording(id);
                if (!Devices.Any(device => device.Id == record.DeviceId && device.RoomId == record.RoomId)) return false;
            }
            foreach (var id in puzzle.RecordingIds)
                Devices.First(device => device.Id == content.Recording(id).DeviceId).Verified = true;
            Collect(puzzle.ClueId);
            return true;
        }

        public bool SubmitTimeline(CastleFlowContent content)
        {
            if (!content.EndingOrder.All(Entries.Contains) || !Timeline.SequenceEqual(content.EndingOrder)) return false;
            EndingSubmitted = true;
            return true;
        }
    }

    /// <summary>新 UI 使用独立存档，不覆盖旧 v1/v2 进度。失败时向调用方返回具体错误。</summary>
    public static class CastleFlowSave
    {
        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, "castle-ui-flow-v1.json");

        public static CastleFlowSession Load(string path, out string error)
        {
            error = null;
            if (!File.Exists(path)) return new CastleFlowSession();
            try
            {
                var state = JsonUtility.FromJson<CastleFlowSession>(File.ReadAllText(path));
                if (state == null || state.Version != 1 || state.Entries == null || state.Dialogues == null) throw new InvalidDataException("存档版本或内容无效");
                return state;
            }
            catch (Exception ex) { error = "无法读取 UI 流程存档：" + ex.Message; return new CastleFlowSession(); }
        }

        public static bool Write(CastleFlowSession state, string path, out string error)
        {
            error = null;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(state, true), new System.Text.UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                else File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception ex) { error = "保存失败：" + ex.Message; return false; }
        }
    }
}
