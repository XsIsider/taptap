using NUnit.Framework;
namespace Castle.Editor.Tests
{
    public sealed class PlanningV1Tests
    {
        [Test] public void ResetArchivesSaveAndBackupBeforeWritingFreshState()
        {
            string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CastleResetTest-" + System.Guid.NewGuid().ToString("N"));
            string path = System.IO.Path.Combine(folder, "save.json");
            try
            {
                var storage = new V2.SaveService(path);
                var state = new V2.SessionSave { ContentVersion = "test", WorldTime = "2026-10-07 18:45:00", Room = "R_HALL", Started = true };
                Assert.IsTrue(storage.Write(state)); state.Solved.Add("old-puzzle"); Assert.IsTrue(storage.Write(state));
                string old = System.IO.File.ReadAllText(path), backup = System.IO.File.ReadAllText(path + ".bak");
                Assert.IsTrue(storage.Reset(new V2.SessionSave { ContentVersion = "test", WorldTime = "2026-10-07 18:45:00", Room = "R_HALL" }));
                var archives = System.IO.Directory.GetFiles(folder, "save.json.reset-*");
                Assert.AreEqual(2, archives.Length);
                CollectionAssert.Contains(System.Array.ConvertAll(archives, System.IO.File.ReadAllText), old);
                CollectionAssert.Contains(System.Array.ConvertAll(archives, System.IO.File.ReadAllText), backup);
                var loaded = storage.Load("test"); Assert.IsFalse(loaded.Started); Assert.IsEmpty(loaded.Solved);
            }
            finally { if (System.IO.Directory.Exists(folder)) System.IO.Directory.Delete(folder, true); }
        }
        [Test] public void FullExampleAndRecoveryContracts()
        {
            CastleProjectTools.Setup(); PrototypeValidation.Run();
        }
        [TestCase("id,text\na,\"unfinished")]
        [TestCase("id,text\na,\"finished\"garbage")]
        public void MalformedCsvIsRejected(string input)
        {
            Assert.Throws<System.FormatException>(() => V2.CsvContent.Parse("Plot", input));
        }
    }
}
