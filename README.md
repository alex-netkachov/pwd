# pwd

`pwd` is a simple cross-platform console password manager, written in .NET6/C#.

Main features:

- Helps managing your sensitive information (e.g. passwords, keys, notes) in
  openssl-encrypted YAML files.
- Cross-platform, i.e. it works where .NET works (windows, macos, linux).
- Exports the passwords to an HTML page, which can be opened in mobile browsers
  or on the systems where no new software can be installed.
- No vendor lock, the encrypted files can be managed with well known open-source
  tools, e.g. openssl.

## Requirements

Latest [.NET 6](https://dotnet.microsoft.com/download).

## Install

Download the portable executable for your platform from the
[releases](https://github.com/AlexAtNet/pwd/releases) or
clone this repository:

    $ git clone https://github.com/AlexAtNet/pwd.git

## Run

In the terminal application ([terminal](https://github.com/microsoft/terminal),
cmd, xterm, etc) navigate to the folder where you want to store your password
files and either run the downloaded executable execute `pwd.sh` or `pwd.bat`
from the cloned repository:

    $ mkdir passwords && cd passwords
    $ ../pwd/pwd.sh

## Quickstart

In the terminal application ([terminal](https://github.com/microsoft/terminal),
cmd, xterm, etc) navigate to the folder where you want to store your
password-protected files and run the tool.

1. When the tool asks, type the password and then confirm it.
2. Type `.add website.com` to add an encrypted file.
3. Type `user: tom` press `Enter`, type `password: secret` press `Enter` and
   then press `Enter` on the empty line.
4. Command prompt is changed to `website.com>`. Type `..` to go back to the list
   of files.
5. Type `websi` and press TAB to autocomplete, then press Enter.
6. Now either copy the username by typing `.ccu`, copy the password by
    typing `.ccp`. The clipboard content will nbe cleared in 10 seconds.
7. Go back to the file list by typing `..`.
8. Type `.edit` to edit the file in the default text editor. If the environment
    variable `EDITOR` is not set, specify the editor's executable after the
    command as follows: `.edit notepad` or `.edit vim`. Save and exit when you
    finish. Confirm overwriting.
9. Quit anytime by typing `.quit`.

## Details

When the tool starts, it checks the integrity of all encrypted files in
the folder: it checks that they can be decrypted are valid YAML files. See more
about YAML on https://yaml.org/.

There are several ways to edit the encrypted file:

- open it in the app, type `.edit` followed by the name of your editor and
  press ENTER or just `.edit` if the environment variable EDITOR is set;
- decrypt `cat file | openssl aes-256-cbc -d -salt -pbkdf2 > file.txt`, edit,
  and then encrypt `cat file.txt | openssl aes-256-cbc -e -salt -pbkdf2 > file`
  with openssl;
- use the bash scripts from this project to decrypt and encrypt files.  

When the passwords folder is a git repository, the tool helps updating
the repository on startup and pushing changes after exiting from the tool.

The tool provides autocomplete for commands and paths.

# Commands

`pwd` is a contextual REPL (read–eval–print loop) tool for managing sensitive
information (e.g. passwords, keys, notes) in openssl-encrypted YAML files. It is
a console (i.e. works in terminal) application. List of available commands
depends on the context, e.g. in the folder context the user can list files or
open them. In the file context user can edit file's content or copy fields.

To list the files in the folder just press Enter. Type a few characters
and press Enter to view only the names that begin with them. If there is only
one match the app will open the file. If you type a first few characters of the
name and press Tab, the app will complete the name with the first match.
Subsequent Tabs iterate over the matches.

Some of the commands:

- The app opens a file when there is a file with name that is equal to the
  command text or there is only with name that starts with the command text. The
  command text should not start with ".".
- `..` leaves the file mode and enters the list mode.
- `.add path` creates a new encrypted password file by reading the lines until
  empty line. Substitutes
  `***` in the input with a newly generated password.
- `.open path` opens the encrypted file.
- `.pwd` prints a strong password.
- `.edit [editor]` opens the `editor` (e.g. `notepad`) with the current file.
  Omitting `editor`
  makes `pwd` look for it in the environment variable `EDITOR`.
- `.rm` removes the currently opened file.
- `.archive` moves the currently opened file to the folder `.archive`. As this
  folder begins with `.`, the directory reader will not display this folder in
  the list of encrypted files. Archived files are checked when the tool starts
  and can be opened with `.open`.
- `.cc name` copies value of a `name: value` pair to clipboard. There are two 
  shortcuts: `.ccu` for `.cc user` and `.ccp` for `.cc password`. The clipboard 
  is cleared in 5 seconds after copying.
- `.clear` clears the console.
- `.html path` writes the encrypted password files to a single HTML file.

## Using on other devices

Export your password files to HTML and open them with browser as follows:

1) Start pwd and use `.html path` command to write your passwords to an HTML
   file.
2) Copy this file to your phone, tablet, or laptop.
3) Open the copied HTML file in a browser and follow the onscreen instruction.

## Technicalities

While copying text to the clipboard `clip.exe` is used for Windows and WSL,
`pbcopy` is for mac, and `xsel` is for Linux.

## TODO

- cleanup this file, use its content in helps
- .export with new password
- .html with new password
- pin external references and protect them with hashsets

## Story of pwd

The right password management tool is hard to find. I've started many, many
years ago, with a text editor. The Internet wasn't a thing yet. Even networks
were not a thing so a plain text file wasn't actually so bad.

Over the years a few things happened:

- Number of records in my passwords database had grown significantly.
- There are not only passwords, but all sorts of files as well (ssh
  keys, pdfs, etc).
- Internet had brought a whole set of new threats.

I'd tried some of the tools that claimed to help me manage passwords. Some of
them was really helpful and I've used these one for a while: truecrypt,
Password Commander, KeePass, 1Password.

Moving from one to another is no fun. I wouldn't if I did not have to. But what
are the alternatives if the application is just discontinued, or
your want to change an operating system? You cannot really let
your software make decisions for you, don't you agree?

Suffering enough, I came up with a list of requirements for the password
management software:

- It should be secure, obviously, but not just secure - understandably secure.
  Its security should be clear enough for me to understand.
- It should be portable, no installation required, just copy it around and it
  should work.
- It should be cross-platform, like really cross-platform: run on windows,
  linux, mac with the same user experience.
- It should have a history of changes. Not immediately accessible history, but
  in case if something goes wrong it should be possible to track things
  backwards.
- It should be decentralised, I use a few windows and mac machines
  simultaneously and would like to make changes and merge them.
- It should be programmable and flexible, and support different workflows.
- Its data format should be human readable and manageable manually if needed be.
- The solution should be from a company that is here for a while, like 10 years
  or more and open source. I do not want to be forced to switch again because
  somebody has decided to retire or sell the business to a competitor.

It was quite a challenge to find a tool that satisfies all these requirements.

But eventually I found a solution. It happened to be not a single tool, though.

I started using a combination of vim encrypted text files with private git
repository. vim is a truly cross-platform text editor and works well on windows,
linux and mac. It has a blowfish encryption built-in, so security is covered.
The passwords database are just text files so I can just use `key: value` in
them and it works like most of the time. Small files, like keys, could be just
base64-ed and stored in text form. git covers backups, history, and merging and
it is a cross-platform solution.

Although vim's encryption is pretty straightforward and it wasn't hard to create
decryptors for Go and node.js, I finally started using openssl's AES encryption,
so I can encrypt and decrypt any files from the command line, not only text
files. And I recently started using yaml as internal format for the files so the
programmability is a piece of cake.

`pwd` is an amalgamation of a few simple tools that I've developed while
automatising my daily password managing tasks. It can be used simultaneously
with openssl to encrypt and decrypt files, it checks the integrity of the
passwords database, and it allows to view several files without needing to type
the password each time.

I want to keep it simple and understandable so everyone can make sure that there
are no hidden threats. The code is clean and simple so new commands can be added
easily.

This solution has been serving me well for several years already. As it is now a
time-proven one, I'm happy to share it with you.