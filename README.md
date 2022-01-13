# pwd

`pwd` is a simple cross-platform console password manager, written in C#.

- It is tiny yet efficient as it only focused on helping you managing passwords.
- No vendor-lock, use encrypted passwords with openssl and your text editor.
- It is opensource and cross-platform, it works where `dotnet` works.

## Requirements

- Latest [.NET core](https://dotnet.microsoft.com/download)

## Quickstart

1. Install [.NET core](https://dotnet.microsoft.com/download)
2. Clone/copy this repository: `git clone git@github.com:AlexAtNet/pwd.git && cd pwd`.
3. Run `dotnet run`.
4. When the tool runs, type a password for your new storage and confirm it.
5. Type `.add website.com` to add a new encrypted file.
6. Type `user: tom` press `Enter`, type `password: secret` press `Enter` and then press `Enter` on the empty line.
7. Command prompt is changed to `website.com>`. Type `..` to go back to the list of files.
8. Type `websi` and press TAB to autocomplete, then press Enter.
9. Now either copy the username by typing `.ccu`, copy the password by typing `.ccp`, go back to the file list
by typing `..`. You have 5 seconds to paste it somewhere.
10. Type `.edit notepad` to edit the file in notepad. Save and exit when you finish. Confirm overwriting.
11. Quit anytime by typing `.quit`.
12. Press enter (i.e. No) if it asks you to update the repository.

## Highlights

The tool's prompt is a readline's input, so there are some shortcuts: https://github.com/tonerdo/readline

Passwords are stored in files. When the tool starts, it checks the integrity of all encrypted files
in the folder: it checks that they can be decrypted are valid YAML files. See more on YAML here:
https://yaml.org/.

There are two ways to modify content of the encrypted file:

- type `.edit` followed by the name of your editor and press ENTER or just `.edit` alone
if the environment variable EDITOR is set; 
- decrypt `cat file | openssl aes-256-cbc -d -salt -pbkdf2 > file.txt`, edit, and then
  encrypt `cat file.txt | openssl aes-256-cbc -e -salt -pbkdf2 > file` with openssl

# Testing

The tests are in the `pwd.test.cs`, and can be run with `dotnet run -- -t`.

# List of Commands

Commands are sent to pwd by typing them and pressing enter. Most of the commands are either
for a single passwords file or for a list of them. When pwd starts, it enters the list mode. From
the list mode you can go to the file mode by typing a part of the file name or by using `.open`. 

Commands:

- The app opens a file when there is a file with name that is equal to the command text or there is
only with name that starts with the command text. The command text should not start with ".".
- `..` leaves the file mode and enters the list mode.
- `.add path` creates a new encrypted password file by reading the lines until empty line. Substitutes
`***` in the input with a newly generated password.
- `.open path` opens the encrypted file.
- `.pwd` prints a strong password.
- `.edit [editor]` opens the `editor` (e.g. `notepad`) with the current file. Omitting `editor`
makes `pwd` look for it in the environment variable `EDITOR`.
- `.rm` removes the currently opened file.
- `.archive` moves the currently opened file to the folder `.archive`. As this folder begins with `.`,
the directory reader will not display this folder in the list of encrypted files. Archived
files are checked when the tool starts and can be opened with `.open`.
- `.cc name` copies value of a `name: value` pair to clipboard. `clip.exe` is used for Windows and WSL,
`pbcopy` is for mac, and `xsel` is for Linux. There are two shortcuts: `.ccu` for `.cc user`
and `.ccp` for `.cc password`. The clipboard is cleared in 5 seconds after copying.
- `.clear` clears the console.
- `.export` writes the encrypted password files to a single HTML file.

Also see a list of readline commands: https://github.com/tonerdo/readline

## Using on Android

### HTML Export

Export your password files to HTML and open them with Android's Chrome broser as follows:

1) In your passwords folder run the command: `dotnet script <path-to-pwd>/to-html.csx > _index.html`.
2) Copy this file to your phone, either by commiting to the shared repository or by copying
it through Bluetooth, or by any other method.
3) Open the copied HTML file in your Android's browser and follow the onscreen instruction.

### Termux

There is no .NET Core on Android. The files encrypted with `pwd` can be decrypted with the script
`decrypt.sh`. Unless the environment variable `PWDPWD` is set, it asks the password every time. If this
variable is set, it reads the password from it.

    $ read -s PWDPWD && export PWDWD
    $ ./decrypt.sh path/to/encrypted_file | less

Clear the environment variable:

    $ export PWDPWD=

Encrypt the file:

    $ cat text_file | ./encrypt.sh path/to/encrypted_file

List files:

    $ ./list.sh

The `pwd` scripts, e.g. `decrypt.sh`, were tested with Termux. 

## Story of pwd

For years I was in a search for the right password management tool. I've started
many, many years ago, with a plain text file. The Internet wasn't a thing yet. OMG, even
networks were not a thing so a plain text file wasn't actually a bad idea.

Over the years a few things happened:

- number of records in my passwords database had grown significantly (like 1500 or something)
- I store not only passwords, but all different types of files as well (ssh keys, pdfs, etc)
- Internet had brought a whole set of new threats

So I'd evaluated a lot and tried some of the tools that claimed to help me manage my
passwords. Some of them really did actually and I've used these one for a while:
truecrypt, Password Commander, KeePass, 1Password.

Believe me, moving from one to another isn't fun. I wouldn't if I didn't have to. But
what are the alternatives if the application is just discontinued, or your dream job
requires you to change an operating system? You cannot really let your software
make live decisions for you, don't you agree?

Suffering enough, I came up with a list of requirements for the password management
software:

- It should be secure, obviously, but not just secure - understandably secure. Its
  security should be clear enough for me to understand.
- It should be portable, no installation required, just copy it around and it should work.
- It should be cross-platform, like really cross-platform: run on windows, linux, mac with
  the same user experience.
- It should have a history of changes. Not immediately accessible history, but
  in case if something goes wrong it should be possible to track things backwards.
- It should be decentralised, I use a few windows and mac machines simultaneously and
  would like to make changes and merge them.
- It should be programmable and flexible, and support different workflows.
- Its data format should be human readable and manageable manually if needed be.
- The solution should be from a company that is here for a while, like 10 years or more
  and open source. I do not want to be forced to switch again because somebody has
  decided to retire or sell the business to a competitor.

It was quite a challenge to find a tool that satisfies all these requirements.

But eventually I found a solution. It happened to be not a single tool, though.

I started using a combination of vim encrypted text files with private git repository.
vim is a truly cross-platform text editor and works well on windows, linux and mac.
It has a blowfish encryption built-in, so security is covered. The passwords database
are just text files so I can just use `key: value` in them and it works like most of
the time. Small files, like keys, could be just base64-ed and stored in text form.
git covers backups, history, and merging and it is a cross-platform solution.

Although vim's encryption is pretty straightforward and it wasn't hard to create
decryptors for Go and node.js, I finally started using openssl's AES encryption,
so I can encrypt and decrypt any files from the command line, not only text files.
And I recently started using yaml as internal format for the files so the
programmability is a piece of cake.

`pwd` is an amalgamation of a few simple tools that I've developed while
automatising my daily password managing tasks. It can be used simultaneously
with openssl to encrypt and decrypt files, it checks the integrity of the passwords
database, and it allows to view several files without needing to type the password
each time.

I want to keep it simple and understandable so everyone can make sure that there
are no hidden threats. The code is clean and simple so new commands can be added
easily.

This solution has been serving me well for several years already. As it is now
a time-proven one, I'm happy to share it with you.

If you like it, consider buying me a coffee
https://www.buymeacoffee.com/alex.alexander. Thanks!