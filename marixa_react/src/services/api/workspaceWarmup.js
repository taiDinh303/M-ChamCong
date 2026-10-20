import axiosClient from "./axiosClient";
import { collectPages } from "./contracts";
import { getAuth, subscribeAuthChanges } from "../auth/auth";
import { connection } from "./connection.js";
import { sessionScope } from "./responseCache.js";
import { workspacePlan, runPrefetch } from "./prefetchPlan.js";

let current = { scope: null, phase: "idle", completed: 0, total: 0, failed: 0 };
let job;
let controller;
const listeners = new Set();
const publish = (value) => { current = { ...current, ...value }; listeners.forEach((fn) => fn()); };
export const subscribeWarmup = (fn) => { listeners.add(fn); return () => listeners.delete(fn); };
export const warmupSnapshot = () => current;

export function warmWorkspace(auth) {
    const scope = sessionScope(auth);
    if (!scope) return Promise.resolve();
    if (current.scope === scope && current.phase === "ready") return Promise.resolve();
    if (current.scope === scope && job) return job;
    controller?.abort();
    const active = new AbortController(); controller = active;
    const tasks = workspacePlan(auth);
    publish({ scope, phase: "connecting", total: tasks.length, completed: 0, failed: 0 });
    job = (async () => {
      try { await connection.ready(); }
      catch { if (controller === active) publish({ phase: "error" }); return; }
      if (active.signal.aborted) return;
      publish({ phase: "loading" });
      const timeout = setTimeout(() => {
          active.abort();
          if (controller === active) publish({ phase: "ready" });
      }, 20_000);
      try { await runPrefetch(tasks, async ({ url, paginated }) => {
        if (scope !== sessionScope(getAuth())) { active.abort(); return; }
        const get = (params) => axiosClient.get(url, { params, signal: active.signal, recover: false });
        return paginated ? collectPages((pageNumber, pageSize) => get({ pageNumber, pageSize })) : get();
    }, { signal: active.signal, onProgress: (progress) => {
        if (controller === active && !active.signal.aborted) publish(progress);
      } }); }
      finally { clearTimeout(timeout); if (controller === active) publish({ phase: "ready" }); }
    })().finally(() => { if (controller === active) job = null; });
    return job;
}
subscribeAuthChanges(resetWarmup);

export function resetWarmup() {
    controller?.abort(); controller = null; job = null;
    publish({ scope: null, phase: "idle", completed: 0, total: 0, failed: 0 });
}
