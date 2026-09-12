using System;
using System.Collections.Generic;

namespace IslandQuest.Story
{
    /// <summary>Who is speaking a <see cref="DialogueLine"/> (GDD §3: Mia the
    /// teacher, Leo the companion who asks the follow-up questions).</summary>
    public enum Speaker
    {
        Mia,
        Leo,
    }

    /// <summary>One line of story dialogue — a speaker and what they say
    /// (GDD §3.5 Layer 1: "2–3 lines of natural dialogue during the scene").</summary>
    public sealed class DialogueLine
    {
        public Speaker Speaker { get; }
        public string Text { get; }

        public DialogueLine(Speaker speaker, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Dialogue text must be non-empty.", nameof(text));

            Speaker = speaker;
            Text = text;
        }
    }

    /// <summary>
    /// An ordered run of dialogue with a cursor — the Layer-1 "story moment"
    /// delivery (GDD §3.5). The presentation layer renders <see cref="Current"/>
    /// and calls <see cref="Advance"/> on tap; <see cref="SkipToEnd"/> covers the
    /// skippable case. Pure data + iteration, no UnityEngine.
    /// </summary>
    public sealed class DialogueSequence
    {
        private readonly IReadOnlyList<DialogueLine> _lines;
        private int _cursor;

        public DialogueSequence(params DialogueLine[] lines)
        {
            if (lines is null || lines.Length == 0)
                throw new ArgumentException("A dialogue sequence needs at least one line.", nameof(lines));
            foreach (var line in lines)
                if (line is null)
                    throw new ArgumentNullException(nameof(lines), "Dialogue lines must not be null.");

            _lines = lines;
            _cursor = 0;
        }

        /// <summary>The line currently being shown.</summary>
        public DialogueLine Current => _lines[_cursor];

        /// <summary>True if there is another line after <see cref="Current"/>.</summary>
        public bool HasNext => _cursor < _lines.Count - 1;

        /// <summary>Total number of lines.</summary>
        public int LineCount => _lines.Count;

        /// <summary>Read-only view of all lines, for inspection/rendering without
        /// touching the playback cursor.</summary>
        public IReadOnlyList<DialogueLine> Lines => _lines;

        /// <summary>Move to the next line. Throws if already at the last line
        /// (guard with <see cref="HasNext"/>).</summary>
        public void Advance()
        {
            if (!HasNext)
                throw new InvalidOperationException("Cannot advance past the last dialogue line.");
            _cursor++;
        }

        /// <summary>Jump straight to the last line (Layer 1 is skippable, §3.5).</summary>
        public void SkipToEnd() => _cursor = _lines.Count - 1;
    }
}
