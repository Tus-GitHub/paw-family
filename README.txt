PAW FAMILY 0.3.0 — Windows preview

A free, local desktop companion for Codex projects.
Independent community software; not an OpenAI product.

GET STARTED
1. Extract the Windows download into a folder you want to keep.
2. Double-click PawFamily.exe. There is no separate .NET runtime to install.
3. Hover over the parent cat to reveal your projects. Click a kitten for its chats.

The parent walks and rests on your desktop. Its background is transparent.
One kitten represents one registered project. Chats outside a project share Others.
Click the parent for all chats. Click a chat once to send its link to Codex.
Right-click a kitten to choose its color. Drag the parent to move it.
Press Esc or the close button to hide the chat list.
Right-click the parent or tray icon for dog/cat, walking, animation import, help or Exit.

WHAT THE ANIMATIONS MEAN
Raised paw + !: a recorded permission request or question needs your attention.
Happy bounce + check: a new completion was recorded.
Working movement: a recent local task is active.
The pet stops roaming when it needs you, while you hover, or when a panel is open.
Old completed tasks are quiet at startup. Clear completed alerts silences new ones.
Paw Family never answers a question or approves an action for you.

SUPPORTED TODAY
Windows x64 and local Codex session files in CODEX_HOME (normally ~/.codex).
This is the Codex desktop application, including builds branded ChatGPT.
Ordinary ChatGPT web/mobile conversations and remote/cloud-only chats are not supported.
Project assignments are read from local desktop metadata; folder matching is a fallback.
The source formats may change between desktop versions.

STATUS AND NAVIGATION LIMITS
This preview reads observed local events every four seconds. It cannot guarantee that
every approval, connector prompt or question appears in those files. A recorded async
question stays highlighted after enqueueing and completion until the next user message
or an aborted task. This does not prove the app's current UI is still waiting.
Initial session reads use metadata plus the last 1 MB; older pending requests may be missed.
Working becomes No live status after ten minutes without a recorded event.
Index-only chats may have no live status and may include older entries.

Chat navigation uses codex://threads/<id>. If a recognized installation is running,
its executable receives the link via --single-argument, for its single-instance handler.
If no recognized instance is found, Windows opens the registered protocol handler.
Window reuse was confirmed on the development setup. It depends on the installed Codex
version and is not verified on every build. This preview does not close, kill or restart Codex.

YOUR OWN ANIMATIONS
Choose a folder containing idle.gif, working.gif, attention.gif and done.gif.
Optionally add walk.gif (facing right). Add walk-faces-left.txt if it faces left.
Use transparent backgrounds and matching canvas sizes; 256 x 256 is a good starting size.
Each image must be no larger than 20 MB or 2048 x 2048 pixels.
Right-click the parent > Import my GIF animations. The files are copied into your settings.
Original artwork can be restored from the same menu. Imported pixels are not recolored;
project ring and label colors remain distinct.
Only import artwork you have permission to use. The public download includes original
code-drawn cat/dog artwork and an original icon. No AgentPaw/Salary Cat assets are bundled.

DATA AND REMOVAL
Paw Family reads local chat titles, event records and project metadata. It has no account,
API key, server, telemetry, advertising or network client. Local chat files are read-only.
Settings and imported art are stored in %LOCALAPPDATA%\PawFamily.
The older portable data/preferences.json is imported when no newer settings exist.
For development, PAWFAMILY_DATA_HOME can select an isolated settings directory.
No startup task, service or automatic update is installed.
To remove: Exit Paw Family, delete its extracted folder, and optionally its settings folder.
The preview EXE is unsigned. Signing and a Windows installer are not included yet.

BUILD FROM SOURCE
Install the .NET 10 SDK on Windows, then run:
  powershell -NoProfile -File .\build.ps1
The script publishes a self-contained Windows x64 EXE, runs local checks, and creates
dist/PawFamily-0.3.0-win-x64.zip with a SHA-256 checksum. Internet is needed on the first
build to retrieve official .NET runtime packages. No third-party NuGet libraries are used.
The included GitHub Actions workflow builds the same artifact on push or manual dispatch.
A v* tag publishes a preview GitHub Release after the build and checks pass.

DEVELOPMENT CHECKS
  PawFamily.exe --self-test          Parser, async questions, grouping and movement checks
  PawFamily.exe --demo               Fictional projects; demo chats never navigate
  PawFamily.exe --animation-preview  Original walking, paw-up and completion poses
  PawFamily.exe --orbit-preview      Circular project-menu preview (Esc closes)
  PawFamily.exe --chat-preview       Chat-list preview with fictional chats
Do not redistribute generated settings, logs, session fixtures or personal animation packs.

LICENSE
Application source and original artwork: MIT, see LICENSE.
Bundled .NET components retain Microsoft's license notices in THIRD-PARTY-NOTICES.txt.
