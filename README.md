# pwd
pwd is a simple console password manager.

Features:

- It is small, less than 400 lines of C# code.
- It is DRY, it is only focused on helping you managing passwords.
- It does not vendor-lock you, use pwd together with openssl and your text editor. 
- It is opensource, so you can see what it does (and it small).
- It is cross-platform, it works where dotnet works.
- It is a script, so add your functionality as you please.
- It is quite powerful (considering its size).

## Requirements

- [.NET core](https://dotnet.microsoft.com/download)
- [dotnet-script](https://github.com/filipw/dotnet-script)

## Quickstart

1. Install [.NET core](https://dotnet.microsoft.com/download) and
   [dotnet-script](https://github.com/filipw/dotnet-script)
2. Clone/copy this repository.
3. Run `dotnet script pwd.csx`.
4. When the tool runs, type a password for your new storage.
5. In the terminal, copy the created file `template` to `website.com`.
6. Back in the tool, press Enter to see the list of files.
7. Type `websi` and press TAB to autocomplete, then press Enter. Ot just press Enter.
7. Replace username by typing `/user: xxx/user: tom/` and password `/password: xxx/password: secret/`.
8. Save the modified file, type `.save`.
9. Go back to the file list, type `..`.
10. Go back to the terminal, type `.quit`.

## Highlights

The tool's prompt is a readline's input, so there are some shortcuts: https://github.com/tonerdo/readline

Passwords are stored in files. When the tool starts, it checks the integrity of all encrypted files
in the folder: it checks that they can be decrypted are valid YAML files. See more on YAML here:
https://yaml.org/.

File content can be modified by using regular expressions. See Quickstart above for an example.
Might be a bit hardcore, so there are two commands for decrypting the files and encrypting them back:

- Encrypt: `cat file.txt | openssl aes-256-cbc -e -salt -pbkdf2 > file`
- Decrypt: `cat file | openssl aes-256-cbc -d -salt -pbkdf2 > file.txt`

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

./pwd.csx is an amalgamation of a few simple tools that I've developed while
automatising my daily password managing tasks. It can be used simultaneously
with openssl to encrypt and decrypt files, it checks the integrity of the passwords
database, and it allows to view several files without needing to type the password
each time.

It is quite small, less than 400 lines. I want to keep it simple and understandable
so everyone can make sure that there are no hidden threats. The code is clean and
simple so new commands can be added easily.

This solution has been serving me well for several years already. As it is now
a time-proven one, I'm happy to share it with you. Enjoy and be safe.
