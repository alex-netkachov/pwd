'use strict';

const readline = require('readline');

function readPassword(query, done) {
  const rl =
    readline.createInterface(
      { input: process.stdin,
        output: process.stdout });

  const stdin = process.openStdin();

  process.stdin.on('data', char => {
    char = char + '';
    switch (char) {
      case '\n': case '\r': case '\u0004':
        stdin.pause();
        break;
      default:
        process.stdout.clearLine();
        readline.cursorTo(process.stdout, 0);
        process.stdout.write(query + ('*'.repeat(rl.line.length)));
        break;
    }
  });

  rl.question(query, value => {
    rl.history = [];
    rl.close();
    done(value);
  });
}

module.exports =
  query => new Promise(resolve => readPassword(query, resolve));
