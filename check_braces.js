
const fs = require('fs');
const content = fs.readFileSync('c:\\Users\\agonzalez7\\Documents\\unityGame1\\game\\Assets\\Scripts\\Enemy\\EnemyAI.cs', 'utf8');
const lines = content.split('\n');

let balance = 0;
for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    for (let char of line) {
        if (char === '{') balance++;
        if (char === '}') balance--;
    }
    if (balance === 0 && line.trim().length > 0 && i < lines.length - 2) {
        console.log(`Balance hit 0 at line ${i + 1}: ${line}`);
    }
}
console.log(`Final balance: ${balance}`);
