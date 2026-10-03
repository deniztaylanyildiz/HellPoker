using System.Runtime.CompilerServices;

// The tests read the player-facing strings (UiText) directly: both languages, every format string.
[assembly: InternalsVisibleTo("HellPoker.Core.Tests")]
[assembly: InternalsVisibleTo("HellPoker.PlayMode.Tests")]
