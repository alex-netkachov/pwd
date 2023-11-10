# pwd

`pwd` is a cross-platform console-based password manager, developed using
.NET 7.

It functions as a contextual REPL (read-eval-print loop) tool. Passwords, along
with other sensitive information such as keys or notes, are stored in
AES-encrypted files.

Main features:

- Helps protect your sensitive information, such as passwords, keys, and notes,
  by providing commands for navigating, accessing, and managing
  password-protected files.
- Cross-platform, meaning it functions wherever .NET is supported (Windows,
  macOS, Linux).
- Free from vendor lock-in: the encrypted files can be managed with well-known
  open-source tools such as OpenSSL.
- Portable: no installation required.
- One file per entry, which makes it easy to manage and merge changes.

## Requirements

Latest [.NET](https://dotnet.microsoft.com/download).

## Install

Download the portable executable for your platform from the
[releases](https://github.com/alex-netkachov/pwd/releases) page
or clone this repository.

```PowerShell
git clone https://github.com/alex-netkachov/pwd.git
```

## Run

In the terminal application (such as
[Windows Terminal](https://github.com/microsoft/terminal), Command Prompt,
xterm, etc.), navigate to the folder where you want to store your
password files, and either execute the downloaded file or run `pwd.sh`
(for Unix-like systems) or `pwd.bat` (for Windows) from the cloned
repository.

## Quickstart

In the terminal application navigate to the folder where you want to store
your password-protected files and run the tool.

1. When the tool asks, type the password and then confirm it.
2. Type `.add website.com` to add an encrypted file.
3. Type `user: tom` press `Enter`, type `password: secret` press `Enter`
   and then press `Enter` on the empty line.
4. Type `web` and press TAB to autocomplete, then press `Enter`.
5. Now copy the password by typing `.ccp` and pressing `Enter`.
   The clipboard content will be cleared in 10 seconds.
6. Go back to the file list by typing `..`.
7. Type `.edit` to edit the file in the default text editor. If
   the environment variable `EDITOR` is not set, specify the editor
   executable after the command as follows: `.edit notepad` or
   `.edit vim`. Save and exit when you finish. Confirm overwriting.
8. Quit anytime by typing `.quit`.

## Commands

The list of available commands depends on the context. For example, in
the folder context, you can list files or open them, while in the file
context, you can edit the file's content or copy fields.

Contexts:

- [session](pwd/res/context_session_help.txt)
- [file](pwd/res/context_file_help.txt)
- [new file](pwd/res/context_new_file_help.txt)

## Technicalities

Both the entry names and content are encrypted with AES 256-bit CBC
and then encoded with Base64Url (RFC 4648).

While copying text to the clipboard `clip.exe` is used for Windows and WSL,
`pbcopy` is for mac, and `xsel` is for Linux.

When the tool starts, it verifies that all files can be decrypted and confirms
they are valid YAML files.

If something goes wrong with the main app, there are fallback scripts that
can be used to decrypt and encrypt files manually. They are located in the
`scripts` folder.

The release binaries are versioned as YEAR.MONTH.DAY.BUILD.

According to <https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html>
the number if iterations for PBKDF2 is 600,000.

## Motivation

From the author:

Over the years, I have come up with a list of requirements for password
management software:

- It should be secure, obviously, but not just secure — understandably
  secure. What it does, apart from using time-proven algorithms, should be
  simple enough to understand.
- It should be portable; no installation required. Just copy it around,
  and it should work.
- It should be truly cross-platform: it must run on Windows, Linux, and
  Mac with the same user experience.
- It should be usable in environments where nothing can be installed
  beyond the standard software.
- It should maintain a history of changes. This history doesn't need to
  be immediately accessible, but it should be possible to trace things
  backward if something goes wrong.
- It should be decentralized. I use several Windows and Mac machines
  simultaneously and would like to make changes and merge them without
  hassle.
- It should be programmable and flexible to support different workflows.
- Its data format should be human-readable and manually manageable if
  necessary.
- The solution should come from a company that has been around for
  a while, like 10 years or more, and it should be open-source. I
  do not want to be compelled to switch again because someone decided
  to retire or sell the business to a competitor.

Unfortunately, I couldn’t find a tool that met all these requirements.
However, it was possible to combine several tools to achieve the desired
outcome. The guide on using Git, Vim, OpenSSL, and rsync together for
password management constituted the first version of this project.

These tools run on all platforms, are open-source, and have been
around for a while. They are also well-known and well-documented, so
there is no vendor lock-in. The only downside is that using them
this way is quite cumbersome. I had to write a few scripts to automate
the most common tasks.

`pwd` is an amalgamation of a few simple scripts that I've developed
while automating my daily password management tasks. It can be used
simultaneously with these tools to manage passwords. It is not
a replacement for them but rather a wrapper that facilitates their
use.

## References

- YAML - <https://yaml.org/>
- openssl - <https://www.openssl.org/>
- git - <https://git-scm.com/>
- .net - <https://dotnet.microsoft.com/>
