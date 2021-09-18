using System;
using UpToYou.Core;

namespace UpToYou.Client.Wpf
{
    public static class UpdateNotesMarkdown
    {
        public static string GetUpdateNotesHeader(this Update update) {
            throw new InvalidOperationException();
        }

        public static (string? header, string? restNotes)
        SplitUpdateNotesHeader(this string updateNotes) {
            var notes = updateNotes.Trim();
            if (notes.StartsWith("#") && notes.Length > 1 && notes[1] != '#') {
                int endOfLineIndex = notes.IndexOf('\n');
                if (endOfLineIndex == -1)
                    return (notes.Substring(1, notes.Length - 1), null);
                return (notes.Substring(1, endOfLineIndex -1).Trim(), notes.Substring(endOfLineIndex + 1, notes.Length - endOfLineIndex - 1));
            }
            return (null, notes);
        }
    }
}
