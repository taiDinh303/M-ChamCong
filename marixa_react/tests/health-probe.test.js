import test from "node:test";
import assert from "node:assert/strict";
import { createServer } from "node:http";
import axios from "axios";
import { createHealthProbe, healthProbeUrl } from "../src/services/api/healthProbe.js";
import { createConnectionManager } from "../src/services/api/connection.js";

test("health URL uses the API origin without the /api prefix or frontend origin", () => {
    assert.equal(healthProbeUrl("/api"), "/health/live");
    assert.equal(healthProbeUrl("/api/"), "/health/live");
    assert.equal(healthProbeUrl("https://backend.example/api"), "https://backend.example/health/live");
    assert.equal(healthProbeUrl("https://backend.example/api/"), "https://backend.example/health/live");
    assert.equal(healthProbeUrl("http://localhost:57432/api"), "http://localhost:57432/health/live");
});

test("anonymous health recovery succeeds when business rules require authentication", async () => {
    const requests = []; let probes = 0, clock = 0;
    const server = createServer((request, response) => {
        requests.push({ path: request.url, token: request.headers.authorization });
        response.setHeader("Content-Type", "application/json");
        if (request.url === "/health/live") {
            response.statusCode = ++probes === 1 ? 503 : 200;
            response.end(JSON.stringify({ statusCode: response.statusCode, data: { status: "live" } }));
        } else { response.statusCode = 401; response.end(JSON.stringify({ code: "UNAUTHORIZED" })); }
    });
    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    try {
        const origin = `http://127.0.0.1:${server.address().port}`;
        const client = axios.create({ proxy: false });
        await assert.rejects(client.get(`${origin}/api/AttendanceRulePublic/active`), (error) => error.response.status === 401);
        requests.length = 0;
        const manager = createConnectionManager({ now: () => clock, pause: async (ms) => { clock += ms; } });
        manager.configure(createHealthProbe(client, `${origin}/api`));
        await manager.ready();
        assert.equal(manager.getSnapshot().phase, "online");
        assert.equal(requests.length, 2);
        assert.ok(requests.every(({ path, token }) => path === "/health/live" && token === undefined));
    } finally { await new Promise((resolve) => server.close(resolve)); }
});

test("health rejects HTML, wrong JSON or status, and forwards the remaining timeout", async () => {
    for (const response of [
        { status: 200, data: "<html>Vite fallback</html>" },
        { status: 200, data: { data: [] } },
        { status: 200, data: { data: { status: "ready" } } },
        { status: 503, data: { data: { status: "live" } } },
    ]) {
        const probe = createHealthProbe({ get: async (url, options) => {
            assert.equal(url, "/health/live"); assert.equal(options.timeout, 321);
            return response;
        } }, "/api");
        await assert.rejects(probe({ timeout: 321 }), (error) => error.response.status === 503);
    }
});
