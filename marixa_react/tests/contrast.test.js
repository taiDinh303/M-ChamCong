import { fileURLToPath } from "node:url";
import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { readableInk } from "../src/utils/readableInk.js";
const luminance = (hex) =>
  hex
    .slice(1)
    .match(/../g)
    .map((c) => parseInt(c, 16) / 255)
    .map((v) => (v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4))
    .reduce((sum, v, i) => sum + v * [0.2126, 0.7152, 0.0722][i], 0);
const ratio = (a, b) => {
  const x = luminance(a),
    y = luminance(b);
  return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05);
};
test("dynamic project/avatar colors always choose a readable black or white foreground", () => {
  for (let r = 0; r <= 255; r += 17)
    for (let g = 0; g <= 255; g += 17)
      for (let b = 0; b <= 255; b += 17) {
        const color =
          "#" + [r, g, b].map((v) => v.toString(16).padStart(2, "0")).join("");
        assert.ok(ratio(readableInk(color), color) >= 4.5, color);
      }
  assert.equal(readableInk("#fff"), "#000000");
  assert.equal(readableInk("#000"), "#ffffff");
});
test("every migrated source color has light and dark definitions", () => {
  const source = fs.readFileSync(
    new URL("../src/contrast-palette.css", import.meta.url),
    "utf8",
  );
  const [light, dark] = source.split('html[data-theme="dark"]');
  const vars = (s) =>
    new Map(
      [...s.matchAll(/(--ui-[\w-]+):\s*(#[\da-f]{6});/g)].map((m) => [
        m[1],
        m[2],
      ]),
    );
  const a = vars(light),
    b = vars(dark);
  assert.ok(a.size > 500);
  assert.deepEqual([...a.keys()].sort(), [...b.keys()].sort());
  function walk(dir) {
    for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
      const p = path.join(dir, e.name);
      if (e.isDirectory()) walk(p);
      else if (p.endsWith(".css"))
        for (const m of fs
          .readFileSync(p, "utf8")
          .matchAll(/var\((--ui-[\w-]+)/g))
          assert.ok(a.has(m[1]), p + ": " + m[1]);
    }
  }
  walk(fileURLToPath(new URL("../src", import.meta.url)));
});
