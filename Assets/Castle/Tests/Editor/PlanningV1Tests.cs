using NUnit.Framework;
namespace Castle.Editor.Tests
{
    public sealed class PlanningV1Tests
    {
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
