import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const css = readFileSync(new URL("../src/theme.css", import.meta.url), "utf8");
const tokens = Object.fromEntries([...css.matchAll(/--(th-[\w-]+):\s*(#[0-9a-f]{6});/gi)].map((match) => [match[1], match[2]]));
const luminance = (hex) => {
    const rgb = hex.slice(1).match(/../g).map((value) => parseInt(value, 16) / 255)
        .map((value) => value <= .04045 ? value / 12.92 : ((value + .055) / 1.055) ** 2.4);
    return .2126 * rgb[0] + .7152 * rgb[1] + .0722 * rgb[2];
};
const contrast = (first, second) => {
    const values = [luminance(first), luminance(second)].sort((a, b) => b - a);
    return (values[0] + .05) / (values[1] + .05);
};

test("workspace accent text and primary actions retain normal-text contrast", () => {
    for (const tone of ["blue", "green", "red", "gold", "purple", "pink", "teal", "gray"]) {
        const ink = tokens[`th-${tone}-ink`];
        for (const surface of ["soft", "icon"]) {
            assert.ok(contrast(ink, tokens[`th-${tone}-${surface}`]) >= 4.5, `${tone} ink on ${surface}`);
            assert.ok(contrast(tokens["th-text"], tokens[`th-${tone}-${surface}`]) >= 4.5, `${tone} numeric text`);
            assert.ok(contrast(tokens["th-text-2"], tokens[`th-${tone}-${surface}`]) >= 4.5, `${tone} label text`);
        }
    }
    for (const tone of ["blue", "purple", "teal"]) assert.ok(contrast("#FFFFFF", tokens[`th-${tone}-ink`]) >= 4.5);
});
