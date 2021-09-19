using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UpToYou.Backend.Runner;
using UpToYou.Core;

namespace UpToYou.Tests.BackendTests {
[TestFixture]
public class UpdateNotes {

    private readonly string _updateNotesFile =  "Hand2Note.UpdateNotes.en.md".ToAbsoluteFilePath(TestData.TestDataDirectory);
    private readonly string _updateNotesFileRu =  "Hand2Note.UpdateNotes.ru.md".ToAbsoluteFilePath(TestData.TestDataDirectory);

    [TestCase("Hand2Note.UpdateNotes.en.md")]
    [TestCase("Hand2Note.UpdateNotes.ru.md")]
    [TestCase("WinningPokerNetwork.UpdateNotes.en.md")]
    [TestCase("WinningPokerNetwork.UpdateNotes.ru.md")]
    public void Parse_update_note_without_package_name(string updateNotesFile) {
        var notes = updateNotesFile.ToAbsoluteFilePath(TestData.TestDataDirectory).ReadAllFileText().ParseUpdateNotes().ToList();
        Assert.IsTrue(notes.Count > 0);

    }

        [TestCase(null, null, ExpectedResult = "UpdateNotes.md")]
    [TestCase(null, "en", ExpectedResult = "UpdateNotes.en.md")]
    [TestCase("MyApp", "en", ExpectedResult = "MyApp.UpdateNotes.en.md")]
    [TestCase("", "en", ExpectedResult = "UpdateNotes.en.md")]
    public string GetUpdateNotesFileName(string packageName, string locale) =>
        UpdateNotesHelper.GetUpdateNotesFileName(packageName, locale);

    [TestCase("UpdateNotes.md", null, null)]
    [TestCase("UpdateNotes.en.md", null, "en")]
    [TestCase("MyApp.UpdateNotes.en.md", "MyApp", "en")]
    public void ParseUpdateNotesFile(string file, string expectedPackageName, string expectedLocale) {
        var result = file.ParseUpdateNotesParsFromFile();
        Assert.AreEqual((expectedPackageName, expectedLocale), result);
    }

}
}
