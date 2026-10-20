// Remembered sessions persist; other sessions stay in the current browser tab.
import { responseCache } from "../api/responseCache.js";
const AUTH_KEY = "marixa_auth";
const listeners = new Set();
export const subscribeAuthChanges = (fn) => { listeners.add(fn); return () => listeners.delete(fn); };

export const saveAuth = (data, rememberMe = false) => {
    const payload = {
        token: data?.token || data?.accessToken || null,
        userId: data?.userId,
        employeeId: data?.employeeId,
        userName: data?.userName,
        employeeCode: data?.employeeCode,
        givenName: data?.givenName,
        familyName: data?.familyName,
        picture: data?.picture || null,
        roles: Array.isArray(data?.roles) ? data.roles : [],
        expiredAt: data?.expiredAt,
    };
    clearAuth();
    (rememberMe ? localStorage : sessionStorage).setItem(AUTH_KEY, JSON.stringify(payload));
};

export const getAuth = () => {
    try {
        const raw = sessionStorage.getItem(AUTH_KEY) || localStorage.getItem(AUTH_KEY);
        const data = raw ? JSON.parse(raw) : null;
        if (data?.expiredAt && new Date(data.expiredAt).getTime() <= Date.now()) {
            clearAuth();
            return null;
        }
        if (data && !Array.isArray(data.roles)) data.roles = [];
        return data;
    } catch {
        return null;
    }
};

export const clearAuth = () => {
    responseCache.clear();
    localStorage.removeItem(AUTH_KEY);
    sessionStorage.removeItem(AUTH_KEY);
    listeners.forEach((fn) => fn());
};
