// Turns Playwright's JSON report into a Markdown job summary.
import { readFileSync } from "node:fs";

const report = JSON.parse(readFileSync(process.argv[2], "utf8"));
const rows = [];
const walk = suite => {
  for (const spec of suite.specs ?? []) {
    const outcome = spec.tests.every(t => t.status === "expected" || t.status === "flaky") ? "✅" : "❌";
    rows.push(`| ${outcome} | ${spec.title} |`);
  }
  (suite.suites ?? []).forEach(walk);
};
report.suites.forEach(walk);

const { expected = 0, unexpected = 0, flaky = 0, skipped = 0 } = report.stats;
const headline = unexpected === 0 ? "✅ All user flows passed" : `❌ ${unexpected} user flow(s) failed`;
console.log(`## ${headline}\n`);
console.log(`${expected} passed, ${unexpected} failed, ${flaky} flaky, ${skipped} skipped against the demo UI, gateway API and bank simulator.\n`);
console.log("| | Flow |\n|---|---|");
console.log(rows.join("\n"));
